using UnityEngine;

public class ZLiftTigger : MonoBehaviour 
{
    public GameObject LiftWeight;
    public GameObject CarriageFrame;

    public ZLiftRotation ROT;
    public ChainMove CHM;
    public ChainMove CHM1;

    // 이동 속도 (초당 이동 거리)
    private float moveSpeed = 0.2f;
    private float moveDistanceY = 5.0f;

    // 각 Lift 시작 위치와 목표 위치 변수들
    private Vector3 LWStartPosition;    // LiftWeight
    private Vector3 LWTargetPosition;  // LiftWeight

    private Vector3 CFStartPosition;    // CarriageFrame
    private Vector3 CFTargetPosition;  // CarriageFrame

    private bool isZLiftCW = false;
    private bool isZLiftCCW = false;

    void Start()
    {
        // Debug.Log(this.gameObject.name + " ZLift 스크립트 시작.");
        // LiftWeight와 CarriageFrame에 Rigidbody가 있다면 Is Kinematic을 체크하는 것이 좋습니다.
        // 직접 Transform.position을 제어할 때 물리 엔진의 간섭을 피할 수 있습니다.
        if (LiftWeight != null && LiftWeight.GetComponent<Rigidbody>() != null)
        {
            LiftWeight.GetComponent<Rigidbody>().isKinematic = true;
        }
        if (CarriageFrame != null && CarriageFrame.GetComponent<Rigidbody>() != null)
        {
            CarriageFrame.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    // Update는 매 프레임 실행될 이동 로직만 담당
    void Update()
    {
        if (isZLiftCW && !isZLiftCCW)
        {
            if (LiftWeight != null)
            {
                LiftWeight.transform.localPosition = Vector3.MoveTowards(LiftWeight.transform.localPosition, LWTargetPosition, moveSpeed * Time.deltaTime);
            }
            if (CarriageFrame != null)
            {
                CarriageFrame.transform.localPosition = Vector3.MoveTowards(CarriageFrame.transform.localPosition, CFTargetPosition, moveSpeed * Time.deltaTime);
            }
        }
        // CCW 이동 로직 (LiftWeight는 위로, CarriageFrame은 아래로)
        else if (isZLiftCCW && !isZLiftCW)
        {
            // LiftWeight 이동 (MoveTowards 사용)
            if (LiftWeight != null)
            {
                LiftWeight.transform.localPosition = Vector3.MoveTowards(LiftWeight.transform.localPosition, LWTargetPosition, moveSpeed * Time.deltaTime);
            }
            // CarriageFrame 이동 (MoveTowards 사용)
            if (CarriageFrame != null)
            {
                CarriageFrame.transform.localPosition = Vector3.MoveTowards(CarriageFrame.transform.localPosition, CFTargetPosition, moveSpeed * Time.deltaTime);
            }
        }
    }

    // CW 이동을 초기화하고 시작합니다. (ActivateZLiftUp으로 변경될 수 있습니다)
    public void ActivateZLiftUp() 
    {
        // 이미 다른 방향으로 움직이거나 같은 방향으로 움직이고 있다면 재시작하지 않음
        if (isZLiftCW || isZLiftCCW) return;

        isZLiftCW = true;

        LWStartPosition = LiftWeight.transform.localPosition;
        LWTargetPosition = LWStartPosition + new Vector3(0, 0,-moveDistanceY);

        CFStartPosition = CarriageFrame.transform.localPosition;
        CFTargetPosition = CFStartPosition + new Vector3(0, 0, moveDistanceY);

        ROT.ActivateZLiftRotationCW();
        CHM.ActiveChainCW();
        CHM1.ActiveChainCW();
        Debug.Log("ZLiftUp 활성화. CW 이동 시작.");
    }

    // CW 이동을 강제로 중지합니다.
    public void DeactivateZLiftUp()
    {
        // CW 이동 중일 때만 중지
        isZLiftCW = false;
        ROT.DeactivateZLiftRotationCW();
        CHM.DeActiveChainCW();
        CHM1.DeActiveChainCW();
    }

    public void ActivateZLiftDown() 
    {
        // 이미 다른 방향으로 움직이거나 같은 방향으로 움직이고 있다면 재시작하지 않음
        if (isZLiftCW || isZLiftCCW) return;

        isZLiftCCW = true;
        LWStartPosition = LiftWeight.transform.localPosition;
        LWTargetPosition = LWStartPosition + new Vector3(0, 0, moveDistanceY);
        CFStartPosition = CarriageFrame.transform.localPosition;
        CFTargetPosition = CFStartPosition + new Vector3(0, 0, -moveDistanceY);

        ROT.ActivateZLiftRotationCCW();
        CHM.ActiveChainCCW();
        CHM1.ActiveChainCCW();
        Debug.Log("ZLiftDown 활성화. CCW 이동 시작.");
    }

    // CCW 이동을 강제로 중지합니다.
    public void DeactivateZLiftDown()
    {
        // CCW 이동 중일 때만 중지
        isZLiftCCW = false;
        ROT.DeactivateZLiftRotationCCW();
        CHM.DeActiveChainCCW();
        CHM1.DeActiveChainCCW();
    }
}