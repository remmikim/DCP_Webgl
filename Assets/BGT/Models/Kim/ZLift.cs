using UnityEngine;

public class ZLift : MonoBehaviour
{
    // 유니티 에디터에서 드래그 앤 드롭으로 연결할 GameObject 변수들
    public GameObject LiftWeight;
    public GameObject CarriageFrame;
   
    public ZLiftRotation ROT;
    public ChainMove CHM;
    public ChainMove CHM1;

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

    private bool isZLiftCW = false;
    private bool isZLiftCCW = false;

    void Start()
    {
    }

    // Update is called once per frame
    private void InitializeZLiftCW()
    {
        // 이미 다른 방향으로 움직이거나 같은 방향으로 움직이고 있다면 재시작하지 않음
        if (isZLiftCW || isZLiftCCW) return;

        isZLiftCW = true;
        elapsedTime = 0f; // 여기서만 초기화!

        LWStartPosition = LiftWeight.transform.position;
        LWTargetPosition = LWStartPosition + new Vector3(0, -MoveAmount1, 0);

        CFStartPosition = CarriageFrame.transform.position;
        CFTargetPosition = CFStartPosition + new Vector3(0, MoveAmount2, 0);

        ROT.ActivateZLiftRotationCW();
        CHM.ActiveChainCW();
        CHM1.ActiveChainCW();
    }

    private void InitializeZLiftCCW() // 이름을 private로 변경하여 Update에서 직접 호출되지 않게 합니다.
    {
        // 이미 다른 방향으로 움직이거나 같은 방향으로 움직이고 있다면 재시작하지 않음
        if (isZLiftCW || isZLiftCCW) return;

        isZLiftCCW = true;
        elapsedTime = 0f; // 여기서만 초기화!

        LWStartPosition = LiftWeight.transform.position;
        LWTargetPosition = LWStartPosition + new Vector3(0, MoveAmount1, 0); 

        CFStartPosition = CarriageFrame.transform.position;
        CFTargetPosition = CFStartPosition + new Vector3(0, -MoveAmount2, 0);

        ROT.ActivateZLiftRotationCCW();
        CHM.ActiveChainCCW();
        CHM1.ActiveChainCCW();
    }

    // Update는 매 프레임 실행될 이동 로직만 담당
    void Update()
    {
        if (isZLiftCW && !isZLiftCCW)
        {
            elapsedTime += Time.deltaTime; // 시간 누적
            float t = Mathf.Clamp01(elapsedTime / MoveTime);

            if (LiftWeight != null)
            {
                LiftWeight.transform.position = Vector3.Lerp(LWStartPosition, LWTargetPosition, t);
            }
            if (CarriageFrame != null)
            {
                CarriageFrame.transform.position = Vector3.Lerp(CFStartPosition, CFTargetPosition, t);
            }
            bool reachedLWTarget = Vector3.Distance(LiftWeight.transform.position, LWTargetPosition) < 0.01f; // 허용 오차
            bool reachedCFTarget = Vector3.Distance(CarriageFrame.transform.position, CFTargetPosition) < 0.01f; // 허용 오차
            if (t >= 1f || (reachedLWTarget && reachedCFTarget))
            {
                isZLiftCW = false;
                elapsedTime = 0f; // 다음 이동을 위해 초기화
                ROT.DeactivateZLiftRotationCW();
                CHM.DeActiveChainCW();
                CHM1.DeActiveChainCW();
            }
        }
        else if (isZLiftCCW && !isZLiftCW)
        {
            elapsedTime += Time.deltaTime; // 시간 누적
            float t = Mathf.Clamp01(elapsedTime / MoveTime);

            if (LiftWeight != null)
            {
                LiftWeight.transform.position = Vector3.Lerp(LWStartPosition, LWTargetPosition, t);
            }
            if (CarriageFrame != null)
            {
                CarriageFrame.transform.position = Vector3.Lerp(CFStartPosition, CFTargetPosition, t);
            }
            bool reachedLWTarget = Vector3.Distance(LiftWeight.transform.position, LWTargetPosition) < 0.01f;
            bool reachedCFTarget = Vector3.Distance(CarriageFrame.transform.position, CFTargetPosition) < 0.01f;

            
            if (t >= 1f || (reachedLWTarget && reachedCFTarget)) 
            {
                isZLiftCCW = false;
                elapsedTime = 0f;
                ROT.DeactivateZLiftRotationCCW();
                CHM.DeActiveChainCCW();
                CHM1.DeActiveChainCCW();
            }
        }
    }
    public void ActivateZLiftUp()
    {
        InitializeZLiftCW(); // 이 함수가 호출될 때 이동 초기화 및 시작
    }

    public void DeactivateZLiftUp()
    {
        isZLiftCW = false;
        ROT.DeactivateZLiftRotationCW();
        CHM.DeActiveChainCW();
        CHM1.DeActiveChainCW();
    }

    public void ActivateZLiftDown()
    {
        InitializeZLiftCCW(); // 이 함수가 호출될 때 이동 초기화 및 시작
    }

    public void DeactivateZLiftDown()
    {
        isZLiftCCW = false;
        ROT.DeactivateZLiftRotationCCW();
        CHM.DeActiveChainCCW();
        CHM1.DeActiveChainCCW();
    }
}