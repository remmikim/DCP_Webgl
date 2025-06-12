using UnityEngine;

public class Leg : MonoBehaviour
{
    public GameObject Leg2;

    private float rotationDuration = 5f;
    private float rotationAmount1 = 40f; // Leg의 최종 회전량
    private float rotationAmount2 = -40f; // Leg2의 최종 회전량 (Leg에 상대적)
    private float elapsedTime = 0f;
    private bool isRotating = true;

    private Quaternion initialRotationLeg1; // Leg의 초기 월드 회전
    private Quaternion initialRotationLeg2; // Leg2의 초기 월드 회전

    void Start()
    {
        // 각 오브젝트의 초기 월드 회전값 저장
        initialRotationLeg1 = transform.rotation;
        initialRotationLeg2 = Leg2.transform.rotation;
    }

    void Update()
    {
        if (isRotating)
        {
            // 시간 누적
            elapsedTime += Time.deltaTime;

            // 0~1 사이로 보간 비율 계산
            float t = Mathf.Clamp01(elapsedTime / rotationDuration);

            // Leg의 회전 계산 (초기 회전에서 rotationAmount1만큼 Z축으로 회전)
            Quaternion targetRotationLeg1 = initialRotationLeg1 * Quaternion.Euler(0, 0, rotationAmount1);
            transform.rotation = Quaternion.Slerp(initialRotationLeg1, targetRotationLeg1, t);

            // Leg2의 회전 계산 (Leg의 회전에 상대적으로 rotationAmount2만큼 Z축으로 회전)
            // Leg2의 초기 로컬 회전을 기준으로 rotationAmount2만큼 Z축으로 회전하는 델타 회전을 생성
            Quaternion deltaRotationLeg2 = Quaternion.Euler(0, 0, rotationAmount2);
            // Leg2의 부모(Leg)의 현재 회전에 Leg2의 초기 로컬 회전과 델타 회전을 곱하여 최종 월드 회전 계산
            // 이 부분은 Leg2가 Leg의 자식일 경우 Leg2의 로컬 회전을 기준으로 계산해야 합니다.
            // 여기서는 Leg2의 월드 회전을 직접 보간하되, Leg1의 회전과는 독립적으로 보간합니다.
            // 만약 Leg2가 Leg의 자식이고 Leg의 회전에 따라 회전하는 것이 목표라면 다음 줄은 달라져야 합니다.
            // 현재 코드에서는 Leg1과 Leg2가 각각 독립적으로 목표 Z축 회전까지 보간됩니다.
            Quaternion targetRotationLeg2 = initialRotationLeg2 * Quaternion.Euler(0, 0, rotationAmount2);
            Leg2.transform.rotation = Quaternion.Slerp(initialRotationLeg2, targetRotationLeg2, t);


            
            if (t >= 1f)
            {
                isRotating = false;
            }
        }
    }
}