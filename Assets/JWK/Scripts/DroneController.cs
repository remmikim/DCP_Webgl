// C:\DroneControlProject\UnityDroneSimulator\Assets\Scripts\DroneController.cs

using UnityEngine;
using System.Collections;
using WebSocketSharp;
using System;
using SimpleJSON;

// --- 드론 컨트롤러 주 클래스 ---
public class DroneController : MonoBehaviour
{
    private Rigidbody rb;
    [Header("드론 기본 성능")]
    public float hoverForce = 70.0f;
    public float moveForce = 15.0f;
    
    // --- 페이로드 및 임무 상태 Enum ---
    public enum PayloadType
    {
        None,
        FireExtinguishingBomb,
        RescueEquipment,
        DisasterReliefBag,
        AluminumSplint
    }

    public enum DroneMissionState
    {
        IdleAtStation, TakingOff, MovingToTarget, PerformingAction, ReturningToStation, Landing, EmergencyReturn, HoldingPosition
    }
    
    [Header("임무 상태 및 페이로드")]
    public DroneMissionState currentMissionState = DroneMissionState.IdleAtStation;
    public PayloadType currentPayload = PayloadType.FireExtinguishingBomb;

    [Header("임무 설정")]
    public Transform droneStationLocation;
    public GameObject bombPrefab;
    public int totalBombs = 6;
    private int currentBombLoad;
    private int fireScaleBombCount;

    public float missionCruisingAGL = 50.0f;
    private Vector3 currentTargetPosition_xz;
    public float arrivalDistanceThreshold = 2.0f;
    public float bombDropInterval = 3.0f;
    public Vector3 bombSpawnOffset = new Vector3(0f, -2.0f, 0.5f);
    public float preActionStabilizationTime = 3.0f;

    [Header("Inspector 테스트용 임무")]
    public Transform testDispatchTarget;
    public string testMissionType = "정찰";
    
    [Header("고도 제어 (PD & AGL)")]
    public float targetAltitude_abs;
    public float Kp_altitude = 2.0f;
    public float Kd_altitude = 2.5f;
    public float landingDescentRate = 0.4f;
    public float terrainCheckDistance = 200.0f;
    public LayerMask groundLayerMask;
    private float currentGroundY_AGL;

    [Header("자율 이동 및 회전 개선")]
    public float Kp_rotation = 0.8f;
    public float Kd_rotation = 0.3f;
    public float turnBeforeMoveAngleThreshold = 15.0f;
    public float decelerationStartDistanceXZ = 15.0f;
    public float maxRotationTorque = 15.0f;

    [Header("웹소켓")]
    private WebSocket ws;
    private string serverUrl = "ws://192.168.0.133:5000/socket.io/?EIO=4&transport=websocket&type=unity";

    [Header("드론 실시간 상태")]
    public float currentAltitude_abs;
    public Vector3 currentPosition_abs;
    public float batteryLevel = 100.0f;

    private bool isTakingOff_subState = false;
    private bool isLanding_subState = false;
    private bool isPhysicallyStopped = true;
    private Coroutine actionCoroutine = null;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) {
            Debug.LogError("[Unity] Rigidbody component not found!");
            enabled = false;
            return;
        }

        rb.useGravity = false;
        rb.angularDamping = 2.5f;

        ConnectWebSocket();
        StartCoroutine(SendDroneDataRoutine());
        UnityMainThreadDispatcher.Instance();

        if (droneStationLocation != null)
        {
            transform.position = droneStationLocation.position;
            transform.rotation = droneStationLocation.rotation;
        }
        else
        {
            Debug.LogError("[Mission] Drone Station Location (LandingPad) not assigned in Inspector!");
        }

        PerformInitialGroundCheckAndSetAltitude();
        currentMissionState = DroneMissionState.IdleAtStation;
        isPhysicallyStopped = true;
        currentBombLoad = totalBombs;
    }
    
    void Update()
    {
        UpdateDroneInternalStatus();
        UpdateTerrainSensingAndDynamicTargetAltitude();

        switch (currentMissionState)
        {
            case DroneMissionState.IdleAtStation: Handle_IdleAtStation(); break;
            case DroneMissionState.TakingOff: Handle_TakingOff(); break;
            case DroneMissionState.MovingToTarget: Handle_MovingToTarget(); break;
            case DroneMissionState.PerformingAction: break; // 코루틴이 제어
            case DroneMissionState.ReturningToStation: Handle_MovingToTarget(); break;
            case DroneMissionState.EmergencyReturn: Handle_MovingToTarget(); break;
            case DroneMissionState.HoldingPosition: Handle_HoldingPosition(); break;
            case DroneMissionState.Landing: Handle_Landing(); break;
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        ApplyForcesBasedOnState();
    }
    
    // --- Public 함수 (Inspector 버튼용) ---
    public void DispatchMissionFromInspector()
    {
        if (currentMissionState != DroneMissionState.IdleAtStation)
        {
            Debug.LogWarning("[Mission] Drone is busy. Cannot dispatch new mission from Inspector.");
            return;
        }
        if (testDispatchTarget == null)
        {
            Debug.LogError("[Mission] Test Dispatch Target is not set in Inspector!");
            return;
        }
        
        StartMission(testDispatchTarget.position, testMissionType);
        
        DispatchData dispatchData = new DispatchData(testMissionType, testDispatchTarget.position);
        string dispatchJson = JsonUtility.ToJson(dispatchData);
        string socketMessage = "42[\"unity_dispatch_mission\"," + dispatchJson + "]";
        
        if (ws != null && ws.IsAlive)
        {
            ws.Send(socketMessage);
            Debug.Log($"[Server] Sent dispatch notification to server: {testMissionType}");
        }
    }

    // --- 초기화 및 업데이트 함수 ---
    void PerformInitialGroundCheckAndSetAltitude()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, terrainCheckDistance, groundLayerMask))
        {
            currentGroundY_AGL = hit.point.y;
        }
        else
        {
            currentGroundY_AGL = transform.position.y;
        }
        targetAltitude_abs = transform.position.y;
    }

    void UpdateTerrainSensingAndDynamicTargetAltitude()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, terrainCheckDistance, groundLayerMask))
        {
            currentGroundY_AGL = hit.point.y;
        }

        if (currentMissionState != DroneMissionState.TakingOff && currentMissionState != DroneMissionState.Landing)
        {
            targetAltitude_abs = currentGroundY_AGL + missionCruisingAGL;
        }
    }
    
    void UpdateDroneInternalStatus()
    {
        currentPosition_abs = transform.position;
        currentAltitude_abs = currentPosition_abs.y;
        if (currentMissionState != DroneMissionState.IdleAtStation || !isPhysicallyStopped)
        {
            batteryLevel = Mathf.Max(0, batteryLevel - Time.deltaTime * 0.05f);
        }
    }

    // --- 상태 처리 핸들러 ---
    void Handle_IdleAtStation() { /* 웹소켓 메시지 대기 */ }

    void Handle_TakingOff()
    {
        if (!isTakingOff_subState && currentAltitude_abs >= targetAltitude_abs - 0.2f)
        {
            currentMissionState = DroneMissionState.MovingToTarget;
        }
    }

    void Handle_MovingToTarget()
    {
        Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
        if (Vector3.Distance(currentPosXZ, currentTargetPosition_xz) < arrivalDistanceThreshold)
        {
            if (currentMissionState == DroneMissionState.MovingToTarget)
            {
                currentMissionState = DroneMissionState.PerformingAction;
                if (actionCoroutine != null) StopCoroutine(actionCoroutine);
                actionCoroutine = StartCoroutine(PerformActionCoroutine());
            }
            else if (currentMissionState == DroneMissionState.ReturningToStation || currentMissionState == DroneMissionState.EmergencyReturn)
            {
                currentMissionState = DroneMissionState.Landing;
                isLanding_subState = true;
                if (droneStationLocation != null)
                {
                    targetAltitude_abs = droneStationLocation.position.y;
                }
            }
        }
    }

    void Handle_HoldingPosition() { /* 제자리 유지 */ }

    void Handle_Landing()
    {
        if (!isLanding_subState && isPhysicallyStopped && Mathf.Abs(currentAltitude_abs - targetAltitude_abs) < 0.15f)
        {
            currentMissionState = DroneMissionState.IdleAtStation;
            currentBombLoad = totalBombs;
            PerformInitialGroundCheckAndSetAltitude();
            if (droneStationLocation != null)
            {
                transform.rotation = droneStationLocation.rotation;
            }
        }
    }

    // --- 코루틴 ---
    IEnumerator PerformActionCoroutine()
    {
        yield return new WaitForSeconds(preActionStabilizationTime);

        if (currentPayload == PayloadType.FireExtinguishingBomb)
        {
            int bombsToDrop = Mathf.Min(fireScaleBombCount, currentBombLoad);
            for (int i = 0; i < bombsToDrop; i++)
            {
                if (bombPrefab != null)
                {
                    Instantiate(bombPrefab, transform.TransformPoint(bombSpawnOffset), Quaternion.identity);
                    currentBombLoad--;
                }
                if (i < bombsToDrop - 1) yield return new WaitForSeconds(bombDropInterval);
            }
        }
        else
        {
            Debug.Log($"[Mission] Performing action for payload: {currentPayload}.");
            yield return new WaitForSeconds(2.0f);
        }
        
        actionCoroutine = null;

        if (droneStationLocation != null)
        {
            currentTargetPosition_xz = new Vector3(droneStationLocation.position.x, 0, droneStationLocation.position.z);
            currentMissionState = DroneMissionState.ReturningToStation;
        }
        else
        {
            currentMissionState = DroneMissionState.HoldingPosition;
        }
    }
    
    // --- 물리 제어 ---
    void ApplyForcesBasedOnState()
    {
        rb.AddForce(Physics.gravity, ForceMode.Acceleration);

        if (isTakingOff_subState)
        {
            if (currentAltitude_abs < targetAltitude_abs - 0.1f)
            {
                float altError = targetAltitude_abs - currentAltitude_abs;
                float pForce = altError * Kp_altitude;
                float dForce = -rb.linearVelocity.y * Kd_altitude;
                float upwardForce = Physics.gravity.magnitude + pForce + dForce;
                rb.AddForce(Vector3.up * Mathf.Clamp(upwardForce, Physics.gravity.magnitude * 0.5f, hoverForce * 1.5f), ForceMode.Acceleration);
            }
            else
            {
                isTakingOff_subState = false;
            }
        }
        else if (isLanding_subState)
        {
            if (currentAltitude_abs > targetAltitude_abs + 0.05f)
            {
                float descentRate = landingDescentRate;
                if (currentAltitude_abs < targetAltitude_abs + 2.0f) descentRate *= 0.5f;
                float upwardThrust = Mathf.Max(0, Physics.gravity.magnitude - descentRate);
                rb.AddForce(Vector3.up * upwardThrust, ForceMode.Acceleration);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.8f, rb.linearVelocity.y, rb.linearVelocity.z * 0.8f);
                rb.angularVelocity *= 0.8f;
            }
            else
            {
                isLanding_subState = false;
                isPhysicallyStopped = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                if (droneStationLocation != null)
                {
                    transform.position = droneStationLocation.position;
                    transform.rotation = droneStationLocation.rotation;
                }
            }
        }
        else if (currentMissionState == DroneMissionState.MovingToTarget || currentMissionState == DroneMissionState.ReturningToStation || currentMissionState == DroneMissionState.EmergencyReturn)
        {
            float altError = targetAltitude_abs - currentAltitude_abs;
            float pForceAlt = altError * Kp_altitude;
            float dForceAlt = -rb.linearVelocity.y * Kd_altitude;
            float totalVertForce = Physics.gravity.magnitude + pForceAlt + dForceAlt;
            rb.AddForce(Vector3.up * Mathf.Clamp(totalVertForce, 0.0f, hoverForce * 2.0f), ForceMode.Acceleration);

            Vector3 targetDirectionOnPlane = (new Vector3(currentTargetPosition_xz.x, 0, currentTargetPosition_xz.z) - new Vector3(transform.position.x, 0, transform.position.z)).normalized;
            float distanceToTargetXZ = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), currentTargetPosition_xz);

            if (distanceToTargetXZ > arrivalDistanceThreshold)
            {
                float effectiveMoveForce = moveForce;
                if (distanceToTargetXZ < decelerationStartDistanceXZ)
                {
                    effectiveMoveForce = Mathf.Lerp(moveForce * 0.2f, moveForce, distanceToTargetXZ / decelerationStartDistanceXZ);
                }

                Vector3 desiredVelocityXZ = targetDirectionOnPlane * effectiveMoveForce;
                Quaternion targetRotation = targetDirectionOnPlane != Vector3.zero ? Quaternion.LookRotation(targetDirectionOnPlane) : transform.rotation;
                if (Quaternion.Angle(transform.rotation, targetRotation) > turnBeforeMoveAngleThreshold)
                {
                    desiredVelocityXZ *= 0.2f;
                }
                
                Vector3 forceNeededXZ = (desiredVelocityXZ - new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z)) * 3.0f;
                rb.AddForce(forceNeededXZ, ForceMode.Acceleration);

                float targetAngleY = Mathf.Atan2(targetDirectionOnPlane.x, targetDirectionOnPlane.z) * Mathf.Rad2Deg;
                float angleErrorY = Mathf.DeltaAngle(rb.rotation.eulerAngles.y, targetAngleY);
                float pTorque = angleErrorY * Mathf.Deg2Rad * Kp_rotation;
                float dTorque = -rb.angularVelocity.y * Kd_rotation;
                rb.AddTorque(Vector3.up * Mathf.Clamp(pTorque + dTorque, -maxRotationTorque, maxRotationTorque), ForceMode.Acceleration);
            }
        }
        else if (!isPhysicallyStopped) // Holding or Performing
        {
            float altError = targetAltitude_abs - currentAltitude_abs;
            float pForceAlt = altError * Kp_altitude;
            float dForceAlt = -rb.linearVelocity.y * Kd_altitude;
            float totalVertForce = Physics.gravity.magnitude + pForceAlt + dForceAlt;
            rb.AddForce(Vector3.up * Mathf.Clamp(totalVertForce, 0.0f, hoverForce * 2.0f), ForceMode.Acceleration);

            float dampingFactor = 0.8f;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * dampingFactor, rb.linearVelocity.y, rb.linearVelocity.z * dampingFactor);
            rb.angularVelocity *= dampingFactor;
        }
    }

    // --- 웹소켓 및 임무 처리 ---
    void StartMission(Vector3 targetPosition, string missionDescription)
    {
        Debug.Log($"[Mission] Starting: {missionDescription}! Target: {targetPosition}");
        currentTargetPosition_xz = new Vector3(targetPosition.x, 0, targetPosition.z);
        
        RaycastHit hit;
        Vector3 takeoffRefPos = droneStationLocation != null ? droneStationLocation.position : transform.position;
        if (Physics.Raycast(takeoffRefPos + Vector3.up, Vector3.down, out hit, terrainCheckDistance, groundLayerMask))
        {
            targetAltitude_abs = hit.point.y + missionCruisingAGL;
        }
        else
        {
            targetAltitude_abs = takeoffRefPos.y + missionCruisingAGL;
        }
        
        currentMissionState = DroneMissionState.TakingOff;
        isTakingOff_subState = true;
        isPhysicallyStopped = false;
        currentBombLoad = totalBombs;
    }

    void ConnectWebSocket()
    {
        ws = new WebSocket(serverUrl);
        ws.OnOpen += (sender, e) => { 
            Debug.Log("[Unity] WebSocket Connected!");
            ws.Send("40"); 
        };
        ws.OnMessage += OnWebSocketMessage;
        ws.OnError += (sender, e) => Debug.LogError("[Unity] WebSocket Error: " + e.Message);
        ws.OnClose += (sender, e) => { if (this != null && gameObject.activeInHierarchy) StartCoroutine(ReconnectWebSocket()); };
        ws.Connect();
    }

    IEnumerator ReconnectWebSocket()
    {
        yield return new WaitForSeconds(5f);
        if (ws == null || !ws.IsAlive) ConnectWebSocket();
    }
    
    IEnumerator SendDroneDataRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.2f);
            if (ws != null && ws.IsAlive)
            {
                DroneStatusData dataToSend = new DroneStatusData(
                    currentPosition_abs,
                    currentAltitude_abs,
                    batteryLevel,
                    currentMissionState.ToString(),
                    currentPayload.ToString(),
                    currentBombLoad
                );
                string droneDataJson = JsonUtility.ToJson(dataToSend);
                string socketIOMessage = "42[\"unity_drone_data\"," + droneDataJson + "]";
                ws.Send(socketIOMessage);
            }
        }
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

                    if (eventName == "change_payload_command")
                    {
                        if (currentMissionState == DroneMissionState.IdleAtStation)
                        {
                            if (System.Enum.TryParse(eventData["payload"].Value, out PayloadType newPayload))
                            {
                                currentPayload = newPayload;
                                Debug.Log($"[Mission] Payload changed to: {currentPayload}");
                            }
                        }
                    }
                    else if (eventName == "wildfire_alert_command")
                    {
                        // ----- 수정된 부분 시작 -----
                        if (currentMissionState == DroneMissionState.IdleAtStation)
                        {
                            Vector3 targetPos = new Vector3(eventData["coordinates"]["x"].AsFloat, eventData["coordinates"]["y"].AsFloat, eventData["coordinates"]["z"].AsFloat);
                            currentPayload = PayloadType.FireExtinguishingBomb;
                            fireScaleBombCount = eventData["fire_scale"].AsInt;
                            StartMission(targetPos, "산불 진압");
                        }
                        else 
                        {
                            // 드론이 바쁠 때 경고 메시지를 출력합니다.
                            Debug.LogWarning($"[Mission] Wildfire alert received, but drone is busy. Current state: {currentMissionState}");
                        }
                        // ----- 수정된 부분 끝 -----
                    }
                    else if (eventName == "force_return_command")
                    {
                        if (droneStationLocation != null)
                        {
                            currentTargetPosition_xz = new Vector3(droneStationLocation.position.x, 0, droneStationLocation.position.z);
                            currentMissionState = DroneMissionState.EmergencyReturn;
                            if (actionCoroutine != null) StopCoroutine(actionCoroutine);
                        }
                    }
                    else if (eventName == "emergency_stop_command")
                    {
                        currentMissionState = DroneMissionState.HoldingPosition;
                        if (actionCoroutine != null) StopCoroutine(actionCoroutine);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Unity] Error parsing JSON: {ex.Message} - Data: {jsonString}");
                }
            });
        }
        else if (e.Data == "2") { ws.Send("3"); }
    }
    
    void OnApplicationQuit()
    {
        if (ws != null && ws.IsAlive) ws.Close();
    }

    void OnDestroy()
    {
        if (ws != null && ws.IsAlive) ws.Close();
        StopAllCoroutines();
    }
}
