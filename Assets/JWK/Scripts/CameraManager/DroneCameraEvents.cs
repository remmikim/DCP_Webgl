using System;
using UnityEngine;

namespace JWK.Scripts.CameraManager
{
    /// <summary>
    /// 드론과 카메라 시스템 간의 통신을 위한 정적 이벤트 클래스입니다.
    /// </summary>
    public static class DroneCameraEvents
    {
        // 임무 시작 (출발지, 목표지)
        public static event Action<Transform, Transform> OnMissionStart;
        public static void MissionStart(Transform start, Transform end) => OnMissionStart?.Invoke(start, end);

        // 목표 지점 도착 (목표지)
        public static event Action<Transform> OnArrivedAtDropZone;
        public static void ArrivedAtDropZone(Transform target) => OnArrivedAtDropZone?.Invoke(target);

        // 소화탄 충돌 (충돌 위치)
        public static event Action<Vector3> OnBombImpact;
        public static void BombImpact(Vector3 position) => OnBombImpact?.Invoke(position);

        // 기지 복귀
        public static event Action OnReturnToStation;
        public static void ReturnToStation() => OnReturnToStation?.Invoke();
    }
}