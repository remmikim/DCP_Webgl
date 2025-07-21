using UnityEngine;

public class CarriageFrameRT : MonoBehaviour
{
    // ����Ƽ �����Ϳ��� ������ ȸ�� �ӵ� (�ʴ� ����)
    public float rotationSpeed = 90f; // 90f�� �����ϸ� 2�� �ȿ� 180�� ȸ��

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
                isZLiftRotationCW = false; // CW �÷��� false
                isZLiftRotationCCW = false; // CCW �÷��� false
                Debug.Log("180�� ȸ�� �Ϸ�!");
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
        targetRotation = startRotation * Quaternion.Euler(0, 0, 180f);
        Debug.Log("CW 180�� ȸ�� ����!");
    }

    public void DeactivateZLiftRotationCW()
    {
        // �� ��ũ��Ʈ������ 180�� ȸ���� �Ϸ�Ǹ� �ڵ����� isRotating�� false�� �ǹǷ�
        // �� Deactivate �Լ��� ���������� ȸ���� ���ߴ� ������ ���� �ʽ��ϴ�.
        // ������ �ܺ� �ý��۰��� �ϰ����� ���� ������ �� �ֽ��ϴ�.
        // isZLiftRotationCW = false; // ȸ���� �Ϸ�Ǹ� Update���� �ڵ����� false�� ������
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
        targetRotation = startRotation * Quaternion.Euler(0, 0, -180f);
        Debug.Log("CCW 180�� ȸ�� ����!");
    }

    public void DeactivateZLiftRotationCCW()
    {
        // �� ��ũ��Ʈ������ 180�� ȸ���� �Ϸ�Ǹ� �ڵ����� isRotating�� false�� �ǹǷ�
        // �� Deactivate �Լ��� ���������� ȸ���� ���ߴ� ������ ���� �ʽ��ϴ�.
        // ������ �ܺ� �ý��۰��� �ϰ����� ���� ������ �� �ֽ��ϴ�.
        // isZLiftRotationCCW = false; // ȸ���� �Ϸ�Ǹ� Update���� �ڵ����� false�� ������
    }
}