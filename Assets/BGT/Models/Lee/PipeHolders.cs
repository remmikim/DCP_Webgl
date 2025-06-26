using UnityEngine;

public class PipeHolders : MonoBehaviour
{
    // 유니티 에디터에서 드래그 앤 드롭으로 연결할 GameObject 변수들
    public GameObject PipeHolder1;
    public GameObject PipeHolder2;
    public GameObject PipeHolder3;
    public GameObject PipeHolder4;

    public Screw screwControl;

    private float MoveTime = 5f; // 모든 파이프 홀더가 이동하는 데 걸리는 시간 (초)

    // 각 파이프 홀더가 Z축으로 이동할 양 (음수 값은 Z축 마이너스 방향으로 이동)
    private float MoveAmount1 = 1.0f;
    private float MoveAmount2 = 1.0f;
    private float MoveAmount3 = 1.0f;
    private float MoveAmount4 = 1.0f;

    // 각 파이프 홀더의 시작 위치와 목표 위치 변수들
    private Vector3 PH1StartPosition;   // PipeHolder1
    private Vector3 PH1TargetPosition;  // PipeHolder1

    private Vector3 PH2StartPosition;   // PipeHolder2
    private Vector3 PH2TargetPosition;  // PipeHolder2

    private Vector3 PH3StartPosition;   // PipeHolder3
    private Vector3 PH3TargetPosition;  // PipeHolder3

    private Vector3 PH4StartPosition;   // PipeHolder4
    private Vector3 PH4TargetPosition;  // PipeHolder4

    private float elapsedTime = 0f;
    private bool isMoving = true; // 이동이 진행 중인지 여부

    private bool isPipeHoldersCW = false;
    private bool isPipeHoldersCCW = false;

    void Start()
    {
        // 각 PipeHolder GameObject의 현재 위치를 시작 위치로 설정
        // 그리고 그 위치에서 MoveAmount만큼 더한 위치를 목표 위치로 설정

        // PipeHolder1 설정
        PH1StartPosition = PipeHolder1.transform.position;
        PH1TargetPosition = PH1StartPosition + new Vector3(0, 0, -MoveAmount1);
        // PipeHolder2 설정
        PH2StartPosition = PipeHolder2.transform.position;
        PH2TargetPosition = PH2StartPosition + new Vector3(0, 0, MoveAmount2);
        // PipeHolder3 설정
        PH3StartPosition = PipeHolder3.transform.position;
        PH3TargetPosition = PH3StartPosition + new Vector3(MoveAmount3, 0, 0);
        // PipeHolder4 설정
        PH4StartPosition = PipeHolder4.transform.position;
        PH4TargetPosition = PH4StartPosition + new Vector3(-MoveAmount4, 0, 0);

        
    }

    // Update is called once per frame
    void Update()
    {
        if(isPipeHoldersCW)
        {
            if(!isPipeHoldersCCW)
            {

                if (isMoving && screwControl != null)
                {
                    screwControl.ActivateScrew(); // 나사 회전 시작
                }

                // 시간 누적
                elapsedTime += Time.deltaTime;

                // 0~1 사이로 보간 비율 계산
                float t = Mathf.Clamp01(elapsedTime / MoveTime);

                // 각 PipeHolder의 위치를 보간
                if (PipeHolder1 != null)
                {
                    PipeHolder1.transform.position = Vector3.Lerp(PH1StartPosition, PH1TargetPosition, t);
                }
                if (PipeHolder2 != null)
                {
                    PipeHolder2.transform.position = Vector3.Lerp(PH2StartPosition, PH2TargetPosition, t);
                }
                if (PipeHolder3 != null)
                {
                    PipeHolder3.transform.position = Vector3.Lerp(PH3StartPosition, PH3TargetPosition, t);
                }
                if (PipeHolder4 != null)
                {
                    PipeHolder4.transform.position = Vector3.Lerp(PH4StartPosition, PH4TargetPosition, t);
                }

                // 이동이 완료되었으면 멈추기
                if (t >= 1f)
                {
                    isPipeHoldersCW = false;
                    elapsedTime = 0f;
                    // 필요하다면 이 스크립트를 비활성화하거나, 추가 로직을 넣을 수 있습니다.
                    // this.enabled = false; 
                    if (screwControl != null)
                    {
                        screwControl.DeactivateScrew(); // 나사 회전 멈춤
                    }

                }
            }
            
            
        }

        if (isPipeHoldersCCW)
        {
            if(!isPipeHoldersCW)
            {

                if (isMoving && screwControl != null)
                {
                    screwControl.ActivateScrew(); // 나사 회전 시작
                }

                // 시간 누적
                elapsedTime += Time.deltaTime;

                // 0~1 사이로 보간 비율 계산
                float t = Mathf.Clamp01(elapsedTime / MoveTime);

                // 각 PipeHolder의 위치를 보간
                if (PipeHolder1 != null)
                {
                    PipeHolder1.transform.position = Vector3.Lerp(PH1TargetPosition, PH1StartPosition, t);
                }
                if (PipeHolder2 != null)
                {
                    PipeHolder2.transform.position = Vector3.Lerp(PH2TargetPosition, PH2StartPosition, t);
                }
                if (PipeHolder3 != null)
                {
                    PipeHolder3.transform.position = Vector3.Lerp(PH3TargetPosition, PH3StartPosition, t);
                }
                if (PipeHolder4 != null)
                {
                    PipeHolder4.transform.position = Vector3.Lerp(PH4TargetPosition, PH4StartPosition, t);
                }

                // 이동이 완료되었으면 멈추기
                if (t >= 1f)
                {
                    isPipeHoldersCCW = false;
                    elapsedTime = 0f;
                    // 필요하다면 이 스크립트를 비활성화하거나, 추가 로직을 넣을 수 있습니다.
                    // this.enabled = false; 
                    if (screwControl != null)
                    {
                        screwControl.DeactivateScrew(); // 나사 회전 멈춤
                    }

                }
            }
            
            
        }
    }

    public void ActivatePipeHoldersCW()
    {
        isPipeHoldersCW = true;
    }

    public void DeactivatePipeHoldersCW()
    {
        isPipeHoldersCW = false;
    }

    public void ActivatePipeHoldersCCW()
    {
        isPipeHoldersCCW = true;
    }

    public void DeactivatePipeHoldersCCW()
    {
        isPipeHoldersCCW = false;
    }
}