// C:\Unity\TeamProject\Assets\JWK\Scripts\MissionData.cs

namespace JWK.Scripts
{
    public enum PayloadType
    {
        None,
        FireExtinguishingBomb,
        RescueEquipment,
        DisasterReliefBag,
        AluminumSplint,
        Gripper
    }

    public enum DroneMissionState
    {
        IdleAtStation,
        TakingOff,
        MovingToTarget,
        PositioningForDrop,
        PerformingAction,
        RetreatingAfterAction,
        ReturningToStation,
        Landing,
        EmergencyReturn,
        HoldingPosition
    }
}