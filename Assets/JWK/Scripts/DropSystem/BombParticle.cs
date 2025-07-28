using System.Collections;
using UnityEngine;
//====================================================================================
// [수정된 부분] 네임스페이스 추가
using JWK.Scripts;
using JWK.Scripts.CameraManager;

//====================================================================================

namespace JWK.Scripts.DropSystem
{
    [RequireComponent(typeof(Rigidbody))]
    public class BombParticle : MonoBehaviour
    {
        [Header("폭탄 충돌 VFX")] [Tooltip("폭탄 충돌 시 생성할 VFX Prefab. (오브젝트 풀링 사용 권장)")] [SerializeField]
        private GameObject impactVFXPrefab;

        [Tooltip("지면으로 인식할 LayerMask")] [SerializeField]
        private LayerMask groundLayerMask;

        [Header(" 유도 기능")] [Tooltip("유도 기능 활성화 여부")] [SerializeField]
        private readonly bool enableGuidance = true;

        [Tooltip("목표 지점까지 유도되는 데 걸리는 시간입니다. 짧을수록 빠르게 유도됩니다.")] [SerializeField]
        private float guidanceDuration = 1.5f;

        private Rigidbody _rb;
        private Coroutine _guidanceCoroutine;
        private Vector3 _targetPosition;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public void ActivateGuidance(Vector3 target)
        {
            if (!enableGuidance) return;

            _targetPosition = target;

            if (_guidanceCoroutine != null)
                StopCoroutine(_guidanceCoroutine);

            _guidanceCoroutine = StartCoroutine(GuidedFallCoroutine(_targetPosition));
        }

        private IEnumerator GuidedFallCoroutine(Vector3 targetPosition)
        {
            _rb.isKinematic = true;

            Vector3 startPosition = transform.position;
            float elapsedTime = 0.0f;

            while (elapsedTime < guidanceDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0.0f, 1.0f, elapsedTime / guidanceDuration);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);

                yield return null;
            }
            
            transform.position = targetPosition;
            _rb.isKinematic = false;
            _guidanceCoroutine = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Collider>())
            {
                HandleImpact(transform.position);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & groundLayerMask) != 0)
            {
                HandleImpact(collision.contacts[0].point);
            }
        }

        private void HandleImpact(Vector3 impactPosition)
        {
            if (_guidanceCoroutine != null)
            {
                StopCoroutine(_guidanceCoroutine);
                _guidanceCoroutine = null;
            }
            
            //====================================================================================
            // [수정된 부분] 소화탄 충돌 이벤트를 카메라 시스템에 알립니다.
            DroneCameraEvents.BombImpact(impactPosition);
            //====================================================================================
            
            if (impactVFXPrefab)
            {
                Instantiate(impactVFXPrefab, impactPosition, Quaternion.identity);
            }
            else
            {
                Debug.LogError("impactVFXPrefab is null!");
            }

            Destroy(gameObject);
        }
    }
}
