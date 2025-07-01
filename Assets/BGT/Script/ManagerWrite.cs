using UnityEngine;

public class ManagerWrite : Manager
{
    // ======= Gameobject 불러오기======
    public GameObject Cube;
    public GameObject Carriage;
   

    public void WriteDevice()
    {
        short value1 = 1;
        short value0 = 0;

        if (Cube.GetComponent<Trigger>().TriggerSensor)
        {
            mxComponent.SetDevice("X0", value1);
        }
        else if (!Cube.GetComponent<Trigger>().TriggerSensor)
        {
            mxComponent.SetDevice("X0", value0);
        }

        if (Carriage.GetComponent<Trigger>().TriggerSensor)
        {
            mxComponent.SetDevice("X1", value1);
        }
        else if (!Carriage.GetComponent<Trigger>().TriggerSensor)
        {
            mxComponent.SetDevice("X1", value0);
        }
    }
    void OnApplicationQuit()
    {
        // 애플리케이션 종료 시 PLC 연결 해제
        if (mxComponent != null)
        {
            int iRet = mxComponent.Close();
            if (iRet == 0)
                Debug.Log("Manager.cs: PLC 연결 해제 성공.");
            else
                Debug.LogError($"Manager.cs: PLC 연결 해제 실패! 에러 코드: {iRet}");
        }
    }
}
