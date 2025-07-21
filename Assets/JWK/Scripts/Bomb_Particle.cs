using UnityEngine;

namespace JWK.Scripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class Bomb_Particle : MonoBehaviour
    {
        [Header("폭탄 충돌 VFX")] 
        [Tooltip("폭탄 충돌 시 생성할 VFX Prefab. (오브젝트 풀링 사용 권장)")]
        [SerializeField] private GameObject impactVFXPrefab;
        
        [Tooltip("지면으로 인식할 LayerMask")]
        [SerializeField] private LayerMask groundLayerMask;

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
                
                // TODO: 오브젝트 풀링을 사용한다면 Destroy 대신 풀에 반환해야 함
                // 예: gameObject.SetActive(false); // 또는 PoolManager.Instance.Return(gameObject);
                Destroy(gameObject);
            }
        }
    }
}