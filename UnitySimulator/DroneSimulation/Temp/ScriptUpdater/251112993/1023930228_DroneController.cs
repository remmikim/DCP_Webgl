// C:\DroneControlProject\UnityDroneSimulator\Assets\Scripts\DroneController.cs

using UnityEngine;
using System.Collections;
using WebSocketSharp; // Make sure this library is correctly in Assets/Plugins
using System;
using SimpleJSON; // Make sure SimpleJSON.cs is in Assets/Scripts or similar

// --- 데이터 구조 클래스 (별도 파일로 분리 권장) ---
[System.Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;

    public Vector3Data(float xVal, float yVal, float zVal)
    {
        x = xVal;
        y = yVal;
        z = zVal;
    }
}

[System.Serializable]
public class DroneStatusData
{
    public Vector3Data position;
    public float altitude; // 드론의 현재 절대 Y 고도 (currentAltitude_abs)
    public float battery;
    public string mission_state;
    public int bomb_load;

    public DroneStatusData(Vector3 pos, float alt, float bat, string state, int bombs)
    {
        position = new Vector3Data(pos.x, pos.y, pos.z);
        altitude = alt;
        battery = bat;
        mission_state = state;
        bomb_load = bombs;
    }
}

// --- 메인 스레드 디스패처 클래스 (별도 파일로 분리 권장) ---
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly System.Collections.Generic.Queue<Action> _executionQueue =
        new System.Collections.Generic.Queue<Action>();

    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            _instance = FindObjectOfType<UnityMainThreadDispatcher>();
            if (_instance == null)
            {
                GameObject go = new GameObject("MainThreadDispatcher_Instance"); // Ensure unique name if multiple exist
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }

        return _instance;
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject); // 중복 인스턴스 제거
        }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}


// --- 드론 컨트롤러 주 클래스 ---
public class DroneController : MonoBehaviour
{
    private Rigidbody rb;
    [Header("드론 기본 성능")] public float hoverForce = 70.0f;
    public float moveForce = 15.0f; // 자율 이동 시 목표 속도
    public float rotateSpeed = 80.0f; // 직접 사용되지 않음, maxRotationTorque와 Kp/Kd_rotation으로 제어

    private Coroutine bombDropCoroutine = null;

    // --- 임무 상태 및 관련 변수 ---
    public enum DroneMissionState
    {
        IdleAtStation,
        TakingOff,
        MovingToWildfire,
        DroppingBombs,
        ReturningToStation,
        Landing,
        EmergencyReturn,
        HoldingPositionAGL
    }

    [Header("임무 상태")] public DroneMissionState currentMissionState = DroneMissionState.IdleAtStation;

    [Header("임무 설정")] public Transform droneStationLocation; // 중요: 여기에 'LandingPad' 오브젝트를 할당하세요!
    public GameObject bombPrefab;
    public int totalBombs = 6;
    private int currentBombLoad;
    private int fireScaleBombCount; // 현재 임무에 사용할 폭탄 수

    public float missionCruisingAGL = 50.0f; // 지면 기준 순항 고도 (AGL)
    private Vector3 currentTargetPosition_xz; // 현재 XZ 이동 목표 (Y는 AGL로 결정)
    public float arrivalDistanceThreshold = 2.0f;
    public float bombDropInterval = 0.5f;

    // --- 고도 제어 (PD 및 AGL) ---
    [Header("고도 제어 (PD & AGL)")] public float targetAltitude_abs; // PD 제어기의 현재 목표 절대 Y 고도 (AGL에 의해 동적 업데이트됨)
    public float Kp_altitude = 2.0f;
    public float Kd_altitude = 2.5f;
    public float landingDescentRate = 0.4f;
    public float terrainCheckDistance = 200.0f;
    public LayerMask groundLayerMask; // 지형으로 인식할 레이어
    private float currentGroundY_AGL; // 감지된 현재 드론 발밑 지면의 Y 좌표

    // --- 자율 이동 및 회전 개선 ---
    [Header("자율 이동 및 회전 개선")] public float Kp_rotation = 0.8f; // 회전 P 이득 (튜닝 필요)
    public float Kd_rotation = 0.3f; // 회전 D 이득 (튜닝 필요)
    public float turnBeforeMoveAngleThreshold = 15.0f; // 도
    public float decelerationStartDistanceXZ = 15.0f;
    public float maxRotationTorque = 15.0f; // 최대 회전 토크 제한 (튜닝 필요)

    // --- 웹소켓 ---
    private WebSocket ws;
    private string serverUrl = "ws://127.0.0.1:5000/socket.io/?EIO=4&transport=websocket&type=unity";

    [Header("드론 실시간 상태")] public float currentAltitude_abs; // 현재 드론의 절대 Y 고도
    public Vector3 currentPosition_abs;
    public float batteryLevel = 100.0f;

    // --- 내부 상태 플래그 ---
    private bool isTakingOff_subState = false;
    private bool isLanding_subState = false;
    private bool isPhysicallyStopped = true;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("[Unity] Rigidbody component not found!");
            enabled = false;
            return;
        }

        rb.useGravity = false;
        rb.angularDamping = 2.5f; // 회전 안정성을 위해 각도 감쇠 약간 증가 (튜닝 필요)

        ConnectWebSocket();
        StartCoroutine(SendDroneDataRoutine());
        UnityMainThreadDispatcher.Instance(); // 디스패처 초기화 보장

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
        fireScaleBombCount = totalBombs;

        Debug.Log(
            $"[Mission] Drone initialized. State: {currentMissionState}, Bombs: {currentBombLoad}, Initial Target Abs Alt: {targetAltitude_abs:F2}");
    }

    void PerformInitialGroundCheckAndSetAltitude()
    {
        RaycastHit hit;
        Vector3 checkPos = transform.position; // 현재 드론 위치 기준
        if (Physics.Raycast(checkPos + Vector3.up * 1.0f, Vector3.down, out hit, terrainCheckDistance + 1.0f,
                groundLayerMask))
        {
            currentGroundY_AGL = hit.point.y;
        }
        else
        {
            currentGroundY_AGL = checkPos.y;
            Debug.LogWarning(
                $"[AGL] Initial ground check failed at {checkPos}. Using its Y as ground reference: {currentGroundY_AGL:F2}");
        }

        targetAltitude_abs = transform.position.y; // 이륙 전에는 현재 고도(지상)를 목표로.
    }

    void Update()
    {
        UpdateDroneInternalStatus();
        UpdateTerrainSensingAndDynamicTargetAltitude();

        switch (currentMissionState)
        {
            case DroneMissionState.IdleAtStation: Handle_IdleAtStation(); break;
            case DroneMissionState.TakingOff: Handle_TakingOff(); break;
            case DroneMissionState.MovingToWildfire: Handle_MovingToTarget(); break;
            case DroneMissionState.DroppingBombs: /* 코루틴이 주로 제어 */ break;
            case DroneMissionState.ReturningToStation: Handle_MovingToTarget(); break;
            case DroneMissionState.EmergencyReturn: Handle_MovingToTarget(); break;
            case DroneMissionState.HoldingPositionAGL: Handle_HoldingPositionAGL(); break;
            case DroneMissionState.Landing: Handle_Landing(); break;
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        ApplyForcesBasedOnState();
    }

    void UpdateTerrainSensingAndDynamicTargetAltitude()
    {
        RaycastHit hit;
        // 드론의 현재 위치 바로 아래에서 Raycast
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, terrainCheckDistance,
                groundLayerMask))
        {
            currentGroundY_AGL = hit.point.y;
        }
        else
        {
            // 지형 감지 실패 시, 마지막으로 유효했던 currentGroundY_AGL 값을 유지 (갑작스러운 고도 변경 방지)
            Debug.LogWarning(
                $"[AGL] Ground not detected below drone. Using last known ground Y: {currentGroundY_AGL:F2}");
        }

        // 이륙 중이거나 착륙 중이 아닐 때만 AGL 목표 고도를 동적으로 업데이트
        if (currentMissionState != DroneMissionState.TakingOff && currentMissionState != DroneMissionState.Landing)
        {
            targetAltitude_abs = currentGroundY_AGL + missionCruisingAGL;
        }
        // 이륙/착륙 중에는 targetAltitude_abs가 해당 로직 시작 시점에 특정 값(이륙 목표 절대 고도, 착륙 지면 절대 고도)으로 설정됨
    }

    // --- 상태 처리 함수들 ---
    void Handle_IdleAtStation()
    {
        /* 웹소켓 메시지로 상태 변경 대기 (OnWebSocketMessage 에서 처리) */
    }

    void Handle_TakingOff()
    {
        // ApplyForcesBasedOnState 내의 isTakingOff_subState 로직이 실제 이륙을 수행
        // targetAltitude_abs는 wildfire_alert_command 수신 시 (초기지면Y + missionCruisingAGL)로 설정됨
        if (!isTakingOff_subState && currentAltitude_abs >= targetAltitude_abs - 0.2f) // 이륙 완료 조건
        {
            Debug.Log("[Mission] Takeoff complete.");
            currentMissionState = DroneMissionState.MovingToWildfire;
            // targetAltitude_abs는 이제 UpdateTerrainSensing에 의해 AGL 기준으로 계속 업데이트됨
            Debug.Log($"[Mission] State: MovingToWildfire, Target XZ: {currentTargetPosition_xz}");
        }
    }

    void Handle_MovingToTarget()
    {
        Vector3 currentPosXZ_flat = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 targetPosXZ_flat = new Vector3(currentTargetPosition_xz.x, 0, currentTargetPosition_xz.z);

        if (Vector3.Distance(currentPosXZ_flat, targetPosXZ_flat) < arrivalDistanceThreshold)
        {
            if (currentMissionState == DroneMissionState.MovingToWildfire)
            {
                Debug.Log("[Mission] Arrived at wildfire target area.");
                currentMissionState = DroneMissionState.DroppingBombs;
                // targetAltitude_abs는 AGL에 의해 유지됨
                if (bombDropCoroutine != null) StopCoroutine(bombDropCoroutine);
                bombDropCoroutine = StartCoroutine(BombDropSequenceCoroutine());
                Debug.Log("[Mission] State: DroppingBombs");
            }
            else if (currentMissionState == DroneMissionState.ReturningToStation ||
                     currentMissionState == DroneMissionState.EmergencyReturn)
            {
                Debug.Log("[Mission] Arrived at station area.");
                currentMissionState = DroneMissionState.Landing;
                isLanding_subState = true;
                // 착륙 목표 Y를 droneStationLocation (LandingPad)의 정확한 Y로 설정
                if (droneStationLocation != null)
                {
                    targetAltitude_abs = droneStationLocation.position.y; // LandingPad의 Y좌표
                }
                else
                {
                    targetAltitude_abs = 0.1f; // 스테이션 정보 없으면 기본 지면 (오류 상황)
                    Debug.LogError("[Mission] Landing initiated but DroneStationLocation (LandingPad) is null!");
                }

                Debug.Log($"[Mission] State: Landing, Target Abs Alt (LandingPad Y): {targetAltitude_abs:F2}");
            }
        }
    }

    void Handle_HoldingPositionAGL()
    {
        // 이 상태에서는 ApplyForcesBasedOnState의 AGL 호버링 로직이 작동 (XZ 이동은 최소화됨)
        // 외부 명령(예: 새로운 임무, 강제 귀환)을 기다리거나 특정 조건 만족 시 다른 상태로 천이
    }

    IEnumerator BombDropSequenceCoroutine()
    {
        Debug.Log($"[Mission] Starting bomb drop sequence. Bombs to drop: {fireScaleBombCount}");
        int actualBombsToDrop = Mathf.Min(fireScaleBombCount, currentBombLoad);

        for (int i = 0; i < actualBombsToDrop; i++)
        {
            if (bombPrefab != null)
            {
                // 폭탄 투하 위치: 드론의 중심 바로 아래 (드론 크기 고려하여 Y 오프셋 조절)
                Vector3 dropPosition = transform.position - (transform.up * 1.5f); // Y오프셋 1.5f는 예시
                Instantiate(bombPrefab, dropPosition, Quaternion.identity);
                currentBombLoad--;
                Debug.Log($"[Mission] Bomb dropped. Remaining: {currentBombLoad}");
            }
            else
            {
                Debug.LogError("[Mission] Bomb prefab missing!");
                break;
            }

            if (i < actualBombsToDrop - 1)
            {
                // 마지막 폭탄 투하 후에는 딜레이 없음
                yield return new WaitForSeconds(bombDropInterval);
            }
        }

        Debug.Log("[Mission] Bomb drop sequence finished.");
        bombDropCoroutine = null;

        if (droneStationLocation != null)
        {
            currentTargetPosition_xz = droneStationLocation.position; // 복귀 목표는 스테이션의 XZ
            currentMissionState = DroneMissionState.ReturningToStation;
            // targetAltitude_abs는 AGL에 의해 자동 설정됨
            Debug.Log($"[Mission] State: ReturningToStation, Target: {currentTargetPosition_xz}");
        }
        else
        {
            Debug.LogError("[Mission] Drone Station Location (LandingPad) not set! Cannot return. Holding position.");
            currentMissionState = DroneMissionState.HoldingPositionAGL; // 스테이션 없으면 현위치 AGL 호버링
            currentTargetPosition_xz = new Vector3(transform.position.x, 0, transform.position.z); // 현재 XZ에서 호버링
        }
    }

    void Handle_Landing()
    {
        // 착륙 완료 조건: isLanding_subState가 false이고, isPhysicallyStopped가 true이고,
        // 현재 고도가 목표 착륙 고도(LandingPad의 Y)에 매우 근접했을 때.
        if (!isLanding_subState && isPhysicallyStopped &&
            Mathf.Abs(currentAltitude_abs - targetAltitude_abs) < 0.15f) // 허용 오차
        {
            Debug.Log("[Mission] Landing complete at station.");
            currentMissionState = DroneMissionState.IdleAtStation;
            currentBombLoad = totalBombs; // 폭탄 재장전
            PerformInitialGroundCheckAndSetAltitude(); // 다음 임무 위해 targetAltitude_abs를 현재 지상으로 재설정
            if (droneStationLocation != null)
            {
                // 착륙 후 드론의 회전도 스테이션의 초기 회전으로
                transform.rotation = droneStationLocation.rotation;
            }

            Debug.Log(
                $"[Mission] State: IdleAtStation. Bombs: {currentBombLoad}. Target Abs Alt (Ground): {targetAltitude_abs:F2}");
        }
    }

    // --- 물리 힘 적용 ---
    void ApplyForcesBasedOnState()
    {
        rb.AddForce(Physics.gravity, ForceMode.Acceleration); // 기본 중력 항상 적용

        if (isTakingOff_subState)
        {
            // targetAltitude_abs는 이륙 명령시 (초기 지면 Y + missionCruisingAGL)로 설정됨
            if (currentAltitude_abs < targetAltitude_abs - 0.1f) // 목표 고도에 도달 전
            {
                float altError = targetAltitude_abs - currentAltitude_abs;
                float pForce = altError * Kp_altitude * 0.8f; // 이륙 시 P게인 약간 조절
                float dForce = -rb.linearVelocity.y * Kd_altitude * 0.5f; // 이륙 시 D게인 약간 조절
                float upwardForce = Physics.gravity.magnitude + pForce + dForce;
                rb.AddForce(Vector3.up * Mathf.Clamp(upwardForce, Physics.gravity.magnitude * 0.5f, hoverForce * 1.5f),
                    ForceMode.Acceleration);
            }
            else
            {
                isTakingOff_subState = false; /* 목표 고도 도달 시 플래그 해제 (Handle_TakingOff에서 다음 상태로) */
            }
        }
        else if (isLanding_subState)
        {
            // targetAltitude_abs는 착륙 명령시 목표 지면 Y (LandingPad의 Y)로 설정됨
            if (currentAltitude_abs > targetAltitude_abs + 0.05f) // 목표 Y보다 살짝 위에 있을 때까지만 힘 조절
            {
                float currentEffectiveDescentRate = landingDescentRate;
                if (currentAltitude_abs < targetAltitude_abs + 2.0f)
                    currentEffectiveDescentRate *= 0.5f; // 지면 가까울수록 더 천천히

                float upwardThrust = Mathf.Max(0, Physics.gravity.magnitude - currentEffectiveDescentRate);
                rb.AddForce(Vector3.up * upwardThrust, ForceMode.Acceleration);
                // 착륙 중 수평 이동 및 회전은 거의 없도록 강하게 감쇠
                rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.8f, rb.linearVelocity.y, rb.linearVelocity.z * 0.8f);
                rb.angularVelocity *= 0.8f;
            }
            else
            {
                // 목표 Y 도달 또는 통과 시 (지면에 거의 닿았을 때)
                isLanding_subState = false;
                isPhysicallyStopped = true; // 물리적 움직임 멈춤
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                // 최종 위치를 droneStationLocation (LandingPad)의 정확한 X, Y, Z로 설정
                if (droneStationLocation != null)
                {
                    transform.position = droneStationLocation.position; // LandingPad의 정중앙 위
                    transform.rotation = droneStationLocation.rotation; // LandingPad의 방향으로 정렬
                }
                else
                {
                    // 비상: 스테이션 정보 없으면 현재 위치에 착륙 시도 (Y는 targetAltitude_abs 기준)
                    transform.position = new Vector3(transform.position.x, targetAltitude_abs + 0.05f,
                        transform.position.z);
                }
            }
        }
        else if (currentMissionState == DroneMissionState.MovingToWildfire ||
                 currentMissionState == DroneMissionState.ReturningToStation ||
                 currentMissionState == DroneMissionState.EmergencyReturn)
        {
            // AGL 고도 유지 (PD 제어), targetAltitude_abs는 UpdateTerrainSensing에서 계속 업데이트됨
            float altError = targetAltitude_abs - currentAltitude_abs;
            float pForceAlt = altError * Kp_altitude;
            float dForceAlt = -rb.linearVelocity.y * Kd_altitude;
            float vertAdjust = pForceAlt + dForceAlt;
            float totalVertForce = Physics.gravity.magnitude + vertAdjust;
            rb.AddForce(Vector3.up * Mathf.Clamp(totalVertForce, 0.0f, hoverForce * 2.0f), ForceMode.Acceleration);

            // 자율 이동 중 수평 이동 및 회전
            Vector3 targetDirectionOnPlane =
                (new Vector3(currentTargetPosition_xz.x, transform.position.y, currentTargetPosition_xz.z) -
                 transform.position);
            float distanceToTargetXZ = new Vector3(targetDirectionOnPlane.x, 0, targetDirectionOnPlane.z).magnitude;

            if (targetDirectionOnPlane.sqrMagnitude > 0.001f) targetDirectionOnPlane.Normalize(); // 0벡터 방지

            if (distanceToTargetXZ > arrivalDistanceThreshold)
            {
                float effectiveMoveForce = moveForce;
                if (distanceToTargetXZ < decelerationStartDistanceXZ)
                {
                    // 목표 근처 감속
                    effectiveMoveForce = Mathf.Lerp(moveForce * 0.2f, moveForce,
                        distanceToTargetXZ / decelerationStartDistanceXZ);
                }

                Vector3 desiredVelocityXZ = targetDirectionOnPlane * effectiveMoveForce;
                Quaternion targetRotation = targetDirectionOnPlane != Vector3.zero
                    ? Quaternion.LookRotation(targetDirectionOnPlane)
                    : transform.rotation; // 0벡터일 때 현재 회전 유지
                float angleToTargetDegrees = Quaternion.Angle(transform.rotation, targetRotation);

                if (angleToTargetDegrees > turnBeforeMoveAngleThreshold)
                {
                    // 회전 우선
                    desiredVelocityXZ *= 0.2f; // 전진 속도 더 많이 줄임
                }

                Vector3 currentVelocityXZ = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                Vector3 forceNeededXZ = (desiredVelocityXZ - currentVelocityXZ) * 3.0f; // 수평 이동 P계수
                rb.AddForce(forceNeededXZ, ForceMode.Acceleration);

                // 회전 PD 제어 로직
                if (targetDirectionOnPlane.sqrMagnitude > 0.01f)
                {
                    // 목표 각도 계산 (Y축 기준)
                    float targetAngleY =
                        Mathf.Atan2(targetDirectionOnPlane.x, targetDirectionOnPlane.z) * Mathf.Rad2Deg;
                    float currentAngleY = rb.rotation.eulerAngles.y; // 현재 드론의 Y축 회전값
                    float angleErrorY = Mathf.DeltaAngle(currentAngleY, targetAngleY); // 목표 각도와의 최소 차이

                    // PD 제어 토크 계산
                    float pTorque = angleErrorY * Mathf.Deg2Rad * Kp_rotation; // 각도 오차에 비례
                    float dTorque = -rb.angularVelocity.y * Kd_rotation; // 각속도에 반비례 (감쇠)
                    float totalTorqueY = pTorque + dTorque;

                    rb.AddTorque(Vector3.up * Mathf.Clamp(totalTorqueY, -maxRotationTorque, maxRotationTorque),
                        ForceMode.Acceleration);
                }
            }
            else
            {
                // 목표 XZ 도착
                rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.7f, rb.linearVelocity.y, rb.linearVelocity.z * 0.7f); // 더 빠르게 감속
                rb.angularVelocity =
                    new Vector3(rb.angularVelocity.x, rb.angularVelocity.y * 0.7f, rb.angularVelocity.z);
            }
        }
        else if (!isPhysicallyStopped) // AGL 호버링 (DroppingBombs, HoldingPositionAGL 등)
        {
            // targetAltitude_abs는 UpdateTerrainSensing에서 AGL 기준으로 계속 업데이트됨
            float altError = targetAltitude_abs - currentAltitude_abs;
            float pForceAlt = altError * Kp_altitude;
            float dForceAlt = -rb.linearVelocity.y * Kd_altitude;
            float vertAdjust = pForceAlt + dForceAlt;
            float totalVertForce = Physics.gravity.magnitude + vertAdjust;
            rb.AddForce(Vector3.up * Mathf.Clamp(totalVertForce, 0.0f, hoverForce * 2.0f), ForceMode.Acceleration);

            // 수평 속도 및 각속도 감쇠 (제자리 유지)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.9f, rb.linearVelocity.y, rb.linearVelocity.z * 0.9f);
            rb.angularVelocity = new Vector3(rb.angularVelocity.x, rb.angularVelocity.y * 0.9f, rb.angularVelocity.z);
        }
        else if (isPhysicallyStopped && currentAltitude_abs < 0.5f) // 지상에서 완전 정지
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 10f); // 더 빠르게 정지
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
        }
    }

    // --- 웹소켓 연결 및 메시지 처리 ---
    void ConnectWebSocket()
    {
        ws = new WebSocket(serverUrl);
        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("[Unity] WebSocket Connected!");
            ws.Send("40");
        };
        ws.OnMessage += OnWebSocketMessage;
        ws.OnError += (sender, e) => { Debug.LogError("[Unity] WebSocket Error: " + e.Message); };
        ws.OnClose += (sender, e) =>
        {
            Debug.Log($"[Unity] WebSocket Closed. Code: {e.Code}, Reason: {e.Reason}");
            if (this != null && gameObject.activeInHierarchy) StartCoroutine(ReconnectWebSocket());
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

                    if (eventName == "emergency_return_command" || eventName == "force_return_command")
                    {
                        Debug.Log(
                            $"[Mission] Received {(eventName == "emergency_return_command" ? "Emergency" : "Force")} RETURN command!");
                        if (droneStationLocation != null)
                        {
                            currentTargetPosition_xz = droneStationLocation.position; // 목표 XZ를 스테이션으로
                            currentMissionState = DroneMissionState.EmergencyReturn;
                            isTakingOff_subState = false;
                            isLanding_subState = false;
                            isPhysicallyStopped = false;
                            if (bombDropCoroutine != null)
                            {
                                StopCoroutine(bombDropCoroutine);
                                bombDropCoroutine = null;
                            }

                            Debug.Log($"[Mission] State: EmergencyReturn, Target: {currentTargetPosition_xz}");
                        }
                        else
                        {
                            Debug.LogError("[Mission] Drone Station Location (LandingPad) not set for return!");
                        }
                    }
                    else if (eventName == "emergency_stop_command")
                    {
                        Debug.Log("[Mission] Received Emergency STOP command!");
                        currentTargetPosition_xz =
                            new Vector3(transform.position.x, 0, transform.position.z); // 현재 XZ 위치에서 호버링
                        currentMissionState = DroneMissionState.HoldingPositionAGL;
                        isTakingOff_subState = false;
                        isLanding_subState = false;
                        isPhysicallyStopped = false;
                        if (bombDropCoroutine != null)
                        {
                            StopCoroutine(bombDropCoroutine);
                            bombDropCoroutine = null;
                        }

                        Debug.Log($"[Mission] State: HoldingPositionAGL at current XZ.");
                    }
                    else if (eventName == "wildfire_alert_command")
                    {
                        if (currentMissionState == DroneMissionState.IdleAtStation)
                        {
                            float x = eventData["coordinates"]["x"].AsFloat;
                            float y_ground = eventData["coordinates"]["y"].AsFloat;
                            float z = eventData["coordinates"]["z"].AsFloat;
                            fireScaleBombCount = eventData["fire_scale"].AsInt; // 화재 규모에 따른 폭탄 수

                            Vector3 wildfireGroundPos = new Vector3(x, y_ground, z);
                            Debug.Log(
                                $"[Mission] Received Wildfire Alert! Ground Coords: {wildfireGroundPos}, Bombs for mission: {fireScaleBombCount}");

                            currentTargetPosition_xz = wildfireGroundPos; // XZ 목표는 산불 지점

                            RaycastHit hit; // 이륙 고도 설정 (스테이션 지면 기준 AGL)
                            Vector3 takeoffRefPos = droneStationLocation != null
                                ? droneStationLocation.position
                                : transform.position;
                            if (Physics.Raycast(takeoffRefPos + Vector3.up * 0.5f, Vector3.down, out hit,
                                    terrainCheckDistance, groundLayerMask))
                            {
                                targetAltitude_abs = hit.point.y + missionCruisingAGL;
                            }
                            else
                            {
                                targetAltitude_abs = takeoffRefPos.y + missionCruisingAGL; // 감지 실패 시 현재 Y + AGL
                                Debug.LogWarning("[AGL] Ground check failed for takeoff, using ref Y + AGL.");
                            }

                            currentMissionState = DroneMissionState.TakingOff;
                            isTakingOff_subState = true;
                            isPhysicallyStopped = false;
                            currentBombLoad = totalBombs; // 임무 시작 시 폭탄 재장전 (또는 실제 적재량 반영)
                            Debug.Log(
                                $"[Mission] State: TakingOff, Target Ground: {currentTargetPosition_xz}, Initial Target Abs Alt for Takeoff: {targetAltitude_abs:F2}");
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"[Mission] Wildfire alert for {eventData["coordinates"]} received, but drone is busy: {currentMissionState}");
                        }
                    }
                    else if (eventName == "server_message")
                    {
                        Debug.Log($"[Unity] Server message: {eventData.Value}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Unity] Error parsing JSON (42): {ex.Message} - Data: {jsonString}");
                }
            });
        }
        else if (e.Data == "2")
        {
            ws.Send("3");
        }
        else if (e.Data.StartsWith("0{"))
        {
            Debug.Log($"[Unity] Engine.IO connected by server: {e.Data}");
        }
    }

    IEnumerator ReconnectWebSocket()
    {
        yield return new WaitForSeconds(5f);
        if (ws == null || !ws.IsAlive)
        {
            Debug.Log("[Unity] Attempting to reconnect...");
            ConnectWebSocket();
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

    IEnumerator SendDroneDataRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.2f);
            if (ws != null && ws.IsAlive)
            {
                DroneStatusData dataToSend = new DroneStatusData(currentPosition_abs, currentAltitude_abs, batteryLevel,
                    currentMissionState.ToString(), currentBombLoad);
                string droneDataJson = JsonUtility.ToJson(dataToSend);
                string socketIOMessage = "42[\"unity_drone_data\"," + droneDataJson + "]";
                ws.Send(socketIOMessage);
            }
        }
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