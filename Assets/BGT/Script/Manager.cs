using ActUtlType64Lib;
using System;
using System.Collections;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public ActUtlType64 mxComponent; // PLC 통신용 객체
    private int logicalStationNumber = 1; // Unity Editor에서 설정할 PLC 논리 스테이션 번호
    //=======(Y)======================
    private bool currentY0State; // Y0 장치 현재 상태
    private bool currentY1State; // Y1 장치 현재 상태
    private bool currentY2State; // Y2 장치 현재 상태
    private bool currentY3State; // Y3 장치 현재 상태
    private bool currentY4State; // Y4 장치 현재 상태
    private bool currentY5State; // Y5 장치 현재 상태


    // ======= Class 불러오기 ============
    public Chain1 chainInstance; // Y0 상태에 따라 제어할 Chain1 스크립트 참조
    public Chain1 chainInstance12; // Y1 상태에 따라 제어할 Chain1 스크립트 참조
    public PipeHolders pipeHolders;
    public ZLift zLift;
    public GameObject Cube;

    // ====== ManagerWrite Class 불러오기 ======
    public ManagerWrite managerWrite;

    void Start()
    {
        mxComponent = new ActUtlType64();
        mxComponent.ActLogicalStationNumber = logicalStationNumber;

        int iRet = mxComponent.Open(); // PLC 연결 시도
        if (iRet == 0)
            Debug.Log("Manager.cs: PLC 연결 성공!");
        else
            Debug.LogError($"Manager.cs: PLC 연결 실패! 에러 코드: {iRet}. 논리 스테이션 번호 확인 요망.");
    }

    void Update()
    {
        ReadDevice(); // 매 프레임마다 PLC 장치 상태 읽기
        managerWrite.WriteDevice();

        //WriteDevice(); // 10/ 11111

    }

    /// <summary>
    /// PLC 'Y' 디바이스(비트) 상태 읽어와 Unity 객체 상태 업데이트
    /// </summary>
    private void ReadDevice()
    {
        int blockCnt = 1; // 1워드(16비트) 읽기
        int[] data = new int[blockCnt];

        int iRet = mxComponent.ReadDeviceBlock("Y0", blockCnt, out data[0]); // Y0부터 1워드 읽기

        if (iRet == 0) // 읽기 성공 시
        {
            int y0ToYF = data[0]; // Y0부터 YF까지의 비트 상태를 담은 워드 값

            // Y0 비트 추출 및 상태 변경 감지
            bool newY0State = ((y0ToYF >> 0) & 1) == 1;
            if (newY0State != currentY0State)
            {
                currentY0State = newY0State;
                if (chainInstance) // Chain1 인스턴스 유효한지 확인
                {
                    if (currentY0State)
                        chainInstance.ActivateChain();
                    else
                        chainInstance.DeactivateChain();
                }
            }

            // Y1 비트 추출 및 상태 변경 감지
            bool newY1State = ((y0ToYF >> 1) & 1) == 1;
            if (newY1State != currentY1State)
            {
                currentY1State = newY1State;
                if (chainInstance12) // Chain12 인스턴스 유효한지 확인
                {
                    if (currentY1State)
                        chainInstance12.ActivateChain();
                    else
                        chainInstance12.DeactivateChain();
                }
            }

            // Y2 비트 추출 및 상태 변경 감지
            bool newY2State = ((y0ToYF >> 2) & 1) == 1;
            if (newY2State != currentY2State)
            {
                currentY2State = newY2State;
                if (pipeHolders)
                {
                    if (currentY2State)
                        pipeHolders.ActivatePipeHoldersCW();
                    else
                        pipeHolders.DeactivatePipeHoldersCW();
                }
            }

            // Y3 비트 추출 및 상태 변경 감지
            bool newY3State = ((y0ToYF >> 3) & 1) == 1;
            if (newY3State != currentY3State)
            {
                currentY3State = newY3State;
                if (pipeHolders)
                {
                    if (currentY3State)
                        pipeHolders.ActivatePipeHoldersCCW();
                    else
                        pipeHolders.DeactivatePipeHoldersCCW();
                }
            }

            // Y4 비트 추출 및 상태 변경 감지
            bool newY4State = ((y0ToYF >> 4) & 1) == 1;
            if (newY4State != currentY4State)
            {
                currentY4State = newY4State;
                if (zLift)
                {
                    if (currentY4State)
                        zLift.ActivateZLiftUp();
                    else
                        zLift.DeactivateZLiftUp();
                }
            }
            // Y5 비트 추출 및 상태 변경 감지
            bool newY5State = ((y0ToYF >> 5) & 1) == 1;
            if (newY5State != currentY5State)
            {
                currentY5State = newY5State;
                if (zLift)
                {
                    if (currentY5State)
                        zLift.ActivateZLiftDown();
                    else
                        zLift.DeactivateZLiftDown();
                }
            }
        }
        else // 읽기 실패 시
            Debug.LogWarning($"Manager.cs: PLC 상태 읽기 실패! 에러 코드: {iRet}.");
    }








    /*------------------- ManagerRead.cs에 구현해놓음-------------------*/
    /// <summary>
    /// PLC 'X' 디바이스(워드)에 값 쓰기
    /// </summary>
    /// <param name="valueToWrite">X0에 쓸 16비트(1워드) 정수 값</param>
    //private void WriteDevice() 
    //{
    //    short value1 = 1;
    //    short value0 = 0;

    //    if (Cube.GetComponent<Trigger>().TriggerSensor)
    //    {
    //        mxComponent.WriteDeviceRandom2("X0", 1, ref value1);
    //    }
    //    else
    //    {
    //        mxComponent.WriteDeviceRandom2("X0", 1, ref value0 );
    //    }
    //}
    //void OnApplicationQuit()
    //{
    //    // 애플리케이션 종료 시 PLC 연결 해제
    //    if (mxComponent != null) 
    //    {
    //        int iRet = mxComponent.Close();
    //        if (iRet == 0)
    //            Debug.Log("Manager.cs: PLC 연결 해제 성공.");
    //        else
    //            Debug.LogError($"Manager.cs: PLC 연결 해제 실패! 에러 코드: {iRet}");
    //    }
    //}
}
