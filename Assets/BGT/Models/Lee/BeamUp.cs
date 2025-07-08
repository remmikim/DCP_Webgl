using UnityEngine;

public class BeamUp : MonoBehaviour
{
    public GameObject BeamNextNum;

    private float targetAngle = 22f; // 목표 회전 각도 (X축 기준)
    private float rotationSpeed = 10; // 초당 회전할 각도 (예: 20도/초)

    private Quaternion initialRotationBeam1; 
    private Quaternion initialRotationBeam2; 

    void Start()
    {
        // 각 오브젝트의 초기 월드 회전값 저장
        initialRotationBeam1 = transform.rotation;
        initialRotationBeam2 = BeamNextNum.transform.rotation;
    }

    void Update()
    {
        // --- Leg (본인 오브젝트) 회전 ---
        // 1. 목표 회전값 계산: 초기 회전값에서 X축으로 targetAngle만큼 회전한 월드 각도
        Quaternion targetRotationBeam1 = initialRotationBeam1 * Quaternion.Euler(-targetAngle, 0, 0);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotationBeam1,   rotationSpeed * Time.deltaTime);

        Quaternion targetRotationBeam2 = initialRotationBeam2 * Quaternion.Euler(targetAngle, 0, 0);
        BeamNextNum.transform.rotation = Quaternion.RotateTowards(BeamNextNum.transform.rotation, targetRotationBeam2, rotationSpeed * Time.deltaTime);
    }
}