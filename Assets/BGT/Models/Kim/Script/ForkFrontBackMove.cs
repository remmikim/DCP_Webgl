using UnityEngine;

public class ForkFrontBackMove : MonoBehaviour
{
    // 유니티 에디터에서 드래그 앤 드롭으로 연결할 GameObject 변수들

    public GameObject Second;
    public GameObject Third;

    private float MoveSpeed = 0.2f;
    //private float MoveAmountY = 0.272f;

    // 각 파이프 홀더의 시작 위치와 목표 위치 변수들
    private Vector3 SecondStartPosition;   
    private Vector3 SecondTargetPosition;  
    private Vector3 ThirdStartPosition;   
    private Vector3 ThirdTargetPosition;  

   
    private bool isForkMoveFront = false;
    private bool isForkMoveBack = false;

    
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (isForkMoveFront && !isForkMoveBack)
        {
            
             Second.transform.localPosition = Vector3.MoveTowards(Second.transform.localPosition, SecondTargetPosition, MoveSpeed * Time.deltaTime);
             Third.transform.localPosition = Vector3.MoveTowards(Third.transform.localPosition, ThirdTargetPosition, 2 * MoveSpeed * Time.deltaTime);
           
        }

        if (isForkMoveBack && !isForkMoveFront)
        {
            // 각 PipeHolder의 위치를 보간
           
             Second.transform.localPosition = Vector3.MoveTowards(Second.transform.localPosition, SecondTargetPosition, MoveSpeed * Time.deltaTime);
             Third.transform.localPosition = Vector3.MoveTowards(Third.transform.localPosition, ThirdTargetPosition, 2 * MoveSpeed * Time.deltaTime);
           
        }
    }

    public void ActivateFront()
    {
        if (isForkMoveFront || isForkMoveBack) return;
        isForkMoveFront = true;
        
        SecondStartPosition = Second.transform.localPosition;
        SecondTargetPosition = SecondStartPosition + new Vector3(0.9f, 0, 0);
        ThirdStartPosition = Third.transform.localPosition;
        ThirdTargetPosition = ThirdStartPosition + new Vector3(1.8f, 0, 0);
        
    }

    public void DeactivateFront()
    {
        isForkMoveFront = false;
       
    }

    public void ActivateBack()
    {
        if (isForkMoveFront || isForkMoveBack) return;
        isForkMoveBack = true;
        
        SecondStartPosition = Second.transform.localPosition;
        SecondTargetPosition = SecondStartPosition + new Vector3(-0.9f, 0, 0);
        ThirdStartPosition = Third.transform.localPosition;
        ThirdTargetPosition = ThirdStartPosition + new Vector3(-1.8f, 0, 0);
       
    }

    public void DeactivateBack()
    {
        isForkMoveBack = false;
        
    }
}