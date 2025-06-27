using UnityEngine;
using System.Collections.Generic;

public class ChainMove : MonoBehaviour
{
    /// <summary>
    /// element 0 ~ element 29
    /// </summary>
    public List<Transform> chainLinks; // 체인 링크 GameObject들 (Inspector에서 할당)
    public float chainSpeed = 1f; // 체인 움직임 속도 (단위: M/s. 한 바퀴 시간 목표 시 이 값은 무시됨)

    [Header("PLC Simulation (For Testing)")]
    private bool isChainCw = false; // PLC 정방향 이동 명령 (true: 정방향, false: 정지 또는 역방향)
    private bool isChainCCw = false; // PLC 역방향 이동 명령 (true: 역방향, false: 정지 또는 정방향)

    [Header("Rotation Settings")]
    public float targetRotationTime = 50.0f; // 체인 한 바퀴를 도는 목표 시간 (단위: 초)
    public bool useTargetRotationTime = true; // 목표 회전 시간을 사용할지 여부

    private List<Vector3> initialLocalPositions; // 체인 링크들의 초기 로컬 위치 저장
    private float currentChainTraversal = 0f; // 체인 경로 상 현재 오프셋
    private float totalInitialChainLength = 0f; // 초기 체인 총 길이

    private float smoothRotationSpeed = 10.0f; // 회전 보간 속도


    void Awake()
    {
        // 1. 자식 오브젝트들을 자동으로 찾아서 chainLinks 리스트에 할당
        chainLinks = new List<Transform>();
        foreach (Transform child in transform) // 현재 GameObject의 모든 직계 자식에 대해 반복
        {
            chainLinks.Add(child);
        }
        if (chainLinks == null || chainLinks.Count == 0)
        {
            Debug.LogError("체인 링크가 자동으로 찾아지지 않았습니다. 부모 GameObject('Chain' 스크립트가 붙은 오브젝트)의 자식으로 체인 링크들을 배치해주세요. 스크립트 비활성화.");
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
    }


    void Start() // Start()는 Awake() 이후에 호출됩니다.
    {
        // Awake에서 이미 chainLinks와 스케일 체크가 되었으므로 중복 체크를 피할 수 있습니다.
        if (!enabled) return; // Awake에서 비활성화되었다면 더 이상 진행하지 않음

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
            totalInitialChainLength += Vector3.Distance(initialLocalPositions[initialLocalPositions.Count - 1], initialLocalPositions[0]); // 마지막 링크와 첫 링크 연결
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

        float actualSpeed;

        // 목표 회전 시간을 사용할지 여부에 따라 속도 계산
        if (useTargetRotationTime && targetRotationTime > 0.001f) // 0으로 나누는 것 방지
        {
            actualSpeed = totalInitialChainLength / targetRotationTime; // 한 바퀴 거리 / 목표 시간
        }
        else
        {
            actualSpeed = chainSpeed; // 기존 chainSpeed 사용
        }

        // PLC 명령에 따라 실제 체인 속도 결정
        float effectiveChainSpeed = 0f;
        if (isChainCw && !isChainCCw) // 정방향 명령만 들어온 경우
        {
            effectiveChainSpeed = actualSpeed; // 양수 속도 (정방향)
        }
        else if (isChainCCw && !isChainCw) // 역방향 명령만 들어온 경우
        {
            effectiveChainSpeed = -actualSpeed; // 음수 속도 (역방향)
        }
        // isChainCw와 isChainCCw가 모두 true이거나 모두 false이면 정지

        currentChainTraversal += effectiveChainSpeed * Time.deltaTime; // 실제 속도 적용

        // totalInitialChainLength 범위 내로 currentChainTraversal 유지
        currentChainTraversal %= totalInitialChainLength;
        if (currentChainTraversal < 0)
            currentChainTraversal += totalInitialChainLength; // 음수 값 보정

        for (int i = 0; i < chainLinks.Count; i++)
        {
            float targetDistanceOnPath = currentChainTraversal;

            float linkOffsetDistance = 0f;
            for (int j = 0; j < i; j++)
            {
                linkOffsetDistance += Vector3.Distance(initialLocalPositions[j], initialLocalPositions[(j + 1) % initialLocalPositions.Count]);
            }

            // effectiveChainSpeed의 부호에 따라 링크 오프셋을 적용
            if (effectiveChainSpeed >= 0) // 정방향
            {
                targetDistanceOnPath += linkOffsetDistance;
            }
            else // 역방향
            {
                targetDistanceOnPath -= linkOffsetDistance;
            }

            targetDistanceOnPath %= totalInitialChainLength;
            // % 연산자는 음수 결과를 반환할 수 있으므로 항상 0 이상으로 보정
            if (targetDistanceOnPath < 0) targetDistanceOnPath += totalInitialChainLength;

            Vector3 targetLocalPosition;
            Quaternion targetLocalRotation;

            // GetPositionAndRotationOnInitialPath 함수에 실제 적용될 속도(effectiveChainSpeed)의 부호를 전달
            GetPositionAndRotationOnInitialPath(targetDistanceOnPath, out targetLocalPosition, out targetLocalRotation, effectiveChainSpeed);

            chainLinks[i].localPosition = targetLocalPosition;
            chainLinks[i].localRotation = Quaternion.Slerp(chainLinks[i].localRotation, targetLocalRotation, Time.deltaTime * smoothRotationSpeed);
        }
    }

    // 로컬 위치, 로컬 회전 계산 함수 (effectiveChainSpeed 부호를 인자로 추가)
    private void GetPositionAndRotationOnInitialPath(float distance, out Vector3 pos, out Quaternion rot, float currentSpeed)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;

        float accumulatedDistance = 0f;
        for (int i = 0; i < initialLocalPositions.Count; i++)
        {
            Vector3 currentLocalPoint = initialLocalPositions[i];
            Vector3 nextLocalPoint = initialLocalPositions[(i + 1) % initialLocalPositions.Count];

            float segmentLength = Vector3.Distance(currentLocalPoint, nextLocalPoint);

            if (segmentLength < 0.0001f) // 매우 짧은 세그먼트 스킵
            {
                accumulatedDistance += segmentLength;
                continue;
            }

            // 찾고 있는 'distance'가 현재 세그먼트 안에 있는지 확인
            if (distance >= accumulatedDistance && distance < accumulatedDistance + segmentLength)
            {
                float t = (distance - accumulatedDistance) / segmentLength; // 세그먼트 내의 비율
                pos = Vector3.Lerp(currentLocalPoint, nextLocalPoint, t);

                Vector3 segmentDirection = nextLocalPoint - currentLocalPoint;

                if (segmentDirection.sqrMagnitude < 0.000001f) // 방향 벡터가 너무 작을 경우
                {
                    // rot는 초기값인 Quaternion.identity를 유지
                }
                else
                {
                    Vector3 forwardDirectionForLink;

                    // 링크의 Z축이 항상 '실제 이동하는 방향'을 바라보도록 합니다.
                    if (currentSpeed >= 0) // 정방향 움직임
                    {
                        forwardDirectionForLink = segmentDirection.normalized;
                    }
                    else // 역방향 움직임
                    {
                        forwardDirectionForLink = -segmentDirection.normalized; // 경로 방향의 반대
                    }

                    // Up 벡터를 부모 GameObject의 Up 벡터로 고정하여 뒤집힘 현상 방지
                    Vector3 calculatedUpVector = transform.up;

                    rot = Quaternion.LookRotation(forwardDirectionForLink, calculatedUpVector);

                    // 체인 모델의 초기 회전 오프셋 보정 (필요시 조정)
                    // 만약 체인 링크 모델 자체가 (0,0,0) 회전일 때 Unity의 Z축(Forward)이 아닌 다른 축이 모델의 "앞"을 가리킨다면 여기에 값을 넣어야 합니다.
                    // 예: 모델의 '앞'이 Y축이라면 rot *= Quaternion.Euler(90, 0, 0);
                    // 예: 모델의 '앞'이 -Z축이라면 rot *= Quaternion.Euler(0, 180, 0);
                }
                return;
            }
            accumulatedDistance += segmentLength;
        }

        Debug.LogWarning("GetPositionAndRotationOnInitialPath: 경로 거리 계산 오류. 첫 번째 초기 로컬 위치로 돌아갑니다.");
        pos = initialLocalPositions[0];
        // 오류 시에도 방향성을 유지하려면, 처음 두 점의 방향을 사용하되 속도에 따라 반전
        Vector3 defaultForward = initialLocalPositions.Count > 1 ? (initialLocalPositions[1] - initialLocalPositions[0]).normalized : Vector3.forward;
        if (currentSpeed < 0) defaultForward = -defaultForward;
        rot = Quaternion.LookRotation(defaultForward, transform.up);
    }

    // 체인 경로, 각 링크의 축을 시각화 (디버깅용)
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

    // PLC 연동을 위한 함수들
    public void ActiveChainCW()
    {
        isChainCw = true;
        isChainCCw = false;
    }
    public void DeActiveChainCW()
    {
        isChainCw = false;
    }
    public void ActiveChainCCW()
    {
        isChainCCw = true;
        isChainCw = false;
    }
    public void DeActiveChainCCW()
    {
        isChainCCw = false;
    }
}