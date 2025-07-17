using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JWK.Scripts
{
    public class ProPellerController : MonoBehaviour
    {
        [Header("프로펠러 설정")]
        [Tooltip("시계방향(CW) 회전 프로펠러 TransForm 리스트")]
        public List<Transform> cwProPellers;
        [Tooltip("반시계방향(CCW) 회전 프로펠러 TransForm 리스트")]
        public List<Transform> ccwProPellers;

        [Header("RPM 설정")]
        [Tooltip("프로펠러의 최대 RPM")]
        public float maxRPM = 2000.0f;
        [Tooltip("최대 RPM 까지 도달하는 데 걸리는 시간(초)")]
        public float accelerationTime = 2.0f;
        [Tooltip("정지하는 데 걸리는 시간")]
        public float decelerationTime = 1.0f;

        #region 내부 변수
        
        private float currentRPM = 0.0f;
        private Coroutine _rpmChangeCoroutine;
        
        #endregion

        // --- 스크립트가 활성화 될 때 이벤트 구독
        private void OnEnable()
        {
            DroneEvents.OnTakeOffSequenceStarted += StartSpinning;
            DroneEvents.OnLandingSequenceCompleted += StopSpinning;
        }
        
        // --- 스크립트가 비활성화 될 때 이벤트 구독 취소
        private void OnDisable()
        {
            DroneEvents.OnTakeOffSequenceStarted -= StartSpinning;
            DroneEvents.OnLandingSequenceCompleted -= StopSpinning;
        }
        
        void Update()
        {
            if (currentRPM > 0.01f)
            {
                float anaglePerSecond = currentRPM * 6.0f;
                float rotationThisFrame = anaglePerSecond * Time.deltaTime;

                foreach (Transform prop in cwProPellers)
                {
                    if(prop)
                        prop.Rotate(Vector3.up, rotationThisFrame, Space.Self);
                }
                
                foreach (Transform prop in ccwProPellers)
                {
                    if(prop)
                        prop.Rotate(Vector3.up, -rotationThisFrame, Space.Self);
                }
            }
        }

        #region DroneEvents에 의해 자동으로 호출되는 함수
        private void StartSpinning()
        {
            if(_rpmChangeCoroutine != null)
                StopCoroutine(_rpmChangeCoroutine);

            _rpmChangeCoroutine = StartCoroutine(ChangeRpmCoroutine(maxRPM, accelerationTime));
        }

        private void StopSpinning()
        {
            if(_rpmChangeCoroutine != null)
                StopCoroutine(_rpmChangeCoroutine);
            
            _rpmChangeCoroutine = StartCoroutine(ChangeRpmCoroutine(0.0f, decelerationTime));
        }

        private IEnumerator ChangeRpmCoroutine(float targetRpm, float duration)
        {
            float startRpm = currentRPM;
            float elapsedTime = 0.0f;

            while (elapsedTime < duration)
            {
                currentRPM = Mathf.Lerp(startRpm, targetRpm, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            currentRPM = targetRpm;
            _rpmChangeCoroutine = null;
        }
        #endregion
    }
}