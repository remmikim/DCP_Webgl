using System;

namespace JWK.Scripts.Drone
{
    // --- 드론의 주요 상태 변경 이벤트를 관리하는 클래스
    public static class DroneEvents
    {
        // --- 드론이 이륙을 시작할 때 호출되는 이벤트 ---
        public static event Action OnTakeOffSequenceStarted;
        public static void TakeOffSequenceStarted() => OnTakeOffSequenceStarted?.Invoke();
        
        // --- 드론이 착륙을 완료했을 때 호출되는 이벤트 ---
        public static event Action OnLandingSequenceCompleted;
        public static void LandingSequenceCompleted() => OnLandingSequenceCompleted?.Invoke();
    }
}