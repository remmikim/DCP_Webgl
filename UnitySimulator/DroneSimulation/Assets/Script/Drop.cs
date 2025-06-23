using UnityEngine;

public class Drop : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject child;
    

    public void OnAttack()
    {
        child.transform.SetParent(null);

    }
}
