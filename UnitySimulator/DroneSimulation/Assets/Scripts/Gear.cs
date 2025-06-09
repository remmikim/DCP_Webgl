using UnityEngine;
using UnityEngine.InputSystem;

public class Gear : MonoBehaviour
{
    public Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
    }
    public void OnRightDown(InputValue value)
    {
        anim.SetFloat("Strength", value.Get<float>());
    }
}
