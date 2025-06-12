using UnityEngine;

public class Roof : MonoBehaviour
{
    public GameObject Roof1;
    public GameObject Roof2;

    private float moveDuration = 5f; // 이동에 걸리는 시간
    private float roof1MoveAmount = -2f; // Roof1이 이동할 거리 
    private float roof2MoveAmount = -1f; // Roof2가 이동할 거리 

    private Vector3 roof1StartPosition;
    private Vector3 roof1TargetPosition;
    private Vector3 roof2StartPosition;
    private Vector3 roof2TargetPosition;

    private float elapsedTime = 0f;
    private bool isMoving = true; // 이동이 진행 중인지 여부

    void Start()
    {
       
        // 각 오브젝트의 초기 위치 저장
        roof1StartPosition = Roof1.transform.position;
        roof2StartPosition = Roof2.transform.position;

        // 각 오브젝트의 목표 위치 계산
        // 여기서는 Y축으로 이동한다고 가정합니다.
        roof1TargetPosition = roof1StartPosition + new Vector3(0, 0, roof1MoveAmount);
        roof2TargetPosition = roof2StartPosition + new Vector3(0, 0, roof2MoveAmount);
    }

    void Update()
    {
        if (isMoving)
        {
            // 시간 누적
            elapsedTime += Time.deltaTime;

            // 0~1 사이로 보간 비율 계산
            float t = Mathf.Clamp01(elapsedTime / moveDuration);

            // Roof1 이동 보간
            Roof1.transform.position = Vector3.Lerp(roof1StartPosition, roof1TargetPosition, t);

            // Roof2 이동 보간
            Roof2.transform.position = Vector3.Lerp(roof2StartPosition, roof2TargetPosition, t);

            // 이동이 완료되었으면 멈추기
            if (t >= 1f)
            {
                isMoving = false;
            }
        }
    }
}