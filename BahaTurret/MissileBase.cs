using System;
using UnityEngine;

namespace BahaTurret
{
    public abstract class MissileBase : PartModule, IBDWeapon
    {
        protected WeaponClasses weaponClass;

        [KSPField]
        public string missileType = "missile";

        [KSPField(isPersistant = true)]
        public string shortName = string.Empty;

        [KSPField]
        public float maxStaticLaunchRange = 3000;

        [KSPField]
        public float minStaticLaunchRange = 10;

        [KSPField]
        public float minLaunchSpeed = 0;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Drop Time"),
            UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float dropTime = 0.4f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "In Cargo Bay: "),
        UI_Toggle(disabledText = "False", enabledText = "True", affectSymCounterparts = UI_Scene.All)]
        public bool inCargoBay = false;

        public enum MissileStates { Idle, Drop, Boost, Cruise, PostThrust }

        public enum TargetingModes { None, Radar, Heat, Laser, Gps, AntiRad }

        public MissileStates MissileState { get; set; } = MissileStates.Idle;

        public enum GuidanceModes { None, AAMLead, AAMPure, AGM, AGMBallistic, Cruise, STS, Bomb, RCS, BeamRiding }

        public GuidanceModes GuidanceMode;

        public bool HasFired { get; set; } = false;

        public bool Team { get; set; }

        public bool HasMissed { get; set; } = false;

        public Vector3 TargetPosition { get; set; } = Vector3.zero;

        public Vector3 TargetVelocity { get; set; } = Vector3.zero;


        public Vector3 TargetAcceleration { get; set; } = Vector3.zero;

        public float TimeIndex { get; set; } = 0;

        public TargetingModes TargetingMode { get; set; }

        public float TimeToImpact { get; set; }

        public bool TargetAcquired { get; set; } = false;

        public bool ActiveRadar { get; set; } = false;

        public Transform MissileReferenceTransform;

        protected ModuleTargetingCamera targetingPod;

        //laser stuff
        public ModuleTargetingCamera lockedCamera = null;
        protected Vector3 lastLaserPoint;
        protected Vector3 laserStartPosition;
        protected Vector3 startDirection;

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

        public abstract void FireMissile();


        public abstract void Jettison();

        public abstract float GetBlastRadius();

    }
}