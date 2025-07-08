using UnityEngine;

public class Base2Down : MonoBehaviour
{
    private float MoveSpeed = 0.14f;
    private float MoveAmountY = -0.6f;

    public BeamDown beamdown1;
    public BeamDown beamdown2;
    public BeamUp beamup1;
    public BeamUp beamup2;
    public PinMove8 pinmove2;
    public PinMove8 pinmove8;
    public PinMove8 Movingempty;

    private Vector3 StartPosition;  
    private Vector3 TargetPosition;
    
    private bool isActiveDown = false;
    private bool isActiveUp = false;
    void Start()
    {
    }

    void Update()
    {
        if(isActiveDown && !isActiveUp)
        {
            transform.position = Vector3.MoveTowards(transform.position,TargetPosition, MoveSpeed * Time.deltaTime);
        }
        if(isActiveUp && !isActiveDown)
        {
            transform.position = Vector3.MoveTowards(transform.position, TargetPosition, MoveSpeed * Time.deltaTime);
        }
    }
    public void ActiveDown()
    {
        if (isActiveUp || isActiveDown) return;
        isActiveDown = true;
        StartPosition = transform.position;
        TargetPosition = StartPosition + new Vector3(0, MoveAmountY, 0);
        beamdown1.ActiveDown(); beamdown2.ActiveDown();
        beamup1.ActiveDown(); beamup2.ActiveDown();
        pinmove2.ActiveForward(); pinmove8.ActiveForward();
        Movingempty.ActiveForward();
    }
    public void DeactiveDown()
    {
        isActiveDown= false;
        beamdown1.DeactiveDown(); beamdown2.DeactiveDown();
        beamup1.DeactiveDown(); beamup2.DeactiveDown();
        pinmove2.DeactiveForward(); pinmove8.DeactiveForward();
        Movingempty.DeactiveForward();
    }
    public void ActiveUp()
    {
        if (isActiveUp || isActiveDown) return;
        isActiveUp = true;
        StartPosition = transform.position;
        TargetPosition = StartPosition + new Vector3(0, -MoveAmountY, 0);
        beamdown1.ActiveUp(); beamdown2.ActiveUp();
        beamup1.ActiveUp(); beamup2.ActiveUp();
    }
    public void DeactiveUp()
    {
        isActiveUp = false;
        beamdown1.DeactiveUp(); beamdown2.DeactiveUp();
        beamup1.DeactiveUp(); beamup2.DeactiveUp();
    }
}