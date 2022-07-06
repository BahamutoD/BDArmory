using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BDArmory.Core;
using BDArmory.Core.Extension;
using BDArmory.Core.Utils;
using BDArmory.FX;
using BDArmory.Misc;
using BDArmory.UI;
using UniLinq;
using UnityEngine;

namespace BDArmory.Modules
{
    public class RocketLauncher : EngageableWeapon, IBDWeapon
    {
        public bool hasRocket = true;

        [KSPField(isPersistant = false)] public string rocketType;

        [KSPField(isPersistant = false)] public string rocketModelPath;

        [KSPField(isPersistant = false)] public float rocketMass;

        [KSPField(isPersistant = false)] public float thrust;

        [KSPField(isPersistant = false)] public float thrustTime;

        [KSPField(isPersistant = false)] public float blastRadius;

        [KSPField(isPersistant = false)] public float blastForce;

        [KSPField] public float blastHeat = -1;

        [KSPField(isPersistant = false)] public bool descendingOrder = true;

        [KSPField(isPersistant = false)] public string explModelPath = "BDArmory/Models/explosion/explosion";

        [KSPField(isPersistant = false)] public string explSoundPath = "BDArmory/Sounds/explode1";

        [KSPField] public float thrustDeviation = 0.10f;

        [KSPField] public float maxTargetingRange = 8000;
        float currentTgtRange = 8000;
        float predictedFlightTime = 1;

        public bool drawAimer;

        Vector3 rocketPrediction = Vector3.zero;
        Texture2D aimerTexture;

        Transform[] rockets;

        public AudioSource sfAudioSource;
        private BDStagingAreaGauge gauge;

        //animation
        [KSPField] public string deployAnimationName;
        [KSPField] public float deployAnimationSpeed = 1;
        AnimationState deployAnimState;
        bool hasDeployAnimation;
        public bool deployed;
        Coroutine deployAnimRoutine;

        public bool readyToFire = true;

        public Vessel legacyGuardTarget = null;
        public float lastAutoFiredTime;
        public float autoRippleRate = 0;
        public float autoFireStartTime = 0;
        public float autoFireDuration = 0;

        //turret
        [KSPField] public int turretID = 0;
        public ModuleTurret turret;
        Vector3 trajectoryOffset = Vector3.zero;
        public MissileFire weaponManager;
        bool targetInTurretView = true;

        public float yawRange
        {
            get { return turret ? turret.yawRange : 0; }
        }

        public float maxPitch
        {
            get { return turret ? turret.maxPitch : 0; }
        }

        public float minPitch
        {
            get { return turret ? turret.minPitch : 0; }
        }

        Vector3 targetPosition;
        public Vector3? FiringSolutionVector => targetPosition.IsZero() ? (Vector3?)null : (targetPosition - rockets[0].parent.transform.position).normalized;

        double lastRocketsLeft;
        double rocketsMax;

        //weapon interface
        public Part GetPart()
        {
            return part;
        }

        public WeaponClasses GetWeaponClass()
        {
            return WeaponClasses.Rocket;
        }

        public string GetSubLabel()
        {
            return string.Empty;
        }

        public string GetMissileType()
        {
            return string.Empty;
        }

        [KSPAction("Fire")]
        public void AGFire(KSPActionParam param)
        {
            FireRocket();
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_BDArmory_Fire", active = true)]//Fire
        public void GuiFire()
        {
            FireRocket();
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_BDArmory_Jettison", active = true, guiActiveEditor = false)]//Jettison
        public void Jettison()
        {
            if (turret)
            {
                return;
            }

            part.decouple(0);
            if (BDArmorySetup.Instance.ActiveWeaponManager != null)
                BDArmorySetup.Instance.ActiveWeaponManager.UpdateList();
        }

        [KSPEvent(guiActive = false, guiName = "#LOC_BDArmory_ToggleTurret", guiActiveEditor = false)]//Toggle Turret
        public void ToggleTurret()
        {
            if (deployed)
            {
                DisableTurret();
            }
            else
            {
                EnableTurret();
            }
        }

        public void EnableTurret()
        {
            deployed = true;
            drawAimer = true;
            hasReturned = false;

            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }

            if (hasDeployAnimation)
            {
                if (deployAnimRoutine != null)
                {
                    StopCoroutine(deployAnimRoutine);
                }
                deployAnimRoutine = StartCoroutine(DeployAnimRoutine(true));
            }
            else
            {
                readyToFire = true;
            }
        }

        public void DisableTurret()
        {
            deployed = false;
            readyToFire = false;
            drawAimer = false;
            hasReturned = false;
            targetInTurretView = false;

            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
            }
            returnRoutine = StartCoroutine(ReturnRoutine());

            if (hasDeployAnimation)
            {
                if (deployAnimRoutine != null)
                {
                    StopCoroutine(deployAnimRoutine);
                }

                deployAnimRoutine = StartCoroutine(DeployAnimRoutine(false));
            }
        }

        bool hasReturned = true;
        Coroutine returnRoutine;

        IEnumerator ReturnRoutine()
        {
            if (deployed)
            {
                hasReturned = false;
                yield break;
            }

            yield return new WaitForSeconds(0.25f);

            while (!turret.ReturnTurret())
            {
                yield return new WaitForFixedUpdate();
            }

            hasReturned = true;
        }

        void SetupAudio()
        {
            sfAudioSource = gameObject.AddComponent<AudioSource>();
            sfAudioSource.minDistance = 1;
            sfAudioSource.maxDistance = 2000;
            sfAudioSource.dopplerLevel = 0;
            sfAudioSource.priority = 230;
            sfAudioSource.spatialBlend = 1;

            UpdateVolume();
            BDArmorySetup.OnVolumeChange += UpdateVolume;
        }

        void UpdateVolume()
        {
            if (sfAudioSource)
            {
                sfAudioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
            }
        }

        public void Start()
        {
            // extension for feature_engagementenvelope
            InitializeEngagementRange(0, maxTargetingRange);

            SetupAudio();

            if (HighLogic.LoadedSceneIsFlight)
            {
                part.force_activate();

                aimerTexture = BDArmorySetup.Instance.greenPointCircleTexture;
                // GameDatabase.Instance.GetTexture("BDArmory/Textures/grayCircle", false);

                MakeRocketArray();
                UpdateRocketScales();

                if (shortName == string.Empty)
                {
                    shortName = part.partInfo.title;
                }

                UpdateAudio();
                BDArmorySetup.OnVolumeChange += UpdateAudio;

                gauge = (BDStagingAreaGauge)part.AddModule("BDStagingAreaGauge");
                gauge.AmmoName = rocketType;
                gauge.AudioSource = sfAudioSource;
            }

            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
            {
                List<ModuleTurret>.Enumerator turr = part.FindModulesImplementing<ModuleTurret>().GetEnumerator();
                while (turr.MoveNext())
                {
                    if (turr.Current == null) continue;
                    if (turr.Current.turretID != turretID) continue;
                    turret = turr.Current;
                    targetInTurretView = false;
                    break;
                }
                turr.Dispose();

                if (turret)
                {
                    Events["GuiFire"].guiActive = false;
                    Events["Jettison"].guiActive = false;
                    Actions["AGFire"].active = false;

                    if (HighLogic.LoadedSceneIsFlight)
                    {
                        Events["ToggleTurret"].guiActive = true;
                    }
                }

                if (!string.IsNullOrEmpty(deployAnimationName))
                {
                    deployAnimState = Misc.Misc.SetUpSingleAnimation(deployAnimationName, part);
                    hasDeployAnimation = true;

                    readyToFire = false;
                }
            }

            blastForce = BlastPhysicsUtils.CalculateExplosiveMass(blastRadius);
        }

        IEnumerator DeployAnimRoutine(bool forward)
        {
            readyToFire = false;
            BDArmorySetup.Instance.UpdateCursorState();

            if (forward)
            {
                while (deployAnimState.normalizedTime < 1)
                {
                    deployAnimState.speed = deployAnimationSpeed;
                    yield return null;
                }

                deployAnimState.normalizedTime = 1;
            }
            else
            {
                while (!hasReturned)
                {
                    deployAnimState.speed = 0;
                    yield return null;
                }

                while (deployAnimState.normalizedTime > 0)
                {
                    deployAnimState.speed = -deployAnimationSpeed;
                    yield return null;
                }

                deployAnimState.normalizedTime = 0;
            }

            deployAnimState.speed = 0;

            readyToFire = deployed;
            BDArmorySetup.Instance.UpdateCursorState();
        }

        void UpdateAudio()
        {
            if (sfAudioSource)
            {
                sfAudioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
            }
        }

        void OnDestroy()
        {
            BDArmorySetup.OnVolumeChange -= UpdateAudio;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !vessel.IsControllable)
            {
                return;
            }

            SimulateTrajectory();

            currentTgtRange = maxTargetingRange;

            if (deployed && readyToFire && (turret || weaponManager?.AI?.pilotEnabled == true))
            {
                Aim();
            }
            else
            {
                targetPosition = Vector3.zero;
            }
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight && !vessel.packed)
            {
                if (readyToFire && deployed)
                {
                    if (returnRoutine != null)
                    {
                        StopCoroutine(returnRoutine);
                        returnRoutine = null;
                    }

                    if (weaponManager && weaponManager.guardMode && weaponManager.selectedWeaponString == GetShortName())
                    {
                        if (Time.time - autoFireStartTime < autoFireDuration)
                        {
                            float fireInterval = 0.5f;
                            if (autoRippleRate > 0) fireInterval = 60f / autoRippleRate;
                            if (Time.time - lastAutoFiredTime > fireInterval)
                            {
                                FireRocket();
                                lastAutoFiredTime = Time.time;
                            }
                        }
                    }
                    else if ((!weaponManager ||
                              (weaponManager.selectedWeaponString == GetShortName() && !weaponManager.guardMode)))
                    {
                        if (BDInputUtils.GetKeyDown(BDInputSettingsFields.WEAP_FIRE_KEY) &&
                            (vessel.isActiveVessel || BDArmorySettings.REMOTE_SHOOTING))
                        {
                            FireRocket();
                        }
                    }
                }

                if (GetRocketResource().amount != lastRocketsLeft)
                {
                    UpdateRocketScales();
                    lastRocketsLeft = GetRocketResource().amount;
                }

                if (vessel.isActiveVessel)
                {
                    gauge.UpdateAmmoMeter((float)(lastRocketsLeft / rocketsMax));
                }
            }
        }

        bool mouseAiming;

        void Aim()
        {
            mouseAiming = false;
            if (weaponManager && (weaponManager.slavingTurrets || weaponManager.guardMode || weaponManager.AI?.pilotEnabled == true))
            {
                SlavedAim();
            }
            else
            {
                if (vessel.isActiveVessel || BDArmorySettings.REMOTE_SHOOTING)
                {
                    MouseAim();
                }
            }
        }

        void SlavedAim()
        {
            Vector3 targetVel;
            Vector3 targetAccel;
            if (weaponManager.slavingTurrets)
            {
                targetPosition = weaponManager.slavedPosition;
                targetVel = weaponManager.slavedVelocity;
                targetAccel = weaponManager.slavedAcceleration;

                //targetPosition -= vessel.Velocity * predictedFlightTime;
            }
            else if (legacyGuardTarget)
            {
                targetPosition = legacyGuardTarget.CoM;
                targetVel = legacyGuardTarget.Velocity();
                targetAccel = legacyGuardTarget.acceleration;
            }
            else
            {
                targetInTurretView = false;
                return;
            }

            currentTgtRange = Vector3.Distance(targetPosition, rockets[0].parent.transform.position);

            targetPosition += trajectoryOffset;
            targetPosition += targetVel * predictedFlightTime;
            targetPosition += 0.5f * targetAccel * predictedFlightTime * predictedFlightTime;

            turret.AimToTarget(targetPosition);
            targetInTurretView = turret.TargetInRange(targetPosition, 2, maxTargetingRange);
        }

        void MouseAim()
        {
            mouseAiming = true;
            Vector3 targetPosition;
            //	float maxTargetingRange = 8000;

            float targetDistance;

            //MouseControl
            Vector3 mouseAim = new Vector3(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height, 0);
            Ray ray = FlightCamera.fetch.mainCamera.ViewportPointToRay(mouseAim);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxTargetingRange, 557057))
            {
                targetPosition = hit.point;

                //aim through self vessel if occluding mouseray
                Part p = hit.collider.gameObject.GetComponentInParent<Part>();
                if (p && p.vessel && p.vessel == vessel)
                {
                    targetPosition = ray.direction * maxTargetingRange + FlightCamera.fetch.mainCamera.transform.position;
                }

                targetDistance = Vector3.Distance(hit.point, rockets[0].parent.position);
            }
            else
            {
                targetPosition = (ray.direction * (maxTargetingRange + (FlightCamera.fetch.Distance * 0.75f))) +
                                 FlightCamera.fetch.mainCamera.transform.position;
                targetDistance = maxTargetingRange;
            }

            currentTgtRange = targetDistance;

            targetPosition += trajectoryOffset;

            turret.AimToTarget(targetPosition);
            targetInTurretView = turret.TargetInRange(targetPosition, 2, maxTargetingRange);
        }

        public void FireRocket()
        {
            if (!readyToFire) return;
            if (!targetInTurretView) return;

            PartResource rocketResource = GetRocketResource();

            if (rocketResource == null)
            {
                Debug.Log(part.name + " doesn't carry the rocket resource it was meant to");
                return;
            }

            int rocketsLeft = (int)Math.Floor(rocketResource.amount);

            if (BDArmorySettings.INFINITE_AMMO && rocketsLeft < 1)
                rocketsLeft = 1;

            if (rocketsLeft >= 1)
            {
                Transform currentRocketTfm = rockets[rocketsLeft - 1];

                GameObject rocketObj = GameDatabase.Instance.GetModel(rocketModelPath);
                rocketObj =
                    (GameObject)Instantiate(rocketObj, currentRocketTfm.position, currentRocketTfm.parent.rotation);
                rocketObj.transform.rotation = currentRocketTfm.parent.rotation;
                rocketObj.transform.localScale = part.rescaleFactor * Vector3.one;
                currentRocketTfm.localScale = Vector3.zero;
                Rocket rocket = rocketObj.AddComponent<Rocket>();
                rocket.explModelPath = explModelPath;
                rocket.explSoundPath = explSoundPath;
                rocket.spawnTransform = currentRocketTfm;
                rocket.mass = rocketMass;
                rocket.blastForce = blastForce;
                rocket.blastHeat = blastHeat;
                rocket.blastRadius = blastRadius;
                rocket.thrust = thrust;
                rocket.thrustTime = thrustTime;
                rocket.randomThrustDeviation = thrustDeviation;

                rocket.sourceVessel = vessel;
                rocketObj.SetActive(true);
                rocketObj.transform.SetParent(currentRocketTfm.parent);
                rocket.parentRB = part.rb;

                sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/launch"));
                if (!BDArmorySettings.INFINITE_AMMO)
                    rocketResource.amount--;

                lastRocketsLeft = rocketResource.amount;
            }
        }

        void SimulateTrajectory()
        {
            if ((BDArmorySettings.AIM_ASSIST && BDArmorySettings.DRAW_AIMERS && drawAimer && vessel.isActiveVessel) ||
                (weaponManager && weaponManager.guardMode && weaponManager.selectedWeaponString == GetShortName()))
            {
                float simTime = 0;
                Transform fireTransform = rockets[0].parent;
                Vector3 pointingDirection = fireTransform.forward;
                Vector3 simVelocity = part.rb.velocity;
                Vector3 simCurrPos = fireTransform.position + (part.rb.velocity * Time.fixedDeltaTime);
                Vector3 simPrevPos = fireTransform.position + (part.rb.velocity * Time.fixedDeltaTime);
                Vector3 simStartPos = fireTransform.position + (part.rb.velocity * Time.fixedDeltaTime);
                bool simulating = true;
                float simDeltaTime = 0.02f;
                List<Vector3> pointPositions = new List<Vector3>();
                pointPositions.Add(simCurrPos);

                bool slaved = turret && weaponManager && (weaponManager.slavingTurrets || weaponManager.guardMode);
                float atmosMultiplier =
                    Mathf.Clamp01(2.5f *
                                  (float)
                                  FlightGlobals.getAtmDensity(vessel.staticPressurekPa, vessel.externalTemperature,
                                      vessel.mainBody));
                while (simulating)
                {
                    RaycastHit hit;

                    if (simTime > thrustTime)
                    {
                        simDeltaTime = 0.1f;
                    }

                    if (simTime > 0.04f)
                    {
                        simDeltaTime = 0.02f;
                        if (simTime < thrustTime)
                        {
                            simVelocity += thrust / rocketMass * simDeltaTime * pointingDirection;
                        }

                        //rotation (aero stabilize)
                        pointingDirection = Vector3.RotateTowards(pointingDirection,
                            simVelocity + Krakensbane.GetFrameVelocity(),
                            atmosMultiplier * (0.5f * (simTime)) * 50 * simDeltaTime * Mathf.Deg2Rad, 0);
                    }

                    //gravity
                    simVelocity += FlightGlobals.getGeeForceAtPosition(simCurrPos) * simDeltaTime;

                    simCurrPos += simVelocity * simDeltaTime;
                    pointPositions.Add(simCurrPos);
                    if (!mouseAiming && !slaved)
                    {
                        if (simTime > 0.1f && Physics.Raycast(simPrevPos, simCurrPos - simPrevPos, out hit,
                                Vector3.Distance(simPrevPos, simCurrPos), 9076737))
                        {
                            rocketPrediction = hit.point;
                            simulating = false;
                            break;
                        }
                        else if (FlightGlobals.getAltitudeAtPos(simCurrPos) < 0)
                        {
                            rocketPrediction = simCurrPos;
                            simulating = false;
                            break;
                        }
                    }

                    simPrevPos = simCurrPos;

                    if ((simStartPos - simCurrPos).sqrMagnitude > currentTgtRange * currentTgtRange)
                    {
                        rocketPrediction = simStartPos + (simCurrPos - simStartPos).normalized * currentTgtRange;
                        //rocketPrediction = simCurrPos;
                        simulating = false;
                    }
                    simTime += simDeltaTime;
                }

                Vector3 pointingPos = fireTransform.position + (fireTransform.forward * currentTgtRange);
                trajectoryOffset = pointingPos - rocketPrediction;
                predictedFlightTime = simTime;

                if (BDArmorySettings.DRAW_DEBUG_LINES && BDArmorySettings.DRAW_AIMERS)
                {
                    Vector3[] pointsArray = pointPositions.ToArray();
                    if (gameObject.GetComponent<LineRenderer>() == null)
                    {
                        LineRenderer lr = gameObject.AddComponent<LineRenderer>();
                        lr.startWidth = .1f;
                        lr.endWidth = .1f;
                        lr.positionCount = pointsArray.Length;
                        for (int i = 0; i < pointsArray.Length; i++)
                        {
                            lr.SetPosition(i, pointsArray[i]);
                        }
                    }
                    else
                    {
                        LineRenderer lr = gameObject.GetComponent<LineRenderer>();
                        lr.enabled = true;
                        lr.positionCount = pointsArray.Length;
                        for (int i = 0; i < pointsArray.Length; i++)
                        {
                            lr.SetPosition(i, pointsArray[i]);
                        }
                    }
                }
                else
                {
                    if (gameObject.GetComponent<LineRenderer>() != null)
                    {
                        gameObject.GetComponent<LineRenderer>().enabled = false;
                    }
                }
            }

            //for straight aimer
            else if (BDArmorySettings.DRAW_AIMERS && drawAimer && vessel.isActiveVessel)
            {
                RaycastHit hit;
                float distance = 2500;
                if (Physics.Raycast(transform.position, transform.forward, out hit, distance, 9076737))
                {
                    rocketPrediction = hit.point;
                }
                else
                {
                    rocketPrediction = transform.position + (transform.forward * distance);
                }
            }
        }

        void OnGUI()
        {
            if (drawAimer && vessel.isActiveVessel && BDArmorySettings.DRAW_AIMERS && !MapView.MapIsEnabled)
            {
                float size = 30;

                Vector3 aimPosition = FlightCamera.fetch.mainCamera.WorldToViewportPoint(rocketPrediction);

                Rect drawRect = new Rect(aimPosition.x * Screen.width - (0.5f * size),
                    (1 - aimPosition.y) * Screen.height - (0.5f * size), size, size);
                float cameraAngle = Vector3.Angle(FlightCamera.fetch.GetCameraTransform().forward,
                    rocketPrediction - FlightCamera.fetch.mainCamera.transform.position);
                if (cameraAngle < 90) GUI.DrawTexture(drawRect, aimerTexture);
            }
        }

        void MakeRocketArray()
        {
            Transform rocketsTransform = part.FindModelTransform("rockets");
            int numOfRockets = rocketsTransform.childCount;
            rockets = new Transform[numOfRockets];

            for (int i = 0; i < numOfRockets; i++)
            {
                string rocketName = rocketsTransform.GetChild(i).name;
                int rocketIndex = int.Parse(rocketName.Substring(7)) - 1;
                rockets[rocketIndex] = rocketsTransform.GetChild(i);
            }

            if (!descendingOrder) Array.Reverse(rockets);
        }

        public PartResource GetRocketResource()
        {
            using (var res = part.Resources.GetEnumerator())
                while (res.MoveNext())
                {
                    if (res.Current == null) continue;
                    if (res.Current.resourceName == rocketType) return res.Current;
                }
            return null;
        }

        void UpdateRocketScales()
        {
            // This is a backup method only, rockets will be rescaled to zero on firing,
            // so this will be called only on start and if something weird happens.
            PartResource rocketResource = GetRocketResource();
            var rocketsLeft = Math.Floor(rocketResource.amount);
            rocketsMax = rocketResource.maxAmount;
            for (int i = 0; i < rocketsMax; i++)
            {
                if (i < rocketsLeft) rockets[i].localScale = Vector3.one;
                else rockets[i].localScale = Vector3.zero;
            }
        }

        // RMB info in editor
        public override string GetInfo()
        {
            StringBuilder output = new StringBuilder();
            output.Append(Environment.NewLine);
            output.AppendLine("Weapon Type: Rocket Launcher");
            output.AppendLine($"Rocket Type: {rocketType}");
            output.AppendLine($"Max Range: {maxTargetingRange} m");

            output.AppendLine($"Blast:");
            output.AppendLine($"- radius: {blastRadius}");
            output.AppendLine($"- power: {blastForce}");
            output.AppendLine($"- heat: {blastHeat}");

            return output.ToString();
        }
    }

    public class Rocket : MonoBehaviour
    {
        public Transform spawnTransform;
        public Vessel sourceVessel;
        public float mass;
        public float thrust;
        public float thrustTime;
        public float blastRadius;
        public float blastForce;
        public float blastHeat;
        public string explModelPath;
        public string explSoundPath;

        public float randomThrustDeviation = 0.05f;

        public Rigidbody parentRB;

        float startTime;
        public AudioSource audioSource;

        Vector3 prevPosition;
        Vector3 currPosition;

        float stayTime = 0.04f;
        float lifeTime = 10;

        //bool isThrusting = true;

        Rigidbody rb;

        KSPParticleEmitter[] pEmitters;

        float randThrustSeed;

        void Start()
        {
            BDArmorySetup.numberOfParticleEmitters++;

            rb = gameObject.AddComponent<Rigidbody>();
            pEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();

            IEnumerator<KSPParticleEmitter> pe = pEmitters.AsEnumerable().GetEnumerator();
            while (pe.MoveNext())
            {
                if (pe.Current == null) continue;
                if (FlightGlobals.getStaticPressure(transform.position) == 0 && pe.Current.useWorldSpace)
                {
                    pe.Current.emit = false;
                }
                else if (pe.Current.useWorldSpace)
                {
                    BDAGaplessParticleEmitter gpe = pe.Current.gameObject.AddComponent<BDAGaplessParticleEmitter>();
                    gpe.rb = rb;
                    gpe.emit = true;
                }
                else
                {
                    EffectBehaviour.AddParticleEmitter(pe.Current);
                }
            }
            pe.Dispose();

            prevPosition = transform.position;
            currPosition = transform.position;
            startTime = Time.time;

            rb.mass = mass;
            rb.isKinematic = true;
            //rigidbody.velocity = startVelocity;
            if (!FlightGlobals.RefFrameIsRotating) rb.useGravity = false;

            rb.useGravity = false;

            randThrustSeed = UnityEngine.Random.Range(0f, 100f);

            SetupAudio();
        }

        void FixedUpdate()
        {
            //floating origin and velocity offloading corrections
            if (!FloatingOrigin.Offset.IsZero() || !Krakensbane.GetFrameVelocity().IsZero())
            {
                transform.position -= FloatingOrigin.OffsetNonKrakensbane;
                prevPosition -= FloatingOrigin.OffsetNonKrakensbane;
            }

            if (Time.time - startTime < stayTime && transform.parent != null)
            {
                transform.rotation = transform.parent.rotation;
                transform.position = spawnTransform.position;
                //+(transform.parent.rigidbody.velocity*Time.fixedDeltaTime);
            }
            else
            {
                if (transform.parent != null && parentRB)
                {
                    transform.parent = null;
                    rb.isKinematic = false;
                    rb.velocity = parentRB.velocity + Krakensbane.GetFrameVelocityV3f();
                }
            }

            if (rb && !rb.isKinematic)
            {
                //physics
                if (FlightGlobals.RefFrameIsRotating)
                {
                    rb.velocity += FlightGlobals.getGeeForceAtPosition(transform.position) * Time.fixedDeltaTime;
                }

                //guidance and attitude stabilisation scales to atmospheric density.
                float atmosMultiplier =
                    Mathf.Clamp01(2.5f *
                                  (float)
                                  FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(transform.position),
                                      FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody));

                //model transform. always points prograde
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(rb.velocity + Krakensbane.GetFrameVelocity(), transform.up),
                    atmosMultiplier * (0.5f * (Time.time - startTime)) * 50 * Time.fixedDeltaTime);

                if (Time.time - startTime < thrustTime && Time.time - startTime > stayTime)
                {
                    float random = randomThrustDeviation * (1 - (Mathf.PerlinNoise(4 * Time.time, randThrustSeed) * 2));
                    float random2 = randomThrustDeviation * (1 - (Mathf.PerlinNoise(randThrustSeed, 4 * Time.time) * 2));
                    rb.AddRelativeForce(new Vector3(random, random2, thrust));
                }
            }

            if (Time.time - startTime > thrustTime)
            {
                //isThrusting = false;
                IEnumerator<KSPParticleEmitter> pEmitter = pEmitters.AsEnumerable().GetEnumerator();
                while (pEmitter.MoveNext())
                {
                    if (pEmitter.Current == null) continue;
                    if (pEmitter.Current.useWorldSpace)
                    {
                        pEmitter.Current.minSize = Mathf.MoveTowards(pEmitter.Current.minSize, 0.1f, 0.05f);
                        pEmitter.Current.maxSize = Mathf.MoveTowards(pEmitter.Current.maxSize, 0.2f, 0.05f);
                    }
                    else
                    {
                        pEmitter.Current.minSize = Mathf.MoveTowards(pEmitter.Current.minSize, 0, 0.1f);
                        pEmitter.Current.maxSize = Mathf.MoveTowards(pEmitter.Current.maxSize, 0, 0.1f);
                        if (pEmitter.Current.maxSize == 0)
                        {
                            pEmitter.Current.emit = false;
                        }
                    }
                }
                pEmitter.Dispose();
            }

            if (Time.time - startTime > 0.1f + stayTime)
            {
                currPosition = transform.position;
                float dist = (currPosition - prevPosition).magnitude;
                Ray ray = new Ray(prevPosition, currPosition - prevPosition);
                RaycastHit hit;
                KerbalEVA hitEVA = null;
                //if (Physics.Raycast(ray, out hit, dist, 2228224))
                //{
                //    try
                //    {
                //        hitEVA = hit.collider.gameObject.GetComponentUpwards<KerbalEVA>();
                //        if (hitEVA != null)
                //            Debug.Log("[BDArmory]:Hit on kerbal confirmed!");
                //    }
                //    catch (NullReferenceException)
                //    {
                //        Debug.Log("[BDArmory]:Whoops ran amok of the exception handler");
                //    }

                //    if (hitEVA && hitEVA.part.vessel != sourceVessel)
                //    {
                //        Detonate(hit.point);
                //    }
                //}

                if (!hitEVA)
                {
                    if (Physics.Raycast(ray, out hit, dist, 9076737))
                    {
                        Part hitPart = null;
                        try
                        {
                            KerbalEVA eva = hit.collider.gameObject.GetComponentUpwards<KerbalEVA>();
                            hitPart = eva ? eva.part : hit.collider.gameObject.GetComponentInParent<Part>();
                        }
                        catch (NullReferenceException)
                        {
                        }

                        if (hitPart == null || (hitPart != null && hitPart.vessel != sourceVessel))
                        {
                            Detonate(hit.point);
                        }
                    }
                    else if (FlightGlobals.getAltitudeAtPos(transform.position) < 0)
                    {
                        Detonate(transform.position);
                    }
                }
            }
            else if (FlightGlobals.getAltitudeAtPos(currPosition) <= 0)
            {
                Detonate(currPosition);
            }
            prevPosition = currPosition;

            if (Time.time - startTime > lifeTime)
            {
                Detonate(transform.position);
            }
        }

        void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (BDArmorySetup.GameIsPaused)
                {
                    if (audioSource.isPlaying)
                    {
                        audioSource.Stop();
                    }
                }
                else
                {
                    if (!audioSource.isPlaying)
                    {
                        audioSource.Play();
                    }
                }
            }
        }

        void Detonate(Vector3 pos)
        {
            BDArmorySetup.numberOfParticleEmitters--;

            ExplosionFx.CreateExplosion(pos, BlastPhysicsUtils.CalculateExplosiveMass(blastRadius),
                explModelPath, explSoundPath, true);

            IEnumerator<KSPParticleEmitter> emitter = pEmitters.AsEnumerable().GetEnumerator();
            while (emitter.MoveNext())
            {
                if (emitter.Current == null) continue;
                if (!emitter.Current.useWorldSpace) continue;
                emitter.Current.gameObject.AddComponent<BDAParticleSelfDestruct>();
                emitter.Current.transform.parent = null;
            }
            emitter.Dispose();
            Destroy(gameObject); //destroy rocket on collision
        }

        void SetupAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.minDistance = 1;
            audioSource.maxDistance = 2000;
            audioSource.dopplerLevel = 0.5f;
            audioSource.volume = 0.9f * BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
            audioSource.pitch = 1f;
            audioSource.priority = 255;
            audioSource.spatialBlend = 1;

            audioSource.clip = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/rocketLoop");

            UpdateVolume();
            BDArmorySetup.OnVolumeChange += UpdateVolume;
        }

        void UpdateVolume()
        {
            if (audioSource)
            {
                audioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
            }
        }
    }
}
