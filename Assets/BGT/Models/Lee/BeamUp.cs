using UnityEngine;

public class BeamUp : MonoBehaviour
{
    public GameObject BeamNextNum;

    private float targetAngle = 22f; // 목표 회전 각도 (X축 기준)
    private float rotationSpeed = 10; // 초당 회전할 각도 (예: 20도/초)

    private Quaternion initialRotationBeam1; 
    private Quaternion initialRotationBeam2;

    private bool isActiveup = false;
    private bool isActivedown = false;
    void Start()
    {
        //// 각 오브젝트의 초기 월드 회전값 저장
        //initialRotationBeam1 = transform.rotation;
        //initialRotationBeam2 = BeamNextNum.transform.rotation;
    }

    void Update()
    {
        if(isActivedown && !isActiveup)
        {
            // 2. 현재 회전에서 목표 회전까지 지정된 속도(rotationSpeed)로 이동
            Quaternion targetRotationBeam1 = initialRotationBeam1 * Quaternion.Euler(-targetAngle, 0, 0);  
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotationBeam1, rotationSpeed * Time.deltaTime);
            // --- Leg2 오브젝트 회전 ---
            // 1. 목표 회전값 계산: 초기 회전값에서 X축으로 -targetAngle만큼 회전한 월드 각도
            // Leg와 반대 방향으로 같은 양만큼 돌게 하려면 -targetAngle을 사용합니다.
            Quaternion targetRotationBeam2 = initialRotationBeam2 * Quaternion.Euler(targetAngle, 0, 0);
            BeamNextNum.transform.rotation = Quaternion.RotateTowards(BeamNextNum.transform.rotation, targetRotationBeam2, rotationSpeed * Time.deltaTime);
        }
        if(isActiveup && !isActivedown)
        {
       
            Quaternion targetRotationBeam1 = initialRotationBeam1 * Quaternion.Euler(targetAngle, 0, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotationBeam1, rotationSpeed * Time.deltaTime);
            Quaternion targetRotationBeam2 = initialRotationBeam2 * Quaternion.Euler(-targetAngle, 0, 0);
            BeamNextNum.transform.rotation = Quaternion.RotateTowards( BeamNextNum.transform.rotation, targetRotationBeam2, rotationSpeed * Time.deltaTime);
        }
    }
    public void ActiveDown()
    {
        if (isActivedown || isActiveup) return;
        isActivedown = true;
        initialRotationBeam1 = transform.rotation;
        initialRotationBeam2 = BeamNextNum.transform.rotation;
    }
    public void DeactiveDown()
    {
        isActivedown = false;
    }
    public void ActiveUp()
    {
        if (isActivedown || isActiveup) return;
        isActiveup = true;
        initialRotationBeam1 = transform.rotation;
        initialRotationBeam2 = BeamNextNum.transform.rotation;
    }
    public void DeactiveUp()
    {
        isActiveup = false;
    }
}