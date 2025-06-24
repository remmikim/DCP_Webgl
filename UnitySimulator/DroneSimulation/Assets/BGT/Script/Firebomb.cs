using UnityEngine;
using System.Collections;

public class Firebomb : MonoBehaviour
{
    public float destroyDelayAfterDrop; // 폭탄이 떨어진 후 사라질 때까지의 지연 시간 (초)
    public float smogSpawnHeightOffset; // 스모그가 폭탄 위치보다 약간 위에 생성되도록 하는 오프셋

    private Rigidbody rb;
    private Collider bombCollider; // 더 이상 충돌 감지하지 않으므로 필수는 아니지만, 물리 시뮬레이션을 위해 유지

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bombCollider = GetComponent<Collider>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
       
        rb.isKinematic = true;
        rb.useGravity = false;
        if (bombCollider != null) bombCollider.enabled = false; // 초기에는 콜라이더 비활성화 (충돌 감지 안함)
    }

    // FirebombDropper 스크립트가 이 함수를 호출하여 폭탄의 물리 작용을 활성화하고,
    // 동시에 파괴 및 스모그 생성 코루틴을 시작합니다.
    public void Destruction()
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        if (bombCollider != null)
        {
            bombCollider.enabled = true; // 물리 활성화와 함께 콜라이더도 활성화 (다른 오브젝트와의 상호작용 위함)
        }
        StartCoroutine(Destroy(destroyDelayAfterDrop));
    }

    // OnCollisionEnter는 더 이상 필요 없으므로 제거합니다.
    // void OnCollisionEnter(Collision collision) { ... }

    private IEnumerator Destroy(float delay)
    {
        // 지정된 딜레이만큼 기다립니다.
        yield return new WaitForSeconds(delay);

        // 스모그 생성
        Vector3 smogSpawnPosition = transform.position + Vector3.up * smogSpawnHeightOffset;
        if (SmogEffectManager.Instance != null)
        {
            SmogEffectManager.Instance.CreateSmog(smogSpawnPosition, Quaternion.identity);
        }
        Destroy(gameObject); 
    }
}