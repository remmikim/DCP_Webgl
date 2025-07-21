using UnityEngine;
using System.Collections; // �ڷ�ƾ ����� ���� �߰��ؾ� �մϴ�.

public class SetAsParentOnTrigger : MonoBehaviour
{
    // �� ������ true�� �����ϸ�, �̹� �θ� �ִ� ������Ʈ�� �ڽ����� ���� �� �ֽ��ϴ�.
    // �⺻���� false��, �̹� �θ� �ִ� ������Ʈ�� �������� �ʽ��ϴ�.
    public bool allowReparenting = false;

    // �ڽ�-�θ� ���谡 �����Ǳ� ���� ���� �ð� (��)
    private float delayBeforeParenting = 1.5f; // ���ο� ���� �ð� ����

    // Rigidbody�� �ʿ��ϸ� Is Kinematic�� üũ�Ǿ�� �մϴ�.
    // Collider�� Is Trigger�� üũ�Ǿ�� �մϴ�.
    void Start()
    {
        // Collider ��� (Rigidbody �ּ��� �������� ó���ص� �������ϴ�.)
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"Collider missing on {gameObject.name}. Please add a Collider and set 'Is Trigger' to true for collision detection.");
        }
        else if (!GetComponent<Collider>().isTrigger)
        {
            Debug.LogWarning($"Collider on {gameObject.name} does not have 'Is Trigger' checked. Please check it.");
        }
    }

    // Ʈ���� �浹�� ���۵� �� ȣ��˴ϴ�.
    private void OnTriggerEnter(Collider other)
    {
        // �ڱ� �ڽŰ��� �浹�� �����մϴ�.
        if (other.gameObject == this.gameObject)
        {
            return;
        }

        // �浹�� ������Ʈ (other.gameObject)�� �̹� �θ� ������ �ִ��� Ȯ���մϴ�.
        // allowReparenting�� false�̰� �̹� �θ� �ִٸ�, �θ� ���� ������ �ǳʶݴϴ�.
        if (!allowReparenting && other.transform.parent != null)
        {
            Debug.Log($"'{other.gameObject.name}' already has a parent. Skipping reparenting. (Set 'allowReparenting' to true to override).");
            return;
        }

        // ���� �� ������Ʈ�� �ڽ����� ����� ���� �ڷ�ƾ�� �����մϴ�.
        StartCoroutine(ParentAfterDelay(other.transform, delayBeforeParenting));
    }

    // ������ ���� �ð� �Ŀ� Transform�� �θ�� �����ϴ� �ڷ�ƾ�Դϴ�.
    private IEnumerator ParentAfterDelay(Transform childTransform, float delay)
    {
        // ������ ���� �ð� ���� ��ٸ��ϴ�.
        yield return new WaitForSeconds(delay);

        // �ڽ� ������Ʈ�� ������ �����ϰ�, ���� �ð� ���� �ٸ� ���� �θ� ���� �ʾҴ��� Ȯ���մϴ�.
        // �̴� ������Ʈ�� ���� �ð� ���� �ı��ǰų� �̹� �ٸ� �θ�� �̵����� ��� �߻��� �� �ִ� ������ �����մϴ�.
        if (childTransform != null && childTransform.parent != this.transform)
        {
            // �浹�� ������Ʈ�� �� GameObject�� �ڽ����� �����մϴ�.
            childTransform.SetParent(this.transform);
            Debug.Log($"'{childTransform.name}' is now a child of '{this.gameObject.name}' after a {delay} second delay.");
        }
        else if (childTransform != null && childTransform.parent == this.transform)
        {
            // �� ���� ������Ʈ�� �̹� �� ��ũ��Ʈ�� ���� �θ� �� ����Դϴ� (��: ���� Ʈ���Ű� ������ �߻����� ��).
            // �Ϲ������� ������ �����ϴ�.
            Debug.Log($"'{childTransform.name}' was already parented to '{this.gameObject.name}'.");
        }
        else
        {
            // �ڽ� ������Ʈ�� ���� �ð��� ������ ���� �ı��� ����Դϴ�.
            Debug.LogWarning("Child object was destroyed before parent-child relationship could be established.");
        }
    }
}