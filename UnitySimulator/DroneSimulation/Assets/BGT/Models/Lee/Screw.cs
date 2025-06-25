using UnityEngine;

public class Screw : MonoBehaviour
{
    public float rotationSpeed = 250f;

    public GameObject Screw1;
    public GameObject Screw2;
    public GameObject Screw3;
    public GameObject Screw4;


    private bool isScrew = false;
    // Update is called once per frame
    void Update()
    {
        if(isScrew)
        {
            // 이 게임 오브젝트를 Y축을 중심으로 'rotationSpeed' 만큼 회전시킵니다.
            // Time.deltaTime을 곱하여 프레임 속도에 독립적인 회전을 보장합니다.
            // Space.Self는 오브젝트 자신의 로컬 Y축을 기준으로 회전시킵니다.
            // Space.World는 월드 좌표계의 Y축을 기준으로 회전시킵니다.
            Screw1.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw2.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw3.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw4.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);

        }

        
    }

    public void ActivateScrew()
    {
        isScrew = true;
    }

    public void DeactivateScrew()
    {
        isScrew = false;
    }

}
