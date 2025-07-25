using System;
using System.Collections;
using Unity.Mathematics.Geometry;
using Unity.VisualScripting;
using UnityEngine;

namespace JWK.Scripts.FireManager
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ExtinguishableFire : MonoBehaviour
    {
        [Tooltip("불이 완전히 꺼지는 데 걸리는 시간")] [SerializeField]
        private float extinguishDuration = 2.0f;
        
        private ParticleSystem _particleSystem;
        private bool _isExtinguishing = false;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if(_isExtinguishing || !other.CompareTag("Bomb"))
                return;

            other.tag = "UsedBomb";
            
            StartCoroutine(ExtinguishCoroutine());
        }

        private IEnumerator ExtinguishCoroutine()
        {
            _isExtinguishing = true;
            yield return new WaitForSeconds(2.0f);
            
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            
            var mainModule = _particleSystem.main;
            float initialSize = mainModule.startSize.constant;
            float elapsedTime = 0.0f;

            while (elapsedTime < extinguishDuration)
            {
                float currentSize = Mathf.Lerp(initialSize, 0.0f, elapsedTime / extinguishDuration);
                mainModule.startSize = currentSize;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            Destroy(gameObject, 10.0f);
        }
    }
}