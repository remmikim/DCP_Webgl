using UnityEngine;

namespace JWK.Scripts.DropSystem
{
    [RequireComponent(typeof(Rigidbody))]
    public class Bomb_Particle : MonoBehaviour
    {
        [Header("폭탄 충돌 VFX")] 
        [Tooltip("폭탄 충돌 시 생성할 VFX Prefab. (오브젝트 풀링 사용 권장)")]
        [SerializeField] private GameObject impactVFXPrefab;
        
        [Tooltip("지면으로 인식할 LayerMask")]
        [SerializeField] private LayerMask groundLayerMask;

        [Header(" 유도 기능")] [Tooltip("유도 기능 활성화 여부")] [SerializeField]
        private readonly bool enableGuidance = true;

        [Tooltip("목표를 향해 유도하는 힘의 크기. 값이 클수록 더 강하게 유도됩니다. 추천: 5.0f ~ 20.0f")] [SerializeField]
        private float guidanceForce = 1.0f;
        
        private Rigidbody _rb;
        private Vector3 _targetPosition;
        private bool _isGuidanceActive = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if(!_isGuidanceActive || !enableGuidance)
                return;
            
            Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosXZ = new Vector3(_targetPosition.x, 0, _targetPosition.z);
            
            Vector3 direction = (targetPosXZ - currentPosXZ).normalized;
            _rb.AddForce(direction * guidanceForce, ForceMode.Force);
        }

        public void ActivateGuidance(Vector3 target)
        {
            if (enableGuidance)
            {
                // Debug.LogError("enableGuidance is true!!!!!!!!!!!!1");
                _targetPosition = target;
                _isGuidanceActive = true;
            }
            
            // else
            //     Debug.LogError("enableGuidance is false!!!!!!!!!!!!1");
        }
        private void OnCollisionEnter(Collision collision)
        {
            // LayerMask 비트 연산으로 충돌한 오브젝트의 레이어를 확인합니다.
            if (((1 << collision.gameObject.layer) & groundLayerMask) != 0)
            {
                if(impactVFXPrefab)
                {
                    // TODO: 성능을 위해 Instantiate 대신 VFX용 오브젝트 풀에서 가져오도록 수정하는 것을 권장함.
                    // 예: VfxPoolManager.Instance.Spawn(impactVFXPrefab, collision.contacts[0].point, Quaternion.identity);
                    Instantiate(impactVFXPrefab, collision.contacts[0].point, Quaternion.identity);
                }
                else
                {
                    Debug.LogError("impactVFXPrefab is null!!!!!!!!!!!!1");
                }
                _isGuidanceActive = false;
                
                // TODO: 오브젝트 풀링을 사용한다면 Destroy 대신 풀에 반환해야 함
                // 예: gameObject.SetActive(false); // 또는 PoolManager.Instance.Return(gameObject);
                Destroy(gameObject);
            }
        }
    }
}