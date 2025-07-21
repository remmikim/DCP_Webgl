using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 추가해야 합니다.

public class SetAsParentOnTrigger : MonoBehaviour
{
    // 이 변수를 true로 설정하면, 이미 부모가 있는 오브젝트도 자식으로 만들 수 있습니다.
    // 기본값은 false로, 이미 부모가 있는 오브젝트는 변경하지 않습니다.
    public bool allowReparenting = false;

    // 자식-부모 관계가 설정되기 전의 지연 시간 (초)
    private float delayBeforeParenting = 1.5f; // 새로운 지연 시간 변수

    // Rigidbody가 필요하며 Is Kinematic이 체크되어야 합니다.
    // Collider는 Is Trigger가 체크되어야 합니다.
    void Start()
    {
        // Collider 경고 (Rigidbody 주석은 수동으로 처리해도 괜찮습니다.)
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"Collider missing on {gameObject.name}. Please add a Collider and set 'Is Trigger' to true for collision detection.");
        }
        else if (!GetComponent<Collider>().isTrigger)
        {
            Debug.LogWarning($"Collider on {gameObject.name} does not have 'Is Trigger' checked. Please check it.");
        }
    }

    // 트리거 충돌이 시작될 때 호출됩니다.
    private void OnTriggerEnter(Collider other)
    {
        // 자기 자신과의 충돌은 무시합니다.
        if (other.gameObject == this.gameObject)
        {
            return;
        }

        // 충돌한 오브젝트 (other.gameObject)가 이미 부모를 가지고 있는지 확인합니다.
        // allowReparenting이 false이고 이미 부모가 있다면, 부모 관계 설정을 건너뜁니다.
        if (!allowReparenting && other.transform.parent != null)
        {
            Debug.Log($"'{other.gameObject.name}' already has a parent. Skipping reparenting. (Set 'allowReparenting' to true to override).");
            return;
        }

        // 지연 후 오브젝트를 자식으로 만들기 위한 코루틴을 시작합니다.
        StartCoroutine(ParentAfterDelay(other.transform, delayBeforeParenting));
    }

    // 지정된 지연 시간 후에 Transform을 부모로 설정하는 코루틴입니다.
    private IEnumerator ParentAfterDelay(Transform childTransform, float delay)
    {
        // 지정된 지연 시간 동안 기다립니다.
        yield return new WaitForSeconds(delay);

        // 자식 오브젝트가 여전히 존재하고, 지연 시간 동안 다른 곳에 부모가 되지 않았는지 확인합니다.
        // 이는 오브젝트가 지연 시간 동안 파괴되거나 이미 다른 부모로 이동했을 경우 발생할 수 있는 오류를 방지합니다.
        if (childTransform != null && childTransform.parent != this.transform)
        {
            // 충돌한 오브젝트를 이 GameObject의 자식으로 설정합니다.
            childTransform.SetParent(this.transform);
            Debug.Log($"'{childTransform.name}' is now a child of '{this.gameObject.name}' after a {delay} second delay.");
        }
        else if (childTransform != null && childTransform.parent == this.transform)
        {
            // 이 경우는 오브젝트가 이미 이 스크립트에 의해 부모가 된 경우입니다 (예: 여러 트리거가 빠르게 발생했을 때).
            // 일반적으로 문제가 없습니다.
            Debug.Log($"'{childTransform.name}' was already parented to '{this.gameObject.name}'.");
        }
        else
        {
            // 자식 오브젝트가 지연 시간이 끝나기 전에 파괴된 경우입니다.
            Debug.LogWarning("Child object was destroyed before parent-child relationship could be established.");
        }
    }
}