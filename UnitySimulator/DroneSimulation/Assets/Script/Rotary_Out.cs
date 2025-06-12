using UnityEngine;

public class Rotary_Out : MonoBehaviour
{
    private float rotationLeftAngle = -30f; // 한 번 회전할 각도
    private float rotationRightAngle = 90f;
    private float rotationDuration = 0.5f; // 회전에 걸리는 시간

    private Quaternion initialRotation;
    private Quaternion targetRotation;

    public float CurrentLocalRotationX { get; private set; }

    private float elapsedTime = 0f;
    private bool isRotating = false;

    // 현재 로컬 X축 회전 각도를 외부에서 읽을 수 있도록 Public 속성 추가
    public float LocalRotationX { get; private set; }

    void Start()
    {
        initialRotation = transform.rotation;
        targetRotation = initialRotation;
        CurrentLocalRotationX = WrapAngle(transform.localEulerAngles.x); // 초기 각도 업데이트
    }

    void Update()
    {
        if (isRotating)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / rotationDuration);
            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, t);
            CurrentLocalRotationX = WrapAngle(transform.localEulerAngles.x); // 회전 중 각도 업데이트

            if (t >= 1f)
            {
                isRotating = false;
                transform.rotation = targetRotation;
                CurrentLocalRotationX = WrapAngle(transform.localEulerAngles.x); // 최종 각도 업데이트
            }
        }
        else
        {
            // 회전 중이 아닐 때도 각도 업데이트 (정확한 상태 유지를 위해)
            CurrentLocalRotationX = WrapAngle(transform.localEulerAngles.x);
        }
    }

    public void OnRightB() // 오른쪽 버튼 클릭 시 호출
    {
        if (!isRotating)
        {
            initialRotation = transform.rotation;
            targetRotation = initialRotation * Quaternion.Euler(rotationRightAngle, 0, 0);
            elapsedTime = 0f;
            isRotating = true;
        }
    }

    public void OnLeftB() // 왼쪽 버튼 클릭 시 호출
    {
        if (!isRotating)
        {
            initialRotation = transform.rotation;
            targetRotation = initialRotation * Quaternion.Euler(rotationLeftAngle, 0, 0);
            elapsedTime = 0f;
            isRotating = true;
        }
    }

    // 오일러 각도를 -180 ~ 180 범위로 래핑하는 헬퍼 함수
    private float WrapAngle(float angle)
    {
        angle %= 360;
        if (angle > 180)
            return angle - 360;
        return angle;
    }
}