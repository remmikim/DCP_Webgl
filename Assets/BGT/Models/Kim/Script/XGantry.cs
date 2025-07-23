using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 추가해야 합니다.

public class XGantry : MonoBehaviour
{
    public GameObject XGantryMoving;

    public XGantryRotaion RotaionObject;
    public Chain1 chainIntance;

    // 이동 속도 (초당 이동 거리)
    public float moveSpeed = 0.2f; // 인스펙터에서 쉽게 조절할 수 있도록 public으로 설정

    // 로컬 Y축 이동 거리들 (델타 값)
    // 오른쪽 이동 (Y축 음수 방향)을 위한 이동 거리들 (예: -3.0f면 현재 위치에서 -3.0만큼 이동)
    private float[] moveDistancesRight = {-0.123f,-0.86f+0.123f, -0.96f, -0.48f, -0.96f, -0.48f }; // 오른쪽 Y8
    // 왼쪽 이동 (Y축 양수 방향)을 위한 이동 거리들 (예: 0.75f면 현재 위치에서 +0.75만큼 이동)
    private float[] moveDistancesLeft = {0.86f + 0.123f + 0.05f, 0.26f, 0.96f, 0.48f, 0.96f, 0.48f }; // 왼쪽 Y9

    
    // 현재 활성 이동을 위한 최종 목표 위치 (로컬 좌표)
    private Vector3 currentLocalTargetPosition;

    // 이동 방향 플래그
    private bool isMovingRight = false;
    private bool isMovingLeft = false;

    // 배열에서 현재 이동 단계를 추적하기 위한 인덱스
    private int currentRightMoveIndex = 0;
    private int currentLeftMoveIndex = 0;

    // 진행 중인 이동을 중지해야 할 경우를 위한 코루틴 참조
    private Coroutine currentMovementCoroutine;

    // Update는 더 이상 직접적인 이동 로직을 담당하지 않습니다.
    // 이동 로직은 이제 코루틴에서 처리됩니다.
    void Update()
    {
        // 필요하다면 디버깅이나 다른 연속적인 검사에 Update를 사용할 수 있지만,
        // 직접적인 이동 로직은 코루틴으로 옮겨졌습니다.
    }

    /// <summary>
    /// '오른쪽' (Y 로컬 위치 감소, 음수 방향) 이동 프로세스를 시작합니다.
    /// moveDistancesRight 배열에 정의된 다음 거리만큼 이동합니다.
    /// </summary>
    public void ActivateXGantryMovingRight()
    {
        // 이미 다른 방향으로 움직이거나 같은 방향으로 움직이고 있다면 새로운 이동을 시작하지 않음
        if (isMovingRight || isMovingLeft) return;

        // 이 방향의 모든 이동 단계를 완료했다면 인덱스를 재설정합니다.
        if (currentRightMoveIndex >= moveDistancesRight.Length)
        {
            currentRightMoveIndex = 0; // 첫 번째 이동 단계로 다시 돌아갑니다.
            Debug.Log("오른쪽 이동 사이클 완료, 인덱스 재설정.");
        }

        isMovingRight = true;
        SetTargetPositionForRightMovement(); // 현재 단계의 최종 목표 위치 설정

        // 기존 코루틴이 있다면 중지하고 새로운 코루틴 시작
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        currentMovementCoroutine = StartCoroutine(MoveGantryToTarget(true)); // '오른쪽' 이동(Y 감소)을 위해 true 전달

        // RotaionObject와 chainIntance 제어는 실제 '오른쪽' 방향 로직에 따라 검토될 수 있습니다.
        RotaionObject.ActivateZLiftRotationCW(); // CW 회전이 '오른쪽'과 연결된다고 가정
        chainIntance.ActiveChainCW(); // ActiveChainCW가 '오른쪽'과 연결된다고 가정
        Debug.Log($"XGantry 오른쪽 이동 활성화. 로컬 Y: {XGantryMoving.transform.localPosition.y} 에서 {currentLocalTargetPosition.y} 로 이동 중.");
    }

    /// <summary>
    /// '오른쪽' 이동을 비활성화합니다.
    /// </summary>
    public void DeactivateXGantryMovingRight()
    {
        if (isMovingRight)
        {
            isMovingRight = false;
            // 이동 중인 코루틴이 있다면 즉시 중지
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
                currentMovementCoroutine = null; // 참조 지우기
            }
            RotaionObject.DeactivateZLiftRotationCW(); // DeactivateZLiftRotationCW가 '오른쪽'과 연결된다고 가정
            chainIntance.DeActiveChainCW(); // DeActiveChainCW가 '오른쪽'과 연결된다고 가정
            Debug.Log("XGantry 오른쪽 이동 비활성화. 이동 중지.");
        }
    }

    /// <summary>
    /// '왼쪽' (Y 로컬 위치 증가, 양수 방향) 이동 프로세스를 시작합니다.
    /// moveDistancesLeft 배열에 정의된 다음 거리만큼 이동합니다.
    /// </summary>
    public void ActivateXGantryMovingLeft()
    {
        // 이미 다른 방향으로 움직이거나 같은 방향으로 움직이고 있다면 새로운 이동을 시작하지 않음
        if (isMovingRight || isMovingLeft) return;

        // 이 방향의 모든 이동 단계를 완료했다면 인덱스를 재설정합니다.
        if (currentLeftMoveIndex >= moveDistancesLeft.Length)
        {
            currentLeftMoveIndex = 0; // 첫 번째 이동 단계로 다시 돌아갑니다.
            Debug.Log("왼쪽 이동 사이클 완료, 인덱스 재설정.");
        }

        isMovingLeft = true;
        SetTargetPositionForLeftMovement(); // 현재 단계의 최종 목표 위치 설정

        // 기존 코루틴이 있다면 중지하고 새로운 코루틴 시작
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        currentMovementCoroutine = StartCoroutine(MoveGantryToTarget(false)); // '왼쪽' 이동(Y 증가)을 위해 false 전달

        // RotaionObject와 chainIntance 제어는 실제 '왼쪽' 방향 로직에 따라 검토될 수 있습니다.
        RotaionObject.ActivateZLiftRotationCCW(); // CCW 회전이 '왼쪽'과 연결된다고 가정
        chainIntance.ActiveChainCCW(); // ActiveChainCCW가 '왼쪽'과 연결된다고 가정
        Debug.Log($"XGantry 왼쪽 이동 활성화. 로컬 Y: {XGantryMoving.transform.localPosition.y} 에서 {currentLocalTargetPosition.y} 로 이동 중.");
    }

    /// <summary>
    /// '왼쪽' 이동을 비활성화합니다.
    /// </summary>
    public void DeactivateXGantryMovingLeft()
    {
        if (isMovingLeft)
        {
            isMovingLeft = false;
            // 이동 중인 코루틴이 있다면 즉시 중지
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
                currentMovementCoroutine = null; // 참조 지우기
            }
            RotaionObject.DeactivateZLiftRotationCCW(); // DeactivateZLiftRotationCCW가 '왼쪽'과 연결된다고 가정
            chainIntance.DeActiveChainCCW(); // DeActiveChainCCW가 '왼쪽'과 연결된다고 가정
            Debug.Log("XGantry 왼쪽 이동 비활성화. 이동 중지.");
        }
    }

    /// <summary>
    /// 갠트리를 현재 목표 위치(로컬 좌표)로 이동시키는 코루틴입니다.
    /// </summary>
    /// <param name="isRightDirection">'오른쪽' 이동 (Y 감소)이면 true, '왼쪽' 이동 (Y 증가)이면 false.</param>
    private IEnumerator MoveGantryToTarget(bool isRightDirection)
    {
        // XGantryMoving 오브젝트가 존재하는지 확인
        if (XGantryMoving == null)
        {
            Debug.LogError("XGantryMoving이 null입니다. 이동할 수 없습니다.");
            yield break; // 코루틴 종료
        }

        // 목표 로컬 위치에 도달하거나 이동이 비활성화될 때까지 계속 이동
        while ((isRightDirection && isMovingRight) || (!isRightDirection && isMovingLeft))
        {
            // 목표 로컬 위치로 이동
            XGantryMoving.transform.localPosition = Vector3.MoveTowards(XGantryMoving.transform.localPosition, currentLocalTargetPosition, moveSpeed * Time.deltaTime);

            // 목표에 도달했는지 확인 (로컬 위치의 거리를 사용)
            if (Vector3.Distance(XGantryMoving.transform.localPosition, currentLocalTargetPosition) < 0.01f)
            {
                // 부동 소수점 오차를 피하기 위해 정확한 목표 로컬 위치로 스냅
                XGantryMoving.transform.localPosition = currentLocalTargetPosition;
                Debug.Log($"XGantry가 로컬 Y: {currentLocalTargetPosition.y}에 도착했습니다.");
                break; // 목표에 도달했으므로 while 루프 종료
            }
            yield return null; // 다음 프레임까지 대기
        }

        // 이동이 완료되었거나 수동으로 비활성화되었습니다.
        // 이동이 성공적으로 완료되었다면 다음 단계를 위해 인덱스를 증가시킵니다.
        if (Vector3.Distance(XGantryMoving.transform.localPosition, currentLocalTargetPosition) < 0.01f)
        {
            if (isRightDirection)
            {
                currentRightMoveIndex++;
            }
            else
            {
                currentLeftMoveIndex++;
            }
        }

        // 플래그를 비활성화하고 관련 액션을 중지합니다. (새로운 Activate 호출이 오지 않는 한 플래그는 false 상태)
        if (isRightDirection)
        {
            isMovingRight = false;
            RotaionObject.DeactivateZLiftRotationCW();
            chainIntance.DeActiveChainCW();
        }
        else
        {
            isMovingLeft = false;
            RotaionObject.DeactivateZLiftRotationCCW();
            chainIntance.DeActiveChainCCW();
        }
        currentMovementCoroutine = null; // 코루틴 참조 지우기
    }

    /// <summary>
    /// 배열을 기반으로 '오른쪽' 이동을 위한 최종 목표 로컬 위치를 설정합니다.
    /// (현재 로컬 Y에 moveDistancesRight[index] 값을 더한 위치)
    /// </summary>
    private void SetTargetPositionForRightMovement()
    {
        float moveAmountY = moveDistancesRight[currentRightMoveIndex]; // 이동할 거리(델타 값)
        // 현재 로컬 Y에 moveAmountY를 더해서 새로운 목표 로컬 Y를 계산합니다.
        currentLocalTargetPosition = new Vector3(XGantryMoving.transform.localPosition.x, XGantryMoving.transform.localPosition.y + moveAmountY, XGantryMoving.transform.localPosition.z);
    }

    /// <summary>
    /// 배열을 기반으로 '왼쪽' 이동을 위한 최종 목표 로컬 위치를 설정합니다.
    /// (현재 로컬 Y에 moveDistancesLeft[index] 값을 더한 위치)
    /// </summary>
    private void SetTargetPositionForLeftMovement()
    {
        float moveAmountY = moveDistancesLeft[currentLeftMoveIndex]; // 이동할 거리(델타 값)
        // 현재 로컬 Y에 moveAmountY를 더해서 새로운 목표 로컬 Y를 계산합니다.
        currentLocalTargetPosition = new Vector3(XGantryMoving.transform.localPosition.x, XGantryMoving.transform.localPosition.y + moveAmountY, XGantryMoving.transform.localPosition.z);
    }
}