using ActUtlType64Lib;
using System;
using System.Collections.Concurrent; // 스레드로부터 안전한 큐 사용
using System.Threading;
using UnityEngine;

public class ActUtlManager : MonoBehaviour
{
    private int logicalStationNumber = 1;

    private ActUtlType64 mxComponent;
    private Thread plcReadWriteThread;
    private volatile bool shutDownInitiated = false; // 스레드 종료 플래그
    private volatile bool isConnected = false; // PLC 연결 상태

    // Unity에서 PLC로 보낼 명령 큐 (예: "SET_X0_1", "SET_X1_0")
    private ConcurrentQueue<string> sendCommandQueue = new ConcurrentQueue<string>();
    // PLC로부터 읽은 데이터 큐 (예: "Y0:1", "Y1:0", "Y0YF:1234")
    private ConcurrentQueue<string> receivedDataQueue = new ConcurrentQueue<string>();

    // 외부에서 연결 상태나 수신 데이터를 알 수 있도록 이벤트 정의
    public static event Action<bool> OnConnectionStatusChanged;
    public static event Action<string> OnPlcDataReceived; // PLC로부터 원시 데이터 수신 시

    void Awake()
    {
        Application.quitting += OnApplicationQuitting; // 앱 종료 시 스레드 안전 종료
    }

    void Start()
    {
        // PLC 통신 스레드 시작
        plcReadWriteThread = new Thread(PlcCommunicationLoop);
        plcReadWriteThread.IsBackground = true; // Unity 앱 종료 시 스레드도 종료되도록
        plcReadWriteThread.Start();
    }

    void Update()
    {
        // 메인 Unity 스레드에서 수신된 PLC 데이터 처리
        while (receivedDataQueue.TryDequeue(out string data))
        {
            OnPlcDataReceived?.Invoke(data); // 데이터를 구독자에게 전달
        }
    }

    /// <summary>
    /// PLC 통신을 위한 백그라운드 스레드 루프
    /// </summary>
    private void PlcCommunicationLoop()
    {
        while (!shutDownInitiated)
        {
            try
            {
                // 1. PLC 연결 시도 (연결이 끊어졌거나 초기 상태일 때)
                if (!isConnected)
                {
                    mxComponent = new ActUtlType64();
                    mxComponent.ActLogicalStationNumber = logicalStationNumber;
                    int iRet = mxComponent.Open(); // 동기 호출, 스레드는 여기서 블로킹됨

                    if (iRet == 0)
                    {
                        isConnected = true;
                        Debug.Log("ActUtlManager: PLC 연결 성공!");
                        OnConnectionStatusChanged?.Invoke(true);
                    }
                    else
                    {
                        isConnected = false;
                        Debug.LogError($"ActUtlManager: PLC 연결 실패! 에러 코드: {iRet}. 논리 스테이션 번호 확인 요망.");
                        OnConnectionStatusChanged?.Invoke(false);
                        // 연결 실패 시 잠시 대기 후 재시도
                        Thread.Sleep(3000); // 3초 대기
                        continue; // 다음 루프에서 다시 연결 시도
                    }
                }

                // 2. PLC로부터 Y 디바이스 상태 읽기
                int blockCnt = 1; // Y0부터 1워드(16비트) 읽기
                int[] data = new int[blockCnt];
                int readRet = mxComponent.ReadDeviceBlock("Y0", blockCnt, out data[0]); // 동기 호출

                if (readRet == 0)
                {
                    // Y0부터 YF까지의 비트 상태를 나타내는 워드 값
                    // 이 값을 문자열로 변환하여 메인 스레드로 전달
                    receivedDataQueue.Enqueue($"Y0YF:{data[0]}");
                }
                // 3. Unity에서 보낼 명령 처리 (X 디바이스 쓰기 등)
                while (sendCommandQueue.TryDequeue(out string command))
                {
                    if (command.StartsWith("X"))
                    {
                        // "X0:1" 또는 "X1:0" 형태의 명령 파싱
                        string[] parts = command.Split(':');
                        if (parts.Length == 2 && short.TryParse(parts[1], out short value))
                        {
                            int writeRet = mxComponent.SetDevice(parts[0], value); // 동기 호출
                        }
                    }
                    // 여기에 다른 유형의 명령 처리 로직을 추가할 수 있습니다.
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"ActUtlManager: PLC 통신 스레드 오류: {e.Message}");
                isConnected = false; // 오류 발생 시 연결 끊김으로 간주하여 재연결 시도
                OnConnectionStatusChanged?.Invoke(false);
                // 오류 발생 시 잠시 대기 후 재시도
                Thread.Sleep(3000); // 3초 대기
            }

            // 너무 빨리 루프가 돌지 않도록 잠시 대기 (CPU 점유율 감소)
            Thread.Sleep(10); // 50ms 마다 PLC와 통신 시도 (조절 가능)
        }

        // 스레드 종료 전 PLC 연결 해제
        if (mxComponent != null && isConnected)
        {
            int closeRet = mxComponent.Close();
            if (closeRet == 0)
                Debug.Log("ActUtlManager: PLC 연결 해제 성공.");
            else
                Debug.LogError($"ActUtlManager: PLC 연결 해제 실패! 에러 코드: {closeRet}");
        }
        Debug.Log("ActUtlManager: PLC 통신 스레드 종료.");
    }

    /// <summary>
    /// PLC로 명령을 보낼 때 사용하는 public 메서드 (다른 스크립트에서 호출)
    /// 이 메서드는 스레드로부터 안전합니다.
    /// </summary>
    /// <param name="command">전송할 명령 문자열 (예: "X0:1")</param>
    public void SendCommandToPlc(string command)
    {
        if (isConnected)
        {
            sendCommandQueue.Enqueue(command); 
        }
    }

    /// <summary>
    /// 애플리케이션 종료 시 호출되어 스레드를 안전하게 종료
    /// </summary>
    private void OnApplicationQuitting()
    {
        shutDownInitiated = true; // 스레드 루프 종료 신호
        if (plcReadWriteThread != null && plcReadWriteThread.IsAlive)
        {
            plcReadWriteThread.Join(1000); // 스레드가 종료될 때까지 최대 1초 대기
        }
        Debug.Log("ActUtlManager: OnApplicationQuitting 처리 완료.");
    }

    void OnDestroy()
    {
        Application.quitting -= OnApplicationQuitting;
        if (!shutDownInitiated)
        {
            OnApplicationQuitting();
        }
    }
}