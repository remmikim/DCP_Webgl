// C:\Unity\TeamProject\Assets\JWK\Scripts\DroneController.cs

using System;
using System.Collections;
using System.Text;
using System.Threading;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;
using WebSocketSharp;

// --- 드론 컨트롤러 주 클래스 ---
namespace JWK.Scripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class DroneController : MonoBehaviour
    {
        #region 변수 선언 (Fields and Properties)

        // 소화탄 페이로드 Class
        [Header("페이로드 및 임무")]
        [Tooltip("드론에 장착된 페이로드 오브젝트의 Payload 스크립트를 할당하세요.")]
        public ExtinguisherDropSystem extinguisherDropSystem;
        public bool isArrived {get; private set;}
    
        // --- 내부 컴포넌트 및 상태 ---
        private Rigidbody _rb;
        private Coroutine _actionCoroutine;
        private CancellationTokenSource _webSocketCts;
        
        // --- 드론 실시간 상태 (읽기 전용 프로퍼티로 외부 노출) ---
        [Header("드론 실시간 상태")] [SerializeField] private float batteryLevel = 100.0f;
        public Vector3 CurrentPositionAbs {get; private set;}
        public float CurrentAltitudeAbs {get; private set;}
        public float BatteryLevel {get; private set;}
        
        // --- 페이로드 및 임무 상태 Enum ---
        public enum PayloadType {None, FireExtinguishingBomb, RescueEquipment, DisasterReliefBag, AluminiumSplint, Gripper}
        public enum DroneMissionState { IdleAtStation, TakingOff, MovingToTarget, PerformingAction, ReturningToStation, Landing, EmergencyReturn, HoldingPosition }

        [Header("임무 상태 및 페이로드")]
        public DroneMissionState currentMissionState = DroneMissionState.IdleAtStation;
        public PayloadType currentPayload = PayloadType.FireExtinguishingBomb;
        
        [Header("Inspector 테스트용 임무")]
        public Transform testDispatchTarget;
        
        private readonly string[] _missionStateStrings = Enum.GetNames(typeof(DroneMissionState));
        private readonly string[] _payloadTypeStrings = Enum.GetNames(typeof(PayloadType));
        
        // --- 드론 기본 성능 ---
        [Header("드론 기본 성능")]
        [SerializeField] private float hoverForce = 70.0f;
        [SerializeField] private float moveForce = 15.0f;

        // --- 임무 설정 ---
        [Header("임무 설정")]
        [SerializeField] private Transform droneStationLocation;
        [SerializeField] private float missionCruisingAgl = 50.0f;
        [SerializeField] private float arrivalDistanceThreshold = 2.0f;
        [SerializeField] private float preActionStabilizationTime = 3.0f;
        private float _arrivalDistanceThresholdSqr; // 최적화: 제곱된 도착 임계값
        private Vector3 _currentTargetPositionXZ;
        private int _currentBombLoad;
        [SerializeField] private int totalBombs = 6;
        
        [Header("고도 제어 (PD & AGL)")]
        [SerializeField] private float kpAltitude = 2.0f;
        [SerializeField] private float kdAltitude = 2.5f;
        [SerializeField] private float landingDescentRate = 0.4f;
        [SerializeField] private float terrainCheckDistance = 200.0f;
        [SerializeField] private LayerMask groundLayerMask;
        private float _currentGroundYAgl;
        private float _targetAltitudeAbs;

        [Header("자율 이동 및 회전 개선")]
        [SerializeField] private float kpRotation = 0.8f;
        [SerializeField] private float kdRotation = 0.3f;
        [SerializeField] private float turnBeforeMoveAngleThreshold = 15.0f;
        [SerializeField] private float decelerationStartDistanceXZ = 15.0f;
        [SerializeField] private float maxRotationTorque = 15.0f;

        // --- 웹소켓 ---
        private WebSocket _ws;
        private const string ServerUrl = "ws://127.0.0.1:5000/socket.io/?EIO=4&transport=websocket&type=unity_main";
        private readonly StringBuilder _socketMessageBuilder = new StringBuilder(256);
        private DroneStatusData _dataToSend;

        // --- 코루틴 캐싱 (GC 최적화) ---
        private readonly WaitForSeconds _sendDataWait = new WaitForSeconds(0.2f);
        private readonly WaitForSeconds _terrainCheckWait = new WaitForSeconds(0.1f);
        private readonly WaitForSeconds _reconnectWait = new WaitForSeconds(5f);
        private WaitForSeconds _preActionWait;
        
        #endregion
        
        #region Unity 생명주기 함수 (Lifecycle Methods)

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.angularDamping = 2.5f;
            
            _arrivalDistanceThresholdSqr = arrivalDistanceThreshold * arrivalDistanceThreshold;
            _preActionWait = new WaitForSeconds(preActionStabilizationTime);
        }

        private void Start()
        {
            _dataToSend = new DroneStatusData(Vector3.zero, 0, 0, "", "", 0);
            _webSocketCts = new CancellationTokenSource();

            ConnectWebSocket();
            StartCoroutine(SendDroneDataRoutine());
            StartCoroutine(TerrainCheckRoutine());

            if (droneStationLocation)
            {
                transform.SetPositionAndRotation(droneStationLocation.position, droneStationLocation.rotation);
            }
            else
            {
                Debug.LogError("[Mission] Drone Station Location (LandingPad) not assigned in Inspector!");
            }
            
            PerformInitialGroundCheckAndSetAltitude();
            currentMissionState = DroneMissionState.IdleAtStation;
            _currentBombLoad = totalBombs;
        }
        
        private void Update()
        {
            UpdateDroneInternalStatus();
            RunStateMachine();
        }

        private void FixedUpdate()
        {
            ApplyForcesBasedOnState();
        }
        
        private void OnDestroy()
        {
            // 앱 종료 또는 오브젝트 파괴 시 모든 리소스를 정리합니다.
            _webSocketCts?.Cancel();
            _webSocketCts?.Dispose();

            if (_ws != null && _ws.IsAlive)
            {
                _ws.Close(CloseStatusCode.Normal, "Client shutting down");
            }
            _ws = null;
            StopAllCoroutines();
        }
        
        #endregion
        
        #region 드론 임무 및 상태 관리 (Drone Mission & State Logic)
        private void RunStateMachine()
        {
            switch (currentMissionState)
            {
                case DroneMissionState.TakingOff:          Handle_TakingOff();         break;
                case DroneMissionState.MovingToTarget:
                case DroneMissionState.ReturningToStation:
                case DroneMissionState.EmergencyReturn:    Handle_MovingToTarget();    break;
                case DroneMissionState.Landing:            Handle_Landing();           break;
            }
        }
        #endregion
        
        #region 상태별 핸들러 (State Handlers)
        private void Handle_TakingOff()
        {
            // 목표 고도에 거의 도달하면 다음 상태로 전환
            if (CurrentAltitudeAbs >= _targetAltitudeAbs - 0.2f)
            {
                currentMissionState = DroneMissionState.MovingToTarget;
            }
        }

        private void Handle_MovingToTarget()
        {
            isArrived = false;
            Vector3 currentPos = transform.position;
            Vector3 targetPos = _currentTargetPositionXZ;
            // Y축을 무시한 평면상의 거리 제곱을 계산
            float distanceSqr = (currentPos.x - targetPos.x) * (currentPos.x - targetPos.x) + 
                                (currentPos.z - targetPos.z) * (currentPos.z - targetPos.z);

            if (distanceSqr < _arrivalDistanceThresholdSqr)
            {
                isArrived = true;
                if (currentMissionState == DroneMissionState.MovingToTarget)
                {
                    currentMissionState = DroneMissionState.PerformingAction;
                    if (_actionCoroutine != null) StopCoroutine(_actionCoroutine);
                    _actionCoroutine = StartCoroutine(PerformActionCoroutine());
                }
                else // ReturningToStation 또는 EmergencyReturn
                {
                    currentMissionState = DroneMissionState.Landing;
                    _targetAltitudeAbs = droneStationLocation ? droneStationLocation.position.y : _currentGroundYAgl;
                }
            }
        }

        private void Handle_Landing()
        {
            // 착륙 완료 조건을 확인합니다.
            if (Mathf.Abs(CurrentAltitudeAbs - _targetAltitudeAbs) < 0.15f)
            {
                // Rigidbody가 거의 멈췄는지 확인합니다.
                if (_rb.linearVelocity.sqrMagnitude < 0.01f && _rb.angularVelocity.sqrMagnitude < 0.01f)
                {
                    currentMissionState = DroneMissionState.IdleAtStation;
                    _currentBombLoad = totalBombs;
                    PerformInitialGroundCheckAndSetAltitude();
            
                    if (droneStationLocation)
                    {
                        transform.rotation = droneStationLocation.rotation;
                    }
                    DroneEvents.LandingSequenceCompleted();
                }
            }
        }
        #endregion
        
        #region 임무 수행 로직 (Action Logic)
        private IEnumerator PerformActionCoroutine()
        {
            yield return _preActionWait; // 캐시된 WaitForSeconds 사용

            if (currentPayload == PayloadType.FireExtinguishingBomb)
            {
                if(extinguisherDropSystem)
                {
                    Debug.Log($"[Mission] Performing action for payload: {currentPayload}.");
                    yield return StartCoroutine(extinguisherDropSystem.PlayDropExtinguishBomb());
                }
                else
                {
                    Debug.LogWarning("ExtinguisherDropSystem is NULL!");
                }
            }
            else
            {
                Debug.Log($"[Mission] Performing action for payload: {currentPayload}.");
                yield return _preActionWait; // 다른 페이로드도 간단한 대기 시간을 가짐
            }
        
            _actionCoroutine = null;

            if (droneStationLocation)
            {
                _currentTargetPositionXZ = droneStationLocation.position;
                _currentTargetPositionXZ.y = 0; // Y값은 사용하지 않으므로 0으로 설정
                currentMissionState = DroneMissionState.ReturningToStation;
            }
            else
            {
                currentMissionState = DroneMissionState.HoldingPosition;
            }
        }
        
        public void StartMission(Vector3 targetPosition, int bombsToUse)
        {
            if (currentMissionState != DroneMissionState.IdleAtStation) return;

            DroneEvents.TakeOffSequenceStarted();
            
            Debug.Log($"[Mission] Starting! Target: {targetPosition}, Bombs: {bombsToUse}");
            _currentTargetPositionXZ = targetPosition;
            _currentTargetPositionXZ.y = 0; // Y값은 사용하지 않으므로 0으로 설정
            
            currentPayload = PayloadType.FireExtinguishingBomb;
        
            Vector3 takeoffRefPos = droneStationLocation ? droneStationLocation.position : transform.position;
        
            if (Physics.Raycast(takeoffRefPos + Vector3.up, Vector3.down, out RaycastHit hit, terrainCheckDistance, groundLayerMask))
            {
                _targetAltitudeAbs = hit.point.y + missionCruisingAgl;
            }
            else
            {
                _targetAltitudeAbs = takeoffRefPos.y + missionCruisingAgl;
            }
        
            currentMissionState = DroneMissionState.TakingOff;
            _currentBombLoad = totalBombs;
        }
        #endregion
        
        #region 드론 물리 및 상태 업데이트 (Physics & Status Updates)

        private void PerformInitialGroundCheckAndSetAltitude()
        {
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, terrainCheckDistance, groundLayerMask))
            {
                _currentGroundYAgl = hit.point.y;
            }
            else
            {
                _currentGroundYAgl = transform.position.y;
            }
            _targetAltitudeAbs = transform.position.y;
        }

        private IEnumerator TerrainCheckRoutine()
        {
            while (true)
            {
                if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, terrainCheckDistance, groundLayerMask))
                {
                    _currentGroundYAgl = hit.point.y;
                }

                if (currentMissionState != DroneMissionState.TakingOff && currentMissionState != DroneMissionState.Landing)
                {
                    _targetAltitudeAbs = _currentGroundYAgl + missionCruisingAgl;
                }
                yield return _terrainCheckWait;
            }
        }
        
        private void UpdateDroneInternalStatus()
        {
            CurrentPositionAbs = transform.position;
            CurrentAltitudeAbs = CurrentPositionAbs.y;
        
            if (currentMissionState != DroneMissionState.IdleAtStation)
            {
                batteryLevel = Mathf.Max(0, batteryLevel - Time.deltaTime * 0.05f);
            }
        }

        private void ApplyForcesBasedOnState()
        {
            _rb.AddForce(Physics.gravity, ForceMode.Acceleration);

            switch (currentMissionState)
            {
                case DroneMissionState.TakingOff:
                    ApplyVerticalForce(1.5f);
                    break;
                case DroneMissionState.Landing:
                    ApplyLandingForce();
                    break;
                case DroneMissionState.MovingToTarget:
                case DroneMissionState.ReturningToStation:
                case DroneMissionState.EmergencyReturn:
                    ApplyVerticalForce(2.0f);
                    ApplyHorizontalAndRotationalForces();
                    break;
                case DroneMissionState.IdleAtStation:
                    // 물리적으로 완전히 멈추도록 속도를 감쇠시킵니다.
                    _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
                    _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
                    break;
                default: // HoldingPosition 또는 PerformingAction
                    ApplyVerticalForce(2.0f);
                    // 수평 속도를 점차 줄여 제자리에 머물도록 합니다.
                    Vector3 horizontalVelocity = _rb.linearVelocity;
                    horizontalVelocity.y = 0;
                    _rb.AddForce(-horizontalVelocity * 2f, ForceMode.Acceleration);
                    break;
            }
        }

        private void ApplyVerticalForce(float maxForceMultiplier)
        {
            float altError = _targetAltitudeAbs - CurrentAltitudeAbs;
            float pForceAlt = altError * kpAltitude;
            float dForceAlt = -_rb.linearVelocity.y * kdAltitude;
            float totalVertForce = Physics.gravity.magnitude + pForceAlt + dForceAlt;
            _rb.AddForce(Vector3.up * Mathf.Clamp(totalVertForce, 0.0f, hoverForce * maxForceMultiplier), ForceMode.Acceleration);
        }

        private void ApplyLandingForce()
        {
            if (CurrentAltitudeAbs > _targetAltitudeAbs + 0.05f)
            {
                float descentRate = landingDescentRate;
                if (CurrentAltitudeAbs < _targetAltitudeAbs + 2.0f) descentRate *= 0.5f;
                
                float upwardThrust = Mathf.Max(0, Physics.gravity.magnitude - descentRate);
                _rb.AddForce(Vector3.up * upwardThrust, ForceMode.Acceleration);
                
                // 수평 및 회전 속도를 급격히 줄임
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x * 0.8f, _rb.linearVelocity.y, _rb.linearVelocity.z * 0.8f);
                _rb.angularVelocity *= 0.8f;
            }
        }

        private void ApplyHorizontalAndRotationalForces()
        {
            Vector3 currentPosXZ = transform.position;
            currentPosXZ.y = 0;
            Vector3 targetPosXZ = _currentTargetPositionXZ;
            targetPosXZ.y = 0;

            Vector3 targetDirectionOnPlane = (targetPosXZ - currentPosXZ).normalized;
            float distanceToTargetXZ = Vector3.Distance(currentPosXZ, targetPosXZ);

            if (distanceToTargetXZ > arrivalDistanceThreshold)
            {
                float effectiveMoveForce = moveForce;
                if (distanceToTargetXZ < decelerationStartDistanceXZ)
                {
                    effectiveMoveForce = Mathf.Lerp(moveForce * 0.2f, moveForce, distanceToTargetXZ / decelerationStartDistanceXZ);
                }

                Vector3 desiredVelocityXZ = targetDirectionOnPlane * effectiveMoveForce;
                Quaternion targetRotation = Quaternion.LookRotation(targetDirectionOnPlane);
                
                if (Quaternion.Angle(transform.rotation, targetRotation) > turnBeforeMoveAngleThreshold)
                {
                    desiredVelocityXZ *= 0.2f;
                }
                
                Vector3 currentVelocityXZ = _rb.linearVelocity;
                currentVelocityXZ.y = 0;
                Vector3 forceNeededXZ = (desiredVelocityXZ - currentVelocityXZ) * 3.0f;
                _rb.AddForce(forceNeededXZ, ForceMode.Acceleration);

                float targetAngleY = targetRotation.eulerAngles.y;
                float angleErrorY = Mathf.DeltaAngle(_rb.rotation.eulerAngles.y, targetAngleY);
                float pTorque = angleErrorY * Mathf.Deg2Rad * kpRotation;
                float dTorque = -_rb.angularVelocity.y * kdRotation;
                _rb.AddTorque(Vector3.up * Mathf.Clamp(pTorque + dTorque, -maxRotationTorque, maxRotationTorque), ForceMode.Acceleration);
            }
        }
        
        public void DispatchMissionFromInspector()
        {
            if (currentMissionState != DroneMissionState.IdleAtStation) 
            {
                Debug.LogWarning("[Mission] 드론이 현재 임무 수행 중입니다."); 
                return; 
            }
    
            if (!testDispatchTarget) 
            {
                Debug.LogError("[Mission] 테스트 임무 타겟이 설정되지 않았습니다!"); 
                return; 
            }
    
            // 최적화된 StartMission 메서드 호출
            StartMission(testDispatchTarget.position, totalBombs);
    
            // 웹소켓으로 테스트 임무 데이터를 보내는 로직 (선택사항)
            // 만약 Inspector 테스트가 서버와 통신할 필요가 없다면 이 부분은 생략 가능합니다.
            DispatchData dispatchData = new DispatchData("산불 진압 (테스트)", testDispatchTarget.position);
            string dispatchJson = JsonUtility.ToJson(dispatchData);
        
            _socketMessageBuilder.Clear();
            _socketMessageBuilder.Append("42[\"unity_dispatch_mission\",");
            _socketMessageBuilder.Append(dispatchJson);
            _socketMessageBuilder.Append("]");
        
            if (_ws != null && _ws.IsAlive)
            {
                _ws.Send(_socketMessageBuilder.ToString());
            }
        }
        #endregion
        
        #region 웹소켓 통신 (WebSocket Communication)

        private void ConnectWebSocket()
        {
            try
            {
                _ws = new WebSocket(ServerUrl);
                _ws.OnOpen += (sender, e) => { 
                    Debug.Log("[Unity] Main Drone WebSocket Connected!");
                    _ws.Send("40"); 
                };
            
                _ws.OnMessage += OnWebSocketMessage;
                _ws.OnError += (sender, e) => Debug.LogError($"[Unity] WebSocket Error: {e.Message}");
                _ws.OnClose += (sender, e) => 
                {
                    if (this != null && gameObject.activeInHierarchy && !_webSocketCts.IsCancellationRequested)
                    {
                        Debug.LogWarning($"[Unity] WebSocket Closed. Code: {e.Code}, Reason: {e.Reason}. Reconnecting...");
                        StartCoroutine(ReconnectWebSocket());
                    }
                };
                _ws.Connect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity] WebSocket connection failed: {ex.Message}");
            }
        }

        private IEnumerator ReconnectWebSocket()
        {
            yield return _reconnectWait;
            if (_ws == null || !_ws.IsAlive)
            {
                ConnectWebSocket();
            }
        }
    
        private IEnumerator SendDroneDataRoutine()
        {
            while (true)
            {
                yield return _sendDataWait;
            
                if (_ws != null && _ws.IsAlive)
                {
                    // 최적화: dataToSend 객체를 재사용하여 GC 부담을 줄입니다.
                    _dataToSend.position.x = CurrentPositionAbs.x;
                    _dataToSend.position.y = CurrentPositionAbs.y;
                    _dataToSend.position.z = CurrentPositionAbs.z;
                    _dataToSend.altitude = CurrentAltitudeAbs;
                    _dataToSend.battery = batteryLevel;
                    _dataToSend.mission_state = _missionStateStrings[(int)currentMissionState];
                    _dataToSend.payload_type = _payloadTypeStrings[(int)currentPayload];
                    _dataToSend.bomb_load = _currentBombLoad;
                    
                    string droneDataJson = JsonUtility.ToJson(_dataToSend);
                    
                    // 최적화: StringBuilder를 사용하여 문자열 연결로 인한 GC 발생을 방지함
                    _socketMessageBuilder.Clear();
                    _socketMessageBuilder.Append("42[\"unity_main_drone_data\",");
                    _socketMessageBuilder.Append(droneDataJson);
                    _socketMessageBuilder.Append("]");
                    
                    _ws.Send(_socketMessageBuilder.ToString());
                }
            }
        }

        // 최적화: 웹소켓 메시지 파싱 및 처리를 위한 작업 클래스 (클로저 GC 방지)
        private class SocketMessageJob
        {
            public DroneController Controller;
            public string JsonString;

            public void Execute()
            {
                try
                {
                    JSONNode node = JSON.Parse(JsonString);
                    string eventName = node[0].Value;
                    JSONNode eventData = node[1];

                    if (eventName == "change_payload_command")
                    {
                        Controller.HandleChangePayloadCommand(eventData);
                    }
                    else if (eventName == "force_return_command")
                    {
                        Controller.HandleForceReturnCommand();
                    }
                    else if (eventName == "emergency_stop_command")
                    {
                        Controller.HandleEmergencyStopCommand();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Unity] Error parsing JSON: {ex.Message} - Data: {JsonString}");
                }
            }
        }

        private void OnWebSocketMessage(object sender, MessageEventArgs e)
        {
            if (e.Data.StartsWith("42"))
            {
                // 메인 스레드에서 실행할 작업을 큐에 넣습니다.
                var job = new SocketMessageJob
                {
                    Controller = this,
                    JsonString = e.Data.Substring(2)
                };
                UnityMainThreadDispatcher.Instance.Enqueue(job.Execute);
            }
            else if (e.Data == "2") // Ping
            { 
                _ws.Send("3"); // Pong
            }
        }
        
        // --- 웹소켓 명령 핸들러 ---
        private void HandleChangePayloadCommand(JSONNode eventData)
        {
            if (currentMissionState == DroneMissionState.IdleAtStation)
            {
                if (Enum.TryParse(eventData["payload"].Value, out PayloadType newPayload))
                {
                    currentPayload = newPayload;
                    Debug.Log($"[Mission] Payload changed to: {currentPayload}");
                }
            }
        }

        private void HandleForceReturnCommand()
        {
            if (droneStationLocation)
            {
                _currentTargetPositionXZ = droneStationLocation.position;
                _currentTargetPositionXZ.y = 0;
                currentMissionState = DroneMissionState.EmergencyReturn;
                if (_actionCoroutine != null) StopCoroutine(_actionCoroutine);
            }
        }

        private void HandleEmergencyStopCommand()
        {
            currentMissionState = DroneMissionState.HoldingPosition;
            if (_actionCoroutine != null) StopCoroutine(_actionCoroutine);
        }
    
        #endregion
    }
}
