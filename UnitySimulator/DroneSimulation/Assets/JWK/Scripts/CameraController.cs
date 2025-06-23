using UnityEngine;

// 클래스 이름을 파일명과 동일하게 CameraController로 수정했습니다.
public class CameraController : MonoBehaviour
{
    public GameObject TargetDrone; // 카메라가 따라다닐 타겟 (드론)

    [Header("카메라 위치 오프셋 (타겟 기준 상대 좌표)")]
    public float distanceBehind = 8.0f;    // 타겟 뒤쪽으로 얼마나 떨어질지
    public float heightAbove = 4.0f;        // 타겟 위쪽으로 얼마나 높이 있을지

    [Header("카메라 움직임")]
    public float positionSmoothTime = 0.2f; // 위치 따라가는 데 걸리는 시간 (작을수록 빠르고 반응적, 클수록 부드러움)
    public float rotationSmoothTime = 0.15f;// 회전 따라가는 데 걸리는 시간

    private Vector3 _targetPositionOffset;  // 타겟의 로컬 좌표계 기준 오프셋 (Start에서 계산됨)
    private Vector3 _currentPositionVelocity; // SmoothDamp 참조용 현재 위치 변화 속도
    private Vector3 _currentLookDirVelocity;  // SmoothDamp 참조용 현재 시선 방향 변화 속도 (회전에 사용)

    void Start()
    {
        if (TargetDrone == null)
        {
            Debug.LogError("카메라의 TargetDrone이 설정되지 않았습니다! 스크립트를 비활성화합니다.");
            enabled = false; // TargetDrone 없으면 스크립트 중지
            return;
        }

        // 초기 오프셋 계산 (드론의 뒤쪽, 약간 위)
        _targetPositionOffset = new Vector3(0, heightAbove, -distanceBehind);

        // 게임 시작 시 카메라 초기 위치 및 방향 설정 (선택적)
        Vector3 initialDesiredPosition = TargetDrone.transform.TransformPoint(_targetPositionOffset);
        transform.position = initialDesiredPosition;

        Vector3 initialLookAtPoint = TargetDrone.transform.position + Vector3.up * 1.5f; // 드론의 약간 위를 바라봄
        // initialLookAtPoint와 현재 카메라 위치가 동일하면 LookRotation 오류 발생 가능성 있으므로 체크
        if ((initialLookAtPoint - transform.position).sqrMagnitude > 0.0001f) 
        {
            transform.rotation = Quaternion.LookRotation(initialLookAtPoint - transform.position);
        }
        else // 매우 드문 경우지만, 초기 위치와 바라보는 지점이 같으면 기본 정면 방향 등으로 설정
        {
            if(TargetDrone.transform.forward != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(TargetDrone.transform.forward);
            else
                transform.rotation = Quaternion.identity;
        }
    }

    void LateUpdate()
    {
        if (TargetDrone == null) return; // 타겟 드론이 없으면 아무것도 안 함

        // 1. 목표 카메라 위치 계산 (드론의 로컬 좌표 기준 오프셋 적용)
        // TargetDrone.transform.TransformPoint는 _targetPositionOffset을 로컬 공간에서 월드 공간으로 변환합니다.
        // 즉, 드론이 회전하면 카메라도 드론의 상대적인 뒤쪽을 유지하며 따라 회전합니다.
        Vector3 desiredPosition = TargetDrone.transform.TransformPoint(_targetPositionOffset);

        // 2. 카메라 위치 부드럽게 이동 (SmoothDamp 사용)
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentPositionVelocity, positionSmoothTime);

        // 3. 카메라가 항상 타겟을 부드럽게 바라보도록 설정
        // 바라볼 목표 지점 (드론의 약간 위)
        Vector3 lookAtPoint = TargetDrone.transform.position + Vector3.up * 1.5f; // Y 오프셋 값 조절 가능

        // 현재 카메라가 바라보는 방향 벡터
        Vector3 currentForward = transform.forward;
        // 목표로 해야 할 방향 벡터
        Vector3 desiredDirection = (lookAtPoint - transform.position);

        // desiredDirection이 0벡터에 매우 가까운 경우 (카메라와 lookAtPoint가 거의 겹칠 때) 오류 방지
        if (desiredDirection.sqrMagnitude < 0.0001f)
        {
            // 현재 회전을 유지하거나, TargetDrone의 forward를 사용해 기본 방향 설정
            // 이 경우 추가적인 회전 로직은 생략할 수 있습니다.
            return;
        }
        desiredDirection.Normalize();


        // SmoothDamp를 사용하여 현재 방향에서 목표 방향으로 부드럽게 변경
        Vector3 smoothedForward = Vector3.SmoothDamp(currentForward, desiredDirection, ref _currentLookDirVelocity, rotationSmoothTime);

        // 만약 smoothedForward가 거의 0벡터가 되면, LookRotation이 오류를 발생시킬 수 있음
        if (smoothedForward.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(smoothedForward);
        }
    }
}
