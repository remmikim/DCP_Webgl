using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationSample : MonoBehaviour
{
    public Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void OnAttack()
    {
        anim.SetTrigger("Change");
    }

  
    
}
