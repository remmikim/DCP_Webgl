// C:\Unity\TeamProject\Assets\JWK\Scripts\DroneController.cs

using System;
using System.Collections;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;
using WebSocketSharp;

// --- 드론 컨트롤러 주 클래스 ---
namespace JWK.Scripts
{
    public class DroneController : MonoBehaviour
    {
        #region 변수 선언 (Fields and Properties)

        // 소화탄 페이로드 Class
        [Header("페이로드 및 임무")]
        [Tooltip("드론에 장착된 페이로드 오브젝트의 Payload 스크립트를 할당하세요.")]
        public ExtinguisherDropSystem extinguisherDropSystem;
        public bool isArrived;
    
    
        // --- 내부 컴포넌트 및 상태 ---
        private Rigidbody _rb;
        private Coroutine _actionCoroutine = null;
        private bool _isTakingOffSubState = false;
        private bool _isLandingSubState = false;
        private bool _isPhysicallyStopped = true;

        // --- 드론 실시간 상태 ---
        [FormerlySerializedAs("currentAltitude_abs")]
        [Header("드론 실시간 상태")]
        [SerializeField] private float currentAltitudeAbs;
        [SerializeField] private Vector3 currentPositionAbs;
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
        private int _currentBombLoad;
        private int _fireScaleBombCount;
        [FormerlySerializedAs("missionCruisingAGL")]
        public float missionCruisingAgl = 50.0f;
        private Vector3 _currentTargetPositionXZ;
        public float arrivalDistanceThreshold = 2.0f;
        public float bombDropInterval = 3.0f;
        public Vector3 bombSpawnOffset = new Vector3(0f, -2.0f, 0.5f);
        public float preActionStabilizationTime = 3.0f;

        [Header("Inspector 테스트용 임무")]
        public Transform testDispatchTarget;
        public string testMissionType = "산불 진압";
    
        [Header("고도 제어 (PD & AGL)")]
        public float targetAltitudeAbs;
        public float kpAltitude = 2.0f;
        public float kdAltitude = 2.5f;
        public float landingDescentRate = 0.4f;
        public float terrainCheckDistance = 200.0f;
        public LayerMask groundLayerMask;
        private float _currentGroundYAgl;

        [Header("자율 이동 및 회전 개선")]
        public float kpRotation = 0.8f;
        public float kdRotation = 0.3f;
        public float turnBeforeMoveAngleThreshold = 15.0f;
        public float decelerationStartDistanceXZ = 15.0f;
        public float maxRotationTorque = 15.0f;

        // --- 웹소켓 ---
        private WebSocket _ws;
        private string serverUrl = "ws://127.0.0.1:5000/socket.io/?EIO=4&transport=websocket&type=unity_main";
    
        #endregion

        #region Unity 생명주기 함수 (Lifecycle Methods)
        void Start()
        {
            _rb = GetComponent<Rigidbody>();
            if (!_rb)
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
            if (!_rb) 
                return;
        
            ApplyForcesBasedOnState();
        }

        void OnApplicationQuit()
        {
            if (_ws != null && _ws.IsAlive) 
                _ws.Close();
        }

        void OnDestroy()
        {
            if (_ws != null && _ws.IsAlive) 
                _ws.Close();
        
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
        #endregion
    
        #region 드론 State
        void Handle_TakingOff()
        {
            if (!_isTakingOffSubState && currentAltitudeAbs >= targetAltitudeAbs - 0.2f)
                currentMissionState = DroneMissionState.MovingToTarget;
        }

        void Handle_MovingToTarget()
        {
            isArrived = false;
            Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);

            if (Vector3.Distance(currentPosXZ, _currentTargetPositionXZ) < arrivalDistanceThreshold)
            {
                if (currentMissionState == DroneMissionState.MovingToTarget)
                {
                    isArrived = true;
                    currentMissionState = DroneMissionState.PerformingAction;
                
                    if (_actionCoroutine != null) 
                        StopCoroutine(_actionCoroutine);
                
                    _actionCoroutine = StartCoroutine(PerformActionCoroutine());
                }
            
                else if (currentMissionState == DroneMissionState.ReturningToStation || currentMissionState == DroneMissionState.EmergencyReturn)
                {
                    currentMissionState = DroneMissionState.Landing;
                    _isLandingSubState = true;
                
                    if (droneStationLocation)
                        targetAltitudeAbs = droneStationLocation.position.y;
                    
                    else
                        targetAltitudeAbs = _currentGroundYAgl;
                }
            }
        }

        void Handle_HoldingPosition() { /* 제자리 유지 */ }

        void Handle_Landing()
        {
            if (!_isLandingSubState && _isPhysicallyStopped && Mathf.Abs(currentAltitudeAbs - targetAltitudeAbs) < 0.15f)
            {
                currentMissionState = DroneMissionState.IdleAtStation;
                _currentBombLoad = totalBombs;
                PerformInitialGroundCheckAndSetAltitude();
            
                if (droneStationLocation)
                    transform.rotation = droneStationLocation.rotation;

                DroneEvents.LandingSequenceCompleted();
            }
        }
        #endregion
    
        #region 화재 포인트 도착 후 행동 로직
    
        // 화재 포인트 도착 후 행동 로직
        // ReSharper disable Unity.PerformanceAnalysis
        IEnumerator PerformActionCoroutine()
        {
            yield return new WaitForSeconds(preActionStabilizationTime);

            // 만약 현재 장착 페이로드가 "소화탄 장착" 이라면
            if (currentPayload == PayloadType.FireExtinguishingBomb)
            {
                isArrived = true;
                
                if(extinguisherDropSystem)
                {
                    yield return StartCoroutine(extinguisherDropSystem.PlayDropExtinguishBomb());
                    Debug.Log($"[Mission] Performing action for payload: {currentPayload}.");
                }
                
                else
                    Debug.LogWarning("extinguisherDropSystem is NULL!!!!!!!");

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
        
            _actionCoroutine = null; // 이 코루틴이 끝났음을 다른 코드에 알리기 위해 추적 변수를 비움.

            // Drone Station이 제대로 할당 되었는가 체크
            if (droneStationLocation)
            {
                _currentTargetPositionXZ = new Vector3(droneStationLocation.position.x, 0, droneStationLocation.position.z);
                currentMissionState = DroneMissionState.ReturningToStation;
            }
        
            // 만약 Drone Station이 제대로 할당되지 않았다면 제자리에서 호버링
            else
                currentMissionState = DroneMissionState.HoldingPosition;
        }
    
        #endregion
    
        // ReSharper disable Unity.PerformanceAnalysis
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
        
            if (_ws != null && _ws.IsAlive)
                _ws.Send(socketMessage);
        }
    
        #region 
        public void StartMission(Vector3 targetPosition, string missionDescription, int bombsToUse)
        {
            if (currentMissionState != DroneMissionState.IdleAtStation) 
                return;

            DroneEvents.TakeOffSequenceStarted();
            
            Debug.Log($"[Mission] Starting: {missionDescription}! Target: {targetPosition}, Bombs: {bombsToUse}");
            _currentTargetPositionXZ = new Vector3(targetPosition.x, 0, targetPosition.z);
            currentPayload = PayloadType.FireExtinguishingBomb;
            _fireScaleBombCount = bombsToUse;
        
            RaycastHit hit;
            Vector3 takeoffRefPos = droneStationLocation ? droneStationLocation.position : transform.position;
        
            if (Physics.Raycast(takeoffRefPos + Vector3.up, Vector3.down, out hit, terrainCheckDistance, groundLayerMask))
                targetAltitudeAbs = hit.point.y + missionCruisingAgl;
            else
                targetAltitudeAbs = takeoffRefPos.y + missionCruisingAgl;
        
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
                _currentGroundYAgl = hit.point.y;
            else
                _currentGroundYAgl = transform.position.y;
        
            targetAltitudeAbs = transform.position.y;
        }

        void UpdateTerrainSensingAndDynamicTargetAltitude()
        {
            RaycastHit hit;

            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, terrainCheckDistance, groundLayerMask))
                _currentGroundYAgl = hit.point.y;

            if (currentMissionState != DroneMissionState.TakingOff && currentMissionState != DroneMissionState.Landing)
                targetAltitudeAbs = _currentGroundYAgl + missionCruisingAgl;
        }
    
        void UpdateDroneInternalStatus()
        {
            currentPositionAbs = transform.position;
            currentAltitudeAbs = currentPositionAbs.y;
        
            if (currentMissionState != DroneMissionState.IdleAtStation || !_isPhysicallyStopped)
                batteryLevel = Mathf.Max(0, batteryLevel - Time.deltaTime * 0.05f);
        }

        void ApplyForcesBasedOnState()
        {
            _rb.AddForce(Physics.gravity, ForceMode.Acceleration);

            if (_isTakingOffSubState)
            {
                if (currentAltitudeAbs < targetAltitudeAbs - 0.1f)
                {
                    float altError = targetAltitudeAbs - currentAltitudeAbs;
                    float pForce = altError * kpAltitude;
                    float dForce = -_rb.linearVelocity.y * kdAltitude;
                    float upwardForce = Physics.gravity.magnitude + pForce + dForce;
                    _rb.AddForce(Vector3.up * Mathf.Clamp(upwardForce, Physics.gravity.magnitude * 0.5f, hoverForce * 1.5f), ForceMode.Acceleration);
                }
                else
                {
                    _isTakingOffSubState = false;
                }
            }
            else if (_isLandingSubState)
            {
                if (currentAltitudeAbs > targetAltitudeAbs + 0.05f)
                {
                    float descentRate = landingDescentRate;
                    if (currentAltitudeAbs < targetAltitudeAbs + 2.0f) 
                        descentRate *= 0.5f;
                
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
                float altError = targetAltitudeAbs - currentAltitudeAbs;
                float pForceAlt = altError * kpAltitude;
                float dForceAlt = -_rb.linearVelocity.y * kdAltitude;
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
                    float pTorque = angleErrorY * Mathf.Deg2Rad * kpRotation;
                    float dTorque = -_rb.angularVelocity.y * kdRotation;
                    _rb.AddTorque(Vector3.up * Mathf.Clamp(pTorque + dTorque, -maxRotationTorque, maxRotationTorque), ForceMode.Acceleration);
                }
            }
            else if (!_isPhysicallyStopped)
            {
                float altError = targetAltitudeAbs - currentAltitudeAbs;
                float pForceAlt = altError * kpAltitude;
                float dForceAlt = -_rb.linearVelocity.y * kdAltitude;
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
            _ws = new WebSocket(serverUrl);
            _ws.OnOpen += (sender, e) => { 
                Debug.Log("[Unity] Main Drone WebSocket Connected!");
                _ws.Send("40"); 
            };
        
            _ws.OnMessage += OnWebSocketMessage;
            _ws.OnError += (sender, e) => Debug.LogError("[Unity] WebSocket Error: " + e.Message);
            _ws.OnClose += (sender, e) => { if (this != null && gameObject.activeInHierarchy) StartCoroutine(ReconnectWebSocket()); };
            _ws.Connect();
        }

        IEnumerator ReconnectWebSocket()
        {
            yield return new WaitForSeconds(5f);
            if (_ws == null || !_ws.IsAlive) 
                ConnectWebSocket();
        }
    
        IEnumerator SendDroneDataRoutine()
        {
            DroneStatusData dataToSend = new DroneStatusData(Vector3.zero, 0, 0, "", "", 0);

            while (true)
            {
                yield return new WaitForSeconds(0.2f);
            
                if (_ws != null && _ws.IsAlive)
                {
                    dataToSend.position = new Vector3Data(currentPositionAbs.x, currentPositionAbs.y, currentPositionAbs.z);
                    dataToSend.altitude = currentAltitudeAbs;
                    dataToSend.battery = batteryLevel;
                    dataToSend.mission_state = currentMissionState.ToString();
                    dataToSend.payload_type = currentPayload.ToString();
                    dataToSend.bomb_load = _currentBombLoad;
                
                    string droneDataJson = JsonUtility.ToJson(dataToSend);
                    string socketIOMessage = "42[\"unity_main_drone_data\"," + droneDataJson + "]";
                    _ws.Send(socketIOMessage);
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
                                _currentTargetPositionXZ = new Vector3(droneStationLocation.position.x, 0, droneStationLocation.position.z);
                                currentMissionState = DroneMissionState.EmergencyReturn;
                            
                                if (_actionCoroutine != null) 
                                    StopCoroutine(_actionCoroutine);
                            }
                        }
                        else if (eventName == "emergency_stop_command")
                        {
                            currentMissionState = DroneMissionState.HoldingPosition;
                        
                            if (_actionCoroutine != null)
                                StopCoroutine(_actionCoroutine);
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
                _ws.Send("3"); 
            }
        }
    
        #endregion
    }
}
