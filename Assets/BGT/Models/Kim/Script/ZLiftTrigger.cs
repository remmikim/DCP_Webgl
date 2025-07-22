using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 추가해야 합니다.

public class ZLiftTrigger : MonoBehaviour
{
    public GameObject LiftWeight;
    public GameObject CarriageFrame;

    public ZLiftRotation ROT;
    public ChainMove CHM;
    public ChainMove CHM1;

    // 이동 속도 (초당 이동 거리)
    public float moveSpeed = 0.2f; // 인스펙터에서 쉽게 조절할 수 있도록 public으로 설정

    // LiftWeight의 Z축 이동 거리들 (델타 값)
    // "Up" (LiftWeight의 Z축 음수 방향)을 위한 이동 거리들
    private float[] liftWeightMoveDistancesUp = {-0.52f, -1.0f, -0.5f, -2.5f };
    // "Down" (LiftWeight의 Z축 양수 방향)을 위한 이동 거리들
    private float[] liftWeightMoveDistancesDown = {0.52f, 1.0f, 0.5f, 0.5f };

    // CarriageFrame의 Z축 이동 거리들 (델타 값)
    // "Up" (CarriageFrame의 Z축 양수 방향)을 위한 이동 거리들
    private float[] carriageFrameMoveDistancesUp = { 0.52f, 1.0f, 0.5f, 2.5f };
    // "Down" (CarriageFrame의 Z축 음수 방향)을 위한 이동 거리들
    private float[] carriageFrameMoveDistancesDown = {-0.52f, -1.0f, -0.5f, -0.5f };

   
    // 현재 활성 이동을 위한 최종 목표 위치 (로컬 좌표)
    private Vector3 currentLWLocalTargetPosition;
    private Vector3 currentCFLocalTargetPosition;

    // 이동 방향 플래그
    private bool isZLiftUpActive = false;
    private bool isZLiftDownActive = false;

    // 배열에서 현재 이동 단계를 추적하기 위한 인덱스
    private int currentUpMoveIndex = 0;
    private int currentDownMoveIndex = 0;

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
    /// ZLift를 '위' (LiftWeight Z축 음수, CarriageFrame Z축 양수)로 이동하는 프로세스를 시작합니다.
    /// 배열에 정의된 다음 거리만큼 이동합니다.
    /// </summary>
    public void ActivateZLiftUp()
    {
        // 이미 다른 방향으로 움직이거나 같은 방향으로 움직이고 있다면 새로운 이동을 시작하지 않음
        if (isZLiftUpActive || isZLiftDownActive) return;

        // 이 방향의 모든 이동 단계를 완료했다면 인덱스를 재설정합니다.
        if (currentUpMoveIndex >= liftWeightMoveDistancesUp.Length)
        {
            currentUpMoveIndex = 0; // 첫 번째 이동 단계로 다시 돌아갑니다.
            Debug.Log("ZLiftUp 이동 사이클 완료, 인덱스 재설정.");
        }

        isZLiftUpActive = true;
        SetTargetPositionsForUpMovement(); // 현재 단계의 최종 목표 위치 설정

        // 기존 코루틴이 있다면 중지하고 새로운 코루틴 시작
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        currentMovementCoroutine = StartCoroutine(MoveZLiftToTarget(true)); // 'Up' 이동을 위해 true 전달

        ROT.ActivateZLiftRotationCW();
        CHM.ActiveChainCW();
        CHM1.ActiveChainCW();
        Debug.Log($"ZLiftUp 활성화. LiftWeight 로컬 Z: {LiftWeight.transform.localPosition.z} 에서 {currentLWLocalTargetPosition.z} 로, CarriageFrame 로컬 Z: {CarriageFrame.transform.localPosition.z} 에서 {currentCFLocalTargetPosition.z} 로 이동 중.");
    }

    /// <summary>
    /// ZLift '위'로 이동을 비활성화합니다.
    /// </summary>
    public void DeactivateZLiftUp()
    {
        if (isZLiftUpActive)
        {
            isZLiftUpActive = false;
            // 이동 중인 코루틴이 있다면 즉시 중지
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
                currentMovementCoroutine = null; // 참조 지우기
            }
            ROT.DeactivateZLiftRotationCW();
            CHM.DeActiveChainCW();
            CHM1.DeActiveChainCW();
            Debug.Log("ZLiftUp 비활성화. 이동 중지.");
        }
    }

    /// <summary>
    /// ZLift를 '아래' (LiftWeight Z축 양수, CarriageFrame Z축 음수)로 이동하는 프로세스를 시작합니다.
    /// 배열에 정의된 다음 거리만큼 이동합니다.
    /// </summary>
    public void ActivateZLiftDown()
    {
        // 이미 다른 방향으로 움직이거나 같은 방향으로 움직이고 있다면 새로운 이동을 시작하지 않음
        if (isZLiftUpActive || isZLiftDownActive) return;

        // 이 방향의 모든 이동 단계를 완료했다면 인덱스를 재설정합니다.
        if (currentDownMoveIndex >= liftWeightMoveDistancesDown.Length)
        {
            currentDownMoveIndex = 0; // 첫 번째 이동 단계로 다시 돌아갑니다.
            Debug.Log("ZLiftDown 이동 사이클 완료, 인덱스 재설정.");
        }

        isZLiftDownActive = true;
        SetTargetPositionsForDownMovement(); // 현재 단계의 최종 목표 위치 설정

        // 기존 코루틴이 있다면 중지하고 새로운 코루틴 시작
        if (currentMovementCoroutine != null)
        {
            StopCoroutine(currentMovementCoroutine);
        }
        currentMovementCoroutine = StartCoroutine(MoveZLiftToTarget(false)); // 'Down' 이동을 위해 false 전달

        ROT.ActivateZLiftRotationCCW();
        CHM.ActiveChainCCW();
        CHM1.ActiveChainCCW();
        Debug.Log($"ZLiftDown 활성화. LiftWeight 로컬 Z: {LiftWeight.transform.localPosition.z} 에서 {currentLWLocalTargetPosition.z} 로, CarriageFrame 로컬 Z: {CarriageFrame.transform.localPosition.z} 에서 {currentCFLocalTargetPosition.z} 로 이동 중.");
    }

    /// <summary>
    /// ZLift '아래'로 이동을 비활성화합니다.
    /// </summary>
    public void DeactivateZLiftDown()
    {
        if (isZLiftDownActive)
        {
            isZLiftDownActive = false;
            // 이동 중인 코루틴이 있다면 즉시 중지
            if (currentMovementCoroutine != null)
            {
                StopCoroutine(currentMovementCoroutine);
                currentMovementCoroutine = null; // 참조 지우기
            }
            ROT.DeactivateZLiftRotationCCW();
            CHM.DeActiveChainCCW();
            CHM1.DeActiveChainCCW();
            Debug.Log("ZLiftDown 비활성화. 이동 중지.");
        }
    }

    /// <summary>
    /// LiftWeight와 CarriageFrame을 현재 목표 위치(로컬 좌표)로 이동시키는 코루틴입니다.
    /// </summary>
    /// <param name="isUpDirection">'Up' 이동이면 true, 'Down' 이동이면 false.</param>
    private IEnumerator MoveZLiftToTarget(bool isUpDirection)
    {
        bool liftWeightReached = false;
        bool carriageFrameReached = false;

        // 두 오브젝트 중 하나라도 목표에 도달하지 않았고, 이동이 활성화되어 있다면 계속 이동
        while ((isUpDirection && isZLiftUpActive) || (!isUpDirection && isZLiftDownActive))
        {
            if (LiftWeight != null && !liftWeightReached)
            {
                LiftWeight.transform.localPosition = Vector3.MoveTowards(LiftWeight.transform.localPosition, currentLWLocalTargetPosition, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(LiftWeight.transform.localPosition, currentLWLocalTargetPosition) < 0.01f)
                {
                    LiftWeight.transform.localPosition = currentLWLocalTargetPosition;
                    liftWeightReached = true;
                    Debug.Log($"LiftWeight가 로컬 Z: {currentLWLocalTargetPosition.z}에 도착했습니다.");
                }
            }

            if (CarriageFrame != null && !carriageFrameReached)
            {
                CarriageFrame.transform.localPosition = Vector3.MoveTowards(CarriageFrame.transform.localPosition, currentCFLocalTargetPosition, moveSpeed * Time.deltaTime);
                if (Vector3.Distance(CarriageFrame.transform.localPosition, currentCFLocalTargetPosition) < 0.01f)
                {
                    CarriageFrame.transform.localPosition = currentCFLocalTargetPosition;
                    carriageFrameReached = true;
                    Debug.Log($"CarriageFrame이 로컬 Z: {currentCFLocalTargetPosition.z}에 도착했습니다.");
                }
            }

            // 두 오브젝트 모두 목표에 도달했다면 반복문 종료
            if (liftWeightReached && carriageFrameReached)
            {
                break;
            }
            yield return null; // 다음 프레임까지 대기
        }

        // 이동 완료 후 인덱스 증가 및 플래그 비활성화
        // 두 오브젝트 모두 목표에 도달했을 때만 인덱스를 증가시킵니다.
        if (liftWeightReached && carriageFrameReached)
        {
            if (isUpDirection)
            {
                currentUpMoveIndex++;
            }
            else
            {
                currentDownMoveIndex++;
            }
        }

        // 이동이 완료되었으므로 플래그와 코루틴 참조를 초기화합니다.
        if (isUpDirection)
        {
            isZLiftUpActive = false;
            ROT.DeactivateZLiftRotationCW();
            CHM.DeActiveChainCW();
            CHM1.DeActiveChainCW();
        }
        else
        {
            isZLiftDownActive = false;
            ROT.DeactivateZLiftRotationCCW();
            CHM.DeActiveChainCCW();
            CHM1.DeActiveChainCCW();
        }
        currentMovementCoroutine = null; // 코루틴 참조 지우기
    }

    /// <summary>
    /// 'Up' 이동을 위한 최종 목표 로컬 위치를 설정합니다.
    /// (LiftWeight: Z축 음수 방향, CarriageFrame: Z축 양수 방향)
    /// </summary>
    private void SetTargetPositionsForUpMovement()
    {
        // LiftWeight의 목표 설정 (현재 로컬 Z에 moveAmountZ를 더함)
        float lwMoveAmountZ = liftWeightMoveDistancesUp[currentUpMoveIndex];
        currentLWLocalTargetPosition = new Vector3(LiftWeight.transform.localPosition.x, LiftWeight.transform.localPosition.y, LiftWeight.transform.localPosition.z + lwMoveAmountZ);

        // CarriageFrame의 목표 설정 (현재 로컬 Z에 moveAmountZ를 더함)
        float cfMoveAmountZ = carriageFrameMoveDistancesUp[currentUpMoveIndex];
        currentCFLocalTargetPosition = new Vector3(CarriageFrame.transform.localPosition.x, CarriageFrame.transform.localPosition.y, CarriageFrame.transform.localPosition.z + cfMoveAmountZ);
    }

    /// <summary>
    /// 'Down' 이동을 위한 최종 목표 로컬 위치를 설정합니다.
    /// (LiftWeight: Z축 양수 방향, CarriageFrame: Z축 음수 방향)
    /// </summary>
    private void SetTargetPositionsForDownMovement()
    {
        // LiftWeight의 목표 설정 (현재 로컬 Z에 moveAmountZ를 더함)
        float lwMoveAmountZ = liftWeightMoveDistancesDown[currentDownMoveIndex];
        currentLWLocalTargetPosition = new Vector3(LiftWeight.transform.localPosition.x, LiftWeight.transform.localPosition.y, LiftWeight.transform.localPosition.z + lwMoveAmountZ);

        // CarriageFrame의 목표 설정 (현재 로컬 Z에 moveAmountZ를 더함)
        float cfMoveAmountZ = carriageFrameMoveDistancesDown[currentDownMoveIndex];
        currentCFLocalTargetPosition = new Vector3(CarriageFrame.transform.localPosition.x, CarriageFrame.transform.localPosition.y, CarriageFrame.transform.localPosition.z + cfMoveAmountZ);
    }
}