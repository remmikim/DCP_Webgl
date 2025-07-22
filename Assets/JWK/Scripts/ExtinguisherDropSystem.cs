using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace JWK.Scripts
{
    public class ExtinguisherDropSystem : MonoBehaviour
    {
        [Header("회전 및 투하 대상")]
        [Tooltip("안쪽 로터의 Transform을 할당하세요.")]
        [SerializeField] private Transform rotaryIn;
        
        [Tooltip("바깥쪽 로터의 Transform을 할당하세요.")]
        [SerializeField] private Transform rotaryOut;
        
        [Tooltip("분리할 모든 폭탄 게임 오브젝트들을 순서대로 할당하세요.")]
        [SerializeField] private List<GameObject> bombList;

        [Header("애니메이션 설정")]
        [Tooltip("로터가 회전하는 속도입니다 (도/초).")]
        [SerializeField] private float rotationSpeed = 180.0f;
        
        [Tooltip("각 행동 사이의 대기 시간입니다 (초).")]
        [SerializeField] private float delayBetweenActions = 1.0f;

        // --- 내부 변수 ---
        private bool _isActionInProgress = false;
        private int _bombsDroppedCount = 0;
        
        // --- 코루틴 캐싱 (GC 최적화) ---
        private readonly WaitForSeconds _bombRotateWait = new WaitForSeconds(2.0f);
        private WaitForSeconds _actionDelayWait;
        private readonly WaitForSeconds _clearanceDelay = new WaitForSeconds(1.0f);

        private void Awake()
        {
            _actionDelayWait = new WaitForSeconds(delayBetweenActions);
        }
        public void ResetBombs()
        {
            _bombsDroppedCount = 0;
        }

        /// <summary>
        /// DroneController에서 호출할 메인 함수.
        /// </summary>
        public IEnumerator DropSingleBomb()
        {
            if (_isActionInProgress || _bombsDroppedCount >= bombList.Count)
            {
                Debug.LogWarning("이미 다른 액션이 진행 중이거나 모든 폭탄을 소진했습니다.");
                yield break;
            }
            
            _isActionInProgress = true;
            Debug.Log($"{_bombsDroppedCount + 1}번째 폭탄 투하 시퀀스를 시작합니다.");

            switch (_bombsDroppedCount)
            {
                case 0: // Bomb 1 투하
                    yield return StartCoroutine(RotateAndDropSequence(_bombsDroppedCount, -45f));
                    break;
                case 1: // Bomb 2 투하 후 재장전
                    yield return StartCoroutine(RotateAndDropSequence(_bombsDroppedCount, -45f));
                    yield return _actionDelayWait;
                    yield return StartCoroutine(ReloadSequence(1));
                    break;
                case 2: // Bomb 3 투하
                    yield return StartCoroutine(RotateAndDropSequence(_bombsDroppedCount, -45f));
                    break;
                case 3: // Bomb 4 투하 후 재장전
                    yield return StartCoroutine(RotateAndDropSequence(_bombsDroppedCount, -45f));
                    yield return _actionDelayWait;
                    yield return StartCoroutine(ReloadSequence(2));
                    break;
                case 4: // Bomb 5 투하
                    yield return StartCoroutine(RotateAndDropSequence(_bombsDroppedCount, -45f));
                    break;
                case 5: // Bomb 6 투하 후 마지막 재장전
                    yield return StartCoroutine(RotateAndDropSequence(_bombsDroppedCount, -45f));
                    yield return _actionDelayWait;
                    yield return StartCoroutine(ReloadSequence(3));
                    break;
            }

            _bombsDroppedCount++;
            _isActionInProgress = false;
        }

       /// <summary>
        /// 로터 회전과 폭탄 투하를 순차적으로 실행하는 헬퍼 코루틴입니다.
        /// </summary>
        private IEnumerator RotateAndDropSequence(int bombIndex, float angle)
        {
            yield return StartCoroutine(RotateRotor(rotaryOut, angle));
            DetachBombByIndex(bombIndex);
            // 투하 후에는 즉시 원위치로 복귀하지 않고, 다음 명령을 기다립니다.
            
            yield return _clearanceDelay;
        }

        /// <summary>
        /// 재장전을 위해 두 로터를 동시에 회전시키는 헬퍼 코루틴입니다.
        /// </summary>
        private IEnumerator ReloadSequence(int sequenceNumber)
        {
            Debug.Log($"Step: 재장전 {sequenceNumber}.");
            yield return StartCoroutine(RotateRotor(rotaryIn, -60f));
            yield return StartCoroutine(RotateRotor(rotaryOut, 90f));
        }

        /// <summary>
        /// 지정된 인덱스의 폭탄을 찾아 분리하고 물리적으로 떨어지게 만듭니다.
        /// </summary>
        private void DetachBombByIndex(int index)
        {
            if (bombList == null || index < 0 || index >= bombList.Count)
            {
                Debug.LogError($"잘못된 폭탄 인덱스({index})입니다.");
                return;
            }

            GameObject bombToDrop = bombList[index];
            if (bombToDrop)
            {
                bombToDrop.transform.SetParent(null);
                if (bombToDrop.TryGetComponent<Rigidbody>(out var bombRb))
                {
                    bombRb.isKinematic = false;
                    bombRb.linearVelocity = Vector3.zero;
                    bombRb.angularVelocity = Vector3.zero;
                    StartCoroutine(RotateBombToGround(bombToDrop));
                }
            }
        }

        #region 코루틴 헬퍼 함수 (애니메이션)

        /// <summary>
        /// 단일 로터를 지정된 각도만큼 부드럽게 회전시키는 코루틴입니다.
        /// </summary>
        private IEnumerator RotateRotor(Transform rotor, float angle)
        {
            Quaternion startRot = rotor.localRotation;
            Quaternion targetRot = startRot * Quaternion.Euler(angle, 0, 0);
            float duration = Mathf.Abs(angle) / rotationSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                rotor.localRotation = Quaternion.SlerpUnclamped(startRot, targetRot, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            rotor.localRotation = targetRot;
        }
        
        /// <summary>
        /// 투하된 폭탄이 떨어지면서 탄두가 서서히 바닥을 바라보도록 회전시키는 코루틴입니다.
        /// </summary>
        private IEnumerator RotateBombToGround(GameObject bomb)
        {
            yield return new WaitForSeconds(0.5f);
            if (!bomb) yield break;

            float rotationDuration = 2.0f;
            float elapsedTime = 0f;
            Quaternion startRotation = bomb.transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            while (elapsedTime < rotationDuration)
            {
                if (!bomb) yield break;
                bomb.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / rotationDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            if (bomb) bomb.transform.rotation = targetRotation;
        }

        #endregion
    }
}
