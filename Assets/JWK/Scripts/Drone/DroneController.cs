// C:\Unity\TeamProject\Assets\JWK\Scripts\DroneController.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using JWK.Scripts.DropSystem;
using JWK.Scripts.FireManager;
using SimpleJSON;
using UnityEngine;
using WebSocketSharp;

namespace JWK.Scripts.Drone
{
    [RequireComponent(typeof(Rigidbody))]
    public class DroneController : MonoBehaviour
    {
        #region 변수 선언 (Fields and Properties)

        [Header("페이로드 및 임무")]
        [SerializeField] private ExtinguisherDropSystem extinguisherDropSystem;
        public bool IsArrived { get; private set; }

        private Rigidbody _rb;
        private Coroutine _actionCoroutine;
        private CancellationTokenSource _webSocketCts;
        
        private Queue<GameObject> _fireTargetsQueue;

        [Header("드론 실시간 상태")]
        [SerializeField] private float batteryLevel = 100.0f;
        public Vector3 CurrentPositionAbs { get; private set; }
        public float CurrentAltitudeAbs { get; private set; }
        public float BatteryLevel => batteryLevel;

        [Header("임무 상태 및 페이로드")]
        public DroneMissionState currentMissionState = DroneMissionState.IdleAtStation;
        public PayloadType currentPayload = PayloadType.FireExtinguishingBomb;
        
        [Header("Inspector 테스트용 임무")]
        public Transform testDispatchTarget;
        
        private readonly string[] _missionStateStrings = Enum.GetNames(typeof(DroneMissionState));
        private readonly string[] _payloadTypeStrings = Enum.GetNames(typeof(PayloadType));

        [Header("드론 기본 성능")]
        [SerializeField] private float hoverForce = 70.0f;
        [SerializeField] private float moveForce = 15.0f;

        [Header("임무 설정")]
        [SerializeField] private Transform droneStationLocation;
        [SerializeField] private float missionCruisingAgl = 50.0f;
        [SerializeField] private float arrivalDistanceThreshold = 0.1f;
        [SerializeField] private float preActionStabilizationTime = 0.5f;
        [Tooltip("폭탄 투하 후 다음 행동까지 대기하는 시간입니다.")]
        [SerializeField] private float postDropMoveDelay = 1.5f;
        [SerializeField] private float retreatDistance = 10.0f;
        private float _arrivalDistanceThresholdSqr;
        private Vector3 _currentTargetPosition; 
        private Vector3 _actualFireTargetPosition; 
        private int _currentBombLoad;
        [SerializeField] private int totalBombs = 6;
        
        [Header("고도 제어 (PD & AGL)")]
        [SerializeField] private float kpAltitude = 2.0f;
        [SerializeField] private float kdAltitude = 2.5f;
        [SerializeField] private float landingDescentRate = 0.4f;
        [SerializeField] private float terrainCheckDistance = 50.0f;
        [SerializeField] private LayerMask groundLayerMask;
        private float _currentGroundYAgl;
        private float _targetAltitudeAbs;

        [Header("자율 이동 및 회전 개선")]
        [SerializeField] private float kpRotation = 0.8f;
        [SerializeField] private float kdRotation = 0.3f;
        [SerializeField] private float turnBeforeMoveAngleThreshold = 15.0f;
        [SerializeField] private float decelerationStartDistanceXZ = 15.0f;
        [SerializeField] private float maxRotationTorque = 15.0f;
        // ====================================================================================
        // 부드러운 이동을 위한 변수 추가
        [Tooltip("회전이 얼마나 부드럽게 될지 결정합니다. 낮을수록 빠르고 예리하게, 높을수록 부드럽게 회전합니다.")]
        [SerializeField] private float rotationSmoothTime = 0.8f;
        [Tooltip("속도 변경(가/감속)이 얼마나 부드럽게 될지 결정합니다. 낮을수록 반응이 빠르고, 높을수록 부드러워집니다.")]
        [SerializeField] private float velocitySmoothTime = 0.8f;
        private Vector3 _smoothedLookDirection;
        private Vector3 _currentSmoothedVelocity;
        // ====================================================================================
        private float _decelerationStartDistanceSqr;

        private WebSocket _ws;
        private const string ServerUrl = "ws://127.0.0.1:5000/socket.io/?EIO=4&transport=websocket&type=unity_main";
        private readonly StringBuilder _socketMessageBuilder = new StringBuilder(256);
        private DroneStatusData _dataToSend;

        private readonly WaitForSeconds _sendDataWait = new WaitForSeconds(0.2f);
        private readonly WaitForSeconds _terrainCheckWait = new WaitForSeconds(0.1f);
        private readonly WaitForSeconds _reconnectWait = new WaitForSeconds(5f);
        private WaitForSeconds _preActionWait;
        private readonly WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();

        #endregion

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.angularDamping = 1.0f;

            _arrivalDistanceThresholdSqr = arrivalDistanceThreshold * arrivalDistanceThreshold;
            _decelerationStartDistanceSqr = decelerationStartDistanceXZ * decelerationStartDistanceXZ;
            
            _preActionWait = new WaitForSeconds(preActionStabilizationTime);
            
            _fireTargetsQueue = new Queue<GameObject>();
        }

        #region Unity 생명주기 함수 (Lifecycle Methods)
        private void Start()
        {
            _dataToSend = new DroneStatusData(Vector3.zero, 0, 0, "", "", 0);
            _webSocketCts = new CancellationTokenSource();

            ConnectWebSocket();
            StartCoroutine(SendDroneDataRoutine());
            StartCoroutine(TerrainCheckRoutine());
            
            PerformInitialGroundCheckAndSetAltitude();
            currentMissionState = DroneMissionState.IdleAtStation;
            _currentBombLoad = totalBombs;

            // 스무딩 변수 초기화
            _smoothedLookDirection = transform.forward;
            _currentSmoothedVelocity = Vector3.zero;
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
            _webSocketCts?.Cancel();
            _webSocketCts?.Dispose();

            if (_ws != null && _ws.IsAlive)
                _ws.Close(CloseStatusCode.Normal, "Client shutting down");
            
            _ws = null;
            StopAllCoroutines();
        }
        #endregion

        #region 드론 임무 및 상태 관리 (Drone Mission & State Logic)
        private void RunStateMachine()
        {
            switch (currentMissionState)
            {
                case DroneMissionState.TakingOff:              Handle_TakingOff();         break;
                case DroneMissionState.MovingToTarget:         Handle_MovingToTarget();    break;
                case DroneMissionState.ReturningToStation:
                case DroneMissionState.EmergencyReturn:        Handle_MovingToStation();   break;
                case DroneMissionState.Landing:                Handle_Landing();           break;
                case DroneMissionState.RetreatingAfterAction:  Handle_MovingToStation();   break;
            }
        }
        #endregion
    
        #region 상태별 핸들러 (State Handlers)
        private void Handle_TakingOff()
        {
            if (CurrentAltitudeAbs >= _targetAltitudeAbs - 0.2f)
                currentMissionState = DroneMissionState.MovingToTarget;
        }
        
        private void Handle_MovingToTarget()
        {
            Vector3 dronePosXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosXZ = new Vector3(_currentTargetPosition.x, 0, _currentTargetPosition.z);
            float distanceSqr = (dronePosXZ - targetPosXZ).sqrMagnitude;

            if (distanceSqr < _arrivalDistanceThresholdSqr)
            {
                IsArrived = true;
                currentMissionState = DroneMissionState.PerformingAction;
                if (_actionCoroutine != null) StopCoroutine(_actionCoroutine);
                _actionCoroutine = StartCoroutine(PerformActionCoroutine());
            }
        }

        private void Handle_MovingToStation()
        {
            Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosXZ = new Vector3(_currentTargetPosition.x, 0, _currentTargetPosition.z);
            float distanceSqr = (currentPosXZ - targetPosXZ).sqrMagnitude;

            if (distanceSqr < _arrivalDistanceThresholdSqr)
            {
                if (currentMissionState == DroneMissionState.RetreatingAfterAction)
                    DecideNextAction();
                
                else 
                {
                    currentMissionState = DroneMissionState.Landing;
                    _targetAltitudeAbs = droneStationLocation ? droneStationLocation.position.y : _currentGroundYAgl;
                }
            }
        }
        
        private void Handle_Landing()
        {
            if (Mathf.Abs(CurrentAltitudeAbs - _targetAltitudeAbs) < 0.15f)
            {
                if (_rb.linearVelocity.sqrMagnitude < 0.01f && _rb.angularVelocity.sqrMagnitude < 0.01f)
                {
                    currentMissionState = DroneMissionState.IdleAtStation;
                    _currentBombLoad = totalBombs;
                    PerformInitialGroundCheckAndSetAltitude();
            
                    if (droneStationLocation)
                        transform.rotation = droneStationLocation.rotation;
                    
                    DroneEvents.LandingSequenceCompleted();
                }
            }
        }
        #endregion
    
        #region 임무 수행 로직 (Action Logic)
        
        private IEnumerator PerformActionCoroutine()
        {
            while (true)
            {
                Vector3 horizontalVelocity = _rb.linearVelocity;
                horizontalVelocity.y = 0;
                
                if (horizontalVelocity.sqrMagnitude < 0.01f && _rb.angularVelocity.sqrMagnitude < 0.01f)
                    break; 
                
                yield return _waitForFixedUpdate;
            }

            if (currentPayload == PayloadType.FireExtinguishingBomb)
            {
                if(extinguisherDropSystem && _currentBombLoad > 0)
                {
                    Debug.Log($"<color=yellow>[좌표 비교 디버그] 소화탄 투하 직전</color>");
                    Debug.Log($" - 드론 현재 위치: {transform.position}");
                    Debug.Log($" - <color=red>실제 화재 목표 위치</color>(_actualFireTargetPosition): {_actualFireTargetPosition}");
                    Debug.Log($" - <color=blue>드론 이동 목표 위치</color>(_currentTargetPosition): {_currentTargetPosition}");
                    float distanceToTarget = Vector3.Distance(transform.position, _currentTargetPosition);
                    Debug.Log($" - 드론-이동목표 간 거리: {distanceToTarget:F2}m");
                    yield return StartCoroutine(extinguisherDropSystem.DropSingleBomb(_actualFireTargetPosition, this.transform));
                    _currentBombLoad--;
                }
                else
                    Debug.LogWarning("ExtinguisherDropSystem이 없거나 폭탄을 모두 소진했습니다.");
            }
            
            // Debug.Log($"폭탄 투하 완료. {postDropMoveDelay}초 후 다음 행동을 시작합니다.");
            yield return new WaitForSeconds(postDropMoveDelay);

            Vector3 retreatDirection = -transform.forward;
            Vector3 retreatPosition = transform.position + retreatDirection * retreatDistance;
            
            _currentTargetPosition = retreatPosition;

            // Debug.Log($"후퇴 지점({retreatPosition})으로 이동합니다.");
            currentMissionState = DroneMissionState.RetreatingAfterAction;
        
            _actionCoroutine = null;
        }
        
        private void DecideNextAction()
        {
            if (currentMissionState != DroneMissionState.RetreatingAfterAction)
            {
                return;
            }

            Debug.Log("다음 행동 결정 시작... 남은 폭탄: " + _currentBombLoad + ", 남은 타겟 큐: " + _fireTargetsQueue.Count);

            while (_fireTargetsQueue.Count > 0 && _currentBombLoad > 0)
            {
                GameObject nextTarget = _fireTargetsQueue.Dequeue();

                if (nextTarget)
                {
                    Debug.Log($"[성공] 다음 유효 목표({nextTarget.name})를 찾았습니다. 이동을 시작합니다.");
                    SetMissionTarget(nextTarget.transform.position);
                    currentMissionState = DroneMissionState.MovingToTarget;
                    // 유효한 목표를 찾았으니, 더 이상 이 함수에서 할 일은 없습니다.
                    return;
                }
                else
                {
                    // 이 로그가 계속해서 찍힌다면, 큐 안의 목표들이 다른 요인에 의해 파괴되고 있다는 명백한 증거입니다.
                    Debug.LogWarning("[정보] 이미 파괴된 목표(유령 참조)를 큐에서 제거하고 다음을 탐색합니다.");
                }
            }

            // while 루프를 빠져나왔다는 것은, 큐가 비었거나 유효한 타겟을 하나도 찾지 못했다는 의미입니다.
            // 이 경우, 임무를 종료하고 기지로 복귀합니다.
            Debug.Log("[임무 종료] 더 이상 처리할 유효 목표가 없습니다. 기지로 복귀합니다.");
    
            _currentTargetPosition = droneStationLocation.position;
            _targetAltitudeAbs = droneStationLocation.position.y + 20f;
            currentMissionState = DroneMissionState.ReturningToStation;
        }
        
        private void SetMissionTarget(Vector3 actualFirePosition)
        {
            _actualFireTargetPosition = actualFirePosition;

            if (extinguisherDropSystem && _currentBombLoad > 0)
            {
                Vector3 directionToTarget = (actualFirePosition - droneStationLocation.position).normalized;
                directionToTarget.y = 0;
                Quaternion predictedRotation = Quaternion.LookRotation(directionToTarget);

                Vector3 bombLocalOffset = extinguisherDropSystem.GetNextBombOffsetFromDroneRoot(this.transform);
                Vector3 bombWorldOffset = predictedRotation * bombLocalOffset;

                _currentTargetPosition = actualFirePosition - bombWorldOffset;
            }
            
            else
                _currentTargetPosition = actualFirePosition;
        }
        
        public void StartSingleTargetMission(Vector3 targetPosition)
        {
            if (currentMissionState != DroneMissionState.IdleAtStation) return;

            _fireTargetsQueue.Clear();
            _currentBombLoad = totalBombs;
            if(extinguisherDropSystem) extinguisherDropSystem.ResetBombs();

            DroneEvents.TakeOffSequenceStarted();
            
            // Debug.Log($"[Mission] 단일 목표 임무 시작! Target: {targetPosition}");
            SetMissionTarget(targetPosition);
            
            SetTakeOffAltitudeAndState();
        }
        
        public void StartFireSuppressionMission(List<GameObject> fireTargets)
        {
            if (currentMissionState != DroneMissionState.IdleAtStation) return;
            if (fireTargets == null || fireTargets.Count == 0)
            {
                Debug.LogWarning("진압할 화재 목표가 없습니다.");
                return;
            }

            _fireTargetsQueue.Clear();
            foreach (var target in fireTargets)
            {
                _fireTargetsQueue.Enqueue(target);
            }

            _currentBombLoad = totalBombs;
            if(extinguisherDropSystem) extinguisherDropSystem.ResetBombs();

            DroneEvents.TakeOffSequenceStarted();
            
            GameObject firstTarget = _fireTargetsQueue.Dequeue();
            SetMissionTarget(firstTarget.transform.position);

            Debug.Log($"[Mission] 순차 화재 진압 임무 시작! 총 {fireTargets.Count}개의 목표. 첫 목표: {firstTarget.name}");

            SetTakeOffAltitudeAndState();
        }
        
        private void SetTakeOffAltitudeAndState()
        {
            currentPayload = PayloadType.FireExtinguishingBomb;
            Vector3 takeoffRefPos = droneStationLocation ? droneStationLocation.position : transform.position;
        
            if (Physics.Raycast(takeoffRefPos + Vector3.up, Vector3.down, out RaycastHit hit, terrainCheckDistance, groundLayerMask))
                _targetAltitudeAbs = hit.point.y + missionCruisingAgl;
            
            else
                _targetAltitudeAbs = takeoffRefPos.y + missionCruisingAgl;
        
            currentMissionState = DroneMissionState.TakingOff;
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
                
                if (currentMissionState == DroneMissionState.MovingToTarget || 
                    currentMissionState == DroneMissionState.RetreatingAfterAction ||
                    currentMissionState == DroneMissionState.PerformingAction ||
                    currentMissionState == DroneMissionState.HoldingPosition)
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
                    ApplyHorizontalDamping();
                    break;
                case DroneMissionState.Landing:
                    ApplyLandingForce();
                    break;
                case DroneMissionState.MovingToTarget:
                case DroneMissionState.ReturningToStation:
                case DroneMissionState.EmergencyReturn:
                case DroneMissionState.RetreatingAfterAction:
                    ApplyVerticalForce(2.0f);
                    ApplyHorizontalAndRotationalForces();
                    break;
                case DroneMissionState.IdleAtStation:
                    _rb.AddForce(-_rb.linearVelocity, ForceMode.VelocityChange);
                    _rb.AddTorque(-_rb.angularVelocity, ForceMode.VelocityChange);
                    // [수정] 정지 시 스무딩 변수도 리셋
                    _currentSmoothedVelocity = Vector3.zero;
                    _smoothedLookDirection = transform.forward;
                    break;
                case DroneMissionState.PerformingAction:
                case DroneMissionState.HoldingPosition:
                    ApplyVerticalForce(2.0f);
                    ApplyHorizontalDamping();
                     // [수정] 호버링 시 스무딩 변수 리셋
                    _currentSmoothedVelocity = Vector3.zero;
                    break;
            }
        }

        private void ApplyHorizontalDamping()
        {
            Vector3 horizontalVel = _rb.linearVelocity;
            horizontalVel.y = 0;
            _rb.AddForce(-horizontalVel, ForceMode.VelocityChange);
            _rb.AddTorque(-_rb.angularVelocity, ForceMode.VelocityChange);
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
                
                Vector3 horizontalVel = _rb.linearVelocity;
                horizontalVel.y = 0;
                _rb.AddForce(-horizontalVel * 0.2f, ForceMode.VelocityChange);
                _rb.AddTorque(-_rb.angularVelocity * 0.2f, ForceMode.VelocityChange);
            }
        }

        // ====================================================================================
        // [핵심 수정] 수평/회전 힘 적용 로직을 스무딩을 사용하도록 변경
        private void ApplyHorizontalAndRotationalForces()
        {
            // 1. 목표 위치와 현재 위치의 수평 벡터 계산
            Vector3 currentPosXZ = transform.position;
            currentPosXZ.y = 0;
            Vector3 targetPosXZ = _currentTargetPosition;
            targetPosXZ.y = 0;
            Vector3 directionToTarget = (targetPosXZ - currentPosXZ);
            float distanceToTarget = directionToTarget.magnitude;

            // 2. 부드러운 회전을 위한 목표 방향 계산 (Slerp)
            Vector3 targetLookDirection = (distanceToTarget > 0.01f) ? directionToTarget.normalized : transform.forward;
            _smoothedLookDirection = Vector3.Slerp(_smoothedLookDirection, targetLookDirection, Time.fixedDeltaTime / rotationSmoothTime);
            Quaternion targetRotation = Quaternion.LookRotation(_smoothedLookDirection);
            
            // 3. 목표에 가까워질수록 감속하는 로직
            float effectiveMoveForce = moveForce;
            if (distanceToTarget < decelerationStartDistanceXZ)
            {
                effectiveMoveForce = Mathf.Lerp(moveForce * 0.2f, moveForce, distanceToTarget / decelerationStartDistanceXZ);
            }

            // 4. 부드러운 속도 변화를 위한 목표 속도 계산 (Lerp)
            Vector3 desiredVelocityXZ = _smoothedLookDirection * effectiveMoveForce;
            float angleToTarget = Quaternion.Angle(transform.rotation, targetRotation);
            
            // 아직 목표를 완전히 바라보지 못했다면 이동 속도를 줄여 회전에 집중
            if (angleToTarget > turnBeforeMoveAngleThreshold)
            {
                desiredVelocityXZ *= 0.2f;
            }

            // 현재의 부드러운 속도를 목표 속도 쪽으로 점진적으로 변경
            _currentSmoothedVelocity = Vector3.Lerp(_currentSmoothedVelocity, desiredVelocityXZ, Time.fixedDeltaTime / velocitySmoothTime);

            // 5. 계산된 부드러운 목표 속도에 도달하기 위한 힘 적용
            Vector3 currentVelocityXZ = _rb.linearVelocity;
            currentVelocityXZ.y = 0;
            Vector3 forceNeededXZ = (_currentSmoothedVelocity - currentVelocityXZ) * 3.0f; // 3.0f는 힘의 강도 조절 계수
            _rb.AddForce(forceNeededXZ, ForceMode.Acceleration);

            // 6. 계산된 부드러운 목표 회전을 향한 토크 적용 (PD 컨트롤러)
            float targetAngleY = targetRotation.eulerAngles.y;
            float angleErrorY = Mathf.DeltaAngle(_rb.rotation.eulerAngles.y, targetAngleY);
            float pTorque = angleErrorY * Mathf.Deg2Rad * kpRotation;
            float dTorque = -_rb.angularVelocity.y * kdRotation;
            _rb.AddTorque(Vector3.up * Mathf.Clamp(pTorque + dTorque, -maxRotationTorque, maxRotationTorque), ForceMode.Acceleration);
        }
        // ====================================================================================
        
        public void DispatchMissionToTestTarget()
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
            StartSingleTargetMission(testDispatchTarget.position);
            SendDispatchDataToServer("수동 타겟 임무 (테스트)", testDispatchTarget.position);
        }

        public void DispatchMissionToRandomFire()
        {
            if (currentMissionState != DroneMissionState.IdleAtStation) 
            {
                Debug.LogWarning("[Mission] 드론이 현재 임무 수행 중입니다."); 
                return;
            }
            if (!WildfireManager.Instance)
            {
                Debug.LogError("[Mission] WildfireManager 인턴스를 찾을 수 없습니다!");
                return;
            }
            if (!WildfireManager.Instance.isFireActive)
            {
                Debug.Log("[Mission] 화재가 없으므로 새로 생성합니다.");
                WildfireManager.Instance.GenerateFires();
            }
            
            List<GameObject> fireTargets = WildfireManager.Instance.GetActiveFires();
            StartFireSuppressionMission(fireTargets);
            
            if (fireTargets.Count > 0)
            {
                SendDispatchDataToServer("랜덤 화재 진압 (테스트)", fireTargets[0].transform.position);
            }
        }

        private void SendDispatchDataToServer(string missionType, Vector3 targetPosition)
        {
            DispatchData dispatchData = new DispatchData(missionType, targetPosition);
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
                    _dataToSend.position.x = CurrentPositionAbs.x;
                    _dataToSend.position.y = CurrentPositionAbs.y;
                    _dataToSend.position.z = CurrentPositionAbs.z;
                    _dataToSend.altitude = CurrentAltitudeAbs;
                    _dataToSend.battery = batteryLevel;
                    _dataToSend.mission_state = _missionStateStrings[(int)currentMissionState];
                    _dataToSend.payload_type = _payloadTypeStrings[(int)currentPayload];
                    _dataToSend.bomb_load = _currentBombLoad;
                    
                    string droneDataJson = JsonUtility.ToJson(_dataToSend);
                    
                    _socketMessageBuilder.Clear();
                    _socketMessageBuilder.Append("42[\"unity_main_drone_data\",");
                    _socketMessageBuilder.Append(droneDataJson);
                    _socketMessageBuilder.Append("]");
                    
                    _ws.Send(_socketMessageBuilder.ToString());
                }
            }
        }

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
                var job = new SocketMessageJob
                {
                    Controller = this,
                    JsonString = e.Data.Substring(2)
                };
                UnityMainThreadDispatcher.Instance.Enqueue(job.Execute);
            }
            else if (e.Data == "2")
            { 
                _ws.Send("3");
            }
        }
        
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
                _currentTargetPosition = droneStationLocation.position;
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