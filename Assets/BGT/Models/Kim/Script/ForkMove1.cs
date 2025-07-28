using UnityEngine;

public class ForkMove1 : MonoBehaviour
{
    // ActUtlManager �ν��Ͻ� ���� �߰�
    public ActUtlManager actUtlManager;

    private float MoveSpeed = 0.2f;
    private float MoveAmountY = 0.272f;

    // �� ������ Ȧ���� ���� ��ġ�� ��ǥ ��ġ ������
    private Vector3 StartPosition;   // PipeHolder1
    private Vector3 TargetPosition;  // PipeHolder1

    private bool isForkMoveRigt = false;
    private bool isForkMoveLeft = false;

    void Start()
    {
        // StartPosition�� ���� ��ġ���� �ʱ�ȭ�ǹǷ� Start������ Ư���� �� ���� �����ϴ�.
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
            // �� PipeHolder�� ��ġ�� ����
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
        if (isForkMoveRigt || isForkMoveLeft) return; // �̹� �����̰� �ִٸ� ����

        isForkMoveRigt = true;
        isForkMoveLeft = false; // �ٸ� ���� �÷��״� �׻� false�� ����

        StartPosition = transform.localPosition;
        TargetPosition = StartPosition + new Vector3(0, MoveAmountY, 0); // Y�� ��� ���� �̵� (Right)

        // --- �߰��� �κ�: ������ �̵� ���� �� X10:1 ��ȣ ���� ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X14:0"); // ��ũ ������ ���� ������ PLC�� �˸� (ON)
            Debug.Log("ForkMove1: PLC�� X10:1 (������ �̵� ����) ��� ����.");
        }
        // --- �߰��� �κ� �� ---

        Debug.Log($"Fork Right move activated. Local Y: {transform.localPosition.y} to {TargetPosition.y}");
    }

    public void DeactivateRight()
    {
        if (isForkMoveRigt) // ������ �̵� ���� ���� ��Ȱ��ȭ
        {
            isForkMoveRigt = false;

            // --- �߰��� �κ�: ������ �̵� ��Ȱ��ȭ �� X10:0 ��ȣ ���� ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X14:1"); // ��ũ ������ ���� ������ PLC�� �˸� (OFF)
                Debug.Log("ForkMove1: PLC�� X10:0 (������ �̵� ��Ȱ��ȭ) ��� ����.");
            }
            // --- �߰��� �κ� �� ---

            Debug.Log("Fork Right move deactivated.");
        }
    }

    public void ActivateLeft()
    {
        if (isForkMoveRigt || isForkMoveLeft) return; // �̹� �����̰� �ִٸ� ����

        isForkMoveLeft = true;
        isForkMoveRigt = false; // �ٸ� ���� �÷��״� �׻� false�� ����

        StartPosition = transform.localPosition;
        TargetPosition = StartPosition + new Vector3(0, -MoveAmountY, 0); // Y�� ���� ���� �̵� (Left)

        // --- �߰��� �κ�: ���� �̵� ���� �� X11:1 ��ȣ ���� ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X15:0"); // ��ũ ���� ���� ������ PLC�� �˸� (ON)
            Debug.Log("ForkMove1: PLC�� X11:1 (���� �̵� ����) ��� ����.");
        }
        // --- �߰��� �κ� �� ---

        Debug.Log($"Fork Left move activated. Local Y: {transform.localPosition.y} to {TargetPosition.y}");
    }

    public void DeactivateLeft()
    {
        if (isForkMoveLeft) // ���� �̵� ���� ���� ��Ȱ��ȭ
        {
            isForkMoveLeft = false;

            // --- �߰��� �κ�: ���� �̵� ��Ȱ��ȭ �� X11:0 ��ȣ ���� ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X15:1"); // ��ũ ���� ���� ������ PLC�� �˸� (OFF)
                Debug.Log("ForkMove1: PLC�� X11:0 (���� �̵� ��Ȱ��ȭ) ��� ����.");
            }
            // --- �߰��� �κ� �� ---

            Debug.Log("Fork Left move deactivated.");
        }
    }
}