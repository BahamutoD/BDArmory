using System;
using UnityEngine;

namespace BahaTurret
{
    public class MissileBase : PartModule, IBDWeapon
    {
        protected WeaponClasses weaponClass;

        [KSPField]
        public string missileType = "missile";

        [KSPField(isPersistant = true)]
        public string shortName = string.Empty;
    
        public enum MissileStates { Idle, Drop, Boost, Cruise, PostThrust }

        public enum TargetingModes { None, Radar, Heat, Laser, Gps, AntiRad }

        public MissileStates MissileState { get; set; } = MissileStates.Idle;

        public bool HasFired { get; set; } = false;

        public bool Team { get; set; }

        public bool HasMissed { get; set; } = false;

        public Vector3 TargetPosition { get; set; } = Vector3.zero;

        public float TimeIndex { get; set; } = 0;

        public TargetingModes TargetingMode { get; set; }

        public float TimeToImpact { get; set; }

        public bool TargetAcquired { get; set; } = false;

        public bool ActiveRadar { get; set; } = false;

        public WeaponClasses GetWeaponClass()
        {
            return weaponClass;
        }

        public string GetShortName()
        {
            return shortName;
        }

        public string GetSubLabel()
        {
            if (Enum.GetName(typeof(TargetingModes), TargetingMode) == "None")
            {
                return string.Empty;
            }
            return Enum.GetName(typeof(TargetingModes), TargetingMode);
        }

        public Part GetPart()
        {
            return part;
        }
    }
}