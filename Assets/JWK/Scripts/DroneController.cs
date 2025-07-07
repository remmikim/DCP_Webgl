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
    private Rigidbody rb;
    private Coroutine actionCoroutine = null;
    private bool isTakingOff_subState = false;
    private bool isLanding_subState = false;
    private bool isPhysicallyStopped = true;

    // --- 드론 실시간 상태 ---
    [Header("드론 실시간 상태")]
    [SerializeField] private float currentAltitude_abs;
    [SerializeField] private Vector3 currentPosition_abs;
    [SerializeField] private float batteryLevel = 100.0f;

    // --- 페이로드 및 임무 상태 Enum ---
    public enum PayloadType { None, FireExtinguishingBomb, RescueEquipment, DisasterReliefBag, AluminumSplint, Gripper }
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
    private int currentBombLoad;
    private int fireScaleBombCount;
    [FormerlySerializedAs("missionCruisingAGL")]
    public float missionCruisingAgl = 50.0f;
    private Vector3 currentTargetPosition_xz;
    public float arrivalDistanceThreshold = 2.0f;
    public float bombDropInterval = 3.0f;
    public Vector3 bombSpawnOffset = new Vector3(0f, -2.0f, 0.5f);
    public float preActionStabilizationTime = 3.0f;

    [Header("Inspector 테스트용 임무")]
    public Transform testDispatchTarget;
    public string testMissionType = "산불 진압";
    
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

    // --- 웹소켓 ---
    private WebSocket ws;
    private string serverUrl = "ws://127.0.0.1:5000/socket.io/?EIO=4&transport=websocket&type=unity_main";
    
    #endregion

    #region Unity 생명주기 함수 (Lifecycle Methods)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb)
        {
            Debug.LogError("[Unity] Rigidbody component not found!");
            enabled = false;
            return;
        }

        rb.useGravity = false;
        rb.angularDamping = 2.5f;

        ConnectWebSocket();
        StartCoroutine(SendDroneDataRoutine());
        UnityMainThreadDispatcher.Instance();

        if (droneStationLocation)
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
        RunStateMachine();
    }

    void FixedUpdate()
    {
        if (!rb) 
            return;
        
        ApplyForcesBasedOnState();
    }

    void OnApplicationQuit()
    {
        if (ws != null && ws.IsAlive) 
            ws.Close();
    }

    void OnDestroy()
    {
        if (ws != null && ws.IsAlive) 
            ws.Close();
        
        StopAllCoroutines();
    }

    #endregion

    #region 드론 임무 및 상태 관리 (Drone Mission & State Logic)

    void RunStateMachine()
    {
        switch (currentMissionState)
        {
            case DroneMissionState.TakingOff: Handle_TakingOff(); break;
            case DroneMissionState.MovingToTarget: Handle_MovingToTarget(); break;
            case DroneMissionState.ReturningToStation: Handle_MovingToTarget(); break;
            case DroneMissionState.EmergencyReturn: Handle_MovingToTarget(); break;
            case DroneMissionState.Landing: Handle_Landing(); break;
        }
    }

    void Handle_TakingOff()
    {
        if (!isTakingOff_subState && currentAltitude_abs >= targetAltitude_abs - 0.2f)
            currentMissionState = DroneMissionState.MovingToTarget;
    }

    void Handle_MovingToTarget()
    {
        Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);

        if (Vector3.Distance(currentPosXZ, currentTargetPosition_xz) < arrivalDistanceThreshold)
        {
            if (currentMissionState == DroneMissionState.MovingToTarget)
            {
                currentMissionState = DroneMissionState.PerformingAction;
                
                if (actionCoroutine != null) 
                    StopCoroutine(actionCoroutine);
                
                actionCoroutine = StartCoroutine(PerformActionCoroutine());
            }
            else if (currentMissionState == DroneMissionState.ReturningToStation || currentMissionState == DroneMissionState.EmergencyReturn)
            {
                currentMissionState = DroneMissionState.Landing;
                isLanding_subState = true;
                
                if (droneStationLocation)
                    targetAltitude_abs = droneStationLocation.position.y;
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
        if (!isLanding_subState && isPhysicallyStopped && Mathf.Abs(currentAltitude_abs - targetAltitude_abs) < 0.15f)
        {
            currentMissionState = DroneMissionState.IdleAtStation;
            currentBombLoad = totalBombs;
            PerformInitialGroundCheckAndSetAltitude();
            
            if (droneStationLocation)
                transform.rotation = droneStationLocation.rotation;
        }
    }

    // 화재 포인트 도착 후 행동 로직
    IEnumerator PerformActionCoroutine()
    {
        yield return new WaitForSeconds(preActionStabilizationTime);

        // 만약 현재 장착 페이로드가 "소화탄 장착" 이라면
        if (currentPayload == PayloadType.FireExtinguishingBomb)
        {
            /*
            테스트용 소화탄 생성 및 투하
            int bombsToDrop = Mathf.Min(fireScaleBombCount, currentBombLoad);
            for (int i = 0; i < bombsToDrop; i++)
            {
                if (bombPrefab)
                {
                    Instantiate(bombPrefab, transform.TransformPoint(bombSpawnOffset), Quaternion.identity);
                    currentBombLoad--;
                }
                
                if (i < bombsToDrop - 1) 
                    yield return new WaitForSeconds(bombDropInterval);
            }
            */
            Debug.Log($"[Mission] Performing action for payload: {currentPayload}.");
            
        }
        
        // 만약 현재 장착 페이로드가 "제헤 구호 가방" 이라면
        else if (currentPayload == PayloadType.DisasterReliefBag)
        {
            Debug.Log($"[Mission] Performing action for payload: {currentPayload}.");
        }
        
        // 만약 현재 장착 페이로드가 "알루미늄 부목" 이라면
        else if (currentPayload == PayloadType.AluminumSplint)
        {
            Debug.Log($"[Mission] Performing action for payload: {currentPayload}.");
        }
        
        // 만약 현재 장착 페이로드가 "구조 장비" 라면
        else if(currentPayload == PayloadType.RescueEquipment)
        {
            Debug.Log($"[Mission] Performing action for payload: {currentPayload}.");
            yield return new WaitForSeconds(2.0f);
        }
        
        actionCoroutine = null; // 이 코루틴이 끝났음을 다른 코드에 알리기 위해 추적 변수를 비움.

        // Drone Station이 제대로 할당 되었는가 체크
        if (droneStationLocation)
        {
            currentTargetPosition_xz = new Vector3(droneStationLocation.position.x, 0, droneStationLocation.position.z);
            currentMissionState = DroneMissionState.ReturningToStation;
        }
        
        // 만약 Drone Station이 제대로 할당되지 않았다면 제자리에서 호버링
        else
            currentMissionState = DroneMissionState.HoldingPosition;
    }
    
    public void DispatchMissionFromInspector()
    {
        if (currentMissionState != DroneMissionState.IdleAtStation) 
        {
            Debug.LogWarning("[Mission] Drone is busy."); 
            return; 
        }
        
        if (!testDispatchTarget) 
        {
            Debug.LogError("[Mission] Test Dispatch Target is not set!"); 
            return; 
        }
        
        StartMission(testDispatchTarget.position, testMissionType, totalBombs);
        
        DispatchData dispatchData = new DispatchData(testMissionType, testDispatchTarget.position);
        string dispatchJson = JsonUtility.ToJson(dispatchData);
        string socketMessage = "42[\"unity_dispatch_mission\"," + dispatchJson + "]";
        
        if (ws != null && ws.IsAlive)
            ws.Send(socketMessage);
    }
    
    public void StartMission(Vector3 targetPosition, string missionDescription, int bombsToUse)
    {
        if (currentMissionState != DroneMissionState.IdleAtStation) 
            return;

        Debug.Log($"[Mission] Starting: {missionDescription}! Target: {targetPosition}, Bombs: {bombsToUse}");
        currentTargetPosition_xz = new Vector3(targetPosition.x, 0, targetPosition.z);
        fireScaleBombCount = bombsToUse;
        
        RaycastHit hit;
        Vector3 takeoffRefPos = droneStationLocation ? droneStationLocation.position : transform.position;
        
        if (Physics.Raycast(takeoffRefPos + Vector3.up, Vector3.down, out hit, terrainCheckDistance, groundLayerMask))
            targetAltitude_abs = hit.point.y + missionCruisingAgl;
        else
            targetAltitude_abs = takeoffRefPos.y + missionCruisingAgl;
        
        currentMissionState = DroneMissionState.TakingOff;
        isTakingOff_subState = true;
        isPhysicallyStopped = false;
        currentBombLoad = totalBombs;
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
        
        if (currentMissionState != DroneMissionState.IdleAtStation || !isPhysicallyStopped)
            batteryLevel = Mathf.Max(0, batteryLevel - Time.deltaTime * 0.05f);
    }

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
                if (currentAltitude_abs < targetAltitude_abs + 2.0f) 
                    descentRate *= 0.5f;
                
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
                
                if (droneStationLocation)
                {
                    transform.position = droneStationLocation.position;
                    transform.rotation = droneStationLocation.rotation;
                }
            }
        }
        else if (currentMissionState == DroneMissionState.MovingToTarget || 
                 currentMissionState == DroneMissionState.ReturningToStation || 
                 currentMissionState == DroneMissionState.EmergencyReturn)
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
                    effectiveMoveForce = Mathf.Lerp(moveForce * 0.2f, moveForce, distanceToTargetXZ / decelerationStartDistanceXZ);

                Vector3 desiredVelocityXZ = targetDirectionOnPlane * effectiveMoveForce;
                Quaternion targetRotation = targetDirectionOnPlane != Vector3.zero ? Quaternion.LookRotation(targetDirectionOnPlane) : transform.rotation;
                
                if (Quaternion.Angle(transform.rotation, targetRotation) > turnBeforeMoveAngleThreshold)
                    desiredVelocityXZ *= 0.2f;
                
                Vector3 forceNeededXZ = (desiredVelocityXZ - new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z)) * 3.0f;
                rb.AddForce(forceNeededXZ, ForceMode.Acceleration);

                float targetAngleY = Mathf.Atan2(targetDirectionOnPlane.x, targetDirectionOnPlane.z) * Mathf.Rad2Deg;
                float angleErrorY = Mathf.DeltaAngle(rb.rotation.eulerAngles.y, targetAngleY);
                float pTorque = angleErrorY * Mathf.Deg2Rad * Kp_rotation;
                float dTorque = -rb.angularVelocity.y * Kd_rotation;
                rb.AddTorque(Vector3.up * Mathf.Clamp(pTorque + dTorque, -maxRotationTorque, maxRotationTorque), ForceMode.Acceleration);
            }
        }
        else if (!isPhysicallyStopped)
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

    #endregion

    #region 웹소켓 통신 (WebSocket Communication)

    void ConnectWebSocket()
    {
        ws = new WebSocket(serverUrl);
        ws.OnOpen += (sender, e) => { 
            Debug.Log("[Unity] Main Drone WebSocket Connected!");
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
        if (ws == null || !ws.IsAlive) 
            ConnectWebSocket();
    }
    
    IEnumerator SendDroneDataRoutine()
    {
        DroneStatusData dataToSend = new DroneStatusData(Vector3.zero, 0, 0, "", "", 0);

        while (true)
        {
            yield return new WaitForSeconds(0.2f);
            
            if (ws != null && ws.IsAlive)
            {
                dataToSend.position = new Vector3Data(currentPosition_abs.x, currentPosition_abs.y, currentPosition_abs.z);
                dataToSend.altitude = currentAltitude_abs;
                dataToSend.battery = batteryLevel;
                dataToSend.mission_state = currentMissionState.ToString();
                dataToSend.payload_type = currentPayload.ToString();
                dataToSend.bomb_load = currentBombLoad;
                
                string droneDataJson = JsonUtility.ToJson(dataToSend);
                string socketIOMessage = "42[\"unity_main_drone_data\"," + droneDataJson + "]";
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
                    else if (eventName == "force_return_command")
                    {
                        if (droneStationLocation)
                        {
                            currentTargetPosition_xz = new Vector3(droneStationLocation.position.x, 0, droneStationLocation.position.z);
                            currentMissionState = DroneMissionState.EmergencyReturn;
                            
                            if (actionCoroutine != null) 
                                StopCoroutine(actionCoroutine);
                        }
                    }
                    else if (eventName == "emergency_stop_command")
                    {
                        currentMissionState = DroneMissionState.HoldingPosition;
                        
                        if (actionCoroutine != null)
                            StopCoroutine(actionCoroutine);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Unity] Error parsing JSON: {ex.Message} - Data: {jsonString}");
                }
            });
        }
        else if (e.Data == "2") 
        { 
            ws.Send("3"); 
        }
    }
    
    #endregion
}
