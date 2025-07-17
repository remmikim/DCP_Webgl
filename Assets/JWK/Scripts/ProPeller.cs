using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JWK.Scripts
{
    public class ProPeller : MonoBehaviour
    {
        [Header("프로펠러 설정")]
        [Tooltip("시계방향(CW) 회전 프로펠러 TransForm 리스트")]
        public List<Transform> cwProPeller;
        [Tooltip("반시계방향(CCW) 회전 프로펠러 TransForm 리스트")]
        public List<Transform> ccwProPeller;

        [Header("RPM 설정")]
        [Tooltip("프로펠러의 최대 RPM")]
        public float maxRPM = 2000.0f;
        [Tooltip("최대 RPM 까지 도달하는 데 걸리는 시간")]
        public float acceleration = 2.0f;
        [Tooltip("정지하는 데 걸리는 시간")]
        public float decelerationTime = 1.0f;

        #region 내부 변수
        private float currentRPM = 0.0f;
        private bool areSpining = false;
        

        #endregion

        void Update()
        {
        }
    }
}