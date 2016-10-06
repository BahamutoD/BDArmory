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

        [KSPField]
        public float maxOffBoresight = 45;

        [KSPField]
        public bool guidanceActive = true;

        [KSPField]
        public float lockedSensorFOV = 2.5f;

        [KSPField]
        public float heatThreshold = 200;

        [KSPField]
        public bool allAspect = false;

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

        public Vessel SourceVessel { get; set; } = null;

        public float timeFired = -1;

        protected float lockFailTimer = -1;

        public Vessel legacyTargetVessel;

        public Transform MissileReferenceTransform;

        protected ModuleTargetingCamera targetingPod;

        //laser stuff
        public ModuleTargetingCamera lockedCamera = null;
        protected Vector3 lastLaserPoint;
        protected Vector3 laserStartPosition;
        protected Vector3 startDirection;

        //GPS stuff
        public Vector3d targetGPSCoords;

        //heat stuff
        public TargetSignatureData heatTarget;

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


        protected abstract void PartDie(Part p);

        public abstract void Detonate();

        public abstract Vector3 GetForwardTransform();

        protected void  AddTargetInfoToVessel()
        {
                TargetInfo info = vessel.gameObject.AddComponent<TargetInfo>();
				info.team = BDATargetManager.BoolToTeam(Team);
				info.isMissile = true;
				info.MissileBaseModule = this;
        }

        public override void OnFixedUpdate()
        {
            TimeIndex = Time.time - timeFired;
        }

        protected void UpdateGPSTarget()
        {
            if (TargetAcquired)
            {
                TargetPosition = VectorUtils.GetWorldSurfacePostion(targetGPSCoords, vessel.mainBody);
                TargetVelocity = Vector3.zero;
                TargetAcceleration = Vector3.zero;
            }
            else
            {

                guidanceActive = false;
            }
        }

        protected void UpdateHeatTarget()
        {
            TargetAcquired = false;

            if (lockFailTimer > 1)
            {
                legacyTargetVessel = null;

                return;
            }

            if (heatTarget.exists && lockFailTimer < 0)
            {
                lockFailTimer = 0;
            }
            if (lockFailTimer >= 0)
            {
                Ray lookRay = new Ray(transform.position, heatTarget.position + (heatTarget.velocity * Time.fixedDeltaTime) - transform.position);
                heatTarget = BDATargetManager.GetHeatTarget(lookRay, lockedSensorFOV / 2, heatThreshold, allAspect);

                if (heatTarget.exists)
                {
                    TargetAcquired = true;
                    TargetPosition = heatTarget.position + (2 * heatTarget.velocity * Time.fixedDeltaTime);
                    TargetVelocity = heatTarget.velocity;
                    TargetAcceleration = heatTarget.acceleration;
                    lockFailTimer = 0;
                }
                else
                {
                    if (FlightGlobals.ready)
                    {
                        lockFailTimer += Time.fixedDeltaTime;
                    }
                }
            }
        }

        protected void SetLaserTargeting()
        {
            if (TargetingMode == TargetingModes.Laser)
            {
                laserStartPosition = MissileReferenceTransform.position;
                if (lockedCamera)
                {
                    TargetAcquired = true;
                    TargetPosition = lastLaserPoint = lockedCamera.groundTargetPosition;
                    targetingPod = lockedCamera;
                }
            }
        }
        protected void UpdateLaserTarget()
        {
            if (TargetAcquired)
            {
                if (lockedCamera && lockedCamera.groundStabilized && !lockedCamera.gimbalLimitReached && lockedCamera.surfaceDetected) //active laser target
                {
                    TargetPosition = lockedCamera.groundTargetPosition;
                    TargetVelocity = (TargetPosition - lastLaserPoint) / Time.fixedDeltaTime;
                    TargetAcceleration = Vector3.zero;
                    lastLaserPoint = TargetPosition;

                    if (GuidanceMode == GuidanceModes.BeamRiding && TimeIndex > 0.25f && Vector3.Dot(part.transform.forward, part.transform.position - lockedCamera.transform.position) < 0)
                    {
                        TargetAcquired = false;
                        lockedCamera = null;
                    }
                }
                else //lost active laser target, home on last known position
                {
                    if (CMSmoke.RaycastSmoke(new Ray(transform.position, lastLaserPoint - transform.position)))
                    {
                        //Debug.Log("Laser missileBase affected by smoke countermeasure");
                        float angle = VectorUtils.FullRangePerlinNoise(0.75f * Time.time, 10) * BDArmorySettings.SMOKE_DEFLECTION_FACTOR;
                        TargetPosition = VectorUtils.RotatePointAround(lastLaserPoint, transform.position, VectorUtils.GetUpDirection(transform.position), angle);
                        TargetVelocity = Vector3.zero;
                        TargetAcceleration = Vector3.zero;
                        lastLaserPoint = TargetPosition;
                    }
                    else
                    {
                        TargetPosition = lastLaserPoint;
                    }
                }
            }
            else
            {
                ModuleTargetingCamera foundCam = null;
                bool parentOnly = (GuidanceMode == GuidanceModes.BeamRiding);
                foundCam = BDATargetManager.GetLaserTarget(this, parentOnly);
                if (foundCam != null && foundCam.cameraEnabled && foundCam.groundStabilized && BDATargetManager.CanSeePosition(foundCam.groundTargetPosition, vessel.transform.position, MissileReferenceTransform.position))
                {
                    Debug.Log("Laser guided missileBase actively found laser point. Enabling guidance.");
                    lockedCamera = foundCam;
                    TargetAcquired = true;
                }
            }
        }
    }
}