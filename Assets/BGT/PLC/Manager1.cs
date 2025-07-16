using UnityEngine;
// ActUtlType64Lib는 이제 Manager.cs에서 직접 사용하지 않습니다.

public class Manager1 : MonoBehaviour
{
    // ActUtlManager 인스턴스에 대한 참조 (인스펙터에서 할당)
    public ActUtlManager actUtlManager;

    // Y 디바이스 상태를 추적 (PLC로부터 읽어옴)
    private bool currentY0State; private bool currentY1State;
    private bool currentY2State; private bool currentY3State;
    private bool currentY4State; private bool currentY5State;
    private bool currentY60State; private bool currentY61State;
    private bool currentY62State; private bool currentY63State;
    private bool currentY70State; private bool currentY71State;
    private bool currentY72State; private bool currentY73State;
    private bool currentY8State; private bool currentY9State;
    private bool currentYAState; private bool currentYBState;
    private bool currentYCState; private bool currentYDState;

    private bool currentY101State; private bool currentY111State;
    private bool currentY102State; private bool currentY112State;


    // 제어할 Unity 오브젝트 스크립트 참조 (인스펙터에서 할당)
    public RoofMove roofMove;
    public PipeHolders pipeHolders;
    public ZLiftTigger zLift;
    public Base2Down base2down1;
    public Base2Down base2down2;
    public Base2Down base2down3;
    public Base2Down base2down4;
    public XGantry xGantry;
    public CarriageFrameRT carriageFrameRT;
    public ForkMove1 forkMove;
    public ForkFrontBackMove forkFrontBackMoveLeft;
    public ForkFrontBackMove forkFrontBackMoveRight;

    // ManagerWrite 스크립트 참조 (인스펙터에서 할당)
    public ManagerWrite1 managerWrite;

    void OnEnable()
    {
        // ActUtlManager에서 데이터 수신 및 연결 상태 변경 이벤트를 구독
        if (actUtlManager != null)
        {
            ActUtlManager.OnPlcDataReceived += HandlePlcData;
            ActUtlManager.OnConnectionStatusChanged += HandleConnectionStatusChange;
        }
    }

    void OnDisable()
    {
        // 스크립트 비활성화 시 이벤트 구독 해제 (메모리 누수 방지)
        if (actUtlManager != null)
        {
            ActUtlManager.OnPlcDataReceived -= HandlePlcData;
            ActUtlManager.OnConnectionStatusChanged -= HandleConnectionStatusChange;
        }
    }

    void Update()
    {
        managerWrite.WriteDevice(); 
    }

    /// <summary>
    /// ActUtlManager로부터 PLC 데이터를 수신하면 호출되는 콜백 함수
    /// 이 함수는 Unity의 메인 스레드에서 실행됩니다.
    /// </summary>
    /// <param name="receivedData">PLC로부터 받은 원시 문자열 데이터 (예: "Y0YF:1234")</param>
    private void HandlePlcData(string receivedData)
    {
        // PLC 통신 프로토콜에 따라 수신된 문자열을 파싱하고 Unity 오브젝트 상태를 업데이트합니다.
        // ActUtlManager는 Y0YF 워드 값을 "Y0YF:값" 형태로 보냅니다.
        if (receivedData.StartsWith("Y0YF:"))
        {
            string[] parts = receivedData.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int y0ToYFValue))
            {
                // Y0 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentY0State, y0ToYFValue, 0, roofMove != null ? roofMove.ActivateFrontRoof : null, roofMove != null ? roofMove.DeactivateFrontRoof : null);
                // Y1 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentY1State, y0ToYFValue, 1, roofMove != null ? roofMove.ActivateBackRoof : null, roofMove != null ? roofMove.DeactivateBackRoof : null);
                // Y2 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentY2State, y0ToYFValue, 2, pipeHolders != null ? pipeHolders.ActivatePipeHoldersCW : null, pipeHolders != null ? pipeHolders.DeactivatePipeHoldersCW : null);
                // Y3 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentY3State, y0ToYFValue, 3, pipeHolders != null ? pipeHolders.ActivatePipeHoldersCCW : null, pipeHolders != null ? pipeHolders.DeactivatePipeHoldersCCW : null);
                // Y4 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentY4State, y0ToYFValue, 4, zLift != null ? zLift.ActivateZLiftUp : null, zLift != null ? zLift.DeactivateZLiftUp : null);
                // Y5 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentY5State, y0ToYFValue, 5, zLift != null ? zLift.ActivateZLiftDown : null, zLift != null ? zLift.DeactivateZLiftDown : null);
                // Y6 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentY60State, y0ToYFValue, 6, base2down1 != null ? base2down1.ActiveDown : null, base2down1 != null ? base2down1.DeactiveDown : null);
                UpdateYStateBit(ref currentY61State, y0ToYFValue, 6, base2down2 != null ? base2down2.ActiveDown : null, base2down2 != null ? base2down2.DeactiveDown : null);
                UpdateYStateBit(ref currentY62State, y0ToYFValue, 6, base2down3 != null ? base2down3.ActiveDown : null, base2down3 != null ? base2down3.DeactiveDown : null);
                UpdateYStateBit(ref currentY63State, y0ToYFValue, 6, base2down4 != null ? base2down4.ActiveDown : null, base2down4 != null ? base2down4.DeactiveDown : null);
                // Y7 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentY70State, y0ToYFValue, 7, base2down1 != null ? base2down1.ActiveUp : null, base2down1 != null ? base2down1.DeactiveUp : null);
                UpdateYStateBit(ref currentY71State, y0ToYFValue, 7, base2down2 != null ? base2down2.ActiveUp : null, base2down2 != null ? base2down2.DeactiveUp : null);
                UpdateYStateBit(ref currentY72State, y0ToYFValue, 7, base2down3 != null ? base2down3.ActiveUp : null, base2down3 != null ? base2down3.DeactiveUp : null);
                UpdateYStateBit(ref currentY73State, y0ToYFValue, 7, base2down4 != null ? base2down4.ActiveUp : null, base2down4 != null ? base2down4.DeactiveUp : null);
                // Y8 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentY8State, y0ToYFValue, 8, xGantry != null ? xGantry.ActivateXGantryMovingRight : null, xGantry != null ? xGantry.DeactivateXGantryMovingRight : null);
                // Y9 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentY9State, y0ToYFValue, 9, xGantry != null ? xGantry.ActivateXGantryMovingLeft : null, xGantry != null ? xGantry.DeactivateXGantryMovingLeft : null);
                // Y10 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentYAState, y0ToYFValue, 10, carriageFrameRT != null ? carriageFrameRT.ActivateZLiftRotationCW : null, carriageFrameRT != null ? carriageFrameRT.DeactivateZLiftRotationCW : null);
                // Y11 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentYBState, y0ToYFValue, 11, carriageFrameRT != null ? carriageFrameRT.ActivateZLiftRotationCCW : null, carriageFrameRT != null ? carriageFrameRT.DeactivateZLiftRotationCCW : null);
                //// Y12 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentYCState, y0ToYFValue, 12, forkMove != null ? forkMove.ActivateRight : null, forkMove != null ? forkMove.DeactivateRight : null);
                // Y13 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentYDState, y0ToYFValue, 13, forkMove != null ? forkMove.ActivateLeft : null, forkMove != null ? forkMove.DeactivateLeft : null);
            }
        }
        if (receivedData.StartsWith("Y10Y1F:"))
        {
            string[] parts = receivedData.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int y10ToY1FValue))
            {
                // Y12 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentY101State, y10ToY1FValue, 0, forkFrontBackMoveLeft != null ? forkFrontBackMoveLeft.ActivateFront : null, forkFrontBackMoveLeft != null ? forkFrontBackMoveLeft.DeactivateFront : null);
                UpdateYStateBit(ref currentY102State, y10ToY1FValue, 0, forkFrontBackMoveRight != null ? forkFrontBackMoveRight.ActivateFront : null, forkFrontBackMoveRight != null ? forkFrontBackMoveRight.DeactivateFront : null);
                // Y13 비트 추출 및 상태 변경 감지
                UpdateYStateBit(ref currentY111State, y10ToY1FValue, 1, forkFrontBackMoveLeft != null ? forkFrontBackMoveLeft.ActivateBack : null, forkFrontBackMoveLeft != null ? forkFrontBackMoveLeft.DeactivateBack : null);
                UpdateYStateBit(ref currentY112State, y10ToY1FValue, 1, forkFrontBackMoveRight != null ? forkFrontBackMoveRight.ActivateBack : null, forkFrontBackMoveRight != null ? forkFrontBackMoveRight.DeactivateBack : null);
                
            }
        }
        // 다른 유형의 PLC 데이터가 있다면 여기에 추가 파싱 로직 구현
    }

    /// <summary>
    /// PLC 워드 값에서 특정 비트의 상태를 추출하고, 상태 변경 시 Unity 액션을 호출하는 헬퍼 메서드
    /// </summary>
    private void UpdateYStateBit(ref bool currentState, int wordValue, int bitIndex, System.Action activateAction, System.Action deactivateAction)
    {
        bool newBitState = ((wordValue >> bitIndex) & 1) == 1;
        if (newBitState != currentState)
        {
            currentState = newBitState;
            if (currentState)
            {
                activateAction?.Invoke();
                // Debug.Log($"Y{bitIndex} 활성화");
            }
            else
            {
                deactivateAction?.Invoke();
                // Debug.Log($"Y{bitIndex} 비활성화");
            }
        }
    }

    /// <summary>
    /// PLC 연결 상태 변화를 처리하는 콜백 함수
    /// </summary>
    private void HandleConnectionStatusChange(bool connected)
    {
        if (connected)
        {
            Debug.Log("Manager: PLC 연결이 활성화되었습니다.");
        }
        else
        {
            Debug.LogWarning("Manager: PLC 연결이 끊어졌거나 설정되지 않았습니다. 백그라운드에서 재연결 시도 중...");
        }
    }
}