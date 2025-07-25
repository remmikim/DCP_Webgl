using UnityEngine;

public class ManagerWrite : MonoBehaviour
{
    // ======= Gameobject 불러오기 ======
    public GameObject Cube;
    public GameObject Carriage;

    private Manager manager; // Manager 스크립트 참조

    // 센서의 이전 상태를 저장하여 상태 변경 시에만 데이터를 전송하도록 함
    private bool wasCubeSensorTriggered;
    private bool wasCarriageSensorTriggered;

    void Start()
    {
        // 같은 게임 오브젝트에 있는 Manager 컴포넌트의 참조를 가져옴
        manager = GetComponent<Manager>();
        if (manager == null)
        {
            Debug.LogError("ManagerWrite.cs: 같은 게임 오브젝트에 Manager.cs가 존재하지 않습니다!");
        }

        // 초기 상태 설정
        if (Cube != null)
            wasCubeSensorTriggered = Cube.GetComponent<Trigger>().TriggerSensor;
        if (Carriage != null)
            wasCarriageSensorTriggered = Carriage.GetComponent<Trigger>().TriggerSensor;
    }

    public void WriteDevice()
    {
        if (manager == null) return;

        // Cube 센서 상태 확인 및 변경 시 데이터 전송
        if (Cube != null)
        {
            bool isCubeSensorTriggered = Cube.GetComponent<Trigger>().TriggerSensor;
            if (isCubeSensorTriggered != wasCubeSensorTriggered)
            {
                manager.SendSensorData("X0", isCubeSensorTriggered);
                wasCubeSensorTriggered = isCubeSensorTriggered; // 상태 업데이트
            }
        }

        // Carriage 센서 상태 확인 및 변경 시 데이터 전송
        if (Carriage != null)
        {
            bool isCarriageSensorTriggered = Carriage.GetComponent<Trigger>().TriggerSensor;
            if (isCarriageSensorTriggered != wasCarriageSensorTriggered)
            {
                manager.SendSensorData("X1", isCarriageSensorTriggered);
                wasCarriageSensorTriggered = isCarriageSensorTriggered; // 상태 업데이트
            }
        }
    }
}
