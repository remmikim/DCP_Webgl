using UnityEngine;

public class CarriageFrameRT : MonoBehaviour
{
    // 유니티 에디터에서 조절할 회전 속도 (초당 각도)
    public float rotationSpeed = 90f; // 90f로 설정하면 2초 안에 180도 회전

    // ActUtlManager 인스턴스 참조 추가
    public ActUtlManager actUtlManager;

    // 회전 방향 플래그
    private bool isZLiftRotationCW = false;
    private bool isZLiftRotationCCW = false;

    // 회전 시작 시의 초기 회전값
    private Quaternion startRotation;
    // 도달해야 할 최종 목표 회전값
    private Quaternion targetRotation;

    // 현재 회전이 진행 중인지 나타내는 플래그
    private bool isRotating = false;

    void Start()
    {
        startRotation = transform.rotation;
    }

    void Update()
    {
        // 회전이 진행 중일 때만 회전 로직을 실행
        if (isRotating)
        {
            // 현재 회전값에서 목표 회전값으로 'rotationSpeed' 만큼 보간하여 이동
            // Time.deltaTime을 곱해 프레임 속도에 독립적인 회전을 보장
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // 회전이 목표에 거의 도달했는지 확인
            // Quaternion.Angle은 두 쿼터니언 사이의 각도 차이를 반환
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f) // 0.1도 이내 오차 허용
            {
                transform.rotation = targetRotation; // 정확한 목표 회전값으로 설정하여 오차 보정
                isRotating = false; // 회전 완료 플래그 false

                // 현재 활성화된 회전 방향에 따라 PLC 신호 전송
                if (isZLiftRotationCW) // CW 회전이 완료됨
                {
                    isZLiftRotationCW = false;
                    Debug.Log("CW 180도 회전 완료!");
                    // --- 추가된 부분: CW 회전 완료 시 X12:0 신호 전송 ---
                    if (actUtlManager != null)
                    {
                        actUtlManager.SendCommandToPlc("X12:1");
                    }
                    // --- 추가된 부분 끝 ---
                }
                else if (isZLiftRotationCCW) // CCW 회전이 완료됨
                {
                    isZLiftRotationCCW = false;
                    Debug.Log("CCW 180도 회전 완료!");
                    // --- 추가된 부분: CCW 회전 완료 시 X13:0 신호 전송 ---
                    if (actUtlManager != null)
                    {
                        actUtlManager.SendCommandToPlc("X13:1");
                    }
                    // --- 추가된 부분 끝 ---
                }
            }
        }
    }

    public void ActivateZLiftRotationCW()
    {
        // 이미 회전 중이라면 새로운 회전 명령 무시
        if (isRotating) return;

        isZLiftRotationCW = true;
        isZLiftRotationCCW = false; // 반대 방향 플래그는 false
        isRotating = true; // 회전 시작

        startRotation = transform.rotation; // 현재 회전값을 시작점으로 저장
        // 현재 회전에서 Z축을 중심으로 180도 시계 방향으로 회전하는 목표 회전값 계산
        targetRotation = startRotation * Quaternion.Euler(180f, 0, 0);
        Debug.Log("CW 180도 회전 시작!");

        // --- 추가된 부분: 회전 시작 시 X9:1 신호 전송 ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X12:0"); 
        }
        // --- 추가된 부분 끝 ---
    }

    public void DeactivateZLiftRotationCW()
    {
        // 이 스크립트에서는 180도 회전이 완료되면 자동으로 isRotating이 false가 되므로
        // 이 Deactivate 함수가 직접적으로 회전을 멈추는 역할은 하지 않습니다.
        // 하지만 외부 시스템과의 일관성을 위해 PLC 신호 전송 로직을 추가할 수 있습니다.
        if (isRotating) // 만약 회전 중에 외부에서 Deactivate가 호출된다면
        {
            isRotating = false;
            isZLiftRotationCW = false;
            // --- 추가된 부분: 수동 비활성화 시 X9:0 신호 전송 ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X12:1");
            }
            // --- 추가된 부분 끝 ---
        }
    }

    public void ActivateZLiftRotationCCW()
    {
        // 이미 회전 중이라면 새로운 회전 명령 무시
        if (isRotating) return;

        isZLiftRotationCCW = true;
        isZLiftRotationCW = false; // 반대 방향 플래그는 false
        isRotating = true; // 회전 시작

        startRotation = transform.rotation; // 현재 회전값을 시작점으로 저장
        // 현재 회전에서 Z축을 중심으로 -180도 (반시계 방향)로 회전하는 목표 회전값 계산
        targetRotation = startRotation * Quaternion.Euler(-180f, 0, 0);
        Debug.Log("CCW 180도 회전 시작!");

        // --- 추가된 부분: 회전 시작 시 X9:1 신호 전송 ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X13:0"); 
        }
        // --- 추가된 부분 끝 ---
    }

    public void DeactivateZLiftRotationCCW()
    {
        // 이 스크립트에서는 180도 회전이 완료되면 자동으로 isRotating이 false가 되므로
        // 이 Deactivate 함수가 직접적으로 회전을 멈추는 역할은 하지 않습니다.
        // 하지만 외부 시스템과의 일관성을 위해 PLC 신호 전송 로직을 추가할 수 있습니다.
        if (isRotating) // 만약 회전 중에 외부에서 Deactivate가 호출된다면
        {
            isRotating = false;
            isZLiftRotationCCW = false;
            // --- 추가된 부분: 수동 비활성화 시 X9:0 신호 전송 ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X13:1"); 
            }
            // --- 추가된 부분 끝 ---
        }
    }
}