using UnityEngine;

public class XGantry : MonoBehaviour 
{
    public GameObject XGantryMoving;

    public XGantryRotaion RotaionObject;
    public Chain1 chainIntance;
    // 이동 속도 (초당 이동 거리)
    private float moveSpeed = 0.2f;
    private float moveDistanceZ = 5.0f;

    // 각 Lift 시작 위치와 목표 위치 변수들
    private Vector3 LWStartPosition;    // XGantryMoving
    private Vector3 LWTargetPosition;  // XGantryMoving

    private bool isXGantryMovingRight = false;
    private bool isXGantryMovingLeft = false;

    void Start()
    {
        // Debug.Log(this.gameObject.name + " XGantryMoving 스크립트 시작.");
        // XGantryMoving와 CarriageFrame에 Rigidbody가 있다면 Is Kinematic을 체크하는 것이 좋습니다.
        // 직접 Transform.position을 제어할 때 물리 엔진의 간섭을 피할 수 있습니다.
        if (XGantryMoving != null && XGantryMoving.GetComponent<Rigidbody>() != null)
        {
            XGantryMoving.GetComponent<Rigidbody>().isKinematic = true;
        }
      
    }

    // Update는 매 프레임 실행될 이동 로직만 담당
    void Update()
    {
        if (isXGantryMovingRight && !isXGantryMovingLeft)
        {
            if (XGantryMoving != null)
            {
                XGantryMoving.transform.position = Vector3.MoveTowards(XGantryMoving.transform.position, LWTargetPosition, moveSpeed * Time.deltaTime);
            }
            
        }
        // CCW 이동 로직 (XGantryMoving는 위로, CarriageFrame은 아래로)
        else if (isXGantryMovingLeft && !isXGantryMovingRight)
        {
            // XGantryMoving 이동 (MoveTowards 사용)
            if (XGantryMoving != null)
            {
                XGantryMoving.transform.position = Vector3.MoveTowards(XGantryMoving.transform.position, LWTargetPosition, moveSpeed * Time.deltaTime);
            }
           
        }
    }

    // CW 이동을 초기화하고 시작합니다. (ActivateXGantryMovingUp으로 변경될 수 있습니다)
    public void ActivateXGantryMovingRight() 
    {
        // 이미 다른 방향으로 움직이거나 같은 방향으로 움직이고 있다면 재시작하지 않음
        if (isXGantryMovingRight || isXGantryMovingLeft) return;
        isXGantryMovingRight = true;
        LWStartPosition = XGantryMoving.transform.position;
        LWTargetPosition = LWStartPosition + new Vector3(0,0, -moveDistanceZ);
        RotaionObject.ActivateZLiftRotationCW();
        chainIntance.ActiveChainCW();
    }

    // CW 이동을 강제로 중지합니다.
    public void DeactivateXGantryMovingRight()
    {
        isXGantryMovingRight = false;
        RotaionObject.DeactivateZLiftRotationCW();
        chainIntance.DeActiveChainCW();
    }

    public void ActivateXGantryMovingLeft() 
    {
        // 이미 다른 방향으로 움직이거나 같은 방향으로 움직이고 있다면 재시작하지 않음
        if (isXGantryMovingRight || isXGantryMovingLeft) return;
        isXGantryMovingLeft = true;
        LWStartPosition = XGantryMoving.transform.position;
        LWTargetPosition = LWStartPosition + new Vector3(0, 0, moveDistanceZ);
        RotaionObject.ActivateZLiftRotationCCW();
        chainIntance.ActiveChainCCW();
       
    }

    // CCW 이동을 강제로 중지합니다.
    public void DeactivateXGantryMovingLeft()
    {
        // CCW 이동 중일 때만 중지
        isXGantryMovingLeft = false;
        RotaionObject.DeactivateZLiftRotationCCW() ;
        chainIntance.DeActiveChainCCW() ;
    }
}