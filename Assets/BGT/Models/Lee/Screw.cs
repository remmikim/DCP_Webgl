using UnityEngine;

public class Screw : MonoBehaviour
{
    private float rotationSpeed = 50f;

    public GameObject Screw1;
    public GameObject Screw2;
    public GameObject Screw3;
    public GameObject Screw4;
    public GameObject Screw5;
    public GameObject Screw6;


    private bool isScrewCW = false;
    private bool isScrewCCW = false;
    // Update is called once per frame
    void Update()
    {
        if(isScrewCW && !isScrewCCW)
        {
            // Space.Self는 오브젝트 자신의 로컬 Y축을 기준으로 회전시킵니다.
            // Space.World는 월드 좌표계의 Y축을 기준으로 회전시킵니다.
            Screw1.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw2.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw3.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw4.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw5.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw6.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);

        }
        if(!isScrewCW && isScrewCCW)
        {
            // Space.Self는 오브젝트 자신의 로컬 Y축을 기준으로 회전시킵니다.
            // Space.World는 월드 좌표계의 Y축을 기준으로 회전시킵니다.
            Screw1.transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw2.transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw3.transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw4.transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw5.transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0, Space.Self);
            Screw6.transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0, Space.Self);

        }

        
    }

    public void ActivateScrewCW()
    {
        isScrewCW = true;
    }

    public void DeactivateScrewCW()
    {
        isScrewCW = false;
    }
    public void ActivateScrewCCW()
    {
        isScrewCCW = true;
    }

    public void DeactivateScrewCCW()
    {
        isScrewCCW = false;
    }

}
