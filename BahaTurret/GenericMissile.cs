using UnityEngine;

namespace BahaTurret
{
    public class GenericMissile : PartModule
    {
        public enum MissileStates { Idle, Drop, Boost, Cruise, PostThrust }

        public enum TargetingModes { None, Radar, Heat, Laser, GPS, AntiRad }

        public MissileStates MissileState { get; set; } = MissileStates.Idle;

        public bool HasFired { get; set; } = false;

        public bool Team { get; set; }

        public bool HasMissed { get; set; } = false;

        public Vector3 TargetPosition { get; set; } = Vector3.zero;

        public float TimeIndex { get; set; } = 0;

        public TargetingModes TargetingMode { get; set; }
     
    }
}