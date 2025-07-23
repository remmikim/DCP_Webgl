using UnityEngine;
using System.Collections; // �ڷ�ƾ ����� ���� �߰��ؾ� �մϴ�.

public class XGantry : MonoBehaviour
{
    public GameObject XGantryMoving;

    public XGantryRotaion RotaionObject;
    public Chain1 chainIntance;

    // �̵� �ӵ� (�ʴ� �̵� �Ÿ�)
    public float moveSpeed = 0.2f; // �ν����Ϳ��� ���� ������ �� �ֵ��� public���� ����

    // ���� Y�� �̵� �Ÿ��� (��Ÿ ��)
    // ������ �̵� (Y�� ���� ����)�� ���� �̵� �Ÿ��� (��: -3.0f�� ���� ��ġ���� -3.0��ŭ �̵�)
    private float[] moveDistancesRight = {-0.123f,-0.86f+0.123f, -0.96f, -0.48f, -0.96f, -0.48f }; // ������ Y8
    // ���� �̵� (Y�� ��� ����)�� ���� �̵� �Ÿ��� (��: 0.75f�� ���� ��ġ���� +0.75��ŭ �̵�)
    private float[] moveDistancesLeft = {0.86f + 0.123f + 0.05f, 0.26f, 0.96f, 0.48f, 0.96f, 0.48f }; // ���� Y9

    
    // ���� Ȱ�� �̵��� ���� ���� ��ǥ ��ġ (���� ��ǥ)
    private Vector3 currentLocalTargetPosition;

    // �̵� ���� �÷���
    private bool isMovingRight = false;
    private bool isMovingLeft = false;

    // �迭���� ���� �̵� �ܰ踦 �����ϱ� ���� �ε���
    private int currentRightMoveIndex = 0;
    private int currentLeftMoveIndex = 0;

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
    /// '������' (Y ���� ��ġ ����, ���� ����) �̵� ���μ����� �����մϴ�.
    /// moveDistancesRight �迭�� ���ǵ� ���� �Ÿ���ŭ �̵��մϴ�.
    /// </summary>
    public void ActivateXGantryMovingRight()
    {
        // �̹� �ٸ� �������� �����̰ų� ���� �������� �����̰� �ִٸ� ���ο� �̵��� �������� ����
        if (isMovingRight || isMovingLeft) return;

        // �� ������ ��� �̵� �ܰ踦 �Ϸ��ߴٸ� �ε����� �缳���մϴ�.
        if (currentRightMoveIndex >= moveDistancesRight.Length)
        {
            currentRightMoveIndex = 0; // ù ��° �̵� �ܰ�� �ٽ� ���ư��ϴ�.
            Debug.Log("������ �̵� ����Ŭ �Ϸ�, �ε��� �缳��.");
        }

        isMovingRight = true;
        SetTargetPositionForRightMovement(); // ���� �ܰ��� ���� ��ǥ ��ġ ����

        // ���� �ڷ�ƾ�� �ִٸ� �����ϰ� ���ο� �ڷ�ƾ ����
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        currentMovementCoroutine = StartCoroutine(MoveGantryToTarget(true)); // '������' �̵�(Y ����)�� ���� true ����

        // RotaionObject�� chainIntance ����� ���� '������' ���� ������ ���� ����� �� �ֽ��ϴ�.
        RotaionObject.ActivateZLiftRotationCW(); // CW ȸ���� '������'�� ����ȴٰ� ����
        chainIntance.ActiveChainCW(); // ActiveChainCW�� '������'�� ����ȴٰ� ����
        Debug.Log($"XGantry ������ �̵� Ȱ��ȭ. ���� Y: {XGantryMoving.transform.localPosition.y} ���� {currentLocalTargetPosition.y} �� �̵� ��.");
    }

    /// <summary>
    /// '������' �̵��� ��Ȱ��ȭ�մϴ�.
    /// </summary>
    public void DeactivateXGantryMovingRight()
    {
        if (isMovingRight)
        {
            isMovingRight = false;
            // �̵� ���� �ڷ�ƾ�� �ִٸ� ��� ����
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
                currentMovementCoroutine = null; // ���� �����
            }
            RotaionObject.DeactivateZLiftRotationCW(); // DeactivateZLiftRotationCW�� '������'�� ����ȴٰ� ����
            chainIntance.DeActiveChainCW(); // DeActiveChainCW�� '������'�� ����ȴٰ� ����
            Debug.Log("XGantry ������ �̵� ��Ȱ��ȭ. �̵� ����.");
        }
    }

    /// <summary>
    /// '����' (Y ���� ��ġ ����, ��� ����) �̵� ���μ����� �����մϴ�.
    /// moveDistancesLeft �迭�� ���ǵ� ���� �Ÿ���ŭ �̵��մϴ�.
    /// </summary>
    public void ActivateXGantryMovingLeft()
    {
        // �̹� �ٸ� �������� �����̰ų� ���� �������� �����̰� �ִٸ� ���ο� �̵��� �������� ����
        if (isMovingRight || isMovingLeft) return;

        // �� ������ ��� �̵� �ܰ踦 �Ϸ��ߴٸ� �ε����� �缳���մϴ�.
        if (currentLeftMoveIndex >= moveDistancesLeft.Length)
        {
            currentLeftMoveIndex = 0; // ù ��° �̵� �ܰ�� �ٽ� ���ư��ϴ�.
            Debug.Log("���� �̵� ����Ŭ �Ϸ�, �ε��� �缳��.");
        }

        isMovingLeft = true;
        SetTargetPositionForLeftMovement(); // ���� �ܰ��� ���� ��ǥ ��ġ ����

        // ���� �ڷ�ƾ�� �ִٸ� �����ϰ� ���ο� �ڷ�ƾ ����
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        currentMovementCoroutine = StartCoroutine(MoveGantryToTarget(false)); // '����' �̵�(Y ����)�� ���� false ����

        // RotaionObject�� chainIntance ����� ���� '����' ���� ������ ���� ����� �� �ֽ��ϴ�.
        RotaionObject.ActivateZLiftRotationCCW(); // CCW ȸ���� '����'�� ����ȴٰ� ����
        chainIntance.ActiveChainCCW(); // ActiveChainCCW�� '����'�� ����ȴٰ� ����
        Debug.Log($"XGantry ���� �̵� Ȱ��ȭ. ���� Y: {XGantryMoving.transform.localPosition.y} ���� {currentLocalTargetPosition.y} �� �̵� ��.");
    }

    /// <summary>
    /// '����' �̵��� ��Ȱ��ȭ�մϴ�.
    /// </summary>
    public void DeactivateXGantryMovingLeft()
    {
        if (isMovingLeft)
        {
            isMovingLeft = false;
            // �̵� ���� �ڷ�ƾ�� �ִٸ� ��� ����
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
                currentMovementCoroutine = null; // ���� �����
            }
            RotaionObject.DeactivateZLiftRotationCCW(); // DeactivateZLiftRotationCCW�� '����'�� ����ȴٰ� ����
            chainIntance.DeActiveChainCCW(); // DeActiveChainCCW�� '����'�� ����ȴٰ� ����
            Debug.Log("XGantry ���� �̵� ��Ȱ��ȭ. �̵� ����.");
        }
    }

    /// <summary>
    /// ��Ʈ���� ���� ��ǥ ��ġ(���� ��ǥ)�� �̵���Ű�� �ڷ�ƾ�Դϴ�.
    /// </summary>
    /// <param name="isRightDirection">'������' �̵� (Y ����)�̸� true, '����' �̵� (Y ����)�̸� false.</param>
    private IEnumerator MoveGantryToTarget(bool isRightDirection)
    {
        // XGantryMoving ������Ʈ�� �����ϴ��� Ȯ��
        if (XGantryMoving == null)
        {
            Debug.LogError("XGantryMoving�� null�Դϴ�. �̵��� �� �����ϴ�.");
            yield break; // �ڷ�ƾ ����
        }

        // ��ǥ ���� ��ġ�� �����ϰų� �̵��� ��Ȱ��ȭ�� ������ ��� �̵�
        while ((isRightDirection && isMovingRight) || (!isRightDirection && isMovingLeft))
        {
            // ��ǥ ���� ��ġ�� �̵�
            XGantryMoving.transform.localPosition = Vector3.MoveTowards(XGantryMoving.transform.localPosition, currentLocalTargetPosition, moveSpeed * Time.deltaTime);

            // ��ǥ�� �����ߴ��� Ȯ�� (���� ��ġ�� �Ÿ��� ���)
            if (Vector3.Distance(XGantryMoving.transform.localPosition, currentLocalTargetPosition) < 0.01f)
            {
                // �ε� �Ҽ��� ������ ���ϱ� ���� ��Ȯ�� ��ǥ ���� ��ġ�� ����
                XGantryMoving.transform.localPosition = currentLocalTargetPosition;
                Debug.Log($"XGantry�� ���� Y: {currentLocalTargetPosition.y}�� �����߽��ϴ�.");
                break; // ��ǥ�� ���������Ƿ� while ���� ����
            }
            yield return null; // ���� �����ӱ��� ���
        }

        // �̵��� �Ϸ�Ǿ��ų� �������� ��Ȱ��ȭ�Ǿ����ϴ�.
        // �̵��� ���������� �Ϸ�Ǿ��ٸ� ���� �ܰ踦 ���� �ε����� ������ŵ�ϴ�.
        if (Vector3.Distance(XGantryMoving.transform.localPosition, currentLocalTargetPosition) < 0.01f)
        {
            if (isRightDirection)
            {
                currentRightMoveIndex++;
            }
            else
            {
                currentLeftMoveIndex++;
            }
        }

        // �÷��׸� ��Ȱ��ȭ�ϰ� ���� �׼��� �����մϴ�. (���ο� Activate ȣ���� ���� �ʴ� �� �÷��״� false ����)
        if (isRightDirection)
        {
            isMovingRight = false;
            RotaionObject.DeactivateZLiftRotationCW();
            chainIntance.DeActiveChainCW();
        }
        else
        {
            isMovingLeft = false;
            RotaionObject.DeactivateZLiftRotationCCW();
            chainIntance.DeActiveChainCCW();
        }
        currentMovementCoroutine = null; // �ڷ�ƾ ���� �����
    }

    /// <summary>
    /// �迭�� ������� '������' �̵��� ���� ���� ��ǥ ���� ��ġ�� �����մϴ�.
    /// (���� ���� Y�� moveDistancesRight[index] ���� ���� ��ġ)
    /// </summary>
    private void SetTargetPositionForRightMovement()
    {
        float moveAmountY = moveDistancesRight[currentRightMoveIndex]; // �̵��� �Ÿ�(��Ÿ ��)
        // ���� ���� Y�� moveAmountY�� ���ؼ� ���ο� ��ǥ ���� Y�� ����մϴ�.
        currentLocalTargetPosition = new Vector3(XGantryMoving.transform.localPosition.x, XGantryMoving.transform.localPosition.y + moveAmountY, XGantryMoving.transform.localPosition.z);
    }

    /// <summary>
    /// �迭�� ������� '����' �̵��� ���� ���� ��ǥ ���� ��ġ�� �����մϴ�.
    /// (���� ���� Y�� moveDistancesLeft[index] ���� ���� ��ġ)
    /// </summary>
    private void SetTargetPositionForLeftMovement()
    {
        float moveAmountY = moveDistancesLeft[currentLeftMoveIndex]; // �̵��� �Ÿ�(��Ÿ ��)
        // ���� ���� Y�� moveAmountY�� ���ؼ� ���ο� ��ǥ ���� Y�� ����մϴ�.
        currentLocalTargetPosition = new Vector3(XGantryMoving.transform.localPosition.x, XGantryMoving.transform.localPosition.y + moveAmountY, XGantryMoving.transform.localPosition.z);
    }
}