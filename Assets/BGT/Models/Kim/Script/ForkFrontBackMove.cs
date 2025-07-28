using UnityEngine;
using System.Collections; // 코루틴을 사용하지 않지만, 일관성을 위해 유지하거나 필요 없으면 삭제해도 무방합니다.

public class ForkFrontBackMove : MonoBehaviour
{
    // 유니티 에디터에서 드래그 앤 드롭으로 연결할 GameObject 변수들
    public GameObject Second;
    public GameObject Third;

    // ActUtlManager 인스턴스 참조 추가
    public ActUtlManager actUtlManager;

    private float MoveSpeed = 0.2f;

    // 각 파이프 홀더의 시작 위치와 목표 위치 변수들
    private Vector3 SecondStartPosition;
    private Vector3 SecondTargetPosition;
    private Vector3 ThirdStartPosition;
    private Vector3 ThirdTargetPosition;

    private bool isForkMoveFront = false;
    private bool isForkMoveBack = false;

    void Start()
    {
        // Second와 Third의 초기 위치를 저장해두는 것이 좋습니다.
        // 현재 코드에서는 StartPosition을 Activate 시점에서 다시 가져오므로 이 부분은 필수는 아닙니다.
    }

    // Update is called once per frame
    void Update()
    {
        // 포크 전진 로직
        if (isForkMoveFront && !isForkMoveBack)
        {
            // Second 오브젝트 이동
            Second.transform.localPosition = Vector3.MoveTowards(Second.transform.localPosition, SecondTargetPosition, MoveSpeed * Time.deltaTime);
            // Third 오브젝트 이동 (Second보다 2배 빠르게)
            Third.transform.localPosition = Vector3.MoveTowards(Third.transform.localPosition, ThirdTargetPosition, 2 * MoveSpeed * Time.deltaTime);

            // 두 오브젝트 모두 목표 위치에 도달했는지 확인
            if (Vector3.Distance(Second.transform.localPosition, SecondTargetPosition) < 0.001f &&
                Vector3.Distance(Third.transform.localPosition, ThirdTargetPosition) < 0.001f)
            {
                // 목표에 도달하면 자동으로 비활성화 및 PLC 신호 전송
                DeactivateFront();
                Debug.Log("포크 전진 동작 완료.");
            }
        }

        // 포크 후진 로직
        if (isForkMoveBack && !isForkMoveFront)
        {
            // 각 PipeHolder의 위치를 보간
            Second.transform.localPosition = Vector3.MoveTowards(Second.transform.localPosition, SecondTargetPosition, MoveSpeed * Time.deltaTime);
            Third.transform.localPosition = Vector3.MoveTowards(Third.transform.localPosition, ThirdTargetPosition, 2 * MoveSpeed * Time.deltaTime);

            // 두 오브젝트 모두 목표 위치에 도달했는지 확인
            if (Vector3.Distance(Second.transform.localPosition, SecondTargetPosition) < 0.001f &&
                Vector3.Distance(Third.transform.localPosition, ThirdTargetPosition) < 0.001f)
            {
                // 목표에 도달하면 자동으로 비활성화 및 PLC 신호 전송
                DeactivateBack();
                Debug.Log("포크 후진 동작 완료.");
            }
        }
    }

    public void ActivateFront()
    {
        if (isForkMoveFront || isForkMoveBack) return; // 이미 움직이고 있다면 중복 실행 방지
        isForkMoveFront = true;

        // 목표 위치 설정
        SecondStartPosition = Second.transform.localPosition;
        SecondTargetPosition = SecondStartPosition + new Vector3(0.9f, 0, 0);
        ThirdStartPosition = Third.transform.localPosition;
        ThirdTargetPosition = ThirdStartPosition + new Vector3(1.8f, 0, 0);

        // --- 추가된 부분: 움직임 시작 시 X8:1 신호 전송 ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X10:0");
        }
        // --- 추가된 부분 끝 ---
    }

    public void DeactivateFront()
    {
        if (!isForkMoveFront) return; // 이미 비활성화되어 있다면 중복 실행 방지
        isForkMoveFront = false;

        // --- 추가된 부분: 수동 또는 자동 완료 시 X8:0 신호 전송 ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X10:1");
        }
        // --- 추가된 부분 끝 ---
    }

    public void ActivateBack()
    {
        if (isForkMoveFront || isForkMoveBack) return; // 이미 움직이고 있다면 중복 실행 방지
        isForkMoveBack = true;

        // 목표 위치 설정
        SecondStartPosition = Second.transform.localPosition;
        SecondTargetPosition = SecondStartPosition + new Vector3(-0.9f, 0, 0);
        ThirdStartPosition = Third.transform.localPosition;
        ThirdTargetPosition = ThirdStartPosition + new Vector3(-1.8f, 0, 0);

        // --- 추가된 부분: 움직임 시작 시 X8:1 신호 전송 ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X11:0"); 
        }
        // --- 추가된 부분 끝 ---
    }

    public void DeactivateBack()
    {
        if (!isForkMoveBack) return; // 이미 비활성화되어 있다면 중복 실행 방지
        isForkMoveBack = false;

        // --- 추가된 부분: 수동 또는 자동 완료 시 X8:0 신호 전송 ---
        if (actUtlManager != null)
        {
            actUtlManager.SendCommandToPlc("X11:1");
        }
        // --- 추가된 부분 끝 ---
    }
}