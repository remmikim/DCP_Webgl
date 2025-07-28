using System.Collections;
using UnityEngine;

namespace JWK.Scripts.DropSystem
{
    [RequireComponent(typeof(Rigidbody))]
    public class BombParticle : MonoBehaviour
    {
        [Header("폭탄 충돌 VFX")] [Tooltip("폭탄 충돌 시 생성할 VFX Prefab. (오브젝트 풀링 사용 권장)")] [SerializeField]
        private GameObject impactVFXPrefab;

        [Tooltip("Particle이 최대 크그에 도달하는 데 걸리는 시간")]
        private float particleScaleUpDuration = 5.0f;
        
        [Tooltip("지면으로 인식할 LayerMask")] [SerializeField]
        private LayerMask groundLayerMask;

        [Header(" 유도 기능")] [Tooltip("유도 기능 활성화 여부")] [SerializeField]
        private readonly bool enableGuidance = true;

        [Tooltip("목표 지점까지 유도되는 데 걸리는 시간입니다. 짧을수록 빠르게 유도됨")] [SerializeField]
        private float guidanceDuration = 1.5f;

        private Rigidbody _rb;
        private Coroutine _guidanceCoroutine;
        private Vector3 _targetPosition;
        private bool _isExploded = false;

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
                Explode(transform.position);
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log("<color=green>[Bomb_Particle]</color> 화재 파티클(Trigger)과 충돌 감지! 폭발 효과를 생성합니다.");

            // LayerMask 비트 연산으로 충돌한 오브젝트의 레이어를 확인합니다.
            if (((1 << collision.gameObject.layer) & groundLayerMask) != 0)
                Explode(collision.contacts[0].point);
        }
        
        private void Explode(Vector3 explosionPosition)
        {
            Debug.Log("<color=green>[Bomb_Particle]</color> 화재 파티클(Trigger)과 충돌 감지! 폭발 효과를 생성합니다.");

            if(_isExploded) return;
            _isExploded = true;
            
            if (_guidanceCoroutine != null)
            {
                StopCoroutine(_guidanceCoroutine);
                _guidanceCoroutine = null;
            }
                
            if(impactVFXPrefab)
            {
                GameObject vfxInstance = Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
                StartCoroutine(ScaleUpParticleCoroutine(vfxInstance));
            }
                
            else
                Debug.LogError("impactVFXPrefab is null!");
                
            Destroy(gameObject);
        }

        private IEnumerator ScaleUpParticleCoroutine(GameObject particleInstance)
        {
            if(!particleInstance) yield break;
            
            Vector3 initialScale = particleInstance.transform.localScale;
            particleInstance.transform.localScale = Vector3.zero;
            
            float elapsedTime = 0.0f;
            while (elapsedTime < particleScaleUpDuration)
            {
                particleInstance.transform.localScale = Vector3.Lerp(initialScale, initialScale, elapsedTime / particleScaleUpDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (particleInstance)
                particleInstance.transform.localScale = initialScale;
        }
    }
}