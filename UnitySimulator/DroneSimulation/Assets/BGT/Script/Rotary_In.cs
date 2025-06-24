using UnityEngine;

public class Rotary_In : MonoBehaviour
{
    private float rotationAngle = 60f; // 한 번 회전할 각도
    private float rotationDuration = 0.5f; // 회전에 걸리는 시간

    private Quaternion initialRotation;
    private Quaternion targetRotation;
    private float elapsedTime = 0f;
    private bool isRotating = false;

    void Start()
    {
        initialRotation = transform.rotation;
        targetRotation = initialRotation;
    }

    void Update()
    {
        if (isRotating)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / rotationDuration);
            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, t);

            if (t >= 1f)
            {
                isRotating = false;
                transform.rotation = targetRotation;
            }
        }
    }

    public void OnM6() // 오른쪽 버튼 클릭 시 호출
    {
        if (!isRotating)
        {
            initialRotation = transform.rotation;
            targetRotation = initialRotation * Quaternion.Euler(rotationAngle, 0, 0);
            elapsedTime = 0f;
            isRotating = true;
        }
    }

    public void OnM4() // 왼쪽 버튼 클릭 시 호출
    {
        if (!isRotating)
        {
            initialRotation = transform.rotation;
            targetRotation = initialRotation * Quaternion.Euler(-rotationAngle, 0, 0);
            elapsedTime = 0f;
            isRotating = true;
        }
    }
}