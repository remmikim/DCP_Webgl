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
using JWK.Scripts;
using JWK.Scripts.CameraManager;

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

        [Header("모델 비주얼 설정")]
        [Tooltip("기울일 드론의 비주얼 모델 Transform")]
        [SerializeField] private Transform droneModelTransform;
        [Tooltip("최대 기울기 각도")]
        [SerializeField] private float maxTiltAngle = 15.0f;
        [Tooltip("기울기 복원 시 스프링처럼 작용하는 힘의 강도입니다. 높을수록 더 빠르게 원래 각도로 돌아옵니다.")]
        [SerializeField] private float tiltSpringStiffness = 50f;
        [Tooltip("기울기 스윙이 멈추는 정도입니다. 0에 가까우면 바이킹처럼 많이 흔들리고, 1에 가까우면 거의 흔들리지 않고 멈춥니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float tiltDamping = 0.25f;
        private Quaternion _modelNeutralRotation;
        private Vector3 _modelNeutralPosition;
        private Vector2 _visualTilt; 
        private Vector2 _tiltVelocity; 


        [Header("임무 설정")]
        [SerializeField] private Transform droneStationLocation;
        [SerializeField] private float missionCruisingAgl = 50.0f;
        [SerializeField] private float arrivalDistanceThreshold = 0.1f;
        [SerializeField] private float preActionStabilizationTime = 0.5f;
        [Tooltip("폭탄 투하 후 다음 행동까지 대기하는 시간입니다.")]
        [SerializeField] private float postDropMoveDelay = 1.5f;
        [Tooltip("프로펠러가 최대 속도에 도달한 후 실제 이륙까지 대기하는 시간입니다.")]
        [SerializeField] private float preTakeoffDelay = 1.5f;
        [SerializeField] private float retreatDistance = 10.0f;
        private float _arrivalDistanceThresholdSqr;
        private Vector3 _currentTargetPosition; 
        private Vector3 _actualFireTargetPosition; 
        private int _currentBombLoad;
        [SerializeField] private int totalBombs = 6;
        
        [Header("고도 제어 (PD & AGL)")]
        [SerializeField] private float kpAltitude = 2.0f;
        [SerializeField] private float kdAltitude = 2.5f;
        [Tooltip("목표 고도 변경 시 얼마나 부드럽게 도달할지 결정합니다. 낮을수록 부드러워집니다.")]
        [SerializeField] private float altitudeSmoothSpeed = 1.5f;
        [SerializeField] private float landingDescentRate = 0.4f;
        [SerializeField] private float terrainCheckDistance = 50.0f;
        [SerializeField] private LayerMask groundLayerMask;
        private float _currentGroundYAgl;
        private float _targetAltitudeAbs;
        private float _smoothedTargetAltitudeAbs; 

        [Header("자율 이동 및 회전 개선")]
        [SerializeField] private float kpRotation = 0.8f;
        [SerializeField] private float kdRotation = 0.3f;
        [SerializeField] private float turnBeforeMoveAngleThreshold = 15.0f;
        [SerializeField] private float decelerationStartDistanceXZ = 15.0f;
        [SerializeField] private float maxRotationTorque = 15.0f;
        [Tooltip("회전이 얼마나 부드럽게 될지 결정합니다. 값을 높이면 회전이 더 부드러워집니다.")]
        [SerializeField] private float rotationSmoothTime = 1.2f;
        [Tooltip("속도 변경(가/감속)이 얼마나 부드럽게 될지 결정합니다. 값을 높이면 가감속이 더 부드러워집니다.")]
        [SerializeField] private float velocitySmoothTime = 1.2f;
        private Vector3 _smoothedLookDirection;
        private Vector3 _currentSmoothedVelocity;
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

            _smoothedLookDirection = transform.forward;
            _currentSmoothedVelocity = Vector3.zero;

            if (droneModelTransform)
            {
                _modelNeutralRotation = droneModelTransform.localRotation;
                _modelNeutralPosition = droneModelTransform.localPosition;
            }
            
            _visualTilt = Vector2.zero;
            _tiltVelocity = Vector2.zero;
        }

        private void Update()
        {
            UpdateDroneInternalStatus();
            RunStateMachine();
        }

        private void FixedUpdate()
        {
            SmoothAltitudeTarget();
            ApplyForcesBasedOnState();
        }

        private void LateUpdate()
        {
            if (droneModelTransform)
            {
                ApplyVisualTilt();
            }
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
            if (Mathf.Abs(CurrentAltitudeAbs - _smoothedTargetAltitudeAbs) < 0.5f)
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

                //====================================================================================
                // [수정된 부분] 목표 지점 도착 이벤트를 카메라 시스템에 알립니다.
                // 임시 게임오브젝트를 생성하여 정확한 위치의 Transform을 전달합니다.
                var fireTargetFocus = new GameObject("FireTargetFocusPoint");
                fireTargetFocus.transform.position = _actualFireTargetPosition;
                DroneCameraEvents.ArrivedAtDropZone(fireTargetFocus.transform);
                Destroy(fireTargetFocus, 5f); // 5초 뒤에 임시 오브젝트를 파괴합니다.
                //====================================================================================

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
                    yield return StartCoroutine(extinguisherDropSystem.DropSingleBomb(_actualFireTargetPosition, this.transform));
                    _currentBombLoad--;
                }
                else
                    Debug.LogWarning("ExtinguisherDropSystem이 없거나 폭탄을 모두 소진했습니다.");
            }
            
            yield return new WaitForSeconds(postDropMoveDelay);

            Vector3 retreatDirection = -transform.forward;
            Vector3 retreatPosition = transform.position + retreatDirection * retreatDistance;
            
            _currentTargetPosition = retreatPosition;

            currentMissionState = DroneMissionState.RetreatingAfterAction;
        
            _actionCoroutine = null;
        }
        
        private void DecideNextAction()
        {
            if (currentMissionState != DroneMissionState.RetreatingAfterAction)
            {
                return;
            }

            while (_fireTargetsQueue.Count > 0 && _currentBombLoad > 0)
            {
                GameObject nextTarget = _fireTargetsQueue.Dequeue();

                if (nextTarget)
                {
                    SetMissionTarget(nextTarget.transform.position);
                    currentMissionState = DroneMissionState.MovingToTarget;
                    
                    //====================================================================================
                    // [수정된 부분] 다음 화재 타겟으로 임무를 다시 시작한다는 이벤트를 보냅니다.
                    DroneCameraEvents.MissionStart(transform, nextTarget.transform);
                    //====================================================================================
                    return;
                }
                else
                {
                    Debug.LogWarning("[정보] 이미 파괴된 목표(유령 참조)를 큐에서 제거하고 다음을 탐색합니다.");
                }
            }
            
            _currentTargetPosition = droneStationLocation.position;
            _targetAltitudeAbs = droneStationLocation.position.y + 20f;
            currentMissionState = DroneMissionState.ReturningToStation;

            //====================================================================================
            // [수정된 부분] 기지로 복귀하므로, 카메라 시스템에 복귀 신호를 보냅니다.
            DroneCameraEvents.ReturnToStation();
            //====================================================================================
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

            SetMissionTarget(targetPosition);
            
            StartCoroutine(TakeOffSequenceCoroutine());
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

            GameObject firstTarget = _fireTargetsQueue.Dequeue();
            SetMissionTarget(firstTarget.transform.position);

            Debug.Log($"[Mission] 순차 화재 진압 임무 시작! 총 {fireTargets.Count}개의 목표. 첫 목표: {firstTarget.name}");

            //====================================================================================
            // [수정된 부분] 첫 화재 타겟으로 임무를 시작한다는 이벤트를 보냅니다.
            DroneCameraEvents.MissionStart(droneStationLocation, firstTarget.transform);
            //====================================================================================

            StartCoroutine(TakeOffSequenceCoroutine());
        }
        
        private IEnumerator TakeOffSequenceCoroutine()
        {
            DroneEvents.TakeOffSequenceStarted();

            yield return new WaitForSeconds(preTakeoffDelay);

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
            _smoothedTargetAltitudeAbs = _targetAltitudeAbs;
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

        private void SmoothAltitudeTarget()
        {
            _smoothedTargetAltitudeAbs = Mathf.Lerp(_smoothedTargetAltitudeAbs, _targetAltitudeAbs, Time.fixedDeltaTime * altitudeSmoothSpeed);
        }

        private void ApplyForcesBasedOnState()
        {
            _rb.AddForce(Physics.gravity, ForceMode.Acceleration);

            switch (currentMissionState)
            {
                case DroneMissionState.TakingOff:
                case DroneMissionState.MovingToTarget:
                case DroneMissionState.ReturningToStation:
                case DroneMissionState.EmergencyReturn:
                case DroneMissionState.RetreatingAfterAction:
                case DroneMissionState.PerformingAction:
                case DroneMissionState.HoldingPosition:
                    ApplyVerticalForce(2.0f);
                    ApplyHorizontalAndRotationalForces();
                    break;
                case DroneMissionState.Landing:
                    ApplyLandingForce();
                    break;
                case DroneMissionState.IdleAtStation:
                    _rb.AddForce(-_rb.linearVelocity, ForceMode.VelocityChange);
                    _rb.AddTorque(-_rb.angularVelocity, ForceMode.VelocityChange);
                    _currentSmoothedVelocity = Vector3.zero;
                    _smoothedLookDirection = transform.forward;
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
            float altError = _smoothedTargetAltitudeAbs - CurrentAltitudeAbs;
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
                
                ApplyHorizontalDamping();
            }
        }

        private void ApplyHorizontalAndRotationalForces()
        {
            if (currentMissionState == DroneMissionState.IdleAtStation ||
                currentMissionState == DroneMissionState.Landing ||
                currentMissionState == DroneMissionState.TakingOff)
            {
                ApplyHorizontalDamping();
                return;
            }
            
            if (currentMissionState == DroneMissionState.PerformingAction ||
                currentMissionState == DroneMissionState.HoldingPosition)
            {
                ApplyHorizontalDamping();
                _currentSmoothedVelocity = Vector3.zero;
                return;
            }

            Vector3 currentPosXZ = transform.position;
            currentPosXZ.y = 0;
            Vector3 targetPosXZ = _currentTargetPosition;
            targetPosXZ.y = 0;
            Vector3 directionToTarget = (targetPosXZ - currentPosXZ);
            float distanceToTarget = directionToTarget.magnitude;

            Vector3 targetLookDirection = (distanceToTarget > 0.01f) ? directionToTarget.normalized : transform.forward;
            _smoothedLookDirection = Vector3.Slerp(_smoothedLookDirection, targetLookDirection, Time.fixedDeltaTime / rotationSmoothTime);
            Quaternion targetRotation = Quaternion.LookRotation(_smoothedLookDirection);
            
            float desiredSpeed = moveForce;
            if (distanceToTarget < decelerationStartDistanceXZ)
            {
                desiredSpeed = Mathf.SmoothStep(0f, moveForce, distanceToTarget / decelerationStartDistanceXZ);
            }

            Vector3 desiredVelocityXZ = _smoothedLookDirection * desiredSpeed;
            
            float angleToTarget = Quaternion.Angle(transform.rotation, targetRotation);
            
            if (angleToTarget > turnBeforeMoveAngleThreshold)
            {
                desiredVelocityXZ *= 0.2f;
            }

            _currentSmoothedVelocity = Vector3.Lerp(_currentSmoothedVelocity, desiredVelocityXZ, Time.fixedDeltaTime / velocitySmoothTime);

            Vector3 currentVelocityXZ = _rb.linearVelocity;
            currentVelocityXZ.y = 0;
            Vector3 forceNeededXZ = (_currentSmoothedVelocity - currentVelocityXZ) * 3.0f;
            _rb.AddForce(forceNeededXZ, ForceMode.Acceleration);

            float targetAngleY = targetRotation.eulerAngles.y;
            float angleErrorY = Mathf.DeltaAngle(_rb.rotation.eulerAngles.y, targetAngleY);
            float pTorque = angleErrorY * Mathf.Deg2Rad * kpRotation;
            float dTorque = -_rb.angularVelocity.y * kdRotation;
            _rb.AddTorque(Vector3.up * Mathf.Clamp(pTorque + dTorque, -maxRotationTorque, maxRotationTorque), ForceMode.Acceleration);
        }
        
        private void ApplyVisualTilt()
        {
            if (_rb == null || droneModelTransform == null) return;

            Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity);
            float targetPitch = -Mathf.Clamp(localVelocity.z / moveForce, -1f, 1f) * maxTiltAngle;
            float roll = Mathf.Clamp(localVelocity.x / moveForce, -1f, 1f) * maxTiltAngle;
            Vector2 targetTilt = new Vector2(targetPitch, roll);

            Vector2 springForce = (targetTilt - _visualTilt) * tiltSpringStiffness;

            float dampingCoefficient = tiltDamping * 2 * Mathf.Sqrt(tiltSpringStiffness);
            
            Vector2 dampingForce = -_tiltVelocity * dampingCoefficient;

            Vector2 acceleration = springForce + dampingForce;

            _tiltVelocity += acceleration * Time.deltaTime;
            
            _visualTilt += _tiltVelocity * Time.deltaTime;

            Quaternion finalRotation = _modelNeutralRotation * Quaternion.Euler(_visualTilt.x, 0, _visualTilt.y);
            droneModelTransform.localRotation = finalRotation;
            
            droneModelTransform.localPosition = _modelNeutralPosition;
        }

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
            
            //====================================================================================
            // [수정된 부분] 테스트 타겟으로 임무 시작 이벤트를 보냅니다.
            DroneCameraEvents.MissionStart(droneStationLocation, testDispatchTarget);
            //====================================================================================
            
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
