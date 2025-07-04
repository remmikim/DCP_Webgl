// C:\Unity\TeamProject\Assets\JWK\Scripts\DroneController.cs

using UnityEngine;
using System.Collections;
using WebSocketSharp;
using System;
using SimpleJSON;
using UnityEngine.Serialization;

// --- 드론 컨트롤러 주 클래스 ---
public class DroneController : MonoBehaviour
{
    #region 변수 선언 (Fields and Properties)

    // --- 내부 컴포넌트 및 상태 ---
    private Rigidbody _rb;
    private Coroutine _actionCoroutine = null; // 폭탄 투하 등 주요 행동 코루틴
    private bool _isTakingOffSubState = false;
    private bool _isLandingSubState = false;
    private bool _isPhysicallyStopped = true;

    // --- 드론 실시간 상태 ---
    [Header("드론 실시간 상태")]
    public float currentAltitude_abs;
    public Vector3 currentPosition_abs;
    public float batteryLevel = 100.0f;

    // --- 페이로드 및 임무 상태 Enum ---
    public enum PayloadType { None, FireExtinguishingBomb, RescueEquipment, DisasterReliefBag, AluminumSplint }
    public enum DroneMissionState { IdleAtStation, TakingOff, MovingToTarget, PerformingAction, ReturningToStation, Landing, EmergencyReturn, HoldingPosition }
    
    [Header("임무 상태 및 페이로드")]
    public DroneMissionState currentMissionState = DroneMissionState.IdleAtStation;
    public PayloadType currentPayload = PayloadType.FireExtinguishingBomb;

    // --- 드론 기본 성능 ---
    [Header("드론 기본 성능")]
    public float hoverForce = 70.0f;
    public float moveForce = 15.0f;

    // --- 임무 설정 ---
    [Header("임무 설정")]
    public Transform droneStationLocation;
    public GameObject bombPrefab;
    public int totalBombs = 6;
    private int _currentBombLoad;
    private int _fireScaleBombCount;
    [FormerlySerializedAs("missionCruisingAGL")] // 이전 변수명과 호환성을 위해 추가
    public float missionCruisingAgl = 50.0f;
    private Vector3 _currentTargetPositionXZ;
    public float arrivalDistanceThreshold = 2.0f;
    public float bombDropInterval = 3.0f;
    public Vector3 bombSpawnOffset = new Vector3(0f, -2.0f, 0.5f);
    public float preActionStabilizationTime = 3.0f;

    // --- Inspector 테스트용 ---
    [Header("Inspector 테스트용 임무")]
    public Transform testDispatchTarget;
    public string testMissionType = "정찰";
    
    // --- 고도 제어 (PD & AGL) ---
    [Header("고도 제어 (PD & AGL)")]
    public float targetAltitude_abs;
    public float Kp_altitude = 2.0f;
    public float Kd_altitude = 2.5f;
    public float landingDescentRate = 0.4f;
    public float terrainCheckDistance = 200.0f;
    public LayerMask groundLayerMask;
    private float currentGroundY_AGL;

    // --- 자율 이동 및 회전 개선 ---
    [Header("자율 이동 및 회전 개선")]
    public float Kp_rotation = 0.8f;
    public float Kd_rotation = 0.3f;
    public float turnBeforeMoveAngleThreshold = 15.0f;
    public float decelerationStartDistanceXZ = 15.0f;
    public float maxRotationTorque = 15.0f;

    // --- 웹소켓 ---
    private WebSocket ws;
    private string serverUrl = "ws://192.168.0.133:5000/socket.io/?EIO=4&transport=websocket&type=unity";
    
    #endregion

    #region Unity 생명주기 함수 (Lifecycle Methods)

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (!_rb) // 스타일 수정
        {
            Debug.LogError("[Unity] Rigidbody component not found!");
            enabled = false;
            return;
        }

        _rb.useGravity = false;
        _rb.angularDamping = 2.5f;

        ConnectWebSocket();
        StartCoroutine(SendDroneDataRoutine());
        UnityMainThreadDispatcher.Instance();

        if (droneStationLocation) // 스타일 수정
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
        _isPhysicallyStopped = true;
        _currentBombLoad = totalBombs;
    }
    
    void Update()
    {
        UpdateDroneInternalStatus();
        UpdateTerrainSensingAndDynamicTargetAltitude();
        RunStateMachine();
    }

    void FixedUpdate()
    {
        if (!_rb) return; // 스타일 수정
        ApplyForcesBasedOnState();
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

    #endregion

    #region 드론 임무 및 상태 관리 (Drone Mission & State Logic)

    void RunStateMachine()
    {
        switch (currentMissionState)
        {
            case DroneMissionState.IdleAtStation: Handle_IdleAtStation(); break;
            case DroneMissionState.TakingOff: Handle_TakingOff(); break;
            case DroneMissionState.MovingToTarget: Handle_MovingToTarget(); break;
            case DroneMissionState.PerformingAction: break;
            case DroneMissionState.ReturningToStation: Handle_MovingToTarget(); break;
            case DroneMissionState.EmergencyReturn: Handle_MovingToTarget(); break;
            case DroneMissionState.HoldingPosition: Handle_HoldingPosition(); break;
            case DroneMissionState.Landing: Handle_Landing(); break;
        }
    }

    void Handle_IdleAtStation() { /* 웹소켓 메시지 대기 */ }

    void Handle_TakingOff()
    {
        if (!_isTakingOffSubState && currentAltitude_abs >= targetAltitude_abs - 0.2f)
            currentMissionState = DroneMissionState.MovingToTarget;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    void Handle_MovingToTarget()
    {
        Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
        if (Vector3.Distance(currentPosXZ, _currentTargetPositionXZ) < arrivalDistanceThreshold)
        {
            if (currentMissionState == DroneMissionState.MovingToTarget)
            {
                currentMissionState = DroneMissionState.PerformingAction;
                if (_actionCoroutine != null) StopCoroutine(_actionCoroutine); // 스타일 수정
                _actionCoroutine = StartCoroutine(PerformActionCoroutine());
            }
            else if (currentMissionState == DroneMissionState.ReturningToStation || currentMissionState == DroneMissionState.EmergencyReturn)
            {
                currentMissionState = DroneMissionState.Landing;
                _isLandingSubState = true;
                if (droneStationLocation) // 스타일 수정
                {
                    targetAltitude_abs = droneStationLocation.position.y;
                }
                else
                {
                    Debug.LogError("[Mission] Cannot land, Drone Station Location is not set!");
                    targetAltitude_abs = currentGroundY_AGL;
                }
            }
        }
    }

    void Handle_HoldingPosition() { /* 제자리 유지 */ }

    void Handle_Landing()
    {
        if (!_isLandingSubState && _isPhysicallyStopped && Mathf.Abs(currentAltitude_abs - targetAltitude_abs) < 0.15f)
        {
            currentMissionState = DroneMissionState.IdleAtStation;
            _currentBombLoad = totalBombs;
            PerformInitialGroundCheckAndSetAltitude();
            if (droneStationLocation) // 스타일 수정
            {
                transform.rotation = droneStationLocation.rotation;
            }
        }
    }

    IEnumerator PerformActionCoroutine()
    {
        yield return new WaitForSeconds(preActionStabilizationTime);

        if (currentPayload == PayloadType.FireExtinguishingBomb)
        {
            int bombsToDrop = Mathf.Min(_fireScaleBombCount, _currentBombLoad);
            for (int i = 0; i < bombsToDrop; i++)
            {
                if (bombPrefab) // 스타일 수정
                {
                    Instantiate(bombPrefab, transform.TransformPoint(bombSpawnOffset), Quaternion.identity);
                    _currentBombLoad--;
                }
                if (i < bombsToDrop - 1) yield return new WaitForSeconds(bombDropInterval);
            }
        }
        else
        {
            Debug.Log($"[Mission] Performing action for payload: {currentPayload}.");
            yield return new WaitForSeconds(2.0f);
        }
        
        _actionCoroutine = null;

        if (droneStationLocation) // 스타일 수정
        {
            _currentTargetPositionXZ = new Vector3(droneStationLocation.position.x, 0, droneStationLocation.position.z);
            currentMissionState = DroneMissionState.ReturningToStation;
        }
        else
        {
            currentMissionState = DroneMissionState.HoldingPosition;
        }
    }
    
    public void DispatchMissionFromInspector()
    {
        if (currentMissionState != DroneMissionState.IdleAtStation) { Debug.LogWarning("[Mission] Drone is busy."); return; }
        if (!testDispatchTarget) { Debug.LogError("[Mission] Test Dispatch Target is not set!"); return; } // 스타일 수정
        
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
    
    void StartMission(Vector3 targetPosition, string missionDescription)
    {
        Debug.Log($"[Mission] Starting: {missionDescription}! Target: {targetPosition}");
        _currentTargetPositionXZ = new Vector3(targetPosition.x, 0, targetPosition.z);
        
        RaycastHit hit;
        Vector3 takeoffRefPos = droneStationLocation ? droneStationLocation.position : transform.position;
        
        if (Physics.Raycast(takeoffRefPos + Vector3.up, Vector3.down, out hit, terrainCheckDistance, groundLayerMask))
            targetAltitude_abs = hit.point.y + missionCruisingAgl;
        else
            targetAltitude_abs = takeoffRefPos.y + missionCruisingAgl;
        
        currentMissionState = DroneMissionState.TakingOff;
        _isTakingOffSubState = true;
        _isPhysicallyStopped = false;
        _currentBombLoad = totalBombs;
    }
    
    #endregion

    #region 드론 물리 및 상태 업데이트 (Drone Physics & Status Updates)

    void PerformInitialGroundCheckAndSetAltitude()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, terrainCheckDistance, groundLayerMask))
            currentGroundY_AGL = hit.point.y;
        
        else
            currentGroundY_AGL = transform.position.y;
        
        targetAltitude_abs = transform.position.y;
    }

    void UpdateTerrainSensingAndDynamicTargetAltitude()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, terrainCheckDistance, groundLayerMask))
            currentGroundY_AGL = hit.point.y;

        if (currentMissionState != DroneMissionState.TakingOff && currentMissionState != DroneMissionState.Landing)
            targetAltitude_abs = currentGroundY_AGL + missionCruisingAgl;
    }
    
    void UpdateDroneInternalStatus()
    {
        currentPosition_abs = transform.position;
        currentAltitude_abs = currentPosition_abs.y;
        
        if (currentMissionState != DroneMissionState.IdleAtStation || !_isPhysicallyStopped)
            batteryLevel = Mathf.Max(0, batteryLevel - Time.deltaTime * 0.05f);
    }

    void ApplyForcesBasedOnState()
    {
        _rb.AddForce(Physics.gravity, ForceMode.Acceleration);

        if (_isTakingOffSubState)
        {
            if (currentAltitude_abs < targetAltitude_abs - 0.1f)
            {
                float altError = targetAltitude_abs - currentAltitude_abs;
                float pForce = altError * Kp_altitude;
                float dForce = -_rb.linearVelocity.y * Kd_altitude;
                float upwardForce = Physics.gravity.magnitude + pForce + dForce;
                _rb.AddForce(Vector3.up * Mathf.Clamp(upwardForce, Physics.gravity.magnitude * 0.5f, hoverForce * 1.5f), ForceMode.Acceleration);
            }
            
            else
                _isTakingOffSubState = false;
        }
        
        else if (_isLandingSubState)
        {
            if (currentAltitude_abs > targetAltitude_abs + 0.05f)
            {
                float descentRate = landingDescentRate;
                if (currentAltitude_abs < targetAltitude_abs + 2.0f) descentRate *= 0.5f;
                float upwardThrust = Mathf.Max(0, Physics.gravity.magnitude - descentRate);
                _rb.AddForce(Vector3.up * upwardThrust, ForceMode.Acceleration);
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x * 0.8f, _rb.linearVelocity.y, _rb.linearVelocity.z * 0.8f);
                _rb.angularVelocity *= 0.8f;
            }
            
            else
            {
                _isLandingSubState = false;
                _isPhysicallyStopped = true;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                if (droneStationLocation) // 스타일 수정
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
            float dForceAlt = -_rb.linearVelocity.y * Kd_altitude;
            float totalVertForce = Physics.gravity.magnitude + pForceAlt + dForceAlt;
            _rb.AddForce(Vector3.up * Mathf.Clamp(totalVertForce, 0.0f, hoverForce * 2.0f), ForceMode.Acceleration);

            Vector3 targetDirectionOnPlane = (new Vector3(_currentTargetPositionXZ.x, 0, _currentTargetPositionXZ.z) - new Vector3(transform.position.x, 0, transform.position.z)).normalized;
            float distanceToTargetXZ = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), _currentTargetPositionXZ);

            if (distanceToTargetXZ > arrivalDistanceThreshold)
            {
                float effectiveMoveForce = moveForce;
                
                if (distanceToTargetXZ < decelerationStartDistanceXZ)
                    effectiveMoveForce = Mathf.Lerp(moveForce * 0.2f, moveForce, distanceToTargetXZ / decelerationStartDistanceXZ);

                Vector3 desiredVelocityXZ = targetDirectionOnPlane * effectiveMoveForce;
                Quaternion targetRotation = targetDirectionOnPlane != Vector3.zero ? Quaternion.LookRotation(targetDirectionOnPlane) : transform.rotation;
                
                if (Quaternion.Angle(transform.rotation, targetRotation) > turnBeforeMoveAngleThreshold)
                    desiredVelocityXZ *= 0.2f;
                
                Vector3 forceNeededXZ = (desiredVelocityXZ - new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z)) * 3.0f;
                _rb.AddForce(forceNeededXZ, ForceMode.Acceleration);

                float targetAngleY = Mathf.Atan2(targetDirectionOnPlane.x, targetDirectionOnPlane.z) * Mathf.Rad2Deg;
                float angleErrorY = Mathf.DeltaAngle(_rb.rotation.eulerAngles.y, targetAngleY);
                float pTorque = angleErrorY * Mathf.Deg2Rad * Kp_rotation;
                float dTorque = -_rb.angularVelocity.y * Kd_rotation;
                _rb.AddTorque(Vector3.up * Mathf.Clamp(pTorque + dTorque, -maxRotationTorque, maxRotationTorque), ForceMode.Acceleration);
            }
        }
        
        else if (!_isPhysicallyStopped) // Holding or Performing
        {
            float altError = targetAltitude_abs - currentAltitude_abs;
            float pForceAlt = altError * Kp_altitude;
            float dForceAlt = -_rb.linearVelocity.y * Kd_altitude;
            float totalVertForce = Physics.gravity.magnitude + pForceAlt + dForceAlt;
            _rb.AddForce(Vector3.up * Mathf.Clamp(totalVertForce, 0.0f, hoverForce * 2.0f), ForceMode.Acceleration);

            float dampingFactor = 0.8f;
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x * dampingFactor, _rb.linearVelocity.y, _rb.linearVelocity.z * dampingFactor);
            _rb.angularVelocity *= dampingFactor;
        }
    }

    #endregion

    #region 웹소켓 통신 (WebSocket Communication)

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
                    _currentBombLoad
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
                        if (currentMissionState == DroneMissionState.IdleAtStation)
                        {
                            Vector3 targetPos = new Vector3(eventData["coordinates"]["x"].AsFloat, eventData["coordinates"]["y"].AsFloat, eventData["coordinates"]["z"].AsFloat);
                            currentPayload = PayloadType.FireExtinguishingBomb;
                            _fireScaleBombCount = eventData["fire_scale"].AsInt;
                            StartMission(targetPos, "산불 진압");
                        }
                        else 
                        {
                            Debug.LogWarning($"[Mission] Wildfire alert received, but drone is busy. Current state: {currentMissionState}");
                        }
                    }
                    else if (eventName == "force_return_command")
                    {
                        if (droneStationLocation) // 스타일 수정
                        {
                            _currentTargetPositionXZ = new Vector3(droneStationLocation.position.x, 0, droneStationLocation.position.z);
                            currentMissionState = DroneMissionState.EmergencyReturn;
                            
                            if (_actionCoroutine != null) StopCoroutine(_actionCoroutine); // 스타일 수정
                        }
                    }
                    else if (eventName == "emergency_stop_command")
                    {
                        currentMissionState = DroneMissionState.HoldingPosition;
                        
                        if (_actionCoroutine  != null) StopCoroutine(_actionCoroutine); // 스타일 수정
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
    
    #endregion
}
