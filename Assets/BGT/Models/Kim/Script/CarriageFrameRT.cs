using UnityEngine;

public class CarriageFrameRT : MonoBehaviour
{
    // ����Ƽ �����Ϳ��� ������ ȸ�� �ӵ� (�ʴ� ����)
    public float rotationSpeed = 90f; // 90f�� �����ϸ� 2�� �ȿ� 180�� ȸ��

    // ActUtlManager �ν��Ͻ� ���� �߰�
    public ActUtlManager actUtlManager;

    // ȸ�� ���� �÷���
    private bool isZLiftRotationCW = false;
    private bool isZLiftRotationCCW = false;

    // ȸ�� ���� ���� �ʱ� ȸ����
    private Quaternion startRotation;
    // �����ؾ� �� ���� ��ǥ ȸ����
    private Quaternion targetRotation;

    // ���� ȸ���� ���� ������ ��Ÿ���� �÷���
    private bool isRotating = false;

    void Start()
    {
        startRotation = transform.rotation;
    }

    void Update()
    {
        // ȸ���� ���� ���� ���� ȸ�� ������ ����
        if (isRotating)
        {
            // ���� ȸ�������� ��ǥ ȸ�������� 'rotationSpeed' ��ŭ �����Ͽ� �̵�
            // Time.deltaTime�� ���� ������ �ӵ��� �������� ȸ���� ����
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // ȸ���� ��ǥ�� ���� �����ߴ��� Ȯ��
            // Quaternion.Angle�� �� ���ʹϾ� ������ ���� ���̸� ��ȯ
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f) // 0.1�� �̳� ���� ���
            {
                transform.rotation = targetRotation; // ��Ȯ�� ��ǥ ȸ�������� �����Ͽ� ���� ����
                isRotating = false; // ȸ�� �Ϸ� �÷��� false

                // ���� Ȱ��ȭ�� ȸ�� ���⿡ ���� PLC ��ȣ ����
                if (isZLiftRotationCW) // CW ȸ���� �Ϸ��
                {
                    isZLiftRotationCW = false;
                    Debug.Log("CW 180�� ȸ�� �Ϸ�!");
                    // --- �߰��� �κ�: CW ȸ�� �Ϸ� �� X12:0 ��ȣ ���� ---
                    if (actUtlManager != null)
                    {
                        actUtlManager.SendCommandToPlc("X12:1");
                    }
                    // --- �߰��� �κ� �� ---
                }
                else if (isZLiftRotationCCW) // CCW ȸ���� �Ϸ��
                {
                    isZLiftRotationCCW = false;
                    Debug.Log("CCW 180�� ȸ�� �Ϸ�!");
                    // --- �߰��� �κ�: CCW ȸ�� �Ϸ� �� X13:0 ��ȣ ���� ---
                    if (actUtlManager != null)
                    {
                        actUtlManager.SendCommandToPlc("X13:1");
                    }
                    // --- �߰��� �κ� �� ---
                }
            }
        }
    }

    public void ActivateZLiftRotationCW()
    {
        // �̹� ȸ�� ���̶�� ���ο� ȸ�� ��� ����
        if (isRotating) return;

        isZLiftRotationCW = true;
        isZLiftRotationCCW = false; // �ݴ� ���� �÷��״� false
        isRotating = true; // ȸ�� ����

        startRotation = transform.rotation; // ���� ȸ������ ���������� ����
        // ���� ȸ������ Z���� �߽����� 180�� �ð� �������� ȸ���ϴ� ��ǥ ȸ���� ���
        targetRotation = startRotation * Quaternion.Euler(180f, 0, 0);
        Debug.Log("CW 180�� ȸ�� ����!");

        // --- �߰��� �κ�: ȸ�� ���� �� X9:1 ��ȣ ���� ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X12:0"); 
        }
        // --- �߰��� �κ� �� ---
    }

    public void DeactivateZLiftRotationCW()
    {
        // �� ��ũ��Ʈ������ 180�� ȸ���� �Ϸ�Ǹ� �ڵ����� isRotating�� false�� �ǹǷ�
        // �� Deactivate �Լ��� ���������� ȸ���� ���ߴ� ������ ���� �ʽ��ϴ�.
        // ������ �ܺ� �ý��۰��� �ϰ����� ���� PLC ��ȣ ���� ������ �߰��� �� �ֽ��ϴ�.
        if (isRotating) // ���� ȸ�� �߿� �ܺο��� Deactivate�� ȣ��ȴٸ�
        {
            isRotating = false;
            isZLiftRotationCW = false;
            // --- �߰��� �κ�: ���� ��Ȱ��ȭ �� X9:0 ��ȣ ���� ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X12:1");
            }
            // --- �߰��� �κ� �� ---
        }
    }

    public void ActivateZLiftRotationCCW()
    {
        // �̹� ȸ�� ���̶�� ���ο� ȸ�� ��� ����
        if (isRotating) return;

        isZLiftRotationCCW = true;
        isZLiftRotationCW = false; // �ݴ� ���� �÷��״� false
        isRotating = true; // ȸ�� ����

        startRotation = transform.rotation; // ���� ȸ������ ���������� ����
        // ���� ȸ������ Z���� �߽����� -180�� (�ݽð� ����)�� ȸ���ϴ� ��ǥ ȸ���� ���
        targetRotation = startRotation * Quaternion.Euler(-180f, 0, 0);
        Debug.Log("CCW 180�� ȸ�� ����!");

        // --- �߰��� �κ�: ȸ�� ���� �� X9:1 ��ȣ ���� ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X13:0"); 
        }
        // --- �߰��� �κ� �� ---
    }

    public void DeactivateZLiftRotationCCW()
    {
        // �� ��ũ��Ʈ������ 180�� ȸ���� �Ϸ�Ǹ� �ڵ����� isRotating�� false�� �ǹǷ�
        // �� Deactivate �Լ��� ���������� ȸ���� ���ߴ� ������ ���� �ʽ��ϴ�.
        // ������ �ܺ� �ý��۰��� �ϰ����� ���� PLC ��ȣ ���� ������ �߰��� �� �ֽ��ϴ�.
        if (isRotating) // ���� ȸ�� �߿� �ܺο��� Deactivate�� ȣ��ȴٸ�
        {
            isRotating = false;
            isZLiftRotationCCW = false;
            // --- �߰��� �κ�: ���� ��Ȱ��ȭ �� X9:0 ��ȣ ���� ---
            if (actUtlManager != null)
            {
                actUtlManager.SendCommandToPlc("X13:1"); 
            }
            // --- �߰��� �κ� �� ---
        }
    }
}