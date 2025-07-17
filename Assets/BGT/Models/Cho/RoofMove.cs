using UnityEngine;

public class RoofMove : MonoBehaviour
{
    // 유니티 에디터에서 드래그 앤 드롭으로 연결할 GameObject 변수들
    public GameObject FourthRoof;
    public GameObject ThirdRoof;
    public GameObject SecondRoof;

    private float MoveSpeed = 0.5f;
    private float MoveAmountY = 1.0f;

    // 각 파이프 홀더의 시작 위치와 목표 위치 변수들
    private Vector3 FourStartPosition;   // FourthRoof
    private Vector3 FourTargetPosition;  // FourthRoof

    private Vector3 ThirdStartPosition;   // ThirdRoof
    private Vector3 ThirdTargetPosition;  // ThirdRoof

    private Vector3 SecondStartPosition;   // SecondRoof
    private Vector3 SecondTargetPosition;  // SecondRoof

    
    
    private bool isPipeHoldersCW = false;
    private bool isPipeHoldersCCW = false;

    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isPipeHoldersCW && !isPipeHoldersCCW)
        {
            // 각 PipeHolder의 위치를 보간
            if (FourthRoof != null)
            {
                FourthRoof.transform.localPosition = Vector3.MoveTowards(FourthRoof.transform.localPosition, FourTargetPosition, MoveSpeed * Time.deltaTime);
            }
            if (ThirdRoof != null)
            {
                ThirdRoof.transform.localPosition = Vector3.MoveTowards(ThirdRoof.transform.localPosition, ThirdTargetPosition, MoveSpeed * Time.deltaTime);
            }
            if (SecondRoof != null)
            {
                SecondRoof.transform.localPosition = Vector3.MoveTowards(SecondRoof.transform.localPosition, SecondTargetPosition, MoveSpeed * Time.deltaTime);
            }
        }

        if (isPipeHoldersCCW && !isPipeHoldersCW)
        {
            // 각 PipeHolder의 위치를 보간
            if (FourthRoof != null)
            {
                FourthRoof.transform.localPosition = Vector3.MoveTowards(FourthRoof.transform.localPosition, FourTargetPosition, MoveSpeed * Time.deltaTime);
            }
            if (ThirdRoof != null)
            {
                ThirdRoof.transform.localPosition = Vector3.MoveTowards(ThirdRoof.transform.localPosition, ThirdTargetPosition, MoveSpeed * Time.deltaTime);
            }
            if (SecondRoof != null)
            {
                SecondRoof.transform.localPosition = Vector3.MoveTowards(SecondRoof.transform.localPosition, SecondTargetPosition, MoveSpeed * Time.deltaTime);
            }
        }
    }

    public void ActivateFrontRoof()
    {
        if (isPipeHoldersCW || isPipeHoldersCCW) return;
        isPipeHoldersCW = true;
     
        // FourthRoof 설정
        FourStartPosition = FourthRoof.transform.localPosition;
        FourTargetPosition = FourStartPosition + new Vector3(4.29f, 0, 0);
        // ThirdRoof 설정
        ThirdStartPosition = ThirdRoof.transform.localPosition;
        ThirdTargetPosition = ThirdStartPosition + new Vector3(2.8f, 0, 0);
        // SecondRoof 설정
        SecondStartPosition = SecondRoof.transform.localPosition;
        SecondTargetPosition = SecondStartPosition + new Vector3(1.4f, 0, 0);
    }

    public void DeactivateFrontRoof()
    {
        isPipeHoldersCW = false;
    }

    public void ActivateBackRoof()
    {
        if (isPipeHoldersCW || isPipeHoldersCCW) return;
        isPipeHoldersCCW = true;
        
        // FourthRoof 설정
        FourStartPosition = FourthRoof.transform.localPosition;
        FourTargetPosition = FourStartPosition + new Vector3(-4.29f, 0, 0);
        // ThirdRoof 설정
        ThirdStartPosition = ThirdRoof.transform.localPosition;
        ThirdTargetPosition = ThirdStartPosition + new Vector3(-2.8f, 0, 0);
        // SecondRoof 설정
        SecondStartPosition = SecondRoof.transform.localPosition;
        SecondTargetPosition = SecondStartPosition + new Vector3(-1.4f, 0, 0);
        
    }

    public void DeactivateBackRoof()
    {
        isPipeHoldersCCW = false;
    }
}