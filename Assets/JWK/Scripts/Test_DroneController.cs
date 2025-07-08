using UnityEngine;
using System.Collections;
using WebSocketSharp;
using System;
using SimpleJSON;

// --- 데이터 구조 클래스 (이 파일 내에서만 사용) ---
// Test_DroneController가 서버와 통신할 때 사용할 데이터 형식을 정의합니다.
[System.Serializable]
public class TestDronePositionData
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class TestDroneStatusData
{
    public TestDronePositionData position;
    public string payload_type;
}


// --- 테스트용 드론 컨트롤러 주 클래스 ---
public class Test_DroneController : MonoBehaviour
{
    private Rigidbody rb;

    [Header("드론 기본 성능")]
    public float hoverForce = 70.0f;
    public float moveForce = 15.0f;
    public float maxRotationTorque = 15.0f;

    [Header("PD 제어 계수 (안정성 튜닝)")]
    public float Kp_position = 3.0f;
    public float Kp_altitude = 2.0f;
    public float Kd_altitude = 2.5f;
    public float Kp_rotation = 0.8f;
    public float Kd_rotation = 0.3f;

    [Header("임무 설정")]
    public Transform droneStationLocation;

    public enum PayloadType { None, FireExtinguishingBomb, RescueEquipment }
    [Header("페이로드")]
    public PayloadType currentPayload = PayloadType.None;

    // --- 내부 변수 ---
    private Vector3 targetPosition;
    private bool hasTarget = false;
    private TestDroneStatusData statusData; // 메모리 최적화를 위해 재사용

    // --- 웹소켓 ---
    private WebSocket ws;
    private string serverUrl = "ws://192.168.0.84:5000/socket.io/?EIO=4&transport=websocket&type=unity_test";

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb)
        {
            Debug.LogError("[Test Drone] Rigidbody component not found!");
            enabled = false;
            return;
        }

        rb.useGravity = false;
        rb.angularDamping = 2.5f;
        
        targetPosition = transform.position;
        // 상태 데이터 객체를 한 번만 생성하여 재사용
        statusData = new TestDroneStatusData();
        statusData.position = new TestDronePositionData();


        ConnectWebSocket();
        StartCoroutine(SendStatusRoutine());
        if (UnityMainThreadDispatcher.Instance() == null)
        {
            Debug.LogWarning("UnityMainThreadDispatcher is not in the scene. Creating one.");
        }
    }

    void FixedUpdate()
    {
        if (!rb) return;

        // 1. 중력 수동 적용
        rb.AddForce(Physics.gravity, ForceMode.Acceleration);

        // 2. 고도 제어 (PD 컨트롤러)
        float altitudeError = targetPosition.y - transform.position.y;
        float verticalVelocity = rb.linearVelocity.y;
        float pForceAlt = altitudeError * Kp_altitude;
        float dForceAlt = -verticalVelocity * Kd_altitude;
        float upwardForce = Physics.gravity.magnitude + pForceAlt + dForceAlt;
        rb.AddForce(Vector3.up * Mathf.Clamp(upwardForce, 0, hoverForce * 2.0f), ForceMode.Acceleration);


        // 3. 수평 이동 및 회전 제어
        if (hasTarget && Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(targetPosition.x, 0, targetPosition.z)) > 1.0f)
        {
            Vector3 directionOnPlane = (new Vector3(targetPosition.x, 0, targetPosition.z) - new Vector3(transform.position.x, 0, transform.position.z)).normalized;
            
            Vector3 desiredVelocity = directionOnPlane * moveForce;
            Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            Vector3 forceNeeded = (desiredVelocity - currentVelocity) * Kp_position;
            rb.AddForce(forceNeeded, ForceMode.Acceleration);

            // 회전
            if (directionOnPlane.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionOnPlane);
                float angleErrorY = Mathf.DeltaAngle(rb.rotation.eulerAngles.y, targetRotation.eulerAngles.y);
                float pTorque = angleErrorY * Mathf.Deg2Rad * Kp_rotation;
                float dTorque = -rb.angularVelocity.y * Kd_rotation;
                rb.AddTorque(Vector3.up * Mathf.Clamp(pTorque + dTorque, -maxRotationTorque, maxRotationTorque), ForceMode.Acceleration);
            }
        }
        else // 목표 도착 시 수평 속도 감쇠
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.9f, rb.linearVelocity.y, rb.linearVelocity.z * 0.9f);
            rb.angularVelocity = new Vector3(rb.angularVelocity.x, rb.angularVelocity.y * 0.9f, rb.angularVelocity.z);
        }
    }

    public void MoveToTarget(Vector3 newTarget)
    {
        this.targetPosition = newTarget;
        this.hasTarget = true;
        Debug.Log($"[Test Drone] New target set to: {newTarget}");
    }

    #region WebSocket Communication
    void ConnectWebSocket()
    {
        ws = new WebSocket(serverUrl);
        
        // 수정된 부분: OnOpen 이벤트 핸들러
        ws.OnOpen += (sender, e) => {
            Debug.Log("[Test Drone] WebSocket Connected!");
            // Socket.IO 프로토콜에 따라 연결 요청 메시지("40")를 보냅니다.
            // 이렇게 해야 서버가 이 클라이언트를 완전히 인식합니다.
            ws.Send("40"); 
        };

        ws.OnMessage += OnWebSocketMessage;
        ws.OnError += (sender, e) => Debug.LogError("[Test Drone] WebSocket Error: " + e.Message);
        ws.OnClose += (sender, e) => {
             Debug.Log($"[Test Drone] WebSocket Closed. Code: {e.Code}, Reason: {e.Reason}");
             // 연결이 끊어졌을 때 재연결 시도 (선택적)
             if(this.gameObject.activeInHierarchy) StartCoroutine(ReconnectWebSocket());
        };
        ws.Connect();
    }

    void OnWebSocketMessage(object sender, MessageEventArgs e)
    {
        if (e.Data.StartsWith("42"))
        {
            string jsonString = e.Data.Substring(2);
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                try
                {
                    JSONNode node = JSON.Parse(jsonString);
                    string eventName = node[0].Value;
                    JSONNode eventData = node[1];

                    if (eventName == "dispatch_command")
                    {
                        JSONNode coords = eventData["coordinates"];
                        Vector3 newTarget = new Vector3(coords["x"].AsFloat, coords["y"].AsFloat, coords["z"].AsFloat);
                        MoveToTarget(newTarget);
                    }
                    else if (eventName == "force_return_command")
                    {
                        if (droneStationLocation) MoveToTarget(droneStationLocation.position);
                    }
                    else if (eventName == "change_payload_command")
                    {
                        if (System.Enum.TryParse(eventData["payload"].Value, out PayloadType newPayload))
                        {
                            currentPayload = newPayload;
                            Debug.Log($"[Test Drone] Payload changed to: {currentPayload}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Test Drone] Error parsing JSON: {ex.Message} - Data: {jsonString}");
                }
            });
        }
    }

    IEnumerator ReconnectWebSocket()
    {
        yield return new WaitForSeconds(5f);
        if(ws == null || !ws.IsAlive)
        {
            Debug.Log("[Test Drone] Attempting to reconnect...");
            ConnectWebSocket();
        }
    }

    IEnumerator SendStatusRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.2f);
            if (ws != null && ws.IsAlive)
            {
                // 값만 업데이트하여 가비지 생성 최소화
                statusData.position.x = transform.position.x;
                statusData.position.y = transform.position.y;
                statusData.position.z = transform.position.z;
                statusData.payload_type = currentPayload.ToString();
                
                string statusJson = JsonUtility.ToJson(statusData);

                string message = "42[\"unity_test_drone_data\"," + statusJson + "]";
                ws.Send(message);
            }
        }
    }

    void OnDestroy()
    {
        if (ws != null)
        {
            ws.Close();
        }
    }
    #endregion
}