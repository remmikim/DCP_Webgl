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
        [SerializeField] private float delayBetweenActions = 2.0f;

        // --- 내부 변수 ---
        private DroneController _droneController;
        private bool _isActionInProgress = false;

        // --- 코루틴 캐싱 (GC 최적화) ---
        private WaitForSeconds _delayWait;
        private readonly WaitForSeconds _bombRotateWait = new WaitForSeconds(1.0f);

        private void Awake()
        {
            // 이 스크립트의 부모 계층에서 DroneController 컴포넌트를 자동으로 찾아 할당
            _droneController = GetComponentInParent<DroneController>();
            if (!_droneController)
            {
                Debug.LogError("부모 오브젝트에서 DroneController를 찾을 수 없습니다! 이 스크립트를 비활성화합니다.");
                enabled = false; // 컴포넌트를 찾지 못하면 스크립트 비활성화
                return;
            }
            
            // 최적화: 코루틴에서 사용할 WaitForSeconds 인스턴스를 미리 생성
            _delayWait = new WaitForSeconds(delayBetweenActions);
        }

        /// <summary>
        /// DroneController에서 호출할 메인 함수.
        /// </summary>
        public IEnumerator PlayDropExtinguishBomb()
        {
            if (_droneController.isArrived && !_isActionInProgress)
            {
                Debug.Log("드론 도착 확인! 소화탄 투하 시퀀스를 시작합니다.");
                yield return StartCoroutine(FullDropSequenceCoroutine());
            }
            else
            {
                if (!_droneController.isArrived) Debug.LogWarning("드론이 아직 목표 지점에 도착하지 않았습니다.");
                if (_isActionInProgress) Debug.LogWarning("이미 다른 투하 액션이 진행 중입니다.");
            }
        }

        /// <summary>
        /// 모든 투하 및 회전 순서를 관리하는 메인 코루틴
        /// </summary>
        private IEnumerator FullDropSequenceCoroutine()
        {
            _isActionInProgress = true;

            // --- 1 & 2. Bomb 1, 2 투하 ---
            yield return StartCoroutine(RotateAndDropSequence(0, -45f));
            yield return _delayWait;
            yield return StartCoroutine(RotateAndDropSequence(1, -45f));
            yield return _delayWait;

            // --- 3. 재장전 1 ---
            yield return StartCoroutine(ReloadSequence());

            // --- 4 & 5. Bomb 3, 4 투하 ---
            yield return StartCoroutine(RotateAndDropSequence(2, -45f));
            yield return _delayWait;
            yield return StartCoroutine(RotateAndDropSequence(3, -45f));
            yield return _delayWait;

            // --- 6. 재장전 2 ---
            yield return StartCoroutine(ReloadSequence());

            // --- 7 & 8. Bomb 5, 6 투하 ---
            yield return StartCoroutine(RotateAndDropSequence(4, -45f));
            yield return _delayWait;
            yield return StartCoroutine(RotateAndDropSequence(5, -45f));
            yield return _delayWait;

            // --- 9. 마지막 재장전 ---
            yield return StartCoroutine(ReloadSequence());

            Debug.Log("모든 소화탄 투하 및 재장전 시퀀스 완료.");
            _isActionInProgress = false;
        }

        // 최적화: 반복되는 로직을 함수로 묶어 가독성 및 재사용성 향상
        private IEnumerator RotateAndDropSequence(int bombIndex, float angle)
        {
            Debug.Log($"Step: Bomb_{bombIndex + 1} 투하 준비.");
            yield return StartCoroutine(RotateRotor(rotaryOut, angle));
            DetachBombByIndex(bombIndex);
        }

        private IEnumerator ReloadSequence()
        {
            Debug.Log("Step: 재장전 회전.");
            yield return StartCoroutine(RotateRotorsSimultaneously(rotaryOut, 90f, rotaryIn, -60f));
        }

        /// <summary>
        /// 지정된 인덱스의 폭탄을 찾아 분리
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
                    Debug.Log($"'{bombToDrop.name}' 폭탄 투하.");
                    StartCoroutine(RotateBombToGround(bombToDrop));
                }
            }
        }

        #region 코루틴 헬퍼 함수

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

        private IEnumerator RotateRotorsSimultaneously(Transform rotor1, float angle1, Transform rotor2, float angle2)
        {
            Quaternion startRot1 = rotor1.localRotation;
            Quaternion targetRot1 = startRot1 * Quaternion.Euler(angle1, 0, 0);
            Quaternion startRot2 = rotor2.localRotation;
            Quaternion targetRot2 = startRot2 * Quaternion.Euler(angle2, 0, 0);

            float duration1 = Mathf.Abs(angle1) / rotationSpeed;
            float duration2 = Mathf.Abs(angle2) / rotationSpeed;
            float maxDuration = Mathf.Max(duration1, duration2);
            float elapsedTime = 0f;

            while (elapsedTime < maxDuration)
            {
                rotor1.localRotation = Quaternion.SlerpUnclamped(startRot1, targetRot1, elapsedTime / maxDuration);
                rotor2.localRotation = Quaternion.SlerpUnclamped(startRot2, targetRot2, elapsedTime / maxDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            rotor1.localRotation = targetRot1;
            rotor2.localRotation = targetRot2;
        }

        private IEnumerator RotateBombToGround(GameObject bomb)
        {
            yield return _bombRotateWait; // 캐시된 WaitForSeconds 사용
            if (!bomb) yield break;

            float rotationDuration = 2.0f;
            float elapsedTime = 0f;
            Quaternion startRotation = bomb.transform.rotation;
            // 목표 회전을 미리 계산
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
