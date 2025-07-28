using UnityEngine;
// ActUtlType64Lib�� ���� Manager.cs���� ���� ������� �ʽ��ϴ�.

public class Manager1 : MonoBehaviour
{
    // ActUtlManager �ν��Ͻ��� ���� ���� (�ν����Ϳ��� �Ҵ�)
    public ActUtlManager actUtlManager;

    // Y ����̽� ���¸� ���� (PLC�κ��� �о��)
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


    // ������ Unity ������Ʈ ��ũ��Ʈ ���� (�ν����Ϳ��� �Ҵ�)
    public RoofMove roofMove;
    public PipeHolders pipeHolders;
    public ZLiftTrigger zLift;
    public Base2Down base2down1;
    public Base2Down base2down2;
    public Base2Down base2down3;
    public Base2Down base2down4;
    public XGantry xGantry;
    public CarriageFrameRT carriageFrameRT;
    public ForkMove1 forkMove;
    public ForkFrontBackMove forkFrontBackMoveLeft;
    public ForkFrontBackMove forkFrontBackMoveRight;

    // ManagerWrite ��ũ��Ʈ ���� (�ν����Ϳ��� �Ҵ�)
    public ManagerWrite1 managerWrite;

    void OnEnable()
    {
        // ActUtlManager���� ������ ���� �� ���� ���� ���� �̺�Ʈ�� ����
        if (actUtlManager != null)
        {
            ActUtlManager.OnPlcDataReceived += HandlePlcData;
            ActUtlManager.OnConnectionStatusChanged += HandleConnectionStatusChange;
        }
    }

    void OnDisable()
    {
        // ��ũ��Ʈ ��Ȱ��ȭ �� �̺�Ʈ ���� ���� (�޸� ���� ����)
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
    /// ActUtlManager�κ��� PLC �����͸� �����ϸ� ȣ��Ǵ� �ݹ� �Լ�
    /// �� �Լ��� Unity�� ���� �����忡�� ����˴ϴ�.
    /// </summary>
    /// <param name="receivedData">PLC�κ��� ���� ���� ���ڿ� ������ (��: "Y0YF:1234")</param>
    private void HandlePlcData(string receivedData)
    {
        // PLC ��� �������ݿ� ���� ���ŵ� ���ڿ��� �Ľ��ϰ� Unity ������Ʈ ���¸� ������Ʈ�մϴ�.
        // ActUtlManager�� Y0YF ���� ���� "Y0YF:��" ���·� �����ϴ�.
        if (receivedData.StartsWith("Y0YF:"))
        {
            string[] parts = receivedData.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int y0ToYFValue))
            {
                // Y0 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY0State, y0ToYFValue, 0, roofMove != null ? roofMove.ActivateFrontRoof : null, roofMove != null ? roofMove.DeactivateFrontRoof : null);
                // Y1 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY1State, y0ToYFValue, 1, roofMove != null ? roofMove.ActivateBackRoof : null, roofMove != null ? roofMove.DeactivateBackRoof : null);
                // Y2 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY2State, y0ToYFValue, 2, pipeHolders != null ? pipeHolders.ActivatePipeHoldersCW : null, pipeHolders != null ? pipeHolders.DeactivatePipeHoldersCW : null);
                // Y3 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY3State, y0ToYFValue, 3, pipeHolders != null ? pipeHolders.ActivatePipeHoldersCCW : null, pipeHolders != null ? pipeHolders.DeactivatePipeHoldersCCW : null);
                // Y4 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY4State, y0ToYFValue, 4, zLift != null ? zLift.ActivateZLiftUp : null, zLift != null ? zLift.DeactivateZLiftUp : null);
                // Y5 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY5State, y0ToYFValue, 5, zLift != null ? zLift.ActivateZLiftDown : null, zLift != null ? zLift.DeactivateZLiftDown : null);
                // Y6 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY60State, y0ToYFValue, 6, base2down1 != null ? base2down1.ActiveDown : null, base2down1 != null ? base2down1.DeactiveDown : null);
                UpdateYStateBit(ref currentY61State, y0ToYFValue, 6, base2down2 != null ? base2down2.ActiveDown : null, base2down2 != null ? base2down2.DeactiveDown : null);
                UpdateYStateBit(ref currentY62State, y0ToYFValue, 6, base2down3 != null ? base2down3.ActiveDown : null, base2down3 != null ? base2down3.DeactiveDown : null);
                UpdateYStateBit(ref currentY63State, y0ToYFValue, 6, base2down4 != null ? base2down4.ActiveDown : null, base2down4 != null ? base2down4.DeactiveDown : null);
                // Y7 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY70State, y0ToYFValue, 7, base2down1 != null ? base2down1.ActiveUp : null, base2down1 != null ? base2down1.DeactiveUp : null);
                UpdateYStateBit(ref currentY71State, y0ToYFValue, 7, base2down2 != null ? base2down2.ActiveUp : null, base2down2 != null ? base2down2.DeactiveUp : null);
                UpdateYStateBit(ref currentY72State, y0ToYFValue, 7, base2down3 != null ? base2down3.ActiveUp : null, base2down3 != null ? base2down3.DeactiveUp : null);
                UpdateYStateBit(ref currentY73State, y0ToYFValue, 7, base2down4 != null ? base2down4.ActiveUp : null, base2down4 != null ? base2down4.DeactiveUp : null);
                // Y8 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY8State, y0ToYFValue, 8, xGantry != null ? xGantry.ActivateXGantryMovingRight : null, xGantry != null ? xGantry.DeactivateXGantryMovingRight : null);
                // Y9 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY9State, y0ToYFValue, 9, xGantry != null ? xGantry.ActivateXGantryMovingLeft : null, xGantry != null ? xGantry.DeactivateXGantryMovingLeft : null);
                // Y10 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentYAState, y0ToYFValue, 10, carriageFrameRT != null ? carriageFrameRT.ActivateZLiftRotationCW : null, carriageFrameRT != null ? carriageFrameRT.DeactivateZLiftRotationCW : null);
                // Y11 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentYBState, y0ToYFValue, 11, carriageFrameRT != null ? carriageFrameRT.ActivateZLiftRotationCCW : null, carriageFrameRT != null ? carriageFrameRT.DeactivateZLiftRotationCCW : null);
                //// Y12 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentYCState, y0ToYFValue, 12, forkMove != null ? forkMove.ActivateRight : null, forkMove != null ? forkMove.DeactivateRight : null);
                // Y13 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentYDState, y0ToYFValue, 13, forkMove != null ? forkMove.ActivateLeft : null, forkMove != null ? forkMove.DeactivateLeft : null);
            }
        }
        if (receivedData.StartsWith("Y10Y1F:"))
        {
            string[] parts = receivedData.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int y10ToY1FValue))
            {
                // Y12 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY101State, y10ToY1FValue, 0, forkFrontBackMoveLeft != null ? forkFrontBackMoveLeft.ActivateFront : null, forkFrontBackMoveLeft != null ? forkFrontBackMoveLeft.DeactivateFront : null);
                UpdateYStateBit(ref currentY102State, y10ToY1FValue, 0, forkFrontBackMoveRight != null ? forkFrontBackMoveRight.ActivateFront : null, forkFrontBackMoveRight != null ? forkFrontBackMoveRight.DeactivateFront : null);
                // Y13 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY111State, y10ToY1FValue, 1, forkFrontBackMoveLeft != null ? forkFrontBackMoveLeft.ActivateBack : null, forkFrontBackMoveLeft != null ? forkFrontBackMoveLeft.DeactivateBack : null);
                UpdateYStateBit(ref currentY112State, y10ToY1FValue, 1, forkFrontBackMoveRight != null ? forkFrontBackMoveRight.ActivateBack : null, forkFrontBackMoveRight != null ? forkFrontBackMoveRight.DeactivateBack : null);
                
            }
        }
        // �ٸ� ������ PLC �����Ͱ� �ִٸ� ���⿡ �߰� �Ľ� ���� ����
    }

    /// <summary>
    /// PLC ���� ������ Ư�� ��Ʈ�� ���¸� �����ϰ�, ���� ���� �� Unity �׼��� ȣ���ϴ� ���� �޼���
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
                // Debug.Log($"Y{bitIndex} Ȱ��ȭ");
            }
            else
            {
                deactivateAction?.Invoke();
                // Debug.Log($"Y{bitIndex} ��Ȱ��ȭ");
            }
        }
    }

    /// <summary>
    /// PLC ���� ���� ��ȭ�� ó���ϴ� �ݹ� �Լ�
    /// </summary>
    private void HandleConnectionStatusChange(bool connected)
    {
        if (connected)
        {
            Debug.Log("Manager: PLC ������ Ȱ��ȭ�Ǿ����ϴ�.");
        }
        else
        {
            Debug.LogWarning("Manager: PLC ������ �������ų� �������� �ʾҽ��ϴ�. ��׶��忡�� �翬�� �õ� ��...");
        }
    }
}