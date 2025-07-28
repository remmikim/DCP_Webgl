using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JWK.Scripts.FireManager
{
    public class SmokeVFXController : MonoBehaviour
    {
        #region 활성화 된 소화탄 이펙트를 추적하기 위한 변수
        
        public static List<SmokeVFXController> ActiveSmokeEffects = new List<SmokeVFXController>();
        private ParticleSystem _particleSystem;

        #endregion

        private void OnEnable()
        {
            if(!ActiveSmokeEffects.Contains(this))
                ActiveSmokeEffects.Add(this);
        }

        private void OnDisable()
        {
            if (ActiveSmokeEffects.Contains(this))
                ActiveSmokeEffects.Remove(this);
        }

        public void StartDelayFadeOut(float delay)
        {
            StartCoroutine(FadeOutSeconds(delay));
        }

        private IEnumerator FadeOutSeconds(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if(_particleSystem)
            {
                _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                yield return new WaitForSeconds(_particleSystem.main.duration);
            }
            
            Destroy(this);
        }

    }
}
