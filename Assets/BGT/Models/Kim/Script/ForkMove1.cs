using UnityEngine;

public class ForkMove1 : MonoBehaviour
{
    // ActUtlManager 인스턴스 참조 추가
    public ActUtlManager actUtlManager;

    private float MoveSpeed = 0.2f;
    private float MoveAmountY = 0.272f;

    // 각 파이프 홀더의 시작 위치와 목표 위치 변수들
    private Vector3 StartPosition;   // PipeHolder1
    private Vector3 TargetPosition;  // PipeHolder1

    private bool isForkMoveRigt = false;
    private bool isForkMoveLeft = false;

    void Start()
    {
        // StartPosition은 현재 위치에서 초기화되므로 Start에서는 특별히 할 것이 없습니다.
    }

    // Update is called once per frame
    void Update()
    {
        if (isForkMoveRigt && !isForkMoveLeft)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, TargetPosition, MoveSpeed * Time.deltaTime);
           if (Vector3.Distance(transform.localPosition, TargetPosition) < 0.001f)
            {
                DeactivateRight();
                Debug.Log("Fork Right move completed automatically.");
            }
        }

        if (isForkMoveLeft && !isForkMoveRigt)
        {
            // 각 PipeHolder의 위치를 보간
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, TargetPosition, MoveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.localPosition, TargetPosition) < 0.001f)
            {
                DeactivateLeft();
                Debug.Log("Fork Left move completed automatically.");
            }
        }
    }

    public void ActivateRight()
    {
        if (isForkMoveRigt || isForkMoveLeft) return; // 이미 움직이고 있다면 무시

        isForkMoveRigt = true;
        isForkMoveLeft = false; // 다른 방향 플래그는 항상 false로 유지

        StartPosition = transform.localPosition;
        TargetPosition = StartPosition + new Vector3(0, MoveAmountY, 0); // Y축 양수 방향 이동 (Right)

        // --- 추가된 부분: 오른쪽 이동 시작 시 X10:1 신호 전송 ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X14:0"); // 포크 오른쪽 동작 시작을 PLC에 알림 (ON)
            Debug.Log("ForkMove1: PLC에 X10:1 (오른쪽 이동 시작) 명령 전송.");
        }
        // --- 추가된 부분 끝 ---

        Debug.Log($"Fork Right move activated. Local Y: {transform.localPosition.y} to {TargetPosition.y}");
    }

    public void DeactivateRight()
    {
        if (isForkMoveRigt) // 오른쪽 이동 중일 때만 비활성화
        {
            isForkMoveRigt = false;

            // --- 추가된 부분: 오른쪽 이동 비활성화 시 X10:0 신호 전송 ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X14:1"); // 포크 오른쪽 동작 정지를 PLC에 알림 (OFF)
                Debug.Log("ForkMove1: PLC에 X10:0 (오른쪽 이동 비활성화) 명령 전송.");
            }
            // --- 추가된 부분 끝 ---

            Debug.Log("Fork Right move deactivated.");
        }
    }

    public void ActivateLeft()
    {
        if (isForkMoveRigt || isForkMoveLeft) return; // 이미 움직이고 있다면 무시

        isForkMoveLeft = true;
        isForkMoveRigt = false; // 다른 방향 플래그는 항상 false로 유지

        StartPosition = transform.localPosition;
        TargetPosition = StartPosition + new Vector3(0, -MoveAmountY, 0); // Y축 음수 방향 이동 (Left)

        // --- 추가된 부분: 왼쪽 이동 시작 시 X11:1 신호 전송 ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X15:0"); // 포크 왼쪽 동작 시작을 PLC에 알림 (ON)
            Debug.Log("ForkMove1: PLC에 X11:1 (왼쪽 이동 시작) 명령 전송.");
        }
        // --- 추가된 부분 끝 ---

        Debug.Log($"Fork Left move activated. Local Y: {transform.localPosition.y} to {TargetPosition.y}");
    }

    public void DeactivateLeft()
    {
        if (isForkMoveLeft) // 왼쪽 이동 중일 때만 비활성화
        {
            isForkMoveLeft = false;

            // --- 추가된 부분: 왼쪽 이동 비활성화 시 X11:0 신호 전송 ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X15:1"); // 포크 왼쪽 동작 정지를 PLC에 알림 (OFF)
                Debug.Log("ForkMove1: PLC에 X11:0 (왼쪽 이동 비활성화) 명령 전송.");
            }
            // --- 추가된 부분 끝 ---

            Debug.Log("Fork Left move deactivated.");
        }
    }
}