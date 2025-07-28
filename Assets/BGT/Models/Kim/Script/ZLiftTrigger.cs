using UnityEngine;
using System.Collections;

public class ZLiftTrigger : MonoBehaviour
{
    public GameObject LiftWeight;
    public GameObject CarriageFrame;

    public ZLiftRotation ROT;
    public ChainMove CHM;
    public ChainMove CHM1;

    // ActUtlManager �ν��Ͻ� ����
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
    /// ZLift�� '��' (LiftWeight Z�� ����, CarriageFrame Z�� ���)�� �̵��ϴ� ���μ����� �����մϴ�.
    /// �迭�� ���ǵ� ���� �Ÿ���ŭ �̵��մϴ�.
    /// </summary>
    public void ActivateZLiftUp()
    {
        if (isZLiftUpActive || isZLiftDownActive) return;

        if (currentUpMoveIndex >= liftWeightMoveDistancesUp.Length)
        {
            currentUpMoveIndex = 0;
            Debug.Log("ZLiftUp �̵� ����Ŭ �Ϸ�, �ε��� �缳��.");
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

        // --- ������ �κ�: ������ ���� �� X4:1 ��ȣ ���� ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X4:0");
        }
    }

    /// <summary>
    /// ZLift '��'�� �̵��� ��Ȱ��ȭ�մϴ�. (���� �ߴ��� ���� �Լ�)
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

            // --- ������ �κ�: ���� ��Ȱ��ȭ �� X6:0 ��ȣ ���� ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X4:1"); 
            }
            // --- ������ �κ� �� ---
            Debug.Log("ZLiftUp ���� ��Ȱ��ȭ. �̵� ����.");
        }
    }

    /// <summary>
    /// ZLift�� '�Ʒ�' (LiftWeight Z�� ���, CarriageFrame Z�� ����)�� �̵��ϴ� ���μ����� �����մϴ�.
    /// �迭�� ���ǵ� ���� �Ÿ���ŭ �̵��մϴ�.
    /// </summary>
    public void ActivateZLiftDown()
    {
        if (isZLiftUpActive || isZLiftDownActive) return;

        if (currentDownMoveIndex >= liftWeightMoveDistancesDown.Length)
        {
            currentDownMoveIndex = 0;
            Debug.Log("ZLiftDown �̵� ����Ŭ �Ϸ�, �ε��� �缳��.");
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

        // --- ������ �κ�: ������ ���� �� X6:1 ��ȣ ���� ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X5:0"); 
        }
    }

    /// <summary>
    /// ZLift '�Ʒ�'�� �̵��� ��Ȱ��ȭ�մϴ�. (���� �ߴ��� ���� �Լ�)
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

            // --- ������ �κ�: ���� ��Ȱ��ȭ �� X6:0 ��ȣ ���� ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X5:1"); 
                Debug.Log("ZLiftTrigger: PLC�� X6:0 (����Ʈ DOWN ���� ��Ȱ��ȭ) ��� ����.");
            }
            // --- ������ �κ� �� ---

            Debug.Log("ZLiftDown ���� ��Ȱ��ȭ. �̵� ����.");
        }
    }

    /// <summary>
    /// LiftWeight�� CarriageFrame�� ���� ��ǥ ��ġ(���� ��ǥ)�� �̵���Ű�� �ڷ�ƾ�Դϴ�.
    /// </summary>
    /// <param name="isUpDirection">'Up' �̵��̸� true, 'Down' �̵��̸� false.</param>
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
                    Debug.Log($"LiftWeight�� ���� Z: {currentLWLocalTargetPosition.z}�� �����߽��ϴ�.");
                }
            }

            if (CarriageFrame != null && !carriageFrameReached)
            {
                CarriageFrame.transform.localPosition = Vector3.MoveTowards(CarriageFrame.transform.localPosition, currentCFLocalTargetPosition, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(CarriageFrame.transform.localPosition, currentCFLocalTargetPosition) < 0.001f)
                {
                    CarriageFrame.transform.localPosition = currentCFLocalTargetPosition;
                    carriageFrameReached = true;
                    Debug.Log($"CarriageFrame�� ���� Z: {currentCFLocalTargetPosition.z}�� �����߽��ϴ�.");
                }
            }

            yield return null;
        }

        // --- �̵� �Ϸ� �� �ڵ� ��Ȱ��ȭ ���� ---
        if (isUpDirection)
        {
            currentUpMoveIndex++;
            isZLiftUpActive = false; // <-- ���⼭ false�� �ٲ�ϴ�.
            ROT.DeactivateZLiftRotationCW();
            CHM.DeActiveChainCW();
            CHM1.DeActiveChainCW();

            // --- ������ �κ�: ���� �Ϸ� �� X6:0 ��ȣ ���� ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X4:1");
            }
            // --- ������ �κ� �� ---
            Debug.Log("ZLiftUp �̵� �Ϸ� �� �ڵ� ��Ȱ��ȭ.");
        }
        else
        {
            currentDownMoveIndex++;
            isZLiftDownActive = false; // <-- ���⼭ false�� �ٲ�ϴ�.
            ROT.DeactivateZLiftRotationCCW();
            CHM.DeActiveChainCCW();
            CHM1.DeActiveChainCCW();

            // --- ������ �κ�: ���� �Ϸ� �� X6:0 ��ȣ ���� ---
            if (actUtlManager != null)
            {
                //actUtlManager.SendCommandToPlc("X4:1"); // ����Ʈ ���� �ϷḦ PLC�� �˸� (OFF)
                actUtlManager.SendCommandToPlc("X5:1"); 
            }
            // --- ������ �κ� �� ---
            Debug.Log("ZLiftDown �̵� �Ϸ� �� �ڵ� ��Ȱ��ȭ.");
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