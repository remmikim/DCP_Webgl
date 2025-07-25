using UnityEngine;
using WebSocketSharp;
using System.Collections.Generic;

// JSON 데이터 파싱을 위한 클래스들
[System.Serializable]
public class CommandData
{
    public string command;
    public bool isActive;
}

[System.Serializable]
public class SensorData
{
    public string sensorName;
    public bool isActive;
}

public class Manager : MonoBehaviour
{
    private WebSocket ws;
    private readonly Queue<CommandData> commandQueue = new Queue<CommandData>();

    // ======= Class 불러오기 ============
    public PipeHolders pipeHolders;
    public ZLiftTrigger zLift;
    public ManagerWrite managerWrite; // ManagerWrite 참조

    void Start()
    {
        string url = "ws://127.0.0.1:5000/socket.io/?EIO=4&transport=websocket&type=unity_test";
        ws = new WebSocket(url);

        ws.OnOpen += (sender, e) => Debug.Log("Manager.cs: WebSocket 연결 성공!");

        ws.OnMessage += (sender, e) =>
        {
            Debug.Log("Manager.cs: 메시지 수신: " + e.Data);
            if (e.Data != null && e.Data.StartsWith("42") && e.Data.Contains("dispatch_command"))
            {
                int jsonStartIndex = e.Data.IndexOf('{');
                int jsonEndIndex = e.Data.LastIndexOf('}');
                if (jsonStartIndex != -1 && jsonEndIndex != -1)
                {
                    string jsonData = e.Data.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);
                    CommandData receivedCommand = JsonUtility.FromJson<CommandData>(jsonData);
                    lock (commandQueue)
                    {
                        commandQueue.Enqueue(receivedCommand);
                    }
                }
            }
        };

        ws.OnError += (sender, e) => Debug.LogError("Manager.cs: WebSocket 에러 발생! " + e.Message);
        ws.OnClose += (sender, e) => Debug.Log("Manager.cs: WebSocket 연결 해제.");
        
        Debug.Log("Manager.cs: WebSocket 서버에 연결을 시도합니다...");
        ws.ConnectAsync();
    }

    void Update()
    {
        // 수신된 명령 처리
        lock (commandQueue)
        {
            while (commandQueue.Count > 0)
            {
                CommandData cmd = commandQueue.Dequeue();
                ProcessCommand(cmd);
            }
        }

        // ManagerWrite를 통해 센서 데이터 전송
        if (managerWrite != null)
        {
            managerWrite.WriteDevice();
        }
    }

    /// <summary>
    /// ManagerWrite에서 호출할 데이터 전송 메소드
    /// </summary>
    public void SendSensorData(string sensorName, bool isActive)
    {
        if (ws == null || !ws.IsAlive) return;

        SensorData sensorData = new SensorData { sensorName = sensorName, isActive = isActive };
        string jsonData = JsonUtility.ToJson(sensorData);

        // Socket.IO 형식에 맞춰 메시지 전송: 42["event_name", payload]
        string message = $"42[\"unity_test_drone_data\",{jsonData}]";
        
        Debug.Log($"서버로 센서 데이터 전송: {message}");
        ws.Send(message);
    }

    private void ProcessCommand(CommandData cmd)
    {
        if (cmd == null) return;
        Debug.Log($"명령 처리: {cmd.command}, 활성 상태: {cmd.isActive}");

        switch (cmd.command)
        {
            case "PipeHoldersCW":
                if (pipeHolders)
                {
                    if (cmd.isActive) pipeHolders.ActivatePipeHoldersCW();
                    else pipeHolders.DeactivatePipeHoldersCW();
                }
                break;
            case "PipeHoldersCCW":
                if (pipeHolders)
                {
                    if (cmd.isActive) pipeHolders.ActivatePipeHoldersCCW();
                    else pipeHolders.DeactivatePipeHoldersCCW();
                }
                break;
            case "ZLiftUp":
                if (zLift)
                {
                    if (cmd.isActive) zLift.ActivateZLiftUp();
                    else zLift.DeactivateZLiftUp();
                }
                break;
            case "ZLiftDown":
                if (zLift)
                {
                    if (cmd.isActive) zLift.ActivateZLiftDown();
                    else zLift.DeactivateZLiftDown();
                }
                break;
            default:
                Debug.LogWarning($"알 수 없는 명령입니다: {cmd.command}");
                break;
        }
    }

    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }
    }
}
