using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 추가

public class PipeHolders : MonoBehaviour
{
    // 유니티 에디터에서 드래그 앤 드롭으로 연결할 GameObject 변수들
    public GameObject PipeHolder1;
    public GameObject PipeHolder2;
    public GameObject PipeHolder3;
    public GameObject PipeHolder4;

    public Screw screwControl;

    private float MoveSpeed = 0.2f; // public으로 변경하여 에디터에서 조절 가능
    private float MoveAmount1and2 = 1.15f;
    private float MoveAmount3and4 = 1.24f;

    // 각 파이프 홀더의 시작 위치와 목표 위치 변수들
    private Vector3 PH1StartPosition;    // PipeHolder1
    private Vector3 PH1TargetPosition;   // PipeHolder1 (첫 번째 목표)
    private Vector3 PH1TargetPosition2;  // PipeHolder1 (두 번째 목표)
    private Vector3 PH1TargetPosition3;  // PipeHolder1 (세 번째 목표)

    private Vector3 PH2StartPosition;    // PipeHolder2
    private Vector3 PH2TargetPosition;   // PipeHolder2 (첫 번째 목표)
    private Vector3 PH2TargetPosition2;  // PipeHolder2 (두 번째 목표)
    private Vector3 PH2TargetPosition3;  // PipeHolder2 (두 번째 목표)

    private Vector3 PH3StartPosition;    // PipeHolder3
    private Vector3 PH3TargetPosition;   // PipeHolder3

    private Vector3 PH4StartPosition;    // PipeHolder4
    private Vector4 PH4TargetPosition;   // PipeHolder4

    private bool isPipeHoldersCW = false;
    private bool isPipeHoldersCCW = false;

    // 현재 실행 중인 코루틴을 저장할 변수 (이전 코루틴을 중지시키기 위함)
    private Coroutine currentMovementCoroutine;

    //void Start()
    //{
    //    // Rigidbody 설정은 Start()에서 한 번만 호출하면 됩니다.
    //    SetRigidbodyKinematic(PipeHolder1);
    //    SetRigidbodyKinematic(PipeHolder2);
    //    SetRigidbodyKinematic(PipeHolder3);
    //    SetRigidbodyKinematic(PipeHolder4);
    //}

    //// Rigidbody를 Kinematic으로 설정하는 헬퍼 함수
    //private void SetRigidbodyKinematic(GameObject obj)
    //{
    //    if (obj != null && obj.GetComponent<Rigidbody>() != null)
    //    {
    //        obj.GetComponent<Rigidbody>().isKinematic = true;
    //    }
    //}

    // Update는 더 이상 직접적인 이동 로직을 포함하지 않습니다.
    // 이제 이동은 코루틴이 처리합니다.
    void Update()
    {
        // Update 함수는 비워둡니다. 모든 이동은 코루틴에서 처리됩니다.
    }

    /// <summary>
    /// PipeHolders를 CW 방향으로 이동시킵니다.
    /// PipeHolder1,2는 두 단계로, PipeHolder3,4는 한 단계로 이동합니다.
    /// </summary>
    public void ActivatePipeHoldersCW()
    {
        // 이미 다른 이동 중이라면 리턴
        if (isPipeHoldersCW || isPipeHoldersCCW) return;

        isPipeHoldersCW = true;
        screwControl.ActivateScrewCW();

        // 시작 위치 저장 (로컬 위치)
        PH1StartPosition = PipeHolder1.transform.localPosition;
        PH2StartPosition = PipeHolder2.transform.localPosition;
        PH3StartPosition = PipeHolder3.transform.localPosition;
        PH4StartPosition = PipeHolder4.transform.localPosition;

        // 첫 번째 목표 위치 설정
        PH1TargetPosition = PH1StartPosition + new Vector3(0, MoveAmount1and2, 0);
        PH2TargetPosition = PH2StartPosition + new Vector3(0, -MoveAmount1and2, 0);
        PH3TargetPosition = PH3StartPosition + new Vector3(MoveAmount3and4, 0, 0);
        PH4TargetPosition = PH4StartPosition + new Vector3(-MoveAmount3and4, 0, 0);

        // 두 번째 목표 위치 설정 (PH1, PH2만 해당)
        PH1TargetPosition2 = PH1TargetPosition + new Vector3(0, -MoveAmount1and2, 0);
        PH2TargetPosition2 = PH2TargetPosition + new Vector3(0, -MoveAmount1and2, 0);

        // 기존 코루틴이 있다면 중지
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        // 새로운 코루틴 시작
        currentMovementCoroutine = StartCoroutine(MovePipeHoldersCWCoroutine());
    }

    /// <summary>
    /// CW 방향 이동을 처리하는 코루틴입니다.
    /// </summary>
    private IEnumerator MovePipeHoldersCWCoroutine()
    {
        // 1단계 이동 (모든 파이프 홀더 동시 이동)
        yield return StartCoroutine(MoveAllHoldersToFirstTarget(
            PH1TargetPosition, PH2TargetPosition, PH3TargetPosition, PH4TargetPosition
        ));

        // 1단계 이동 완료 후 추가 동작 (예: 잠시 대기)
        yield return new WaitForSeconds(0.2f); // 0.5초 대기 (선택 사항)

        // 2단계 이동 (PipeHolder1, 2만 해당)
        yield return StartCoroutine(MoveSpecificHoldersToSecondTarget(
            PipeHolder1, PH1TargetPosition2,
            PipeHolder2, PH2TargetPosition2
        ));

        // 모든 이동 완료 후 상태 초기화
        isPipeHoldersCW = false;
        screwControl.DeactivateScrewCW(); // 모든 움직임이 끝나면 스크류 제어 비활성화
    }

    /// <summary>
    /// 모든 파이프 홀더를 첫 번째 목표 위치로 이동시키는 코루틴입니다.
    /// </summary>
    private IEnumerator MoveAllHoldersToFirstTarget(Vector3 target1, Vector3 target2, Vector3 target3, Vector3 target4)
    {
        bool allReached = false;
        while (!allReached)
        {
            // 각 PipeHolder의 위치를 목표로 이동
            PipeHolder1.transform.localPosition = Vector3.MoveTowards(PipeHolder1.transform.localPosition, target1, MoveSpeed * Time.deltaTime);
            PipeHolder2.transform.localPosition = Vector3.MoveTowards(PipeHolder2.transform.localPosition, target2, MoveSpeed * Time.deltaTime);
            PipeHolder3.transform.localPosition = Vector3.MoveTowards(PipeHolder3.transform.localPosition, target3, MoveSpeed * Time.deltaTime);
            PipeHolder4.transform.localPosition = Vector3.MoveTowards(PipeHolder4.transform.localPosition, target4, MoveSpeed * Time.deltaTime);

            // 모든 오브젝트가 목표에 도달했는지 확인
            bool ph1Reached = (PipeHolder1 == null || Vector3.Distance(PipeHolder1.transform.localPosition, target1) < 0.01f);
            bool ph2Reached = (PipeHolder2 == null || Vector3.Distance(PipeHolder2.transform.localPosition, target2) < 0.01f);
            bool ph3Reached = (PipeHolder3 == null || Vector3.Distance(PipeHolder3.transform.localPosition, target3) < 0.01f);
            bool ph4Reached = (PipeHolder4 == null || Vector3.Distance(PipeHolder4.transform.localPosition, target4) < 0.01f);

            allReached = ph1Reached && ph2Reached && ph3Reached && ph4Reached;

            yield return null; // 다음 프레임까지 대기
        }
        // 정확한 목표 위치로 스냅 (오차 보정)
        PipeHolder1.transform.localPosition = target1;
        PipeHolder2.transform.localPosition = target2;
        PipeHolder3.transform.localPosition = target3;
        PipeHolder4.transform.localPosition = target4;
    }

    /// <summary>
    /// 특정 파이프 홀더(PipeHolder1, 2)를 두 번째 목표 위치로 이동시키는 코루틴입니다.
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

            yield return null; // 다음 프레임까지 대기
        }
        // 정확한 목표 위치로 스냅
        ph1.transform.localPosition = target1;
        ph2.transform.localPosition = target2;
    }
    /// <summary>
    /// CW 이동 중 강제 중지
    /// </summary>
    public void DeactivatePipeHoldersCW()
    {
        if (isPipeHoldersCW)
        {
            isPipeHoldersCW = false;
            // 코루틴이 실행 중이라면 중지
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
            }
            screwControl.DeactivateScrewCW();
        }
    }

    /// <summary>
    /// PipeHolders를 CCW 방향으로 이동시킵니다.
    /// 이 부분은 CW와 유사하게 2단계 이동을 구현할 수 있습니다.
    /// </summary>
    public void ActivatePipeHoldersCCW()
    {
        if (isPipeHoldersCW || isPipeHoldersCCW) return;
        isPipeHoldersCCW = true;
        screwControl.ActivateScrewCCW();

        // 시작 위치 저장
        PH1StartPosition = PipeHolder1.transform.localPosition;
        PH2StartPosition = PipeHolder2.transform.localPosition;
        PH3StartPosition = PipeHolder3.transform.localPosition;
        PH4StartPosition = PipeHolder4.transform.localPosition;

        // CCW 첫 번째 목표 위치 설정
        PH1TargetPosition3 = PH1StartPosition + new Vector3(0, MoveAmount1and2, 0);
        PH2TargetPosition3 = PH2StartPosition + new Vector3(0, MoveAmount1and2, 0);
        // CW와 반대 방향으로 이동한다고 가정

        PH1TargetPosition = PH1TargetPosition3 + new Vector3(0, -MoveAmount1and2, 0);
        PH2TargetPosition = PH2TargetPosition3 + new Vector3(0, MoveAmount1and2, 0);
        PH3TargetPosition = PH3StartPosition + new Vector3(-MoveAmount3and4, 0, 0);
        PH4TargetPosition = PH4StartPosition + new Vector3(MoveAmount3and4, 0, 0);


        // 기존 코루틴이 있다면 중지
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        // CCW 이동 코루틴 시작 (현재는 1단계만 구현됨)
        currentMovementCoroutine = StartCoroutine(MovePipeHoldersCCWCoroutine());
    }

    /// <summary>
    /// CCW 방향 이동을 처리하는 코루틴입니다.
    /// (현재는 모든 홀더가 첫 번째 목표로 한 단계만 이동하도록 구현되어 있습니다)
    /// </summary>
    private IEnumerator MovePipeHoldersCCWCoroutine()
    {
        yield return StartCoroutine(MoveSpecificHoldersToSecondTarget(
            PipeHolder1, PH1TargetPosition3,
            PipeHolder2, PH2TargetPosition3
        ));
        // 모든 파이프 홀더를 첫 번째 목표 위치로 이동
        yield return StartCoroutine(MoveAllHoldersToFirstTarget(
            PH1TargetPosition, PH2TargetPosition, PH3TargetPosition, PH4TargetPosition
        ));
        // 2단계 이동 (PipeHolder1, 2만 해당)
        // CCW 이동 완료 후 상태 초기화
        isPipeHoldersCCW = false;
        screwControl.DeactivateScrewCCW();
    }

    /// <summary>
    /// CCW 이동 중 강제 중지
    /// </summary>
    public void DeactivatePipeHoldersCCW()
    {
        if (isPipeHoldersCCW)
        {
            isPipeHoldersCCW = false;
            // 코루틴이 실행 중이라면 중지
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
            }
            screwControl.DeactivateScrewCCW();
        }
    }
}