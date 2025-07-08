using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

namespace JWK.Scripts
{
    public class ExtinguisherDropSystem : MonoBehaviour
    {
        [Header("회전 대상 오브젝트 할당")] [Tooltip("안쪽 로터를 할당해주세요.")]
        public Transform rotaryIn;
        [Tooltip("바깥쪽 로터를 할당해주세요.")]
        public Transform rotaryOut;
        [Tooltip("회전 애니메이션의 속도입니다.")]
        public float rotationSpeed = 90.0f; // 초당 90도 회전
        
        private DroneController _droneController;
        private bool isActionInProgress = false;

        public float innerRotateAngle = 30.0f;
        public float outerRotateAngle = 30.0f;

        void Start()
        {
            _droneController = GetComponent<DroneController>();

            if (!_droneController)
            {
                Debug.LogError("부모 오브젝트에서 DroneController를 찾을 수 없습니다.");
                StartCoroutine(DropSequenceCouroutine());
            }

            else
            {
                if (!_droneController)
                    Debug.LogWarning("DroneController가 연결되지 않았습니다.");

                if (_droneController && !_droneController.isArrived)
                    Debug.LogWarning("드론이 아직 화재 포인트에 도착하지 않았습니다.");

                if (isActionInProgress)
                    Debug.LogWarning("이미 다른 투하 시퀀스가 진행 중입니다.");
            }
        }

        /// <summary>
        /// DroneController에서 호출할 함수
        /// 드론이 화재 포인트에 도착하면, 회전 및 투하 코루틴을 시작함.
        /// </summary>
        public void PlayDropExtinguishBomb()
        {
            if (!_droneController && _droneController.isArrived && !isActionInProgress)
                _droneController.isArrived = false;
        }

        /// <summary>
        /// 로터 회전 및 폭탄 투하를 순차적으로 진행하는 코루틴
        /// </summary>
        private IEnumerator DropSequenceCouroutine()
        {
            isActionInProgress = true;
            Debug.Log("로터를 회전시킵니다....");
            
            Quaternion initialRotIn = rotaryIn.localRotation;
            Quaternion initialRotOut = rotaryOut.localRotation;
            
            // 목표 회전값 계산 (서로 반대 방향으로 회전)
            Quaternion targetRotIn = initialRotIn * Quaternion.Euler(0, innerRotateAngle, 0); // Y축 기준
            Quaternion targetRotOut = initialRotOut * Quaternion.Euler(0, -outerRotateAngle, 0); // Y축 기준, 반대 방향

            float elapsedTime = 0f;
            // 더 큰 각도를 기준으로 회전 시간을 계산하여 동시에 끝나도록 함
            float rotationDuration = Mathf.Max(Mathf.Abs(innerRotateAngle), Mathf.Abs(outerRotateAngle)) / rotationSpeed;

            while (elapsedTime < rotationDuration)
            {
                // 시간에 따라 부드럽게 회전 (Slerp 사용)
                rotaryIn.localRotation = Quaternion.Slerp(initialRotIn, targetRotIn, elapsedTime / rotationDuration);
                rotaryOut.localRotation = Quaternion.Slerp(initialRotOut, targetRotOut, elapsedTime / rotationDuration);

                elapsedTime += Time.deltaTime;
                yield return null; // 다음 프레임까지 대기
            }

            // 오차 보정을 위해 최종 회전값을 정확하게 설정
            rotaryIn.localRotation = targetRotIn;
            rotaryOut.localRotation = targetRotOut;
            Debug.Log("로터 회전 완료.");
            
            // --- 2. (여기에 폭탄 투하 로직 추가) ---
            // 예: yield return new WaitForSeconds(0.5f); // 회전 후 잠시 대기
            //     _droneController.InstantiateBomb(); // DroneController에 폭탄 생성 함수를 만들고 호출

            // --- 3. (선택적) 로터 원위치 복귀 ---
            yield return new WaitForSeconds(1.0f); // 투하 후 잠시 대기
            Debug.Log("로터를 원위치로 복귀시킵니다.");
            
            elapsedTime = 0f; // 시간 초기화
            while (elapsedTime < rotationDuration)
            {
                rotaryIn.localRotation = Quaternion.Slerp(targetRotIn, initialRotIn, elapsedTime / rotationDuration);
                rotaryOut.localRotation = Quaternion.Slerp(targetRotOut, initialRotOut, elapsedTime / rotationDuration);

                elapsedTime += Time.deltaTime;
                yield return null;
            }
            rotaryIn.localRotation = initialRotIn;
            rotaryOut.localRotation = initialRotOut;

            isActionInProgress = false; // 액션 완료
            Debug.Log("소화탄 투하 시퀀스 전체 완료.");
        }
    }
}