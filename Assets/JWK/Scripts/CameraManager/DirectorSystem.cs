using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JWK.Scripts.CameraManager;

namespace JWK.Scripts
{
    /// <summary>
    /// 모든 카메라 워크를 총괄하는 시네마틱 감독 시스템입니다.
    /// 드론의 상태에 따라 역동적인 카메라 샷을 연출합니다.
    /// </summary>
    public class DirectorSystem : MonoBehaviour
    {
        [Header("핵심 타겟")]
        public Transform DroneTarget;
        public Camera ImpactCamera;

        [Header("카메라 워크 설정")]
        [SerializeField] private float _smoothTime = 1.2f;

        private Coroutine _currentCameraWork;

        private void OnEnable()
        {
            // 드론의 상태 변경 이벤트를 구독합니다.
            DroneCameraEvents.OnMissionStart += HandleMissionStart;
            DroneCameraEvents.OnArrivedAtDropZone += HandleArrivedAtDropZone;
            DroneCameraEvents.OnBombImpact += HandleBombImpact;
            DroneCameraEvents.OnReturnToStation += HandleReturnToStation;
        }

        private void OnDisable()
        {
            // 이벤트 구독을 해제합니다.
            DroneCameraEvents.OnMissionStart -= HandleMissionStart;
            DroneCameraEvents.OnArrivedAtDropZone -= HandleArrivedAtDropZone;
            DroneCameraEvents.OnBombImpact -= HandleBombImpact;
            DroneCameraEvents.OnReturnToStation -= HandleReturnToStation;
        }

        private void Start()
        {
            if (ImpactCamera != null)
            {
                ImpactCamera.gameObject.SetActive(false);
            }
            // 시작 시 기본 추적 카메라를 실행합니다.
            HandleReturnToStation();
        }

        // 임무 시작: 웨이포인트 카메라 워크 시작
        private void HandleMissionStart(Transform startPoint, Transform fireTarget)
        {
            SwitchCameraWork(WaypointTransition(startPoint, fireTarget));
        }

        // 목표 도착: 투하 지점 오르빗 카메라 워크 시작
        private void HandleArrivedAtDropZone(Transform fireTarget)
        {
            SwitchCameraWork(OrbitDropZone(fireTarget));
        }
        
        // 소화탄 충돌: 임팩트 카메라 활성화
        private void HandleBombImpact(Vector3 impactPosition)
        {
            if (ImpactCamera != null)
            {
                StartCoroutine(ShowImpact(impactPosition));
            }
        }

        // 기지 복귀: 기본 추적 카메라 워크 시작
        private void HandleReturnToStation()
        {
            SwitchCameraWork(FollowDrone());
        }

        // 현재 진행 중인 카메라 워크를 중단하고 새로운 워크로 전환합니다.
        private void SwitchCameraWork(IEnumerator newCameraWork)
        {
            if (_currentCameraWork != null)
            {
                StopCoroutine(_currentCameraWork);
            }
            _currentCameraWork = StartCoroutine(newCameraWork);
        }

        // --- 카메라 워크 코루틴들 ---

        // 1. 웨이포인트 통과 샷
        private IEnumerator WaypointTransition(Transform start, Transform end)
        {
            Vector3 startPos = start.position;
            Vector3 endPos = end.position;
            float distance = Vector3.Distance(startPos, endPos);

            // 웨이포인트 생성
            List<Vector3> waypoints = new List<Vector3>();
            Vector3 direction = (endPos - startPos).normalized;
            Vector3 side = Vector3.Cross(direction, Vector3.up).normalized * (distance / 8f); // 경로 측면으로 벗어나는 정도

            waypoints.Add(transform.position); // 현재 카메라 위치에서 시작
            waypoints.Add(startPos + direction * (distance * 0.2f) + side);
            if (distance > 50f) // 거리가 멀면 중간 포인트 추가
            {
                waypoints.Add(startPos + direction * (distance * 0.5f) - side * 1.2f);
            }
            waypoints.Add(endPos - direction * 20f + new Vector3(0, 10f, 0)); // 목표 지점 근처 상공

            // 웨이포인트를 따라 부드럽게 이동
            foreach (var point in waypoints)
            {
                float journey = 0f;
                float duration = Vector3.Distance(transform.position, point) / 30f; // 이동 속도
                duration = Mathf.Max(duration, 1.5f); // 최소 이동 시간

                Vector3 startPoint = transform.position;
                Quaternion startRotation = transform.rotation;

                while (journey < duration)
                {
                    journey += Time.deltaTime;
                    float percent = Mathf.SmoothStep(0, 1, journey / duration);
                    transform.position = Vector3.Lerp(startPoint, point, percent);
                    Quaternion targetRotation = Quaternion.LookRotation(DroneTarget.position - transform.position);
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, percent);
                    yield return null;
                }
            }
        }

        // 2. 투하 지점 오르빗 샷
        private IEnumerator OrbitDropZone(Transform fireTarget)
        {
            float orbitSpeed = 10f;
            while (true)
            {
                transform.RotateAround(fireTarget.position, Vector3.up, orbitSpeed * Time.deltaTime);
                Quaternion targetRotation = Quaternion.LookRotation(DroneTarget.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / _smoothTime);
                yield return null;
            }
        }

        // 3. 기본 추적 샷
        private IEnumerator FollowDrone()
        {
            Vector3 offset = new Vector3(0, 7f, -15f);
            while (true)
            {
                Vector3 desiredPosition = DroneTarget.position + offset;
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime / _smoothTime);
                Quaternion targetRotation = Quaternion.LookRotation(DroneTarget.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / _smoothTime);
                yield return null;
            }
        }

        // 4. 임팩트 샷 (서브 카메라)
        private IEnumerator ShowImpact(Vector3 position)
        {
            ImpactCamera.gameObject.SetActive(true);
            ImpactCamera.transform.position = position + new Vector3(0, 3f, -5f);
            ImpactCamera.transform.LookAt(position);
            
            yield return new WaitForSeconds(2.0f); // 2초간 보여줌
            
            ImpactCamera.gameObject.SetActive(false);
        }
    }
}
