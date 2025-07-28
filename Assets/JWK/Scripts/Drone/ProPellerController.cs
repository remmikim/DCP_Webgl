using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JWK.Scripts.Drone
{
    public class ProPellerController : MonoBehaviour
    {
        [Header("프로펠러 설정")]
        [SerializeField] private List<Transform> cwProPellers;
        [SerializeField] private List<Transform> ccwProPellers;

        [Header("RPM 설정")]
        [SerializeField] private float maxRPM = 2000.0f;
        [SerializeField] private float accelerationTime = 2.0f;
        [SerializeField] private float decelerationTime = 1.0f;

        private float _currentRPM = 0.0f;
        private Coroutine _rpmChangeCoroutine;
        
        // 리스트의 Count 프로퍼티에 매번 접근하는 것을 방지하기 위해 개수를 캐싱
        private int _cwCount;
        private int _ccwCount;

        private void OnEnable()
        {
            DroneEvents.OnTakeOffSequenceStarted += StartSpinning;
            DroneEvents.OnLandingSequenceCompleted += StopSpinning;
        }
        
        private void OnDisable()
        {
            DroneEvents.OnTakeOffSequenceStarted -= StartSpinning;
            DroneEvents.OnLandingSequenceCompleted -= StopSpinning;
        }

        private void Start()
        {
            // 프로펠러 개수를 미리 캐싱
            _cwCount = cwProPellers.Count;
            _ccwCount = ccwProPellers.Count;
        }

        private void Update()
        {
            if (_currentRPM > 0.01f)
            {
                float rotationThisFrame = _currentRPM * 6.0f * Time.deltaTime;

                // 최적화: 캐시된 카운트를 사용함.
                for (int i = 0; i < _cwCount; i++)
                    cwProPellers[i].Rotate(Vector3.up, rotationThisFrame, Space.Self);
                
                for (int i = 0; i < _ccwCount; i++)
                    ccwProPellers[i].Rotate(Vector3.up, -rotationThisFrame, Space.Self);
            }
        }
        
        private void StartSpinning()
        {
            if(_rpmChangeCoroutine != null) StopCoroutine(_rpmChangeCoroutine);
            _rpmChangeCoroutine = StartCoroutine(ChangeRpmCoroutine(maxRPM, accelerationTime));
        }

        private void StopSpinning()
        {
            if(_rpmChangeCoroutine != null) StopCoroutine(_rpmChangeCoroutine);
            _rpmChangeCoroutine = StartCoroutine(ChangeRpmCoroutine(0.0f, decelerationTime));
        }

        private IEnumerator ChangeRpmCoroutine(float targetRpm, float duration)
        {
            float startRpm = _currentRPM;
            float elapsedTime = 0.0f;

            while (elapsedTime < duration)
            {
                // Mathf.SmoothStep을 사용하여 더 부드러운 가감속 곡선 표현 가능 (선택사항)
                _currentRPM = Mathf.Lerp(startRpm, targetRpm, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            _currentRPM = targetRpm;
            _rpmChangeCoroutine = null;
        }
    }
}