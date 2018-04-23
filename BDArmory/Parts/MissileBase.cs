using System;
using System.Collections.Generic;
using System.Linq;
using BDArmory.Core.Extension;
using BDArmory.CounterMeasure;
using BDArmory.Misc;
using BDArmory.Radar;
using BDArmory.UI;
using UnityEngine;
using System.Text;
using BDArmory.Core;
using BDArmory.Core.Enum;
using BDArmory.FX;
using BDArmory.Guidances;

namespace BDArmory.Parts
{
    public abstract class MissileBase : EngageableWeapon, IBDWeapon
    {
       protected WeaponClasses weaponClass;
        public WeaponClasses GetWeaponClass()
        {
            return weaponClass;
        }

        [KSPField(isPersistant = true)]
        public string shortName = string.Empty;

        public string GetShortName()
        {
            return shortName;
        }

        public string GetMissileType()
        {
            return missileType;
        }       

        [KSPField]
        public string missileType = "missile";

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max Static Launch Range"), UI_FloatRange(minValue = 5000f, maxValue = 50000f, stepIncrement = 1000f, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All)]
        public float maxStaticLaunchRange = 5000;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Min Static Launch Range"), UI_FloatRange(minValue = 10f, maxValue = 4000f, stepIncrement = 100f, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All)]
        public float minStaticLaunchRange = 10;

        [KSPField]
        public float minLaunchSpeed = 0;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max Off Boresight"), 
            UI_FloatRange(minValue = 0f, maxValue = 360f, stepIncrement = 5f, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All)]
        public float maxOffBoresight = 360;


        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Detonation distance override"), UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 1f, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All)]
        public float DetonationDistance = -1;

       //[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "SLW Offset"), UI_FloatRange(minValue = -1000f, maxValue = 0f, stepIncrement = 100f, affectSymCounterparts = UI_Scene.All)]
       public float SLWOffset = 0;

        public float getSWLWOffset
        {
            get
            {
                return SLWOffset;
            }
        }

        [KSPField]
        public bool guidanceActive = true;

        [KSPField]
        public float lockedSensorFOV = 2.5f;

        [KSPField]
        public float heatThreshold = 150;

        [KSPField]
        public bool allAspect = false;

        [KSPField]
        public bool isTimed = false;

        [KSPField]
        public bool radarLOAL = false;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Drop Time"),
            UI_FloatRange(minValue = 0f, maxValue = 5f, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
        public float dropTime = 0.5f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "In Cargo Bay: "),
            UI_Toggle(disabledText = "False", enabledText = "True", affectSymCounterparts = UI_Scene.All)]
        public bool inCargoBay = false;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Detonation Time"),
            UI_FloatRange(minValue = 2f, maxValue = 30f, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
        public float detonationTime = 2;

        [KSPField]
        public float activeRadarRange = 6000;

        [Obsolete("Use activeRadarLockTrackCurve!")]
        [KSPField] public float activeRadarMinThresh = 140;

        [KSPField]
        public FloatCurve activeRadarLockTrackCurve = new FloatCurve();             // floatcurve to define min/max range and lockable radar cross section

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Ballistic Overshoot factor"),
         UI_FloatRange(minValue = 0.5f, maxValue = 1.5f, stepIncrement = 0.01f, scene = UI_Scene.Editor)]
        public float BallisticOverShootFactor = 0.7f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cruise Altitude"), UI_FloatRange(minValue = 1f, maxValue = 500f, stepIncrement = 10f, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All)]
        public float CruiseAltitude = 500;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cruise speed"), UI_FloatRange(minValue = 100f, maxValue = 2000f, stepIncrement = 50f, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All)]
        public float CruiseSpeed = 300;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cruise prediction time"), UI_FloatRange(minValue = 1f, maxValue = 15f, stepIncrement = 1f, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All)]
        public float CruisePredictionTime = 5;

        [KSPField]
        public float missileRadarCrossSection = RadarUtils.RCS_MISSILES;            // radar cross section of this missile for detection purposes

        public enum MissileStates { Idle, Drop, Boost, Cruise, PostThrust }

        public enum DetonationDistanceStates {NotSafe, Cruising, CheckingProximity, Detonate}

        public enum TargetingModes { None, Radar, Heat, Laser, Gps, AntiRad }

        public MissileStates MissileState { get; set; } = MissileStates.Idle;

        public DetonationDistanceStates DetonationDistanceState { get; set; } = DetonationDistanceStates.NotSafe;

        public enum GuidanceModes { None, AAMLead, AAMPure, AGM, AGMBallistic, Cruise, STS, Bomb, RCS, BeamRiding, SLW}

        public GuidanceModes GuidanceMode;

        public bool HasFired { get; set; } = false;

        public bool Team { get; set; }

        public bool HasMissed { get; set; } = false;

        public Vector3 TargetPosition { get; set; } = Vector3.zero;

        public Vector3 TargetVelocity { get; set; } = Vector3.zero;

        public Vector3 TargetAcceleration { get; set; } = Vector3.zero;

        public float TimeIndex => Time.time - TimeFired;

        public TargetingModes TargetingMode { get; set; }

        public TargetingModes TargetingModeTerminal { get; set; }

        public float TimeToImpact { get; set; }

        public bool TargetAcquired { get; set; }

        public bool ActiveRadar { get; set; }

        public Vessel SourceVessel { get; set; } = null;

        public bool HasExploded { get; set; } = false;

        protected CruiseGuidance _cruiseGuidance;

        public float Throttle
        {
            get
            {
                return _throttle;
            }

            set
            {
                _throttle = Mathf.Clamp01(value);
            }
        }

        public float TimeFired = -1;

        protected float lockFailTimer = -1;

        public Vessel legacyTargetVessel;

        public Transform MissileReferenceTransform;

        protected ModuleTargetingCamera targetingPod;

        //laser stuff
        public ModuleTargetingCamera lockedCamera;
        protected Vector3 lastLaserPoint;
        protected Vector3 laserStartPosition;
        protected Vector3 startDirection;

        //GPS stuff
        public Vector3d targetGPSCoords;

        //heat stuff
        public TargetSignatureData heatTarget;

        //radar stuff
        public VesselRadarData vrd;
        public TargetSignatureData radarTarget;
        private TargetSignatureData[] scannedTargets;
        public MissileFire TargetMf = null;
        private LineRenderer LR;

        private int snapshotTicker;
        private int locksCount = 0;        
        private float _radarFailTimer = 0;
        private float maxRadarFailTime = 5;
        private float lastRWRPing = 0;
        private bool radarLOALSearching = false;
        protected bool checkMiss = false;
        public StringBuilder debugString = new StringBuilder();

        private float _throttle = 1f;
        private float _originalDistance = float.MinValue;
        private Vector3 _startPoint;


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

        protected void DisablingExplosives(Part p)
        {
            if (p == null) return;

            var explosive = p.FindModuleImplementing<BDExplosivePart>();
            if (explosive != null)
            {
                p.FindModuleImplementing<BDExplosivePart>().Armed = false;
            }
        }

        protected void SetupExplosive(Part p)
        {
            if (p == null) return;

            var explosive = p.FindModuleImplementing<BDExplosivePart>();
            if (explosive != null)
            {
                p.FindModuleImplementing<BDExplosivePart>().Armed = true;
                if (GuidanceMode == GuidanceModes.AGM || GuidanceMode == GuidanceModes.AGMBallistic)
                {
                    p.FindModuleImplementing<BDExplosivePart>().Shaped = true;
                }
            }
        }

        public abstract void Detonate();

        public abstract Vector3 GetForwardTransform();

        protected void  AddTargetInfoToVessel()
        {
                TargetInfo info = vessel.gameObject.AddComponent<TargetInfo>();
				info.team = BDATargetManager.BoolToTeam(Team);
				info.isMissile = true;
				info.MissileBaseModule = this;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "GPS Target", active = true, name = "GPSTarget")]
        public void assignGPSTarget()
        {
            if (HighLogic.LoadedSceneIsFlight)
                PickGPSTarget();
        }

        [KSPField(isPersistant = true)]
        public bool gpsSet = false;

        [KSPField(isPersistant = true)]
        public Vector3 assignedGPSCoords;

        [KSPField(isPersistant = true,guiName = "GPS Target")]
        public string gpsTargetName = "";

        void PickGPSTarget()
        {
            gpsSet = true;
            Fields["gpsTargetName"].guiActive = true;
            gpsTargetName = BDArmorySetup.Instance.ActiveWeaponManager.designatedGPSInfo.name;
            assignedGPSCoords = BDArmorySetup.Instance.ActiveWeaponManager.designatedGPSCoords;
        }

        public Vector3d UpdateGPSTarget()
        {
            Vector3 gpsTargetCoords_;

            if (gpsSet && assignedGPSCoords != null)
            {
                gpsTargetCoords_ = assignedGPSCoords;
            }
            else
            {
                gpsTargetCoords_ = targetGPSCoords;
            }

            if (TargetAcquired)
            {
                TargetPosition = VectorUtils.GetWorldSurfacePostion(gpsTargetCoords_, vessel.mainBody);
                TargetVelocity = Vector3.zero;
                TargetAcceleration = Vector3.zero;
            }
            else
            {
                guidanceActive = false;
            }

            return gpsTargetCoords_;
        }

        protected void UpdateHeatTarget()
        {
            if (lockFailTimer > 1)
            {
                legacyTargetVessel = null;
                TargetAcquired = false;
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
                    TargetAcquired = false;
                    if (FlightGlobals.ready)
                    {
                        lockFailTimer += Time.fixedDeltaTime;
                    }
                }
            }
        }

        protected void SetAntiRadTargeting()
        {
            if (TargetingMode == TargetingModes.AntiRad && TargetAcquired)
            {
                RadarWarningReceiver.OnRadarPing += ReceiveRadarPing;
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

                    if (GuidanceMode == GuidanceModes.BeamRiding && TimeIndex > 0.25f && Vector3.Dot(GetForwardTransform(), part.transform.position - lockedCamera.transform.position) < 0)
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
                    Debug.Log("[BDArmory]: Laser guided missileBase actively found laser point. Enabling guidance.");
                    lockedCamera = foundCam;
                    TargetAcquired = true;
                }
            }
        }

        protected void UpdateRadarTarget()
        {
            TargetAcquired = false;

            float angleToTarget = Vector3.Angle(radarTarget.predictedPosition - transform.position, GetForwardTransform());

            if (radarTarget.exists)
            {
                // locked-on before launch, passive radar guidance or waiting till in active radar range:
                if (!ActiveRadar && ((radarTarget.predictedPosition - transform.position).sqrMagnitude > Mathf.Pow(activeRadarRange, 2) || angleToTarget > maxOffBoresight * 0.75f))
                {
                    if (vrd)
                    {
                        TargetSignatureData t = TargetSignatureData.noTarget;
                        List<TargetSignatureData> possibleTargets = vrd.GetLockedTargets();
                        for (int i = 0; i < possibleTargets.Count; i++)
                        {
                            if (possibleTargets[i].vessel == radarTarget.vessel)
                            {
                                t = possibleTargets[i];
                            }
                        }

                        if (t.exists)
                        {
                            TargetAcquired = true;
                            radarTarget = t;
                            TargetPosition = radarTarget.predictedPositionWithChaffFactor;
                            TargetVelocity = radarTarget.velocity;
                            TargetAcceleration = radarTarget.acceleration;
                            _radarFailTimer = 0;
                            return;
                        }
                        else
                        {
                            if (_radarFailTimer > maxRadarFailTime)
                            {
                                Debug.Log("[BDArmory]: Semi-Active Radar guidance failed. Parent radar lost target.");
                                radarTarget = TargetSignatureData.noTarget;
                                legacyTargetVessel = null;
                                return;
                            }
                            else
                            {
                                if (_radarFailTimer == 0)
                                {
                                    Debug.Log("[BDArmory]: Semi-Active Radar guidance failed - waiting for data");
                                }
                                _radarFailTimer += Time.fixedDeltaTime;
                                radarTarget.timeAcquired = Time.time;
                                radarTarget.position = radarTarget.predictedPosition;
                                TargetPosition = radarTarget.predictedPositionWithChaffFactor;
                                TargetVelocity = radarTarget.velocity;
                                TargetAcceleration = Vector3.zero;
                                TargetAcquired = true;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("[BDArmory]: Semi-Active Radar guidance failed. Out of range and no data feed.");
                        radarTarget = TargetSignatureData.noTarget;
                        legacyTargetVessel = null;
                        return;
                    }
                }
                else
                {
                    // active radar with target locked:
                    vrd = null;

                    if (angleToTarget > maxOffBoresight)
                    {
                        Debug.Log("[BDArmory]: Active Radar guidance failed.  Target is out of active seeker gimbal limits.");
                        radarTarget = TargetSignatureData.noTarget;
                        legacyTargetVessel = null;
                        return;
                    }
                    else
                    {
                        if (scannedTargets == null) scannedTargets = new TargetSignatureData[5];
                        TargetSignatureData.ResetTSDArray(ref scannedTargets);
                        Ray ray = new Ray(transform.position, radarTarget.predictedPosition - transform.position);
                        bool pingRWR = Time.time - lastRWRPing > 0.4f;
                        if (pingRWR) lastRWRPing = Time.time;
                        bool radarSnapshot = (snapshotTicker > 10);
                        if (radarSnapshot)
                        {
                            snapshotTicker = 0;
                        }
                        else
                        {
                            snapshotTicker++;
                        }

                        //RadarUtils.UpdateRadarLock(ray, lockedSensorFOV, activeRadarMinThresh, ref scannedTargets, 0.4f, pingRWR, RadarWarningReceiver.RWRThreatTypes.MissileLock, radarSnapshot);
                        RadarUtils.RadarUpdateMissileLock(ray, lockedSensorFOV, ref scannedTargets, 0.4f, this);

                        float sqrThresh = radarLOALSearching ? Mathf.Pow(500, 2) : Mathf.Pow(40, 2);

                        if (radarLOAL && radarLOALSearching && !radarSnapshot)
                        {
                            //only scan on snapshot interval
                        }
                        else
                        {
                            for (int i = 0; i < scannedTargets.Length; i++)
                            {
                                if (scannedTargets[i].exists && (scannedTargets[i].predictedPosition - radarTarget.predictedPosition).sqrMagnitude < sqrThresh)
                                {
                                    //re-check engagement envelope, only lock appropriate targets
                                    if (CheckTargetEngagementEnvelope(scannedTargets[i].targetInfo))
                                    {
                                        radarTarget = scannedTargets[i];
                                        TargetAcquired = true;
                                        radarLOALSearching = false;
                                        TargetPosition = radarTarget.predictedPositionWithChaffFactor + (radarTarget.velocity * Time.fixedDeltaTime);
                                        TargetVelocity = radarTarget.velocity;
                                        TargetAcceleration = radarTarget.acceleration;
                                        _radarFailTimer = 0;
                                        if (!ActiveRadar && Time.time - TimeFired > 1)
                                        {
                                            if (locksCount == 0)
                                            {
                                                if (weaponClass == WeaponClasses.SLW)
                                                    RadarWarningReceiver.PingRWR(ray, lockedSensorFOV, RadarWarningReceiver.RWRThreatTypes.Torpedo, 2f);
                                                else
                                                    RadarWarningReceiver.PingRWR(ray, lockedSensorFOV, RadarWarningReceiver.RWRThreatTypes.MissileLaunch, 2f);
                                                Debug.Log("[BDArmory]: Pitbull! Radar missilebase has gone active.  Radar sig strength: " + radarTarget.signalStrength.ToString("0.0"));

                                            }
                                            else if (locksCount > 2)
                                            {
                                                guidanceActive = false;
                                                checkMiss = true;
                                                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                                                {
                                                    Debug.Log("[BDArmory]: Active Radar guidance failed. Radar missileBase reached max re-lock attempts.");
                                                }
                                            }
                                            locksCount++;
                                        }
                                        ActiveRadar = true;
                                        return;
                                    }
                                }
                            }
                        }

                        if (radarLOAL)
                        {
                            radarLOALSearching = true;
                            TargetAcquired = true;
                            TargetPosition = radarTarget.predictedPositionWithChaffFactor + (radarTarget.velocity * Time.fixedDeltaTime);
                            TargetVelocity = radarTarget.velocity;
                            TargetAcceleration = Vector3.zero;
                            ActiveRadar = false;
                            _radarFailTimer = 0;
                        }
                        else
                        {
                            Debug.Log("[BDArmory]: Active Radar guidance failed.  No target locked.");
                            radarTarget = TargetSignatureData.noTarget;
                            legacyTargetVessel = null;
                            radarLOALSearching = false;
                            TargetAcquired = false;
                            ActiveRadar = false;
                        }

                    }
                }
            }
            else if (radarLOAL && radarLOALSearching)
            {
                // not locked on before launch, trying lock-on after launch:

                if (scannedTargets == null) scannedTargets = new TargetSignatureData[5];
                TargetSignatureData.ResetTSDArray(ref scannedTargets);
                Ray ray = new Ray(transform.position, GetForwardTransform());
                bool pingRWR = Time.time - lastRWRPing > 0.4f;
                if (pingRWR) lastRWRPing = Time.time;
                bool radarSnapshot = (snapshotTicker > 5);
                if (radarSnapshot)
                {
                    snapshotTicker = 0;
                }
                else
                {
                    snapshotTicker++;
                }

                //RadarUtils.UpdateRadarLock(ray, lockedSensorFOV * 3, activeRadarMinThresh * 2, ref scannedTargets, 0.4f, pingRWR, RadarWarningReceiver.RWRThreatTypes.MissileLock, radarSnapshot);
                RadarUtils.RadarUpdateMissileLock(ray, lockedSensorFOV * 3, ref scannedTargets, 0.4f, this);

                float sqrThresh = Mathf.Pow(300, 2);

                float smallestAngle = 360;
                TargetSignatureData lockedTarget = TargetSignatureData.noTarget;

                for (int i = 0; i < scannedTargets.Length; i++)
                {
                    if (scannedTargets[i].exists && (scannedTargets[i].predictedPosition - radarTarget.predictedPosition).sqrMagnitude < sqrThresh)
                    {
                        //re-check engagement envelope, only lock appropriate targets
                        if (CheckTargetEngagementEnvelope(scannedTargets[i].targetInfo))
                        {
                            float angle = Vector3.Angle(scannedTargets[i].predictedPosition - transform.position, GetForwardTransform());
                            if (angle < smallestAngle)
                            {
                                lockedTarget = scannedTargets[i];
                                smallestAngle = angle;
                            }


                            ActiveRadar = true;
                            return;
                        }
                    }
                }

                if (lockedTarget.exists)
                {
                    radarTarget = lockedTarget;
                    TargetAcquired = true;
                    radarLOALSearching = false;
                    TargetPosition = radarTarget.predictedPositionWithChaffFactor + (radarTarget.velocity * Time.fixedDeltaTime);
                    TargetVelocity = radarTarget.velocity;
                    TargetAcceleration = radarTarget.acceleration;

                    if (!ActiveRadar && Time.time - TimeFired > 1)
                    {
                        if (weaponClass == WeaponClasses.SLW)
                            RadarWarningReceiver.PingRWR(new Ray(transform.position, radarTarget.predictedPosition - transform.position), lockedSensorFOV, RadarWarningReceiver.RWRThreatTypes.Torpedo, 2f);
                        else
                            RadarWarningReceiver.PingRWR(new Ray(transform.position, radarTarget.predictedPosition - transform.position), lockedSensorFOV, RadarWarningReceiver.RWRThreatTypes.MissileLaunch, 2f);

                        Debug.Log("[BDArmory]: Pitbull! Radar missileBase has gone active.  Radar sig strength: " + radarTarget.signalStrength.ToString("0.0"));
                    }
                    return;
                }
                else
                {
                    TargetAcquired = true;
                    TargetPosition = transform.position + (startDirection * 500);
                    TargetVelocity = Vector3.zero;
                    TargetAcceleration = Vector3.zero;
                    radarLOALSearching = true;
                    _radarFailTimer += Time.fixedDeltaTime;
                    if (_radarFailTimer > maxRadarFailTime)
                    {
                        Debug.Log("[BDArmory]: Active Radar guidance failed. LOAL could not lock a target.");
                        radarTarget = TargetSignatureData.noTarget;
                        legacyTargetVessel = null;
                        radarLOALSearching = false;
                        TargetAcquired = false;
                        ActiveRadar = false;
                    }
                    return;                    
                }
            }

            if (!radarTarget.exists)
            {
                legacyTargetVessel = null;
            }
        }

        protected bool CheckTargetEngagementEnvelope(TargetInfo ti)
        {
            return (ti.isMissile && engageMissile) ||
                    (!ti.isMissile && ti.isFlying && engageAir) ||
                    ((ti.isLanded || ti.isSplashed) && engageGround) ||
                    (ti.isUnderwater && engageSLW);
        }

        protected void ReceiveRadarPing(Vessel v, Vector3 source, RadarWarningReceiver.RWRThreatTypes type, float persistTime)
        {
            if (TargetingMode == TargetingModes.AntiRad && TargetAcquired && v == vessel)
            {
                if ((source - VectorUtils.GetWorldSurfacePostion(targetGPSCoords, vessel.mainBody)).sqrMagnitude < Mathf.Pow(maxStaticLaunchRange / 4, 2) //drastically increase update range for anti-radiation missile to track moving targets!
                    && Vector3.Angle(source - transform.position, GetForwardTransform()) < maxOffBoresight)
                {
                    TargetAcquired = true;
                    TargetPosition = source;
                    targetGPSCoords = VectorUtils.WorldPositionToGeoCoords(TargetPosition, vessel.mainBody);
                    TargetVelocity = Vector3.zero;
                    TargetAcceleration = Vector3.zero;
                    lockFailTimer = 0;
                }
            }
        }

        protected void UpdateAntiRadiationTarget()
        {
            if (!TargetAcquired)
            {
                guidanceActive = false;
                return;
            }

            if (FlightGlobals.ready)
            {
                if (lockFailTimer < 0)
                {
                    lockFailTimer = 0;
                }
                lockFailTimer += Time.fixedDeltaTime;
            }

            if (lockFailTimer > 8)
            {
                guidanceActive = false;
                TargetAcquired = false;
            }
            else
            {
                TargetPosition = VectorUtils.GetWorldSurfacePostion(targetGPSCoords, vessel.mainBody);
            }
        }

        public void DrawDebugLine(Vector3 start, Vector3 end)
        {
            if (BDArmorySettings.DRAW_DEBUG_LINES)
            {
                if (!gameObject.GetComponent<LineRenderer>())
                {
                    LR = gameObject.AddComponent<LineRenderer>();
                    LR.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
                    LR.material.SetColor("_EmissiveColor", Color.red);
                }
                else
                {
                    LR = gameObject.GetComponent<LineRenderer>();
                }
                LR.SetVertexCount(2);
                LR.SetPosition(0, start);
                LR.SetPosition(1, end);
            }
        }

        protected void CheckDetonationDistance()
        {
            if (DetonationDistanceState == DetonationDistanceStates.Detonate)
            {
                Debug.Log("[BDArmory]: Target detected inside sphere - detonating");

                Detonate();
            }
        }

        protected Vector3 CalculateAGMBallisticGuidance(MissileBase missile, Vector3 targetPosition)
        {
            //set up
            if (_originalDistance == float.MinValue)
            {
                _startPoint = missile.vessel.CoM;
                _originalDistance = Vector3.Distance(targetPosition, missile.vessel.CoM);

                //calculating expected apogee bases on isosceles triangle
               
            }

            debugString.Append($"_originalDistance: {_originalDistance}");
            debugString.Append(Environment.NewLine);
            debugString.Append($"BallisticOverShootFactor: {BallisticOverShootFactor}");
            debugString.Append(Environment.NewLine);

            var surfaceDistanceVector = Vector3
                .Project((missile.vessel.CoM - _startPoint), (targetPosition - _startPoint).normalized);

            var pendingDistance = _originalDistance - surfaceDistanceVector.magnitude;

            debugString.Append($"pendingDistance: {pendingDistance}");
            debugString.Append(Environment.NewLine);

            if (TimeIndex < 1)
            {
                return missile.vessel.CoM + missile.vessel.Velocity() * 10;
            }

            Vector3 agmTarget;
            // Getting apoapsis
            if (missile.vessel.verticalSpeed > 0 && pendingDistance > _originalDistance * 0.5)
            {
                debugString.Append($"Ascending");
                debugString.Append(Environment.NewLine);

                var freeFallTime = CalculateFreeFallTime();
                debugString.Append($"freeFallTime: {freeFallTime}");
                debugString.Append(Environment.NewLine);

                var horizontalTime = pendingDistance / missile.vessel.horizontalSrfSpeed;
                debugString.Append($"horizontalTime: {horizontalTime}");
                debugString.Append(Environment.NewLine);

                if (freeFallTime  >= horizontalTime)
                {
                    debugString.Append($"Free fall achieved:");
                    debugString.Append(Environment.NewLine);

                    missile.Throttle = 0;
                    agmTarget = missile.vessel.CoM + missile.vessel.Velocity() * 10;
                }
                else
                {
                    debugString.Append($"Free fall not achieved:");
                    debugString.Append(Environment.NewLine);

                    missile.Throttle = 1;
                    Vector3 dToTarget = targetPosition - missile.vessel.CoM;
                    Vector3 direction = Quaternion.AngleAxis(Mathf.Clamp(missile.maxOffBoresight * 0.9f, 0, 45f), Vector3.Cross(dToTarget, VectorUtils.GetUpDirection(missile.vessel.CoM))) * dToTarget;
                    agmTarget = missile.vessel.CoM + direction;
                }


                debugString.Append($"Throttle: {missile.Throttle}");
                debugString.Append(Environment.NewLine);
            }
            else
            {
                debugString.Append($"Descending");
                debugString.Append(Environment.NewLine);
                agmTarget = MissileGuidance.GetAirToGroundTarget(targetPosition, missile.vessel, 1.85f);

                if (missile is BDModularGuidance)
                {
                    if (missile.vessel.InVacuum())
                    {
                        agmTarget = missile.vessel.CoM + missile.vessel.Velocity() * 10;
                    }
                }
                missile.Throttle = Mathf.Clamp((float) (missile.vessel.atmDensity * 10f), 0.01f, 1f);

            }
            return agmTarget;
        }

        private double CalculateFreeFallTime()
        {
            double vi = -vessel.verticalSpeed;
            double a = 9.80665f * BallisticOverShootFactor;
            double d = vessel.altitude;

            double time1 = (-vi + Math.Sqrt(Math.Pow(vi, 2) - 4 * (0.5f * a) * (-d))) / a;
            double time2 = (-vi - Math.Sqrt(Math.Pow(vi, 2) - 4 * (0.5f * a) * (-d))) / a;


            return Math.Max(time1, time2);
        }

        protected void drawLabels()
        {
            if (vessel == null || !vessel.isActiveVessel) return;
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                GUI.Label(new Rect(200, Screen.height - 200, 400, 400), this.shortName + ":" + debugString.ToString());
            }
        }

        public float GetTntMass()
        {
           return vessel.FindPartModulesImplementing<BDExplosivePart>().Max(x => x.tntMass);
        }

        public void CheckDetonationState()
        {
            //Guard clauses     
            if (!TargetAcquired) return;
            //skip check of user set to zero, rely on OnCollisionEnter
            if (DetonationDistance == 0) return;

            var targetDistancePerFrame = TargetVelocity * Time.deltaTime;
            var missileDistancePerFrame = vessel.Velocity() * Time.deltaTime;

            var futureTargetPosition = (TargetPosition + targetDistancePerFrame);
            var futureMissilePosition = (vessel.CoM + missileDistancePerFrame);
             
            switch (DetonationDistanceState)
            {
                case DetonationDistanceStates.NotSafe:
                    //Lets check if we are at a safe distance from the source vessel
                    Ray raySourceVessel = new Ray(futureMissilePosition, SourceVessel.CoM + SourceVessel.Velocity()*Time.deltaTime );

                    var hits = Physics.RaycastAll(raySourceVessel, GetBlastRadius() * 2, 557057).AsEnumerable();
       
                    using (var hitsEnu = hits.GetEnumerator())
                    {
                        while (hitsEnu.MoveNext())
                        {
                            RaycastHit hit = hitsEnu.Current;

                            try
                            {
                                var hitPart = hit.collider.gameObject.GetComponentInParent<Part>();

                                if (hitPart?.vessel == SourceVessel)
                                {
                                    //We found a hit to the vessel
                                    return;
                                }
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                    }

                    //We are safe and we can continue with the next state
                    DetonationDistanceState = DetonationDistanceStates.Cruising;
                    break;
                case DetonationDistanceStates.Cruising:
                  
                    if (Vector3.Distance(futureMissilePosition, futureTargetPosition) < GetBlastRadius() * 10)
                    {
                        //We are now close enough to start checking the detonation distance
                        DetonationDistanceState = DetonationDistanceStates.CheckingProximity;
                    }
                    break;
                case DetonationDistanceStates.CheckingProximity:
                  
                    float optimalDistance = (float) (DetonationDistance + missileDistancePerFrame.magnitude);

                    using (var hitsEnu = Physics.OverlapSphere(vessel.CoM, optimalDistance, 557057).AsEnumerable().GetEnumerator())
                    {
                        while (hitsEnu.MoveNext())
                        {
                            if (hitsEnu.Current == null) continue;

                            try
                            {
                                Part partHit = hitsEnu.Current.GetComponentInParent<Part>();
                      
                                if (partHit?.vessel == vessel) continue;

                                Debug.Log("[BDArmory]: Missile proximity sphere hit | Distance overlap = " + optimalDistance + "| Part name = " + partHit.name);

                                //We found a hit a different vessel than ours
                                DetonationDistanceState =   DetonationDistanceStates.Detonate;
                                return;
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                    }

                    break;
            }

            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                Debug.Log("[BDArmory]: DetonationDistanceState = : " + DetonationDistanceState);
            }
        }

        protected void SetInitialDetonationDistance()
        {
          
            if (this.DetonationDistance == -1)
            {
                if (GuidanceMode == GuidanceModes.AAMLead || GuidanceMode == GuidanceModes.AAMPure)
                {
                    DetonationDistance = GetBlastRadius() * 0.25f;
                }
                else
                {
                    //DetonationDistance = GetBlastRadius() * 0.05f;
                    DetonationDistance = 0f;
                }
            }
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                Debug.Log("[BDArmory]: DetonationDistance = : " + DetonationDistance);
            }
        }

        protected void CollisionEnter(Collision col)
        {
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
                Debug.Log("[BDArmory]: Missile Collided");

            if (TimeIndex > 2 && HasFired && col.collider.gameObject.GetComponentInParent<Part>().GetFireFX())
            {
                ContactPoint contact = col.contacts[0];
                Vector3 pos = contact.point;
                BulletHitFX.AttachFlames(pos, col.collider.gameObject.GetComponentInParent<Part>());
            }

            if (HasExploded || !HasFired) return;

            if (DetonationDistanceState != DetonationDistanceStates.CheckingProximity) return;    
            
            Debug.Log("[BDArmory]: Missile Collided - Triggering Detonation");
            Detonate();
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Change to Low Altitude Range", active = true)]
        public void CruiseAltitudeRange()
        {
            if (Events["CruiseAltitudeRange"].guiName == "Change to Low Altitude Range")
            {
                Events["CruiseAltitudeRange"].guiName = "Change to High Altitude Range";

                UI_FloatRange cruiseAltitudField = (UI_FloatRange) Fields["CruiseAltitude"].uiControlEditor;
                cruiseAltitudField.maxValue = 500f;
                cruiseAltitudField.minValue = 1f;
                cruiseAltitudField.stepIncrement = 5f;
            }
            else
            {
                Events["CruiseAltitudeRange"].guiName = "Change to Low Altitude Range";
                UI_FloatRange cruiseAltitudField = (UI_FloatRange)Fields["CruiseAltitude"].uiControlEditor;
                cruiseAltitudField.maxValue = 25000f;
                cruiseAltitudField.minValue = 500;
                cruiseAltitudField.stepIncrement = 500f;
            }
            this.part.RefreshAssociatedWindows();
        }
    }
}