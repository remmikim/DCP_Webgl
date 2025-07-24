using UnityEngine;
using System.Collections; // �ڷ�ƾ ����� ���� �߰�

public class PipeHolders : MonoBehaviour
{
    // ����Ƽ �����Ϳ��� �巡�� �� ������� ������ GameObject ������
    public GameObject PipeHolder1;
    public GameObject PipeHolder2;
    public GameObject PipeHolder3;
    public GameObject PipeHolder4;

    public Screw screwControl;

    private float MoveSpeed = 0.2f; // public���� �����Ͽ� �����Ϳ��� ���� ����
    private float MoveAmount1and2 = 1.15f;
    private float MoveAmount3and4 = 1.24f;

    // �� ������ Ȧ���� ���� ��ġ�� ��ǥ ��ġ ������
    private Vector3 PH1StartPosition;    // PipeHolder1
    private Vector3 PH1TargetPosition;   // PipeHolder1 (ù ��° ��ǥ)
    private Vector3 PH1TargetPosition2;  // PipeHolder1 (�� ��° ��ǥ)
    private Vector3 PH1TargetPosition3;  // PipeHolder1 (�� ��° ��ǥ)

    private Vector3 PH2StartPosition;    // PipeHolder2
    private Vector3 PH2TargetPosition;   // PipeHolder2 (ù ��° ��ǥ)
    private Vector3 PH2TargetPosition2;  // PipeHolder2 (�� ��° ��ǥ)
    private Vector3 PH2TargetPosition3;  // PipeHolder2 (�� ��° ��ǥ)

    private Vector3 PH3StartPosition;    // PipeHolder3
    private Vector3 PH3TargetPosition;   // PipeHolder3

    private Vector3 PH4StartPosition;    // PipeHolder4
    private Vector4 PH4TargetPosition;   // PipeHolder4

    private bool isPipeHoldersCW = false;
    private bool isPipeHoldersCCW = false;

    // ���� ���� ���� �ڷ�ƾ�� ������ ���� (���� �ڷ�ƾ�� ������Ű�� ����)
    private Coroutine currentMovementCoroutine;

    //void Start()
    //{
    //    // Rigidbody ������ Start()���� �� ���� ȣ���ϸ� �˴ϴ�.
    //    SetRigidbodyKinematic(PipeHolder1);
    //    SetRigidbodyKinematic(PipeHolder2);
    //    SetRigidbodyKinematic(PipeHolder3);
    //    SetRigidbodyKinematic(PipeHolder4);
    //}

    //// Rigidbody�� Kinematic���� �����ϴ� ���� �Լ�
    //private void SetRigidbodyKinematic(GameObject obj)
    //{
    //    if (obj != null && obj.GetComponent<Rigidbody>() != null)
    //    {
    //        obj.GetComponent<Rigidbody>().isKinematic = true;
    //    }
    //}

    // Update�� �� �̻� �������� �̵� ������ �������� �ʽ��ϴ�.
    // ���� �̵��� �ڷ�ƾ�� ó���մϴ�.
    void Update()
    {
        // Update �Լ��� ����Ӵϴ�. ��� �̵��� �ڷ�ƾ���� ó���˴ϴ�.
    }

    /// <summary>
    /// PipeHolders�� CW �������� �̵���ŵ�ϴ�.
    /// PipeHolder1,2�� �� �ܰ��, PipeHolder3,4�� �� �ܰ�� �̵��մϴ�.
    /// </summary>
    public void ActivatePipeHoldersCW()
    {
        // �̹� �ٸ� �̵� ���̶�� ����
        if (isPipeHoldersCW || isPipeHoldersCCW) return;

        isPipeHoldersCW = true;
        screwControl.ActivateScrewCW();

        // ���� ��ġ ���� (���� ��ġ)
        PH1StartPosition = PipeHolder1.transform.localPosition;
        PH2StartPosition = PipeHolder2.transform.localPosition;
        PH3StartPosition = PipeHolder3.transform.localPosition;
        PH4StartPosition = PipeHolder4.transform.localPosition;

        // ù ��° ��ǥ ��ġ ����
        PH1TargetPosition = PH1StartPosition + new Vector3(0, MoveAmount1and2, 0);
        PH2TargetPosition = PH2StartPosition + new Vector3(0, -MoveAmount1and2, 0);
        PH3TargetPosition = PH3StartPosition + new Vector3(MoveAmount3and4, 0, 0);
        PH4TargetPosition = PH4StartPosition + new Vector3(-MoveAmount3and4, 0, 0);

        // �� ��° ��ǥ ��ġ ���� (PH1, PH2�� �ش�)
        PH1TargetPosition2 = PH1TargetPosition + new Vector3(0, -MoveAmount1and2, 0);
        PH2TargetPosition2 = PH2TargetPosition + new Vector3(0, -MoveAmount1and2, 0);

        // ���� �ڷ�ƾ�� �ִٸ� ����
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        // ���ο� �ڷ�ƾ ����
        currentMovementCoroutine = StartCoroutine(MovePipeHoldersCWCoroutine());
    }

    /// <summary>
    /// CW ���� �̵��� ó���ϴ� �ڷ�ƾ�Դϴ�.
    /// </summary>
    private IEnumerator MovePipeHoldersCWCoroutine()
    {
        // 1�ܰ� �̵� (��� ������ Ȧ�� ���� �̵�)
        yield return StartCoroutine(MoveAllHoldersToFirstTarget(
            PH1TargetPosition, PH2TargetPosition, PH3TargetPosition, PH4TargetPosition
        ));

        // 1�ܰ� �̵� �Ϸ� �� �߰� ���� (��: ��� ���)
        yield return new WaitForSeconds(0.2f); // 0.5�� ��� (���� ����)

        // 2�ܰ� �̵� (PipeHolder1, 2�� �ش�)
        yield return StartCoroutine(MoveSpecificHoldersToSecondTarget(
            PipeHolder1, PH1TargetPosition2,
            PipeHolder2, PH2TargetPosition2
        ));

        // ��� �̵� �Ϸ� �� ���� �ʱ�ȭ
        isPipeHoldersCW = false;
        screwControl.DeactivateScrewCW(); // ��� �������� ������ ��ũ�� ���� ��Ȱ��ȭ
    }

    /// <summary>
    /// ��� ������ Ȧ���� ù ��° ��ǥ ��ġ�� �̵���Ű�� �ڷ�ƾ�Դϴ�.
    /// </summary>
    private IEnumerator MoveAllHoldersToFirstTarget(Vector3 target1, Vector3 target2, Vector3 target3, Vector3 target4)
    {
        bool allReached = false;
        while (!allReached)
        {
            // �� PipeHolder�� ��ġ�� ��ǥ�� �̵�
            PipeHolder1.transform.localPosition = Vector3.MoveTowards(PipeHolder1.transform.localPosition, target1, MoveSpeed * Time.deltaTime);
            PipeHolder2.transform.localPosition = Vector3.MoveTowards(PipeHolder2.transform.localPosition, target2, MoveSpeed * Time.deltaTime);
            PipeHolder3.transform.localPosition = Vector3.MoveTowards(PipeHolder3.transform.localPosition, target3, MoveSpeed * Time.deltaTime);
            PipeHolder4.transform.localPosition = Vector3.MoveTowards(PipeHolder4.transform.localPosition, target4, MoveSpeed * Time.deltaTime);

            // ��� ������Ʈ�� ��ǥ�� �����ߴ��� Ȯ��
            bool ph1Reached = (PipeHolder1 == null || Vector3.Distance(PipeHolder1.transform.localPosition, target1) < 0.01f);
            bool ph2Reached = (PipeHolder2 == null || Vector3.Distance(PipeHolder2.transform.localPosition, target2) < 0.01f);
            bool ph3Reached = (PipeHolder3 == null || Vector3.Distance(PipeHolder3.transform.localPosition, target3) < 0.01f);
            bool ph4Reached = (PipeHolder4 == null || Vector3.Distance(PipeHolder4.transform.localPosition, target4) < 0.01f);

            allReached = ph1Reached && ph2Reached && ph3Reached && ph4Reached;

            yield return null; // ���� �����ӱ��� ���
        }
        // ��Ȯ�� ��ǥ ��ġ�� ���� (���� ����)
        PipeHolder1.transform.localPosition = target1;
        PipeHolder2.transform.localPosition = target2;
        PipeHolder3.transform.localPosition = target3;
        PipeHolder4.transform.localPosition = target4;
    }

    /// <summary>
    /// Ư�� ������ Ȧ��(PipeHolder1, 2)�� �� ��° ��ǥ ��ġ�� �̵���Ű�� �ڷ�ƾ�Դϴ�.
    /// </summary>
    private IEnumerator MoveSpecificHoldersToSecondTarget(GameObject ph1, Vector3 target1, GameObject ph2, Vector3 target2)
    {
        bool allReached = false;
        while (!allReached)
        {
            ph1.transform.localPosition = Vector3.MoveTowards(ph1.transform.localPosition, target1, MoveSpeed * Time.deltaTime);
            ph2.transform.localPosition = Vector3.MoveTowards(ph2.transform.localPosition, target2, MoveSpeed * Time.deltaTime);

            bool ph1Reached = (ph1 == null || Vector3.Distance(ph1.transform.localPosition, target1) < 0.01f);
            bool ph2Reached = (ph2 == null || Vector3.Distance(ph2.transform.localPosition, target2) < 0.01f);

            allReached = ph1Reached && ph2Reached;

            yield return null; // ���� �����ӱ��� ���
        }
        // ��Ȯ�� ��ǥ ��ġ�� ����
        ph1.transform.localPosition = target1;
        ph2.transform.localPosition = target2;
    }
    /// <summary>
    /// CW �̵� �� ���� ����
    /// </summary>
    public void DeactivatePipeHoldersCW()
    {
        if (isPipeHoldersCW)
        {
            isPipeHoldersCW = false;
            // �ڷ�ƾ�� ���� ���̶�� ����
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
            }
            screwControl.DeactivateScrewCW();
        }
    }

    /// <summary>
    /// PipeHolders�� CCW �������� �̵���ŵ�ϴ�.
    /// �� �κ��� CW�� �����ϰ� 2�ܰ� �̵��� ������ �� �ֽ��ϴ�.
    /// </summary>
    public void ActivatePipeHoldersCCW()
    {
        if (isPipeHoldersCW || isPipeHoldersCCW) return;
        isPipeHoldersCCW = true;
        screwControl.ActivateScrewCCW();

        // ���� ��ġ ����
        PH1StartPosition = PipeHolder1.transform.localPosition;
        PH2StartPosition = PipeHolder2.transform.localPosition;
        PH3StartPosition = PipeHolder3.transform.localPosition;
        PH4StartPosition = PipeHolder4.transform.localPosition;

        // CCW ù ��° ��ǥ ��ġ ����
        PH1TargetPosition3 = PH1StartPosition + new Vector3(0, MoveAmount1and2, 0);
        PH2TargetPosition3 = PH2StartPosition + new Vector3(0, MoveAmount1and2, 0);
        // CW�� �ݴ� �������� �̵��Ѵٰ� ����

        PH1TargetPosition = PH1TargetPosition3 + new Vector3(0, -MoveAmount1and2, 0);
        PH2TargetPosition = PH2TargetPosition3 + new Vector3(0, MoveAmount1and2, 0);
        PH3TargetPosition = PH3StartPosition + new Vector3(-MoveAmount3and4, 0, 0);
        PH4TargetPosition = PH4StartPosition + new Vector3(MoveAmount3and4, 0, 0);


        // ���� �ڷ�ƾ�� �ִٸ� ����
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        // CCW �̵� �ڷ�ƾ ���� (����� 1�ܰ踸 ������)
        currentMovementCoroutine = StartCoroutine(MovePipeHoldersCCWCoroutine());
    }

    /// <summary>
    /// CCW ���� �̵��� ó���ϴ� �ڷ�ƾ�Դϴ�.
    /// (����� ��� Ȧ���� ù ��° ��ǥ�� �� �ܰ踸 �̵��ϵ��� �����Ǿ� �ֽ��ϴ�)
    /// </summary>
    private IEnumerator MovePipeHoldersCCWCoroutine()
    {
        yield return StartCoroutine(MoveSpecificHoldersToSecondTarget(
            PipeHolder1, PH1TargetPosition3,
            PipeHolder2, PH2TargetPosition3
        ));
        // ��� ������ Ȧ���� ù ��° ��ǥ ��ġ�� �̵�
        yield return StartCoroutine(MoveAllHoldersToFirstTarget(
            PH1TargetPosition, PH2TargetPosition, PH3TargetPosition, PH4TargetPosition
        ));
        // 2�ܰ� �̵� (PipeHolder1, 2�� �ش�)
        // CCW �̵� �Ϸ� �� ���� �ʱ�ȭ
        isPipeHoldersCCW = false;
        screwControl.DeactivateScrewCCW();
    }

    /// <summary>
    /// CCW �̵� �� ���� ����
    /// </summary>
    public void DeactivatePipeHoldersCCW()
    {
        if (isPipeHoldersCCW)
        {
            isPipeHoldersCCW = false;
            // �ڷ�ƾ�� ���� ���̶�� ����
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
            }
            screwControl.DeactivateScrewCCW();
        }
    }
}