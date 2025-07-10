using UnityEngine;

namespace JWK.Scripts
{
    public class Bomb_Particle : MonoBehaviour
    {
        [Header("폭탄 충돌 VFX")] [Tooltip("폭탄이 화재 포인트와 충돌 후 생기는 VFX Prefab을 할당하세요")]
        public GameObject impactVFXPrefab;

        [Tooltip("VFX가 몇 초 뒤에 파괴될지")] public float vfxDestroyDelay = 5.0f;
        
        [Tooltip("지면으로 인식할 LayerMask")]
        public LayerMask groundlayerMask;

        private void OnCollisionEnter(Collision collision)
        {
            if ( ((1 << collision.gameObject.layer) & groundlayerMask) != 0)
            {
                Destroy(gameObject);
                
                if(impactVFXPrefab)
                {
                    GameObject vfx = Instantiate(impactVFXPrefab, collision.contacts[0].point, Quaternion.identity);
                    // Destroy(vfx, vfxDestroyDelay);
                }
            }
            
        }
    }
}