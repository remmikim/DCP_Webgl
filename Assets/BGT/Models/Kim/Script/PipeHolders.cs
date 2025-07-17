using UnityEngine;

public class ForkMove : MonoBehaviour
{
    // 유니티 에디터에서 드래그 앤 드롭으로 연결할 GameObject 변수들
    

    private float MoveSpeed = 0.2f;
    private float MoveAmountY = 1.0f;

    // 각 파이프 홀더의 시작 위치와 목표 위치 변수들
    private Vector3 StartPosition;   // PipeHolder1
    private Vector3 TargetPosition;  // PipeHolder1

   
    private bool isForkMoveRigt = false;
    private bool isForkMoveLeft = false;

    
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (isForkMoveRigt && !isForkMoveLeft)
        {
            
             transform.localPosition = Vector3.MoveTowards(transform.localPosition, TargetPosition, MoveSpeed * Time.deltaTime);
           
        }

        if (isForkMoveLeft && !isForkMoveRigt)
        {
            // 각 PipeHolder의 위치를 보간
           
             transform.localPosition = Vector3.MoveTowards(transform.localPosition, TargetPosition, MoveSpeed * Time.deltaTime);
           
        }
    }

    public void ActivatePipeHoldersRigt()
    {
        if (isForkMoveRigt || isForkMoveLeft) return;
        isForkMoveRigt = true;
        
        StartPosition = transform.localPosition;
        TargetPosition = StartPosition + new Vector3(0, MoveAmountY, 0);
        
    }

    public void DeactivatePipeHoldersRight()
    {
        isForkMoveRigt = false;
       
    }

    public void ActivatePipeHoldersLeft()
    {
        if (isForkMoveRigt || isForkMoveLeft) return;
        isForkMoveLeft = true;
        
        StartPosition = transform.localPosition;
        TargetPosition = StartPosition + new Vector3(0, -MoveAmountY, 0);
       
    }

    public void DeactivatePipeHoldersLeft()
    {
        isForkMoveLeft = false;
        
    }
}