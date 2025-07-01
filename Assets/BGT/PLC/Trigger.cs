using UnityEngine;

public class Trigger : MonoBehaviour
{
    public bool TriggerSensor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        TriggerSensor = true;
    }

    private void OnTriggerExit(Collider other)
    {
        TriggerSensor = false;
    }
}
