using UnityEngine;
using System.Collections; // �ڷ�ƾ ����� ���� �߰��ؾ� �մϴ�.

public class ZLiftTrigger : MonoBehaviour
{
    public GameObject LiftWeight;
    public GameObject CarriageFrame;

    public ZLiftRotation ROT;
    public ChainMove CHM;
    public ChainMove CHM1;

    // �̵� �ӵ� (�ʴ� �̵� �Ÿ�)
    public float moveSpeed = 0.2f; // �ν����Ϳ��� ���� ������ �� �ֵ��� public���� ����

    // LiftWeight�� Z�� �̵� �Ÿ��� (��Ÿ ��)
    // "Up" (LiftWeight�� Z�� ���� ����)�� ���� �̵� �Ÿ���
    private float[] liftWeightMoveDistancesUp = {-0.52f, -1.0f, -0.5f, -2.5f };
    // "Down" (LiftWeight�� Z�� ��� ����)�� ���� �̵� �Ÿ���
    private float[] liftWeightMoveDistancesDown = {0.52f, 1.0f, 0.5f, 0.5f };

    // CarriageFrame�� Z�� �̵� �Ÿ��� (��Ÿ ��)
    // "Up" (CarriageFrame�� Z�� ��� ����)�� ���� �̵� �Ÿ���
    private float[] carriageFrameMoveDistancesUp = { 0.52f, 1.0f, 0.5f, 2.5f };
    // "Down" (CarriageFrame�� Z�� ���� ����)�� ���� �̵� �Ÿ���
    private float[] carriageFrameMoveDistancesDown = {-0.52f, -1.0f, -0.5f, -0.5f };

   
    // ���� Ȱ�� �̵��� ���� ���� ��ǥ ��ġ (���� ��ǥ)
    private Vector3 currentLWLocalTargetPosition;
    private Vector3 currentCFLocalTargetPosition;

    // �̵� ���� �÷���
    private bool isZLiftUpActive = false;
    private bool isZLiftDownActive = false;

    // �迭���� ���� �̵� �ܰ踦 �����ϱ� ���� �ε���
    private int currentUpMoveIndex = 0;
    private int currentDownMoveIndex = 0;

    // ���� ���� �̵��� �����ؾ� �� ��츦 ���� �ڷ�ƾ ����
    private Coroutine currentMovementCoroutine;

    
    // Update�� �� �̻� �������� �̵� ������ ������� �ʽ��ϴ�.
    // �̵� ������ ���� �ڷ�ƾ���� ó���˴ϴ�.
    void Update()
    {
        // �ʿ��ϴٸ� ������̳� �ٸ� �������� �˻翡 Update�� ����� �� ������,
        // �������� �̵� ������ �ڷ�ƾ���� �Ű������ϴ�.
    }

    /// <summary>
    /// ZLift�� '��' (LiftWeight Z�� ����, CarriageFrame Z�� ���)�� �̵��ϴ� ���μ����� �����մϴ�.
    /// �迭�� ���ǵ� ���� �Ÿ���ŭ �̵��մϴ�.
    /// </summary>
    public void ActivateZLiftUp()
    {
        // �̹� �ٸ� �������� �����̰ų� ���� �������� �����̰� �ִٸ� ���ο� �̵��� �������� ����
        if (isZLiftUpActive || isZLiftDownActive) return;

        // �� ������ ��� �̵� �ܰ踦 �Ϸ��ߴٸ� �ε����� �缳���մϴ�.
        if (currentUpMoveIndex >= liftWeightMoveDistancesUp.Length)
        {
            currentUpMoveIndex = 0; // ù ��° �̵� �ܰ�� �ٽ� ���ư��ϴ�.
            Debug.Log("ZLiftUp �̵� ����Ŭ �Ϸ�, �ε��� �缳��.");
        }

        isZLiftUpActive = true;
        SetTargetPositionsForUpMovement(); // ���� �ܰ��� ���� ��ǥ ��ġ ����

        // ���� �ڷ�ƾ�� �ִٸ� �����ϰ� ���ο� �ڷ�ƾ ����
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        currentMovementCoroutine = StartCoroutine(MoveZLiftToTarget(true)); // 'Up' �̵��� ���� true ����

        ROT.ActivateZLiftRotationCW();
        CHM.ActiveChainCW();
        CHM1.ActiveChainCW();
        Debug.Log($"ZLiftUp Ȱ��ȭ. LiftWeight ���� Z: {LiftWeight.transform.localPosition.z} ���� {currentLWLocalTargetPosition.z} ��, CarriageFrame ���� Z: {CarriageFrame.transform.localPosition.z} ���� {currentCFLocalTargetPosition.z} �� �̵� ��.");
    }

    /// <summary>
    /// ZLift '��'�� �̵��� ��Ȱ��ȭ�մϴ�.
    /// </summary>
    public void DeactivateZLiftUp()
    {
        if (isZLiftUpActive)
        {
            isZLiftUpActive = false;
            // �̵� ���� �ڷ�ƾ�� �ִٸ� ��� ����
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
                currentMovementCoroutine = null; // ���� �����
            }
            ROT.DeactivateZLiftRotationCW();
            CHM.DeActiveChainCW();
            CHM1.DeActiveChainCW();
            Debug.Log("ZLiftUp ��Ȱ��ȭ. �̵� ����.");
        }
    }

    /// <summary>
    /// ZLift�� '�Ʒ�' (LiftWeight Z�� ���, CarriageFrame Z�� ����)�� �̵��ϴ� ���μ����� �����մϴ�.
    /// �迭�� ���ǵ� ���� �Ÿ���ŭ �̵��մϴ�.
    /// </summary>
    public void ActivateZLiftDown()
    {
        // �̹� �ٸ� �������� �����̰ų� ���� �������� �����̰� �ִٸ� ���ο� �̵��� �������� ����
        if (isZLiftUpActive || isZLiftDownActive) return;

        // �� ������ ��� �̵� �ܰ踦 �Ϸ��ߴٸ� �ε����� �缳���մϴ�.
        if (currentDownMoveIndex >= liftWeightMoveDistancesDown.Length)
        {
            currentDownMoveIndex = 0; // ù ��° �̵� �ܰ�� �ٽ� ���ư��ϴ�.
            Debug.Log("ZLiftDown �̵� ����Ŭ �Ϸ�, �ε��� �缳��.");
        }

        isZLiftDownActive = true;
        SetTargetPositionsForDownMovement(); // ���� �ܰ��� ���� ��ǥ ��ġ ����

        // ���� �ڷ�ƾ�� �ִٸ� �����ϰ� ���ο� �ڷ�ƾ ����
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        currentMovementCoroutine = StartCoroutine(MoveZLiftToTarget(false)); // 'Down' �̵��� ���� false ����

        ROT.ActivateZLiftRotationCCW();
        CHM.ActiveChainCCW();
        CHM1.ActiveChainCCW();
        Debug.Log($"ZLiftDown Ȱ��ȭ. LiftWeight ���� Z: {LiftWeight.transform.localPosition.z} ���� {currentLWLocalTargetPosition.z} ��, CarriageFrame ���� Z: {CarriageFrame.transform.localPosition.z} ���� {currentCFLocalTargetPosition.z} �� �̵� ��.");
    }

    /// <summary>
    /// ZLift '�Ʒ�'�� �̵��� ��Ȱ��ȭ�մϴ�.
    /// </summary>
    public void DeactivateZLiftDown()
    {
        if (isZLiftDownActive)
        {
            isZLiftDownActive = false;
            // �̵� ���� �ڷ�ƾ�� �ִٸ� ��� ����
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
                currentMovementCoroutine = null; // ���� �����
            }
            ROT.DeactivateZLiftRotationCCW();
            CHM.DeActiveChainCCW();
            CHM1.DeActiveChainCCW();
            Debug.Log("ZLiftDown ��Ȱ��ȭ. �̵� ����.");
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

        // �� ������Ʈ �� �ϳ��� ��ǥ�� �������� �ʾҰ�, �̵��� Ȱ��ȭ�Ǿ� �ִٸ� ��� �̵�
        while ((isUpDirection && isZLiftUpActive) || (!isUpDirection && isZLiftDownActive))
        {
            if (LiftWeight != null && !liftWeightReached)
            {
                LiftWeight.transform.localPosition = Vector3.MoveTowards(LiftWeight.transform.localPosition, currentLWLocalTargetPosition, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(LiftWeight.transform.localPosition, currentLWLocalTargetPosition) < 0.01f)
                {
                    LiftWeight.transform.localPosition = currentLWLocalTargetPosition;
                    liftWeightReached = true;
                    Debug.Log($"LiftWeight�� ���� Z: {currentLWLocalTargetPosition.z}�� �����߽��ϴ�.");
                }
            }

            if (CarriageFrame != null && !carriageFrameReached)
            {
                CarriageFrame.transform.localPosition = Vector3.MoveTowards(CarriageFrame.transform.localPosition, currentCFLocalTargetPosition, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(CarriageFrame.transform.localPosition, currentCFLocalTargetPosition) < 0.01f)
                {
                    CarriageFrame.transform.localPosition = currentCFLocalTargetPosition;
                    carriageFrameReached = true;
                    Debug.Log($"CarriageFrame�� ���� Z: {currentCFLocalTargetPosition.z}�� �����߽��ϴ�.");
                }
            }

            // �� ������Ʈ ��� ��ǥ�� �����ߴٸ� �ݺ��� ����
            if (liftWeightReached && carriageFrameReached)
            {
                break;
            }
            yield return null; // ���� �����ӱ��� ���
        }

        // �̵� �Ϸ� �� �ε��� ���� �� �÷��� ��Ȱ��ȭ
        // �� ������Ʈ ��� ��ǥ�� �������� ���� �ε����� ������ŵ�ϴ�.
        if (liftWeightReached && carriageFrameReached)
        {
            if (isUpDirection)
            {
                currentUpMoveIndex++;
            }
            else
            {
                currentDownMoveIndex++;
            }
        }

        // �̵��� �Ϸ�Ǿ����Ƿ� �÷��׿� �ڷ�ƾ ������ �ʱ�ȭ�մϴ�.
        if (isUpDirection)
        {
            isZLiftUpActive = false;
            ROT.DeactivateZLiftRotationCW();
            CHM.DeActiveChainCW();
            CHM1.DeActiveChainCW();
        }
        else
        {
            isZLiftDownActive = false;
            ROT.DeactivateZLiftRotationCCW();
            CHM.DeActiveChainCCW();
            CHM1.DeActiveChainCCW();
        }
        currentMovementCoroutine = null; // �ڷ�ƾ ���� �����
    }

    /// <summary>
    /// 'Up' �̵��� ���� ���� ��ǥ ���� ��ġ�� �����մϴ�.
    /// (LiftWeight: Z�� ���� ����, CarriageFrame: Z�� ��� ����)
    /// </summary>
    private void SetTargetPositionsForUpMovement()
    {
        // LiftWeight�� ��ǥ ���� (���� ���� Z�� moveAmountZ�� ����)
        float lwMoveAmountZ = liftWeightMoveDistancesUp[currentUpMoveIndex];
        currentLWLocalTargetPosition = new Vector3(LiftWeight.transform.localPosition.x, LiftWeight.transform.localPosition.y, LiftWeight.transform.localPosition.z + lwMoveAmountZ);

        // CarriageFrame�� ��ǥ ���� (���� ���� Z�� moveAmountZ�� ����)
        float cfMoveAmountZ = carriageFrameMoveDistancesUp[currentUpMoveIndex];
        currentCFLocalTargetPosition = new Vector3(CarriageFrame.transform.localPosition.x, CarriageFrame.transform.localPosition.y, CarriageFrame.transform.localPosition.z + cfMoveAmountZ);
    }

    /// <summary>
    /// 'Down' �̵��� ���� ���� ��ǥ ���� ��ġ�� �����մϴ�.
    /// (LiftWeight: Z�� ��� ����, CarriageFrame: Z�� ���� ����)
    /// </summary>
    private void SetTargetPositionsForDownMovement()
    {
        // LiftWeight�� ��ǥ ���� (���� ���� Z�� moveAmountZ�� ����)
        float lwMoveAmountZ = liftWeightMoveDistancesDown[currentDownMoveIndex];
        currentLWLocalTargetPosition = new Vector3(LiftWeight.transform.localPosition.x, LiftWeight.transform.localPosition.y, LiftWeight.transform.localPosition.z + lwMoveAmountZ);

        // CarriageFrame�� ��ǥ ���� (���� ���� Z�� moveAmountZ�� ����)
        float cfMoveAmountZ = carriageFrameMoveDistancesDown[currentDownMoveIndex];
        currentCFLocalTargetPosition = new Vector3(CarriageFrame.transform.localPosition.x, CarriageFrame.transform.localPosition.y, CarriageFrame.transform.localPosition.z + cfMoveAmountZ);
    }
}