using UnityEngine;

public class PinMove8 : MonoBehaviour
{
    public GameObject sourceObject; // Z축 이동의 기준이 될 오브젝트

    private Vector3 previousSourcePosition; // 이전 프레임의 sourceObject 위치 (전체 Vector3 저장)

    void Start()
    {
        previousSourcePosition = sourceObject.transform.position;
    }

    void Update()
    {
        if (sourceObject == null)
        {
            return;
        }

        // sourceObject의 현재 Z축 위치를 가져옴
        float currentSourceZ = sourceObject.transform.position.z;

      
        float deltaZ = currentSourceZ - previousSourcePosition.z;


        Vector3 currentMyPosition = transform.position;

        // 이 오브젝트의 X축 위치를 sourceObject의 Z축 이동량(deltaZ)만큼 변경
        currentMyPosition.z += deltaZ;
        transform.position = currentMyPosition;

        // 다음 프레임을 위해 sourceObject의 현재 전체 위치를 이전 위치로 업데이트
        previousSourcePosition = sourceObject.transform.position;
    }
}