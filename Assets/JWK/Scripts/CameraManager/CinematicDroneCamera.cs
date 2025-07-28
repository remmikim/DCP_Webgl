using UnityEngine;

namespace JWK.Scripts
{
    /// <summary>
    /// 드론과 화재 지점을 동적으로 추적하며 시네마틱한 뷰를 제공하는 카메라 스크립트입니다.
    /// </summary>
    public class CinematicDroneCamera : MonoBehaviour
    {
        [Header("타겟 설정")]
        [Tooltip("카메라가 따라갈 드론의 Transform 입니다.")]
        public Transform DroneTarget;

        // 화재 지점은 DroneController가 자동으로 설정해줍니다.
        private Transform _fireTarget;

        [Header("카메라 포지셔닝")]
        [Tooltip("카메라가 두 타겟의 중심점에서 얼마나 높이 있을지 결정합니다.")]
        [SerializeField] private float heightOffset = 4.0f;
        [Tooltip("카메라가 타겟 측면에서 얼마나 떨어져 있을지 결정합니다.")]
        [SerializeField] private float sideOffset = 12.0f;
        [Tooltip("두 타겟 사이의 거리에 따라 카메라가 얼마나 뒤로 물러날지 결정하는 계수입니다.")]
        [SerializeField] private float distanceMultiplier = 1.1f;
        [Tooltip("카메라가 타겟에 가장 가까워질 수 있는 최소 거리입니다.")]
        [SerializeField] private float minDistance = 8.0f;

        [Header("카메라 움직임 부드러움")]
        [Tooltip("카메라 위치 이동의 부드러움입니다. 낮을수록 빠르게 반응합니다.")]
        [SerializeField] private float positionSmoothTime = 0.8f;
        [Tooltip("카메라 회전의 부드러움입니다. 낮을수록 빠르게 반응합니다.")]
        [SerializeField] private float rotationSmoothTime = 0.8f;
        //====================================================================================
        // [수정된 부분] 카메라 회전 고정 옵션 추가
        [Tooltip("이 옵션을 체크하면 카메라가 회전하지 않고 초기 방향을 유지합니다.")]
        [SerializeField] private bool lockRotation = false;
        //====================================================================================

        private Vector3 _currentPositionVelocity;
        
        private Vector3 _followModeOffset; 
        private bool _isInFollowMode = false; 

        //====================================================================================
        // [수정된 부분] 카메라의 초기 회전값을 저장할 변수
        private Quaternion _initialRotation;
        //====================================================================================

        private void Start()
        {
            //====================================================================================
            // [수정된 부분] 게임 시작 시 카메라의 초기 회전값을 저장합니다.
            _initialRotation = transform.rotation;
            //====================================================================================
        }

        /// <summary>
        /// DroneController가 화재 타겟을 설정하거나 해제할 때 호출하는 함수입니다.
        /// </summary>
        public void SetFireTarget(Transform newTarget)
        {
            _fireTarget = newTarget;
            
            if (_fireTarget == null && DroneTarget != null)
            {
                _followModeOffset = transform.position - DroneTarget.position;
                _isInFollowMode = true; 
            }
            else
            {
                _isInFollowMode = false;
            }
        }

        private void LateUpdate()
        {
            if (DroneTarget == null) return;

            // 1. 카메라의 목표 위치와 바라볼 지점을 계산합니다.
            Vector3 focusPoint;
            Vector3 desiredPosition;
            
            if (_isInFollowMode)
            {
                focusPoint = DroneTarget.position;
                desiredPosition = DroneTarget.position + _followModeOffset;
            }
            else if (_fireTarget != null)
            {
                Vector3 directionToTarget;
                focusPoint = (DroneTarget.position + _fireTarget.position) / 2f;
                directionToTarget = (_fireTarget.position - DroneTarget.position).normalized;
                directionToTarget.y = 0; 

                float distanceBetweenTargets = Vector3.Distance(DroneTarget.position, _fireTarget.position);
                float dynamicDistance = Mathf.Max(minDistance, distanceBetweenTargets * distanceMultiplier);

                Vector3 sideOffsetVector = Vector3.Cross(directionToTarget, Vector3.up) * sideOffset;
                desiredPosition = focusPoint - (directionToTarget * dynamicDistance) + sideOffsetVector + (Vector3.up * heightOffset);
            }
            else
            {
                focusPoint = DroneTarget.position;
                Vector3 directionToTarget = DroneTarget.forward; 
                
                Vector3 sideOffsetVector = -DroneTarget.right * sideOffset;
                desiredPosition = focusPoint - (directionToTarget * minDistance) + sideOffsetVector + (Vector3.up * heightOffset);
            }

            // 2. 카메라 위치를 부드럽게 이동시킵니다.
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentPositionVelocity, positionSmoothTime);

            //====================================================================================
            // [수정된 부분] lockRotation 옵션에 따라 회전 로직을 제어합니다.
            if (lockRotation)
            {
                // 회전이 고정되었으면, 초기 회전값을 유지합니다.
                transform.rotation = _initialRotation;
            }
            else
            {
                // 회전이 고정되지 않았으면, 포커스 포인트를 부드럽게 바라봅니다.
                Quaternion targetRotation = Quaternion.LookRotation(focusPoint - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / rotationSmoothTime);
            }
            //====================================================================================
        }
    }
}
