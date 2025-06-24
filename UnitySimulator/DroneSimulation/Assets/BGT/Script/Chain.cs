using UnityEngine;
using System.Collections.Generic;

public class Chain : MonoBehaviour
{
    /// <summary>
    /// element 0 ~ element 29
    /// </summary>
    public List<Transform> chainLinks; // 체인 링크 GameObject들 (Inspector에서 할당)
    public float chainSpeed = 1.0f; // 체인 움직임 속도

    private List<Vector3> initialLocalPositions; // 체인 링크들의 초기 로컬 위치 저장
    private float currentChainTraversal = 0f; // 체인 경로 상 현재 오프셋
    private float totalInitialChainLength = 0f; // 초기 체인 총 길이

    private float smoothRotationSpeed = 8.0f; // 회전 보간 속도


    void Start()
    {
        if (chainLinks == null || chainLinks.Count == 0)
        {
            Debug.LogError("체인 링크가 할당되지 않았습니다. Inspector에서 'Chain Links' 리스트에 GameObject들을 할당해주세요. 스크립트 비활성화.");
            enabled = false;
            return;
        }

        // ChainALL GameObject의 스케일 경고: (1,1,1)이 아닐 경우 왜곡 발생 가능성.
        if (transform.localScale != Vector3.one)
        {
            Debug.LogError($"**크리티컬 경고: ChainALL GameObject의 스케일이 {transform.localScale} 입니다!** 체인 움직임 계산에 심각한 왜곡을 초래합니다. 반드시 Transform -> Scale을 (1,1,1)로 변경해야 합니다. 체인이 작아지면, Chainone 모델의 Import Settings에서 Scale Factor를 조정하거나, ChainALL의 스케일을 (X,X,X) 형태로 균일하게 (예: 10,10,10) 키우세요. 스크립트 비활성화.");
            enabled = false;
            return;
        }

        initialLocalPositions = new List<Vector3>();
        for (int i = 0; i < chainLinks.Count; i++)
        {
            initialLocalPositions.Add(chainLinks[i].localPosition); // 체인 링크의 로컬 위치 저장
        }

        // 체인 경로의 총 길이를 계산 (최소 두 개 이상의 링크 필요)
        if (initialLocalPositions.Count > 1)
        {
            for (int i = 0; i < initialLocalPositions.Count - 1; i++)
                totalInitialChainLength += Vector3.Distance(initialLocalPositions[i], initialLocalPositions[(i + 1) % initialLocalPositions.Count]);
            totalInitialChainLength += Vector3.Distance(initialLocalPositions[initialLocalPositions.Count - 1], initialLocalPositions[0]);
        }

        else
        {
            Debug.LogError("최소 두 개 이상의 체인 링크가 필요합니다. 스크립트 비활성화.");
            enabled = false;
            return;
        }

        Debug.Log($"총 {chainLinks.Count}개의 체인 링크가 할당되었습니다.");
        Debug.Log($"초기 체인 링크 배치에 따른 총 유효 경로 길이: {totalInitialChainLength} 유닛.");
    }

    void Update()
    {
        if (chainLinks == null || chainLinks.Count == 0 || totalInitialChainLength == 0)
            return;

        currentChainTraversal += chainSpeed * Time.deltaTime;
        currentChainTraversal %= totalInitialChainLength;

        if (currentChainTraversal < 0)
            currentChainTraversal += totalInitialChainLength; // 음수 값 보정

        for (int i = 0; i < chainLinks.Count; i++)
        {
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
            chainLinks[i].localRotation = Quaternion.Slerp(chainLinks[i].localRotation, targetLocalRotation, Time.deltaTime * smoothRotationSpeed);
        }
    }

    //로컬 위치, 로컬 회전 계산 함수
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

            if (segmentLength < 0.0001f)
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
                    // rot는 초기값인 Quaternion.identity를 유지
                }
                else
                {
                    Vector3 forward = lookDirection.normalized;
                    Vector3 calculatedUpVector;

                    /********************* Up 벡터 결정 로직: 커브 안쪽을 향하며 Z축 롤링 방지 *********************/
                    Vector3 vectorToLocalOrigin = -pos.normalized;
                    Vector3 tempRight = Vector3.Cross(forward, vectorToLocalOrigin).normalized;
                    calculatedUpVector = Vector3.Cross(tempRight, forward).normalized;

                    // 강건성(Robustness) 처리
                    if (calculatedUpVector.sqrMagnitude < 0.000001f || Mathf.Abs(Vector3.Dot(forward, calculatedUpVector)) > 0.99f)
                    {
                        calculatedUpVector = transform.up; // Fallback
                    }

                    rot = Quaternion.LookRotation(forward, calculatedUpVector.normalized);

                    // Chainone 모델의 초기 회전 오프셋 보정 (필요시 조정)
                    // rot *= Quaternion.Euler(0, 0, 0); 
                }
                return;
            }
            accumulatedDistance += segmentLength;
        }

        Debug.LogWarning("GetPositionAndRotationOnInitialPath: Path distance calculation error. Returning to first initial local position.");
    }

    // 체인 경로, 각 링크의 축을 시각화
    //void OnDrawGizmos()
    //{
    //    // initialLocalPositions가 초기화되지 않았거나 링크가 부족할 경우, 현재 chainLinks를 기반으로 대략적인 경로를 그림.
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

    //    // 초기 체인 경로를 녹색으로 그림.
    //    Gizmos.color = Color.green;
    //    for (int i = 0; i < initialLocalPositions.Count; i++)
    //    {
    //        Vector3 currentWorldPoint = transform.TransformPoint(initialLocalPositions[i]);
    //        Vector3 nextWorldPoint = transform.TransformPoint(initialLocalPositions[(i + 1) % initialLocalPositions.Count]);
    //        Gizmos.DrawLine(currentWorldPoint, nextWorldPoint);
    //    }

    //    // 플레이 모드에서 각 체인 링크의 계산된 Look/Right/Up 축을 그림 (디버깅용)
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