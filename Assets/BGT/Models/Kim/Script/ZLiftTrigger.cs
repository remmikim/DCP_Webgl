using UnityEngine;
using System.Collections;

public class ZLiftTrigger : MonoBehaviour
{
    public GameObject LiftWeight;
    public GameObject CarriageFrame;

    public ZLiftRotation ROT;
    public ChainMove CHM;
    public ChainMove CHM1;

    // ActUtlManager 인스턴스 참조
    public ActUtlManager actUtlManager;

    public float moveSpeed = 0.2f;

    private float[] liftWeightMoveDistancesUp = { -1.725f, -1.269f, -0.5f, -2.5f };
    private float[] liftWeightMoveDistancesDown = { 0.797615f, 1.0f, 0.5f, 0.5f };

    private float[] carriageFrameMoveDistancesUp = { 1.725f, 1.269f, 0.5f, 2.5f };
    private float[] carriageFrameMoveDistancesDown = { -0.797615f, -1.0f, -0.5f, -0.5f };

    private Vector3 currentLWLocalTargetPosition;
    private Vector3 currentCFLocalTargetPosition;

    private bool isZLiftUpActive = false;
    private bool isZLiftDownActive = false;

    private int currentUpMoveIndex = 0;
    private int currentDownMoveIndex = 0;

    private Coroutine currentMovementCoroutine;

    void Update() { }

    /// <summary>
    /// ZLift를 '위' (LiftWeight Z축 음수, CarriageFrame Z축 양수)로 이동하는 프로세스를 시작합니다.
    /// 배열에 정의된 다음 거리만큼 이동합니다.
    /// </summary>
    public void ActivateZLiftUp()
    {
        if (isZLiftUpActive || isZLiftDownActive) return;

        if (currentUpMoveIndex >= liftWeightMoveDistancesUp.Length)
        {
            currentUpMoveIndex = 0;
            Debug.Log("ZLiftUp 이동 사이클 완료, 인덱스 재설정.");
        }

        isZLiftUpActive = true;
        SetTargetPositionsForUpMovement();

        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        currentMovementCoroutine = StartCoroutine(MoveZLiftToTarget(true));

        ROT.ActivateZLiftRotationCW();
        CHM.ActiveChainCW();
        CHM1.ActiveChainCW();

        // --- 수정된 부분: 움직임 시작 시 X4:1 신호 전송 ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X4:0");
        }
    }

    /// <summary>
    /// ZLift '위'로 이동을 비활성화합니다. (수동 중단을 위한 함수)
    /// </summary>
    public void DeactivateZLiftUp()
    {
        if (isZLiftUpActive)
        {
            isZLiftUpActive = false;
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
                currentMovementCoroutine = null;
            }
            ROT.DeactivateZLiftRotationCW();
            CHM.DeActiveChainCW();
            CHM1.DeActiveChainCW();

            // --- 수정된 부분: 수동 비활성화 시 X6:0 신호 전송 ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X4:1"); 
            }
            // --- 수정된 부분 끝 ---
            Debug.Log("ZLiftUp 수동 비활성화. 이동 중지.");
        }
    }

    /// <summary>
    /// ZLift를 '아래' (LiftWeight Z축 양수, CarriageFrame Z축 음수)로 이동하는 프로세스를 시작합니다.
    /// 배열에 정의된 다음 거리만큼 이동합니다.
    /// </summary>
    public void ActivateZLiftDown()
    {
        if (isZLiftUpActive || isZLiftDownActive) return;

        if (currentDownMoveIndex >= liftWeightMoveDistancesDown.Length)
        {
            currentDownMoveIndex = 0;
            Debug.Log("ZLiftDown 이동 사이클 완료, 인덱스 재설정.");
        }

        isZLiftDownActive = true;
        SetTargetPositionsForDownMovement();

        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        currentMovementCoroutine = StartCoroutine(MoveZLiftToTarget(false));

        ROT.ActivateZLiftRotationCCW();
        CHM.ActiveChainCCW();
        CHM1.ActiveChainCCW();

        // --- 수정된 부분: 움직임 시작 시 X6:1 신호 전송 ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X5:0"); 
        }
    }

    /// <summary>
    /// ZLift '아래'로 이동을 비활성화합니다. (수동 중단을 위한 함수)
    /// </summary>
    public void DeactivateZLiftDown()
    {
        if (isZLiftDownActive)
        {
            isZLiftDownActive = false;
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
                currentMovementCoroutine = null;
            }
            ROT.DeactivateZLiftRotationCCW();
            CHM.DeActiveChainCCW();
            CHM1.DeActiveChainCCW();

            // --- 수정된 부분: 수동 비활성화 시 X6:0 신호 전송 ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X5:1"); 
                Debug.Log("ZLiftTrigger: PLC에 X6:0 (리프트 DOWN 수동 비활성화) 명령 전송.");
            }
            // --- 수정된 부분 끝 ---

            Debug.Log("ZLiftDown 수동 비활성화. 이동 중지.");
        }
    }

    /// <summary>
    /// LiftWeight와 CarriageFrame을 현재 목표 위치(로컬 좌표)로 이동시키는 코루틴입니다.
    /// </summary>
    /// <param name="isUpDirection">'Up' 이동이면 true, 'Down' 이동이면 false.</param>
    private IEnumerator MoveZLiftToTarget(bool isUpDirection)
    {
        bool liftWeightReached = false;
        bool carriageFrameReached = false;

        while (!liftWeightReached || !carriageFrameReached)
        {
            if (LiftWeight != null && !liftWeightReached)
            {
                LiftWeight.transform.localPosition = Vector3.MoveTowards(LiftWeight.transform.localPosition, currentLWLocalTargetPosition, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(LiftWeight.transform.localPosition, currentLWLocalTargetPosition) < 0.001f)
                {
                    LiftWeight.transform.localPosition = currentLWLocalTargetPosition;
                    liftWeightReached = true;
                    Debug.Log($"LiftWeight가 로컬 Z: {currentLWLocalTargetPosition.z}에 도착했습니다.");
                }
            }

            if (CarriageFrame != null && !carriageFrameReached)
            {
                CarriageFrame.transform.localPosition = Vector3.MoveTowards(CarriageFrame.transform.localPosition, currentCFLocalTargetPosition, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(CarriageFrame.transform.localPosition, currentCFLocalTargetPosition) < 0.001f)
                {
                    CarriageFrame.transform.localPosition = currentCFLocalTargetPosition;
                    carriageFrameReached = true;
                    Debug.Log($"CarriageFrame이 로컬 Z: {currentCFLocalTargetPosition.z}에 도착했습니다.");
                }
            }

            yield return null;
        }

        // --- 이동 완료 후 자동 비활성화 로직 ---
        if (isUpDirection)
        {
            currentUpMoveIndex++;
            isZLiftUpActive = false; // <-- 여기서 false로 바뀝니다.
            ROT.DeactivateZLiftRotationCW();
            CHM.DeActiveChainCW();
            CHM1.DeActiveChainCW();

            // --- 수정된 부분: 동작 완료 시 X6:0 신호 전송 ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X4:1");
            }
            // --- 수정된 부분 끝 ---
            Debug.Log("ZLiftUp 이동 완료 및 자동 비활성화.");
        }
        else
        {
            currentDownMoveIndex++;
            isZLiftDownActive = false; // <-- 여기서 false로 바뀝니다.
            ROT.DeactivateZLiftRotationCCW();
            CHM.DeActiveChainCCW();
            CHM1.DeActiveChainCCW();

            // --- 수정된 부분: 동작 완료 시 X6:0 신호 전송 ---
            if (actUtlManager != null)
            {
                //actUtlManager.SendCommandToPlc("X4:1"); // 리프트 동작 완료를 PLC에 알림 (OFF)
                actUtlManager.SendCommandToPlc("X5:1"); 
            }
            // --- 수정된 부분 끝 ---
            Debug.Log("ZLiftDown 이동 완료 및 자동 비활성화.");
        }
        currentMovementCoroutine = null;
    }

    private void SetTargetPositionsForUpMovement()
    {
        float lwMoveAmountZ = liftWeightMoveDistancesUp[currentUpMoveIndex];
        currentLWLocalTargetPosition = new Vector3(LiftWeight.transform.localPosition.x, LiftWeight.transform.localPosition.y, LiftWeight.transform.localPosition.z + lwMoveAmountZ);

        float cfMoveAmountZ = carriageFrameMoveDistancesUp[currentUpMoveIndex];
        currentCFLocalTargetPosition = new Vector3(CarriageFrame.transform.localPosition.x, CarriageFrame.transform.localPosition.y, CarriageFrame.transform.localPosition.z + cfMoveAmountZ);
    }

    private void SetTargetPositionsForDownMovement()
    {
        float lwMoveAmountZ = liftWeightMoveDistancesDown[currentDownMoveIndex];
        currentLWLocalTargetPosition = new Vector3(LiftWeight.transform.localPosition.x, LiftWeight.transform.localPosition.y, LiftWeight.transform.localPosition.z + lwMoveAmountZ);

        float cfMoveAmountZ = carriageFrameMoveDistancesDown[currentDownMoveIndex];
        currentCFLocalTargetPosition = new Vector3(CarriageFrame.transform.localPosition.x, CarriageFrame.transform.localPosition.y, CarriageFrame.transform.localPosition.z + cfMoveAmountZ);
    }
}