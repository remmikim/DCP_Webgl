using UnityEngine;

public class ZLiftRotation : MonoBehaviour
{
    private float rotationSpeed = 50f;

    public GameObject Pulley1;
    public GameObject Pulley2;
    public GameObject Shaft;
    
    private bool isZLiftRotationCW = false;
    private bool isZLiftRotationCCW = false;
    // Update is called once per frame
    void Update()
    {
        if(isZLiftRotationCW && !isZLiftRotationCCW)
        {
            // 이 게임 오브젝트를 Y축을 중심으로 'rotationSpeed' 만큼 회전시킵니다.
            // Time.deltaTime을 곱하여 프레임 속도에 독립적인 회전을 보장합니다.
            // Space.Self는 오브젝트 자신의 로컬 Y축을 기준으로 회전시킵니다.
            // Space.World는 월드 좌표계의 Y축을 기준으로 회전시킵니다.
            Pulley1.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
            Pulley2.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
            Shaft.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
        }

        else if(!isZLiftRotationCW && isZLiftRotationCCW)
        {
            // 이 게임 오브젝트를 Y축을 중심으로 'rotationSpeed' 만큼 회전시킵니다.
            // Time.deltaTime을 곱하여 프레임 속도에 독립적인 회전을 보장합니다.
            // Space.Self는 오브젝트 자신의 로컬 Y축을 기준으로 회전시킵니다.
            // Space.World는 월드 좌표계의 Y축을 기준으로 회전시킵니다.
            Pulley1.transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0, Space.Self);
            Pulley2.transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0, Space.Self);
            Shaft.transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0, Space.Self);
        }

        
    }

    public void ActivateZLiftRotationCW()
    {
        isZLiftRotationCW = true;
    }

    public void DeactivateZLiftRotationCW()
    {
        isZLiftRotationCW = false;
    }
    public void ActivateZLiftRotationCCW()
    {
        isZLiftRotationCCW = true;
    }

    public void DeactivateZLiftRotationCCW()
    {
        isZLiftRotationCCW = false;
    }
}
