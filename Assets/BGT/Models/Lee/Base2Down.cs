using UnityEngine;

public class Base2Down : MonoBehaviour
{
    private float MoveSpeed = 0.14f;
    private float MoveAmountY = -0.6f;

    private Vector3 StartPosition;  
    private Vector3 TargetPosition;  
    void Start()
    {
        StartPosition = transform.position;
        TargetPosition = StartPosition + new Vector3(0, MoveAmountY,0);
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position,TargetPosition, MoveSpeed * Time.deltaTime);
    }
}