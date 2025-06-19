using UnityEngine;

public class Rotary_Out : MonoBehaviour
{
    private float rotationLeftAngle = -30f; // ?? ?? ????? ????
    private float rotationRightAngle = 90f;
    private float rotationDuration = 0.5f; // ????? ????? ????

    private Quaternion initialRotation;
    private Quaternion targetRotation;

    public float CurrentLocalRotationX { get; private set; }

    private float elapsedTime = 0f;
    private bool isRotating = false;

    // ???? ???? X?? ??? ?????? ??????? ???? ?? ????? Public ??? ???
    public float LocalRotationX { get; private set; }

    void Start()
    {
        initialRotation = transform.rotation;
        targetRotation = initialRotation;
        CurrentLocalRotationX = WrapAngle(transform.localEulerAngles.x); // ??? ???? ???????
    }

    void Update()
    {
        if (isRotating)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / rotationDuration);
            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, t);
            CurrentLocalRotationX = WrapAngle(transform.localEulerAngles.x); // ??? ?? ???? ???????

            if (t >= 1f)
            {
                isRotating = false;
                transform.rotation = targetRotation;
                CurrentLocalRotationX = WrapAngle(transform.localEulerAngles.x); // ???? ???? ???????
            }
        }
        else
        {
            // ??? ???? ??? ???? ???? ??????? (????? ???? ?????? ????)
            CurrentLocalRotationX = WrapAngle(transform.localEulerAngles.x);
        }
    }

    public void OnRightB() // ?????? ??? ??? ?? ???
    {
        if (!isRotating)
        {
            initialRotation = transform.rotation;
            targetRotation = initialRotation * Quaternion.Euler(rotationRightAngle, 0, 0);
            elapsedTime = 0f;
            isRotating = true;
        }
    }

    public void OnLeftB() // ???? ??? ??? ?? ???
    {
        if (!isRotating)
        {
            initialRotation = transform.rotation;
            targetRotation = initialRotation * Quaternion.Euler(rotationLeftAngle, 0, 0);
            elapsedTime = 0f;
            isRotating = true;
        }
    }

    // ????? ?????? -180 ~ 180 ?????? ??????? ???? ???
    private float WrapAngle(float angle)
    {
        angle %= 360;
        if (angle > 180)
            return angle - 360;
        return angle;
    }
}