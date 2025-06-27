using UnityEngine;

public class ZLift : MonoBehaviour
{
    // 유니티 에디터에서 드래그 앤 드롭으로 연결할 GameObject 변수들
    public GameObject LiftWeight;
    public GameObject CarriageFrame;
   
    public ZLiftRotation ROT;
    public ChainMove CHM;

    private float MoveTime = 5f; // 모든 파이프 홀더가 이동하는 데 걸리는 시간 (초)

    // 각 파이프 홀더가 Z축으로 이동할 양 (음수 값은 Z축 마이너스 방향으로 이동)
    private float MoveAmount1 = 1.0f;
    private float MoveAmount2 = 1.0f;
    

    // 각 Lift 시작 위치와 목표 위치 변수들
    private Vector3 LWStartPosition;   // LiftWeight
    private Vector3 LWTargetPosition;  // LiftWeight

    private Vector3 CFStartPosition;   // CarriageFrame
    private Vector3 CFTargetPosition;  // CarriageFrame



    private float elapsedTime = 0f;
    private bool isMoving = true; // 이동이 진행 중인지 여부

    private bool isZLiftCW = false;
    private bool isZLiftCCW = false;

    void Start()
    {
        // 각 PipeHolder GameObject의 현재 위치를 시작 위치로 설정
        // 그리고 그 위치에서 MoveAmount만큼 더한 위치를 목표 위치로 설정

        // LiftWeight 설정
        LWStartPosition = LiftWeight.transform.position;
        LWTargetPosition = LWStartPosition + new Vector3(0, -MoveAmount1, 0);
        // CarriageFrame 설정
        CFStartPosition = CarriageFrame.transform.position;
        CFTargetPosition = CFStartPosition + new Vector3(0, MoveAmount2, 0);
        

    }

    // Update is called once per frame
    void Update()
    {
        if (isZLiftCW && !isZLiftCCW)
        {
           
            ROT.ActivateZLiftRotationCW();
            CHM.ActiveChainCW();
            // 시간 누적
            elapsedTime += Time.deltaTime;

            // 0~1 사이로 보간 비율 계산
            float t = Mathf.Clamp01(elapsedTime / MoveTime);

            // 각 PipeHolder의 위치를 보간
            if (LiftWeight != null)
            {
                LiftWeight.transform.position = Vector3.Lerp(LWStartPosition, LWTargetPosition, t);
            }
            if (CarriageFrame != null)
            {
                CarriageFrame.transform.position = Vector3.Lerp(CFStartPosition, CFTargetPosition, t);
            }
            

            // 이동이 완료되었으면 멈추기
            if (t >= 1f)
            {
                isZLiftCW = false;
                elapsedTime = 0f;
                ROT.DeactivateZLiftRotationCW(); // 나사 회전 멈춤
                CHM.DeActiveChainCW();
            }
        }

        if (!isZLiftCW && isZLiftCCW)
        {
            ROT.ActivateZLiftRotationCCW(); // 나사 회전 시작
            CHM.ActiveChainCCW();
            // 시간 누적
            elapsedTime += Time.deltaTime;

            // 0~1 사이로 보간 비율 계산
            float t = Mathf.Clamp01(elapsedTime / MoveTime);

            // 각 PipeHolder의 위치를 보간
            if (LiftWeight != null)
            {
                LiftWeight.transform.position = Vector3.Lerp(LWTargetPosition, LWStartPosition, t);
            }
            if (CarriageFrame != null)
            {
                CarriageFrame.transform.position = Vector3.Lerp(CFTargetPosition, CFStartPosition, t);
            }
            

            // 이동이 완료되었으면 멈추기
            if (t >= 1f)
            {
                isZLiftCCW = false;
                elapsedTime = 0f;
                ROT.DeactivateZLiftRotationCCW(); // 나사 회전 멈춤
                CHM.DeActiveChainCCW();
            }
        }
    }

    public void ActivateZLiftUp()
    {
        isZLiftCW = true;
    }

    public void DeactivateZLiftUp()
    {
        isZLiftCW = false;
        ROT.DeactivateZLiftRotationCW(); // 나사 회전 멈춤
        CHM.DeActiveChainCW();
    }

    public void ActivateZLiftDown()
    {
        isZLiftCCW = true;
    }

    public void DeactivateZLiftDown()
    {
        isZLiftCCW = false;
        ROT.DeactivateZLiftRotationCCW(); // 나사 회전 멈춤
        CHM.DeActiveChainCCW();
    }
}