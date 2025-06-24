using UnityEngine;
using System.Collections.Generic;

public class Chain1 : MonoBehaviour
{
    // 유니티 인스펙터에서 할당할 체인 링크들 (각 체인 조각의 Transform)
    public List<Transform> chainLinks;

    // 체인 움직임 속도. 이 값은 체인이 활성화되었을 때만 적용됩니다.
    public float chainSpeed = 1.0f;

    // 체인이 현재 움직이는지 여부를 나타내는 내부 플래그
    private bool isChainActive = false;

    
    private List<Vector3> initialLocalPositions; // 체인 링크들의 초기 로컬 위치 저장
    private float currentChainTraversal = 0f;  // 체인 경로 상 현재 오프셋
    private float totalInitialChainLength = 0f;  // 초기 체인 총 길이
    private float smoothRotationSpeed = 8.0f;  // 회전 보간 속도

    void Start()
    {
        // 필수 할당 확인
        if (chainLinks == null || chainLinks.Count == 0)
        {
            Debug.LogError("Chain.cs: 체인 링크가 할당되지 않았습니다. Inspector에서 'Chain Links' 리스트에 GameObject들을 할당해주세요. 스크립트 비활성화.");
            enabled = false;
            return;
        }

        // 스케일 경고 (부모 오브젝트의 스케일이 1,1,1이 아니면 움직임 계산에 왜곡 발생)
        if (transform.localScale != Vector3.one)
        {
            Debug.LogError($"Chain.cs: **경고: Chain GameObject의 스케일이 {transform.localScale} 입니다!** 체인 움직임 계산에 심각한 왜곡을 초래할 수 있습니다. 반드시 이 GameObject의 Transform -> Scale을 (1,1,1)로 변경하거나, 체인 모델의 Import Settings에서 Scale Factor를 조정하여 전체 크기를 맞추세요. 스크립트 비활성화.");
            enabled = false;
            return;
        }

        // 초기 로컬 위치 저장
        initialLocalPositions = new List<Vector3>();
        for (int i = 0; i < chainLinks.Count; i++)
        {
            initialLocalPositions.Add(chainLinks[i].localPosition);
        }

        // 체인 경로 총 길이 계산
        if (initialLocalPositions.Count > 1)
        {
            for (int i = 0; i < initialLocalPositions.Count - 1; i++)
                totalInitialChainLength += Vector3.Distance(initialLocalPositions[i], initialLocalPositions[i + 1]);
            // 마지막 링크에서 첫 링크까지의 거리 추가 (루프형 체인)
            totalInitialChainLength += Vector3.Distance(initialLocalPositions[initialLocalPositions.Count - 1], initialLocalPositions[0]);
        }
        else
        {
            Debug.LogError("Chain.cs: 최소 두 개 이상의 체인 링크가 필요합니다. 스크립트 비활성화.");
            enabled = false;
            return;
        }

        Debug.Log($"Chain.cs: 총 {chainLinks.Count}개의 체인 링크가 할당되었습니다.");
        Debug.Log($"Chain.cs: 초기 체인 링크 배치에 따른 총 유효 경로 길이: {totalInitialChainLength} 유닛.");

        // 초기에는 체인이 멈춰있는 상태로 시작
        isChainActive = false;
    }

    void Update()
    {
        // 초기화되지 않았거나 링크가 없으면 아무것도 하지 않음
        if (chainLinks == null || chainLinks.Count == 0 || totalInitialChainLength == 0)
            return;

        // isChainActive가 true일 때만 체인 움직임 로직 실행
        if (isChainActive)
        {
            // 체인 현재 위치 계산 및 순환
            currentChainTraversal += chainSpeed * Time.deltaTime;
            currentChainTraversal %= totalInitialChainLength;

            // 음수 보정 (Modulus 연산 결과가 음수일 수 있음)
            if (currentChainTraversal < 0)
                currentChainTraversal += totalInitialChainLength;

            // 각 체인 링크의 위치 및 회전 업데이트
            for (int i = 0; i < chainLinks.Count; i++)
            {
                // 각 링크의 경로 상 목표 거리 계산
                float targetDistanceOnPath = currentChainTraversal;
                for (int j = 0; j < i; j++)
                {
                    float segmentLength = Vector3.Distance(initialLocalPositions[j], initialLocalPositions[(j + 1) % initialLocalPositions.Count]);
                    targetDistanceOnPath += segmentLength;
                }

                targetDistanceOnPath %= totalInitialChainLength;
                if (targetDistanceOnPath < 0) targetDistanceOnPath += totalInitialChainLength;

                Vector3 targetLocalPosition;
                Quaternion targetLocalRotation;
                GetPositionAndRotationOnInitialPath(targetDistanceOnPath, out targetLocalPosition, out targetLocalRotation);

                chainLinks[i].localPosition = targetLocalPosition;
                // 회전은 부드럽게 보간
                chainLinks[i].localRotation = Quaternion.Slerp(chainLinks[i].localRotation, targetLocalRotation, Time.deltaTime * smoothRotationSpeed);
            }
        }
    }

    /// <summary>
    /// Y0 신호가 켜졌을 때 호출되어 체인 동작을 시작합니다.
    /// </summary>
    public void ActivateChain()
    {
        isChainActive = true;
    }

    /// <summary>
    /// Y0 신호가 꺼졌을 때 호출되어 체인 동작을 정지합니다.
    /// </summary>
    public void DeactivateChain()
    {
        isChainActive = false;
    }

    // 로컬 위치, 로컬 회전 계산 함수 (변경 없음)
    private void GetPositionAndRotationOnInitialPath(float distance, out Vector3 pos, out Quaternion rot)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;

        float accumulatedDistance = 0f;
        for (int i = 0; i < initialLocalPositions.Count; i++)
        {
            Vector3 currentLocalPoint = initialLocalPositions[i];
            Vector3 nextLocalPoint = initialLocalPositions[(i + 1) % initialLocalPositions.Count];

            float segmentLength = Vector3.Distance(currentLocalPoint, nextLocalPoint);

            if (segmentLength < 0.0001f) // 세그먼트 길이가 너무 짧을 때 건너뛰기
            {
                accumulatedDistance += segmentLength;
                continue;
            }

            if (distance <= accumulatedDistance + segmentLength)
            {
                float t = (distance - accumulatedDistance) / segmentLength;
                pos = Vector3.Lerp(currentLocalPoint, nextLocalPoint, t);

                Vector3 lookDirection = nextLocalPoint - currentLocalPoint;

                if (lookDirection.sqrMagnitude < 0.000001f)
                {
                    // 방향이 없으면 회전은 기본값 유지
                }
                else
                {
                    Vector3 forward = lookDirection.normalized;
                    Vector3 calculatedUpVector;

                    // Up 벡터 결정 로직 (커브 안쪽을 향하며 Z축 롤링 방지)
                    Vector3 vectorToLocalOrigin = -pos.normalized;
                    Vector3 tempRight = Vector3.Cross(forward, vectorToLocalOrigin).normalized;
                    calculatedUpVector = Vector3.Cross(tempRight, forward).normalized;

                    // 강건성(Robustness) 처리
                    if (calculatedUpVector.sqrMagnitude < 0.000001f || Mathf.Abs(Vector3.Dot(forward, calculatedUpVector)) > 0.99f)
                    {
                        calculatedUpVector = transform.up; // Fallback: 부모 오브젝트의 Y축 사용
                    }
                    rot = Quaternion.LookRotation(forward, calculatedUpVector.normalized);
                }
                return;
            }
            accumulatedDistance += segmentLength;
        }

        Debug.LogWarning("Chain.cs: GetPositionAndRotationOnInitialPath: 경로 거리 계산 오류. 첫 번째 초기 로컬 위치로 반환됩니다.");
    }

    // Gizmos (개발/디버깅용 시각화, 변경 없음)
    //void OnDrawGizmos()
    //{
    //    if (initialLocalPositions == null || initialLocalPositions.Count < 2)
    //    {
    //        if (chainLinks != null && chainLinks.Count >= 2)
    //        {
    //            Gizmos.color = Color.cyan;
    //            for (int i = 0; i < chainLinks.Count; i++)
    //            {
    //                Vector3 currentWorldPos = chainLinks[i].transform.position;
    //                Vector3 nextWorldPos = chainLinks[(i + 1) % chainLinks.Count].transform.position;
    //                Gizmos.DrawLine(currentWorldPos, nextWorldPos);
    //            }
    //        }
    //        return;
    //    }

    //    Gizmos.color = Color.green;
    //    for (int i = 0; i < initialLocalPositions.Count; i++)
    //    {
    //        Vector3 currentWorldPoint = transform.TransformPoint(initialLocalPositions[i]);
    //        Vector3 nextWorldPoint = transform.TransformPoint(initialLocalPositions[(i + 1) % initialLocalPositions.Count]);
    //        Gizmos.DrawLine(currentWorldPoint, nextWorldPoint);
    //    }

    //    if (Application.isPlaying && chainLinks != null && chainLinks.Count > 0)
    //    {
    //        for (int i = 0; i < chainLinks.Count; i++)
    //        {
    //            Vector3 currentWorldPos = chainLinks[i].position;
    //            Quaternion currentWorldRot = chainLinks[i].rotation;

    //            Gizmos.color = Color.blue; // Z축 (Forward)
    //            Gizmos.DrawRay(currentWorldPos, currentWorldRot * Vector3.forward * 0.3f);
    //            Gizmos.color = Color.red; // X축 (Right)
    //            Gizmos.DrawRay(currentWorldPos, currentWorldRot * Vector3.right * 0.2f);
    //            Gizmos.color = Color.green; // Y축 (Up)
    //            Gizmos.DrawRay(currentWorldPos, currentWorldRot * Vector3.up * 0.2f);
    //        }
    //    }
    //}
}