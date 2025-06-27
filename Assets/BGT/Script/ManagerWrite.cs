using UnityEngine;

public class ManagerWrite : Manager
{
    /// <summary>
    /// PLC 'X' 디바이스(워드)에 값 쓰기
    /// </summary>
    /// <param name="valueToWrite">X0에 쓸 16비트(1워드) 정수 값</param>
    public void WriteDevice()
    {
        short value1 = 1;
        short value0 = 0;

        if (Cube.GetComponent<Trigger>().TriggerSensor)
        {
            mxComponent.WriteDeviceRandom2("X0", 1, ref value1);
        }
        else
        {
            mxComponent.WriteDeviceRandom2("X0", 1, ref value0);
        }
    }
    public void OnApplicationQuit()
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
