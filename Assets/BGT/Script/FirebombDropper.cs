using UnityEngine;
using System.Collections.Generic;

public class FirebombDropper : MonoBehaviour
{
   
    public Rotary_Out rotaryOutScript;
    public GameObject[] firebombs;
    public float[] DropAngles;

    private bool[] hasBombDropped;
    private int nextBombIndexToDrop = 0;
    private bool isTargetAngle = false;
    private float prvRotaryOutXAngle; // previousRotaryOutXAngle


    void Start()
    {
        hasBombDropped = new bool[firebombs.Length];
        isTargetAngle = false;

        prvRotaryOutXAngle = rotaryOutScript.CurrentLocalRotationX;

        // FirebombDropper에서는 Rigidbody/Collider 초기 설정을 하지 않습니다.
        // 이 부분은 각 Firebomb.cs 스크립트가 Awake에서 담당합니다.
    }

    void Update()
    {
        if (rotaryOutScript == null) return;
        if (nextBombIndexToDrop >= firebombs.Length) return;

        float RotaryOutXAngle = rotaryOutScript.CurrentLocalRotationX;
        float angleDelta = WrapAngle(RotaryOutXAngle - prvRotaryOutXAngle);

        float targetAbsoluteAngle = DropAngles[nextBombIndexToDrop];
        float angleDifference = Mathf.Abs(RotaryOutXAngle - targetAbsoluteAngle);
        bool currentAngleMeetsCondition = (angleDifference <= 5f);

        // 조건: 목표 각도 범위에 '진입'했을 때 AND '왼쪽으로 회전 중일 때'
        if (currentAngleMeetsCondition && !isTargetAngle && angleDelta < -0.1f) // 왼쪽으로 회전하는 동안만 감지
        {
            isTargetAngle = true;

            if (!hasBombDropped[nextBombIndexToDrop])
            {
                DropFirebomb(nextBombIndexToDrop); // bomb 떨어뜨림 함수 호출
                hasBombDropped[nextBombIndexToDrop] = true;
                nextBombIndexToDrop++;
            }
        }
        else if (!currentAngleMeetsCondition && isTargetAngle)
        {
            isTargetAngle = false;
        }

        prvRotaryOutXAngle = RotaryOutXAngle;
    }

    // 특정 인덱스의 Firebomb을 떨어뜨리는 함수
    public void DropFirebomb(int index)
    {
        if (index < 0 || index >= firebombs.Length) return;
        if (hasBombDropped[index]) return;

        GameObject bomb = firebombs[index];
        if (bomb == null) return;

        bomb.transform.SetParent(null); // 부모 관계 해제 (물리적으로 독립시키기 위함)

        // Firebomb에 붙어있는 Firebomb.cs 스크립트를 가져와 물리 활성화 및 소멸 스케줄링 함수를 호출
        Firebomb firebombScript = bomb.GetComponent<Firebomb>();
        if (firebombScript != null)
        {
            firebombScript.Destruction(); // 함수를 호출
           
        }
        else
        {
            Rigidbody rb = bomb.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
        }
    }

    // 오일러 각도를 -180 ~ 180 범위로 래핑하는 헬퍼 함수
    private float WrapAngle(float angle)
    {
        angle %= 360;
        if (angle > 180)
            return angle - 360;
        if (angle < -180)
            return angle + 360;
        return angle;
    }
}