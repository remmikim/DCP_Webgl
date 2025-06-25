using ActUtlType64Lib;
using UnityEngine;

public class Manager : MonoBehaviour
{
    private ActUtlType64 mxComponent;
    private bool currentY0State;
    private bool currentY1State;

    public Chain1 chainInstance ;
    public Chain1 chainInstance12;
    void Start()
    {
        mxComponent = new ActUtlType64Lib.ActUtlType64();
        mxComponent.ActLogicalStationNumber = 1;
        int iRet = mxComponent.Open(); // PLC 연결 시도
        if (iRet == 0)
        {
            Debug.Log("Manager.cs: PLC 연결 성공!");
        }
        else
        {
            Debug.LogError($"Manager.cs: PLC 연결 실패! 에러 코드: {iRet}");
        }
    }

    void Update()
    {
       
        ReadDevice();
        
    }
    private void ReadDevice()
    {
        // blockCnt를 1로 하면 startDevice(Y0)부터 16비트(1워드)를 읽어옵니다.
        // 이 1워드 안에 Y0, Y1, Y2, ..., YF까지의 비트가 포함됩니다.
        int blockCnt = 2;
        int[] data = new int[blockCnt]; // 크기를 blockCnt에 맞춥니다.

        // Y0부터 시작하는 1워드를 읽어옵니다.
        int iRet = mxComponent.ReadDeviceBlock("Y0", blockCnt, out data[0]);

        if (iRet == 0)
        {
            int y0ToY0F = data[0]; // Y0부터 Y15까지의 비트 상태를 담은 워드 값
            int y10ToY1F = data[1]; // Y16부터 Y31까지의 비트 상태를 담은 워드 값

            // Y0 비트 추출
            bool newY0State = ((y0ToYF >> 0) & 1) == 1; // 0번째 비트
            if (newY0State != currentY0State) // 상태 변경 감지
            {
                currentY0State = newY0State;
                if (chainInstance != null)
                {
                    if (currentY0State)
                        chainInstance.ActivateChain();
                    else
                        chainInstance.DeactivateChain();
                }
            }
            // Y1 비트 추출
            bool newY1State = ((y10ToY1F >> 15) & 1) == 1; // 1번째 비트
            if (newY1State != currentY1State) // 상태 변경 감지
            {
                currentY1State = newY1State;
                if (chainInstance12 != null) // chainInstance12가 Y1을 제어한다고 가정
                {
                    if (currentY1State)
                        chainInstance12.ActivateChain();
                    else
                        chainInstance12.DeactivateChain();
                }
            }
        }
        else
        {
            Debug.LogWarning($"Manager.cs: 상태 읽기 실패! 에러 코드: {iRet}.");
        }
    }
    private void WirteDevice(int X0ToXF)
    {
        // blockCnt를 1로 하면 startDevice(X0)부터 16비트(1워드)를 읽어옵니다.
        int blockCnt = 1;
        int[] data = new int[blockCnt]; // 크기를 blockCnt에 맞춥니다.

        // X0부터 시작하는 1워드를 읽어옵니다.
        int iRet = mxComponent.WriteDeviceBlock("X0", blockCnt, data[0]);

        if(iRet == 0)
        {
            X0ToXF = data[0];
        }
    }
    void OnApplicationQuit()
    {
        // 애플리케이션 종료 시 PLC 연결 해제
        if (mxComponent != null)
        {
            int iRet = mxComponent.Close();
            if (iRet == 0)
            {
                Debug.Log("Manager.cs: PLC 연결 해제 성공.");
            }
            else
            {
                Debug.LogError($"Manager.cs: PLC 연결 해제 실패! 에러 코드: {iRet}");
            }
        }
    }
}