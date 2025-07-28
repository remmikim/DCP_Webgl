using UnityEngine;
using System.Collections; // �ڷ�ƾ�� ������� ������, �ϰ����� ���� �����ϰų� �ʿ� ������ �����ص� �����մϴ�.

public class ForkFrontBackMove : MonoBehaviour
{
    // ����Ƽ �����Ϳ��� �巡�� �� ������� ������ GameObject ������
    public GameObject Second;
    public GameObject Third;

    // ActUtlManager �ν��Ͻ� ���� �߰�
    public ActUtlManager actUtlManager;

    private float MoveSpeed = 0.2f;

    // �� ������ Ȧ���� ���� ��ġ�� ��ǥ ��ġ ������
    private Vector3 SecondStartPosition;
    private Vector3 SecondTargetPosition;
    private Vector3 ThirdStartPosition;
    private Vector3 ThirdTargetPosition;

    private bool isForkMoveFront = false;
    private bool isForkMoveBack = false;

    void Start()
    {
        // Second�� Third�� �ʱ� ��ġ�� �����صδ� ���� �����ϴ�.
        // ���� �ڵ忡���� StartPosition�� Activate �������� �ٽ� �������Ƿ� �� �κ��� �ʼ��� �ƴմϴ�.
    }

    // Update is called once per frame
    void Update()
    {
        // ��ũ ���� ����
        if (isForkMoveFront && !isForkMoveBack)
        {
            // Second ������Ʈ �̵�
            Second.transform.localPosition = Vector3.MoveTowards(Second.transform.localPosition, SecondTargetPosition, MoveSpeed * Time.deltaTime);
            // Third ������Ʈ �̵� (Second���� 2�� ������)
            Third.transform.localPosition = Vector3.MoveTowards(Third.transform.localPosition, ThirdTargetPosition, 2 * MoveSpeed * Time.deltaTime);

            // �� ������Ʈ ��� ��ǥ ��ġ�� �����ߴ��� Ȯ��
            if (Vector3.Distance(Second.transform.localPosition, SecondTargetPosition) < 0.001f &&
                Vector3.Distance(Third.transform.localPosition, ThirdTargetPosition) < 0.001f)
            {
                // ��ǥ�� �����ϸ� �ڵ����� ��Ȱ��ȭ �� PLC ��ȣ ����
                DeactivateFront();
                Debug.Log("��ũ ���� ���� �Ϸ�.");
            }
        }

        // ��ũ ���� ����
        if (isForkMoveBack && !isForkMoveFront)
        {
            // �� PipeHolder�� ��ġ�� ����
            Second.transform.localPosition = Vector3.MoveTowards(Second.transform.localPosition, SecondTargetPosition, MoveSpeed * Time.deltaTime);
            Third.transform.localPosition = Vector3.MoveTowards(Third.transform.localPosition, ThirdTargetPosition, 2 * MoveSpeed * Time.deltaTime);

            // �� ������Ʈ ��� ��ǥ ��ġ�� �����ߴ��� Ȯ��
            if (Vector3.Distance(Second.transform.localPosition, SecondTargetPosition) < 0.001f &&
                Vector3.Distance(Third.transform.localPosition, ThirdTargetPosition) < 0.001f)
            {
                // ��ǥ�� �����ϸ� �ڵ����� ��Ȱ��ȭ �� PLC ��ȣ ����
                DeactivateBack();
                Debug.Log("��ũ ���� ���� �Ϸ�.");
            }
        }
    }

    public void ActivateFront()
    {
        if (isForkMoveFront || isForkMoveBack) return; // �̹� �����̰� �ִٸ� �ߺ� ���� ����
        isForkMoveFront = true;

        // ��ǥ ��ġ ����
        SecondStartPosition = Second.transform.localPosition;
        SecondTargetPosition = SecondStartPosition + new Vector3(0.9f, 0, 0);
        ThirdStartPosition = Third.transform.localPosition;
        ThirdTargetPosition = ThirdStartPosition + new Vector3(1.8f, 0, 0);

        // --- �߰��� �κ�: ������ ���� �� X8:1 ��ȣ ���� ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X10:0");
        }
        // --- �߰��� �κ� �� ---
    }

    public void DeactivateFront()
    {
        if (!isForkMoveFront) return; // �̹� ��Ȱ��ȭ�Ǿ� �ִٸ� �ߺ� ���� ����
        isForkMoveFront = false;

        // --- �߰��� �κ�: ���� �Ǵ� �ڵ� �Ϸ� �� X8:0 ��ȣ ���� ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X10:1");
        }
        // --- �߰��� �κ� �� ---
    }

    public void ActivateBack()
    {
        if (isForkMoveFront || isForkMoveBack) return; // �̹� �����̰� �ִٸ� �ߺ� ���� ����
        isForkMoveBack = true;

        // ��ǥ ��ġ ����
        SecondStartPosition = Second.transform.localPosition;
        SecondTargetPosition = SecondStartPosition + new Vector3(-0.9f, 0, 0);
        ThirdStartPosition = Third.transform.localPosition;
        ThirdTargetPosition = ThirdStartPosition + new Vector3(-1.8f, 0, 0);

        // --- �߰��� �κ�: ������ ���� �� X8:1 ��ȣ ���� ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X11:0"); 
        }
        // --- �߰��� �κ� �� ---
    }

    public void DeactivateBack()
    {
        if (!isForkMoveBack) return; // �̹� ��Ȱ��ȭ�Ǿ� �ִٸ� �ߺ� ���� ����
        isForkMoveBack = false;

        // --- �߰��� �κ�: ���� �Ǵ� �ڵ� �Ϸ� �� X8:0 ��ȣ ���� ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X11:1");
        }
        // --- �߰��� �κ� �� ---
    }
}