using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JWK.Scripts
{
    public class ExtinguisherDropSystem : MonoBehaviour
    {
        [Header("회전 및 투하 대상")] [Tooltip("안쪽 로터의 Transform을 할당하세요.")]
        public Transform rotaryIn;

        [Tooltip("바깥쪽 로터의 Transform을 할당하세요.")] public Transform rotaryOut;

        [Tooltip("분리할 모든 폭탄 게임 오브젝트들을 순서대로 할당하세요.")]
        public List<GameObject> bombList; // Bomb_1, Bomb_2, ... 순서로 할당

        [Header("애니메이션 설정")] [Tooltip("로터가 회전하는 속도입니다 (도/초).")]
        public float rotationSpeed = 180.0f;

        [Tooltip("각 행동 사이의 대기 시간입니다 (초).")] public float delayBetweenActions = 2.0f;

        // --- 내부 변수 ---
        private DroneController _droneController;
        private bool isActionInProgress = false; // 현재 액션이 진행 중인지 확인하는 플래그

        void Start()
        {
            // 이 스크립트의 부모 계층에서 DroneController 컴포넌트를 자동으로 찾아 할당합니다.
            _droneController = GetComponentInParent<DroneController>();

            if (!_droneController)
                Debug.LogError("부모 오브젝트에서 DroneController를 찾을 수 없습니다!");
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// DroneController에서 호출할 메인 함수입니다.
        /// 드론이 도착했는지 확인하고, 전체 투하 시퀀스 코루틴을 시작
        /// </summary>
        public IEnumerator PlayDropExtinguishBomb()
        {
            // DroneController가 연결되어 있고, 드론이 목표에 도착했으며, 다른 액션이 진행 중이 아닐 때만 실행
            if (_droneController && _droneController.isArrived && !isActionInProgress)
            {
                Debug.Log("드론 도착 확인! 소화탄 투하 시퀀스를 시작합니다.");
                yield return StartCoroutine(FullDropSequenceCoroutine());
            }

            else
            {
                if (!_droneController) Debug.LogWarning("DroneController가 연결되지 않았습니다.");
                if (_droneController && !_droneController.isArrived) Debug.LogWarning("드론이 아직 목표 지점에 도착하지 않았습니다.");
                if (isActionInProgress) Debug.LogWarning("이미 다른 투하 액션이 진행 중입니다.");
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 모든 투하 및 회전 순서를 관리하는 메인 코루틴
        /// </summary>
        private IEnumerator FullDropSequenceCoroutine()
        {
            isActionInProgress = true; // 액션 시작 플래그

            // --- 1. Bomb_1 투하 시퀀스 ---
            Debug.Log("Step 1: Bomb_1 투하 준비.");
            yield return StartCoroutine(RotateRotor(rotaryOut, -45f)); // Rotary_Out 시계 방향 45도 회전
            DetachBombByIndex(0); // 첫 번째 폭탄 (Bomb_1) 투하

            // --- 2. Bomb_2 투하 시퀀스 ---
            yield return new WaitForSeconds(delayBetweenActions);
            Debug.Log("Step 2: Bomb_2 투하 준비.");
            yield return StartCoroutine(RotateRotor(rotaryOut, -45f)); // Rotary_Out 시계 방향 -45도 회전 (원위치 복귀와 유사)
            DetachBombByIndex(1); // 두 번째 폭탄 (Bomb_2) 투하

            // --- 3. 재장전 회전 1 ---
            yield return new WaitForSeconds(delayBetweenActions);
            Debug.Log("Step 3: 재장전 회전 1.");
            yield return
                StartCoroutine(RotateRotorsSimultaneously(rotaryOut, 90f, rotaryIn, -60f)); // Out 반시계 90도, In 시계 30도

            // --- 4. Bomb_3 투하 시퀀스 ---
            yield return new WaitForSeconds(delayBetweenActions);
            Debug.Log("Step 4: Bomb_3 투하 준비.");
            yield return StartCoroutine(RotateRotor(rotaryOut, -45f));
            DetachBombByIndex(2);

            // --- 5. Bomb_4 투하 시퀀스 ---
            yield return new WaitForSeconds(delayBetweenActions);
            Debug.Log("Step 5: Bomb_4 투하 준비.");
            yield return StartCoroutine(RotateRotor(rotaryOut, -45f));
            DetachBombByIndex(3);

            // --- 6. 재장전 회전 2 ---
            yield return new WaitForSeconds(delayBetweenActions);
            Debug.Log("Step 6: 재장전 회전 2.");
            yield return StartCoroutine(RotateRotorsSimultaneously(rotaryOut, 90f, rotaryIn, -60f));

            // --- 7. Bomb_5 투하 시퀀스 ---
            yield return new WaitForSeconds(delayBetweenActions);
            Debug.Log("Step 7: Bomb_5 투하 준비.");
            yield return StartCoroutine(RotateRotor(rotaryOut, -45f));
            DetachBombByIndex(4);

            // --- 8. Bomb_6 투하 시퀀스 ---
            yield return new WaitForSeconds(delayBetweenActions);
            Debug.Log("Step 8: Bomb_6 투하 준비.");
            yield return StartCoroutine(RotateRotor(rotaryOut, -45f));
            DetachBombByIndex(5);

            // --- 9. 재장전 회전 3 (마지막) ---
            yield return new WaitForSeconds(delayBetweenActions);
            Debug.Log("Step 9: 마지막 재장전 회전.");
            yield return StartCoroutine(RotateRotorsSimultaneously(rotaryOut, 90f, rotaryIn, -60f));

            // --- 10. 임무 완료 ---
            Debug.Log("모든 소화탄 투하 및 재장전 시퀀스 완료.");
            isActionInProgress = false; // 액션 완료 플래그
        }

        /// <summary>
        /// 지정된 인덱스의 폭탄을 찾아 분리(Joint 해제)합니다.
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
                FixedJoint joint = bombToDrop.GetComponent<FixedJoint>();

                if (joint)
                {
                    Destroy(joint);
                    Debug.Log($"'{bombToDrop.name}' 폭탄의 FixedJoint가 성공적으로 해제되었습니다.");

                    bombToDrop.transform.SetParent(null);

                    StartCoroutine(RotateBombToGround(bombToDrop));
                }

                else
                    Debug.LogWarning($"'{bombToDrop.name}' 폭탄에 FixedJoint 컴포넌트가 없습니다.");
            }
            else
                Debug.LogWarning($"bombList의 {index}번째 요소가 비어있습니다.");
        }

        #region 단일 로터를 지정된 각도만큼 회전시키는 코루틴

        private IEnumerator RotateRotor(Transform rotor, float angle)
        {
            Quaternion startRot = rotor.localRotation;
            Quaternion targetRot = startRot * Quaternion.Euler(angle, 0, 0); // Z축 기준 회전
            float duration = Mathf.Abs(angle) / rotationSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                rotor.localRotation = Quaternion.Slerp(startRot, targetRot, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            rotor.localRotation = targetRot; // 최종 위치 보정
        }

        #endregion

        #region 두 개의 로터를 동시에 다른 각도로 회전시키는 코루틴

        private IEnumerator RotateRotorsSimultaneously(Transform rotor1, float angle1, Transform rotor2, float angle2)
        {
            Quaternion startRot1 = rotor1.localRotation;
            Quaternion targetRot1 = startRot1 * Quaternion.Euler(angle1, 0, 0);

            Quaternion startRot2 = rotor2.localRotation;
            Quaternion targetRot2 = startRot2 * Quaternion.Euler(angle2, 0, 0);

            // 두 회전 중 더 오래 걸리는 시간을 기준으로 duration 설정
            float duration1 = Mathf.Abs(angle1) / rotationSpeed;
            float duration2 = Mathf.Abs(angle2) / rotationSpeed;
            float maxDuration = Mathf.Max(duration1, duration2);
            float elapsedTime = 0f;

            while (elapsedTime < maxDuration)
            {
                rotor1.localRotation = Quaternion.Slerp(startRot1, targetRot1, elapsedTime / maxDuration);
                rotor2.localRotation = Quaternion.Slerp(startRot2, targetRot2, elapsedTime / maxDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            rotor1.localRotation = targetRot1;
            rotor2.localRotation = targetRot2;
        }

        #endregion

        #region 폭탄이 떨어지며 탄두가 서서히 바닥을 바라보게 회전 시키는 코루틴

        private IEnumerator RotateBombToGround(GameObject bomb)
        {
            yield return new WaitForSeconds(1.0f);

            if (!bomb) yield break;

            float rotationDuration = 2.0f; // 회전에 걸리는 시간
            float elapsedTime = 0f;

            Quaternion startRotation = bomb.transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.down);

            while (elapsedTime < rotationDuration)
            {
                if (!bomb)  yield break;

                bomb.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / rotationDuration);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if(bomb) bomb.transform.rotation = targetRotation;
        }

        #endregion
    }
}