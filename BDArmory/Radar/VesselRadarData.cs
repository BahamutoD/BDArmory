using System.Collections;
using System.Collections.Generic;
using BDArmory.Core;
using BDArmory.Misc;
using BDArmory.Modules;
using BDArmory.Targeting;
using BDArmory.UI;
using UnityEngine;

namespace BDArmory.Radar
{
    public class VesselRadarData : MonoBehaviour
    {
        private List<ModuleRadar> availableRadars;
        private List<ModuleRadar> externalRadars;
        private List<VesselRadarData> externalVRDs;
        private float _maxRadarRange = 0;
        internal bool resizingWindow = false;

        public Rect RADARresizeRect = new Rect(
            BDArmorySetup.WindowRectRadar.width - (17 * BDArmorySettings.RADAR_WINDOW_SCALE),
            BDArmorySetup.WindowRectRadar.height - (17 * BDArmorySettings.RADAR_WINDOW_SCALE),
            (16 * BDArmorySettings.RADAR_WINDOW_SCALE),
            (16 * BDArmorySettings.RADAR_WINDOW_SCALE));

        private int rCount;

        public int radarCount
        {
            get { return rCount; }
        }

        public bool guiEnabled
        {
            get { return drawGUI; }
        }

        private bool drawGUI;

        public MissileFire weaponManager;
        public bool canReceiveRadarData;

        //GUI
        public bool linkWindowOpen;
        private float numberOfAvailableLinks;
        public Rect linkWindowRect = new Rect(0, 0, 0, 0);
        private float linkRectWidth = 200;
        private float linkRectEntryHeight = 26;

        public static bool radarRectInitialized;
        internal static float RadarScreenSize = 360;
        internal static Rect RadarDisplayRect;

        internal static float BorderSize = 10;
        internal static float HeaderSize = 15;
        internal static float ControlsWidth = 125;
        internal static float Gap = 2;

        private Vector2 pingSize = new Vector2(16, 8);

        private static readonly Texture2D rollIndicatorTexture =
            GameDatabase.Instance.GetTexture(BDArmorySetup.textureDir + "radarRollIndicator", false);

        internal static readonly Texture2D omniBgTexture =
            GameDatabase.Instance.GetTexture(BDArmorySetup.textureDir + "omniRadarTexture", false);

        internal static readonly Texture2D radialBgTexture = GameDatabase.Instance.GetTexture(
            BDArmorySetup.textureDir + "radialRadarTexture", false);

        private static readonly Texture2D scanTexture = GameDatabase.Instance.GetTexture(BDArmorySetup.textureDir + "omniRadarScanTexture",
            false);

        private static readonly Texture2D lockIcon = GameDatabase.Instance.GetTexture(BDArmorySetup.textureDir + "lockedRadarIcon", false);

        private static readonly Texture2D lockIconActive =
            GameDatabase.Instance.GetTexture(BDArmorySetup.textureDir + "lockedRadarIconActive", false);

        private static readonly Texture2D radarContactIcon = GameDatabase.Instance.GetTexture(BDArmorySetup.textureDir + "radarContactIcon",
            false);

        private static readonly Texture2D friendlyContactIcon =
            GameDatabase.Instance.GetTexture(BDArmorySetup.textureDir + "friendlyContactIcon", false);

        private GUIStyle distanceStyle;
        private GUIStyle lockStyle;
        private GUIStyle radarTopStyle;

        private bool noData;

        private float guiInputTime;
        private float guiInputCooldown = 0.2f;

        //range increments
        //TODO:  Determine how to dynamically generate this list from the radar being used.
        public float[] baseIncrements = new float[] { 500, 2500, 5000, 10000, 20000, 40000, 100000, 250000, 500000, 750000, 1000000, 2000000 };

        public float[] rIncrements = new float[] { 500, 2500, 5000, 10000, 20000, 40000, 100000, 250000, 500000, 750000, 1000000, 2000000 };
        private int rangeIndex = 0;

        //lock cursor
        private bool showSelector;
        private Vector2 selectorPos = Vector2.zero;

        //data link
        private List<VesselRadarData> availableExternalVRDs;

        private Transform referenceTransform;
        private Transform vesselReferenceTransform;

        public MissileBase LastMissile;

        //bool boresightScan = false;

        //TargetSignatureData[] contacts = new TargetSignatureData[30];
        private List<RadarDisplayData> displayedTargets;
        public bool locked;
        private int activeLockedTargetIndex;
        private List<int> lockedTargetIndexes;

        public bool hasLoadedExternalVRDs = false;

        public List<TargetSignatureData> GetLockedTargets()
        {
            List<TargetSignatureData> lockedTargets = new List<TargetSignatureData>();
            for (int i = 0; i < lockedTargetIndexes.Count; i++)
            {
                lockedTargets.Add(displayedTargets[lockedTargetIndexes[i]].targetData);
            }
            return lockedTargets;
        }

        public RadarDisplayData lockedTargetData
        {
            get { return displayedTargets[lockedTargetIndexes[activeLockedTargetIndex]]; }
        }

        //turret slaving
        public bool slaveTurrets;

        private Vessel myVessel;

        public Vessel vessel
        {
            get { return myVessel; }
        }

        public void AddRadar(ModuleRadar mr)
        {
            if (availableRadars.Contains(mr))
            {
                return;
            }

            availableRadars.Add(mr);
            rCount = availableRadars.Count;
            //UpdateDataLinkCapability();
            linkCapabilityDirty = true;
            rangeCapabilityDirty = true;
        }

        public void RemoveRadar(ModuleRadar mr)
        {
            availableRadars.Remove(mr);
            rCount = availableRadars.Count;
            RemoveDataFromRadar(mr);
            //UpdateDataLinkCapability();
            linkCapabilityDirty = true;
            rangeCapabilityDirty = true;
        }

        public bool linkCapabilityDirty;
        public bool rangeCapabilityDirty;
        public bool radarsReady;

        private void Awake()
        {
            availableRadars = new List<ModuleRadar>();
            externalRadars = new List<ModuleRadar>();
            myVessel = GetComponent<Vessel>();
            lockedTargetIndexes = new List<int>();
            availableExternalVRDs = new List<VesselRadarData>();

            distanceStyle = new GUIStyle
            {
                normal = { textColor = new Color(0, 1, 0, 0.75f) },
                alignment = TextAnchor.UpperLeft
            };

            lockStyle = new GUIStyle
            {
                normal = { textColor = new Color(0, 1, 0, 0.75f) },
                alignment = TextAnchor.LowerCenter,
                fontSize = 16
            };

            radarTopStyle = new GUIStyle
            {
                normal = { textColor = new Color(0, 1, 0, 0.65f) },
                alignment = TextAnchor.UpperCenter,
                fontSize = 12
            };

            vesselReferenceTransform = (new GameObject()).transform;
            //vesselReferenceTransform.parent = vessel.transform;
            //vesselReferenceTransform.localPosition = Vector3.zero;
            vesselReferenceTransform.localScale = Vector3.one;

            displayedTargets = new List<RadarDisplayData>();
            externalVRDs = new List<VesselRadarData>();
            waitingForVessels = new List<string>();

            RadarDisplayRect = new Rect(BorderSize / 2, BorderSize / 2 + HeaderSize,
              RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE,
              RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE);

            if (!radarRectInitialized)
            {
                float width = RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE + BorderSize + ControlsWidth + Gap * 3;
                float height = RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE + BorderSize + HeaderSize;
                BDArmorySetup.WindowRectRadar = new Rect(Screen.width - width, Screen.height - height, width, height);
                radarRectInitialized = true;
            }
        }

        private void Start()
        {
            rangeIndex = rIncrements.Length - 6;

            //determine configured physics ranges and add a radar range level for the highest range
            if (vessel.vesselRanges.flying.load > rIncrements[rIncrements.Length - 1])
            {
                rIncrements = new[] { 500, 2500, 5000, 10000, 20000, 40000, 100000, 250000, 500000, 750000, 1000000, vessel.vesselRanges.flying.load };
                rangeIndex--;
            }

            UpdateLockedTargets();
            List<MissileFire>.Enumerator mf = vessel.FindPartModulesImplementing<MissileFire>().GetEnumerator();
            while (mf.MoveNext())
            {
                if (mf.Current == null) continue;
                mf.Current.vesselRadarData = this;
            }
            mf.Dispose();
            GameEvents.onVesselDestroy.Add(OnVesselDestroyed);
            GameEvents.onVesselCreate.Add(OnVesselDestroyed);
            MissileFire.OnChangeTeam += OnChangeTeam;
            GameEvents.onGameStateSave.Add(OnGameStateSave);
            GameEvents.onPartDestroyed.Add(PartDestroyed);

            if (!weaponManager)
            {
                List<MissileFire>.Enumerator mfa = vessel.FindPartModulesImplementing<MissileFire>().GetEnumerator();
                while (mfa.MoveNext())
                {
                    if (mfa.Current == null) continue;
                    weaponManager = mfa.Current;
                    break;
                }
                mfa.Dispose();
            }

            StartCoroutine(StartupRoutine());
        }

        private IEnumerator StartupRoutine()
        {
            while (!FlightGlobals.ready || vessel.packed)
            {
                yield return null;
            }

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            radarsReady = true;
        }

        private void OnGameStateSave(ConfigNode n)
        {
            SaveExternalVRDVessels();
        }

        private void SaveExternalVRDVessels()
        {
            string linkedVesselID = "";

            List<VesselRadarData>.Enumerator v = externalVRDs.GetEnumerator();
            while (v.MoveNext())
            {
                if (v.Current == null) continue;
                linkedVesselID += v.Current.vessel.id + ",";
            }
            v.Dispose();

            List<string>.Enumerator id = waitingForVessels.GetEnumerator();
            while (id.MoveNext())
            {
                if (id.Current == null) continue;
                linkedVesselID += id.Current + ",";
            }
            id.Dispose();

            List<ModuleRadar>.Enumerator radar = availableRadars.GetEnumerator();
            while (radar.MoveNext())
            {
                if (radar.Current == null) continue;
                if (radar.Current.vessel != vessel) continue;
                radar.Current.linkedVesselID = linkedVesselID;
                return;
            }
            radar.Dispose();
        }

        private void OnDestroy()
        {
            GameEvents.onVesselDestroy.Remove(OnVesselDestroyed);
            GameEvents.onVesselCreate.Remove(OnVesselDestroyed);
            MissileFire.OnChangeTeam -= OnChangeTeam;
            GameEvents.onGameStateSave.Remove(OnGameStateSave);
            GameEvents.onPartDestroyed.Remove(PartDestroyed);

            if (weaponManager)
            {
                if (slaveTurrets)
                {
                    weaponManager.slavingTurrets = false;
                }
            }
        }

        private void OnChangeTeam(MissileFire wm, BDTeam team)
        {
            if (!weaponManager || !wm) return;

            if (team != weaponManager.Team)
            {
                if (wm.vesselRadarData)
                {
                    UnlinkVRD(wm.vesselRadarData);
                }
            }
            else if (wm.vessel == vessel)
            {
                UnlinkAllExternalRadars();
            }

            RemoveDisconnectedRadars();
        }

        private void UpdateRangeCapability()
        {
            _maxRadarRange = 0;
            List<ModuleRadar>.Enumerator rad = availableRadars.GetEnumerator();
            while (rad.MoveNext())
            {
                if (rad.Current == null) continue;
                float maxRange = rad.Current.radarDetectionCurve.maxTime * 1000;
                if (rad.Current.vessel != vessel || !(maxRange > 0)) continue;
                if (maxRange > _maxRadarRange) _maxRadarRange = maxRange;
            }
            rad.Dispose();
            // Now rebuild range display array
            List<float> newArray = new List<float>();
            for (int x = 0; x < baseIncrements.Length; x++)
            {
                newArray.Add(baseIncrements[x]);
                if (_maxRadarRange <= baseIncrements[x])
                {
                    break;
                }
            }
            if (newArray.Count > 0) rIncrements = newArray.ToArray();
        }

        private void UpdateDataLinkCapability()
        {
            canReceiveRadarData = false;
            noData = true;
            List<ModuleRadar>.Enumerator rad = availableRadars.GetEnumerator();
            while (rad.MoveNext())
            {
                if (rad.Current == null) continue;
                if (rad.Current.vessel == vessel && rad.Current.canRecieveRadarData)
                {
                    canReceiveRadarData = true;
                }

                if (rad.Current.canScan)
                {
                    noData = false;
                }
            }
            rad.Dispose();

            if (!canReceiveRadarData)
            {
                UnlinkAllExternalRadars();
            }

            List<ModuleRadar>.Enumerator mr = availableRadars.GetEnumerator();
            while (mr.MoveNext())
            {
                if (mr.Current == null) continue;
                if (mr.Current.canScan)
                {
                    noData = false;
                }
            }
            mr.Dispose();
        }

        private void UpdateReferenceTransform()
        {
            if (radarCount == 1 && !availableRadars[0].omnidirectional && !vessel.Landed)
            {
                referenceTransform = availableRadars[0].referenceTransform;
            }
            else
            {
                referenceTransform = vesselReferenceTransform;
            }
        }

        private void PartDestroyed(Part p)
        {
            RemoveDisconnectedRadars();
            UpdateLockedTargets();
            RefreshAvailableLinks();
        }

        private void OnVesselDestroyed(Vessel v)
        {
            RemoveDisconnectedRadars();
            UpdateLockedTargets();
            RefreshAvailableLinks();
        }

        private void RemoveDisconnectedRadars()
        {
            availableRadars.RemoveAll(r => r == null);
            List<ModuleRadar> radarsToRemove = new List<ModuleRadar>();
            List<ModuleRadar>.Enumerator radar = availableRadars.GetEnumerator();
            while (radar.MoveNext())
            {
                if (radar.Current == null) continue;
                if (!radar.Current.radarEnabled || (radar.Current.vessel != vessel && !externalRadars.Contains(radar.Current)))
                {
                    radarsToRemove.Add(radar.Current);
                }
                else if (!radar.Current.weaponManager || (weaponManager && radar.Current.weaponManager.Team != weaponManager.Team))
                {
                    radarsToRemove.Add(radar.Current);
                }
            }
            radar.Dispose();

            List<ModuleRadar>.Enumerator rrad = radarsToRemove.GetEnumerator();
            while (rrad.MoveNext())
            {
                if (rrad.Current == null) continue;
                RemoveRadar(rrad.Current);
            }
            rrad.Dispose();
            rCount = availableRadars.Count;

            RemoveEmptyVRDs();
        }

        public void UpdateLockedTargets()
        {
            locked = false;

            lockedTargetIndexes.Clear(); // = new List<int>();

            for (int i = 0; i < displayedTargets.Count; i++)
            {
                if (!displayedTargets[i].vessel || !displayedTargets[i].locked) continue;
                locked = true;
                lockedTargetIndexes.Add(i);
            }

            activeLockedTargetIndex = locked
                ? Mathf.Clamp(activeLockedTargetIndex, 0, lockedTargetIndexes.Count - 1)
                : 0;
        }

        private void UpdateSlaveData()
        {
            if (!slaveTurrets || !weaponManager) return;
            weaponManager.slavingTurrets = true;
            if (!locked) return;
            TargetSignatureData lockedTarget = lockedTargetData.targetData;
            weaponManager.slavedPosition = lockedTarget.predictedPosition;
            weaponManager.slavedVelocity = lockedTarget.velocity;
            weaponManager.slavedAcceleration = lockedTarget.acceleration;
            weaponManager.slavedTarget = lockedTarget;
        }

        private void Update()
        {
            if (!vessel)
            {
                Destroy(this);
                return;
            }

            UpdateReferenceTransform();

            if (radarCount > 0)
            {
                //vesselReferenceTransform.parent = linkedRadars[0].transform;
                vesselReferenceTransform.localScale = Vector3.one;
                vesselReferenceTransform.position = vessel.CoM;

                vesselReferenceTransform.rotation = Quaternion.LookRotation(vessel.LandedOrSplashed ?
                  VectorUtils.GetNorthVector(vessel.transform.position, vessel.mainBody) :
                  Vector3.ProjectOnPlane(vessel.transform.up, vessel.upAxis), vessel.upAxis);

                CleanDisplayedContacts();

                UpdateInputs();

                UpdateSlaveData();
            }
            else
            {
                if (slaveTurrets)
                {
                    UnslaveTurrets();
                }
            }

            if (linkCapabilityDirty)
            {
                UpdateDataLinkCapability();
                linkCapabilityDirty = false;
            }

            if (rangeCapabilityDirty)
            {
                UpdateRangeCapability();
                rangeCapabilityDirty = false;
            }

            drawGUI = (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready && !vessel.packed && rCount > 0 &&
                       vessel.isActiveVessel && BDArmorySetup.GAME_UI_ENABLED && !MapView.MapIsEnabled);

            if (!vessel.loaded && radarCount == 0)
            {
                Destroy(this);
            }
        }

        public bool autoCycleLockOnFire = true;

        public void CycleActiveLock()
        {
            if (!locked) return;
            activeLockedTargetIndex++;
            if (activeLockedTargetIndex >= lockedTargetIndexes.Count)
            {
                activeLockedTargetIndex = 0;
            }

            lockedTargetData.detectedByRadar.SetActiveLock(lockedTargetData.targetData);

            UpdateLockedTargets();
        }

        private void IncreaseRange()
        {
            int origIndex = rangeIndex;
            rangeIndex = Mathf.Clamp(rangeIndex + 1, 0, rIncrements.Length - 1);
            if (origIndex == rangeIndex) return;
            pingPositionsDirty = true;
            UpdateRWRRange();
        }

        private void DecreaseRange()
        {
            int origIndex = rangeIndex;
            rangeIndex = Mathf.Clamp(rangeIndex - 1, 0, rIncrements.Length - 1);
            if (origIndex == rangeIndex) return;
            pingPositionsDirty = true;
            UpdateRWRRange();
        }

        /// <summary>
        /// Update the radar range also on the rwr display
        /// </summary>
        private void UpdateRWRRange()
        {
            List<RadarWarningReceiver>.Enumerator rwr = vessel.FindPartModulesImplementing<RadarWarningReceiver>().GetEnumerator();
            while (rwr.MoveNext())
            {
                if (rwr.Current == null) continue;
                rwr.Current.rwrDisplayRange = rIncrements[rangeIndex];
            }
            rwr.Dispose();
        }

        private bool TryLockTarget(RadarDisplayData radarTarget)
        {
            if (radarTarget.locked) return false;

            ModuleRadar lockingRadar = null;
            //first try using the last radar to detect that target
            if (CheckRadarForLock(radarTarget.detectedByRadar, radarTarget))
            {
                lockingRadar = radarTarget.detectedByRadar;
            }
            else
            {
                List<ModuleRadar>.Enumerator radar = availableRadars.GetEnumerator();
                while (radar.MoveNext())
                {
                    if (radar.Current == null) continue;
                    if (!CheckRadarForLock(radar.Current, radarTarget)) continue;
                    lockingRadar = radar.Current;
                    break;
                }
                radar.Dispose();
            }

            if (lockingRadar != null)
            {
                return lockingRadar.TryLockTarget(radarTarget.targetData.predictedPosition);
            }

            UpdateLockedTargets();
            StartCoroutine(UpdateLocksAfterFrame());
            return false;
        }

        private IEnumerator UpdateLocksAfterFrame()
        {
            yield return null;
            UpdateLockedTargets();
        }

        public void TryLockTarget(Vector3 worldPosition)
        {
            List<RadarDisplayData>.Enumerator displayData = displayedTargets.GetEnumerator();
            while (displayData.MoveNext())
            {
                if (!(Vector3.SqrMagnitude(worldPosition - displayData.Current.targetData.predictedPosition) <
                      40 * 40)) continue;
                TryLockTarget(displayData.Current);
                return;
            }
            displayData.Dispose();
            return;
        }

        public bool TryLockTarget(Vessel v)
        {
            if (!v) return false;

            List<RadarDisplayData>.Enumerator displayData = displayedTargets.GetEnumerator();
            while (displayData.MoveNext())
            {
                if (v == displayData.Current.vessel)
                {
                    return TryLockTarget(displayData.Current);
                }
            }
            displayData.Dispose();

            RadarDisplayData newData = new RadarDisplayData
            {
                vessel = v,
                detectedByRadar = null,
                targetData = new TargetSignatureData(v, 999)
            };

            return TryLockTarget(newData);

            //return false;
        }

        private bool CheckRadarForLock(ModuleRadar radar, RadarDisplayData radarTarget)
        {
            if (!radar) return false;

            return
            (
                radar.canLock
                && (!radar.locked || radar.currentLocks < radar.maxLocks)
                && radarTarget.targetData.signalStrength > radar.radarLockTrackCurve.Evaluate((radarTarget.targetData.predictedPosition - radar.transform.position).magnitude / 1000f)
                &&
                (radar.omnidirectional ||
                 Vector3.Angle(radar.transform.up, radarTarget.targetData.predictedPosition - radar.transform.position) <
                 radar.directionalFieldOfView / 2)
            );
        }

        private void DisableAllRadars()
        {
            //rCount = 0;
            UnlinkAllExternalRadars();

            List<ModuleRadar>.Enumerator radar = vessel.FindPartModulesImplementing<ModuleRadar>().GetEnumerator();
            while (radar.MoveNext())
            {
                if (radar.Current == null) continue;
                radar.Current.DisableRadar();
            }
            radar.Dispose();
        }

        public void SlaveTurrets()
        {
            List<ModuleTargetingCamera>.Enumerator mtc = vessel.FindPartModulesImplementing<ModuleTargetingCamera>().GetEnumerator();
            while (mtc.MoveNext())
            {
                if (mtc.Current == null) continue;
                mtc.Current.slaveTurrets = false;
            }
            mtc.Dispose();
            slaveTurrets = true;
        }

        public void UnslaveTurrets()
        {
            List<ModuleTargetingCamera>.Enumerator mtc = vessel.FindPartModulesImplementing<ModuleTargetingCamera>().GetEnumerator();
            while (mtc.MoveNext())
            {
                if (mtc.Current == null) continue;
                mtc.Current.slaveTurrets = false;
            }
            mtc.Dispose();

            slaveTurrets = false;

            if (weaponManager)
            {
                weaponManager.slavingTurrets = false;
            }
        }

        private void OnGUI()
        {
            if (!drawGUI) return;

            for (int i = 0; i < lockedTargetIndexes.Count; i++)
            {
                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                {
                    string label = string.Empty;
                    if (i == activeLockedTargetIndex)
                    {
                        label += "Active: ";
                    }
                    if (!displayedTargets[lockedTargetIndexes[i]].vessel)
                    {
                        label += "data with no vessel";
                    }
                    else
                    {
                        label += displayedTargets[lockedTargetIndexes[i]].vessel.vesselName;
                    }
                    GUI.Label(new Rect(20, 60 + (i * 26), 800, 446), label);
                }

                TargetSignatureData lockedTarget = displayedTargets[lockedTargetIndexes[i]].targetData;
                if (i == activeLockedTargetIndex)
                {
                    if (weaponManager && weaponManager.Team.IsFriendly(lockedTarget.Team))
                    {
                        BDGUIUtils.DrawTextureOnWorldPos(lockedTarget.predictedPosition,
                            BDArmorySetup.Instance.crossedGreenSquare, new Vector2(20, 20), 0);
                    }
                    else
                    {
                        BDGUIUtils.DrawTextureOnWorldPos(lockedTarget.predictedPosition,
                            BDArmorySetup.Instance.openGreenSquare, new Vector2(20, 20), 0);
                    }
                }
                else
                {
                    BDGUIUtils.DrawTextureOnWorldPos(lockedTarget.predictedPosition,
                        BDArmorySetup.Instance.greenDiamondTexture, new Vector2(17, 17), 0);
                }
            }

            if (Event.current.type == EventType.MouseUp && resizingWindow)
            {
                resizingWindow = false;
            }
            const string windowTitle = "Radar";
            BDArmorySetup.WindowRectRadar = GUI.Window(524141, BDArmorySetup.WindowRectRadar, WindowRadar, windowTitle, GUI.skin.window);
            BDGUIUtils.UseMouseEventInRect(BDArmorySetup.WindowRectRadar);

            if (linkWindowOpen && canReceiveRadarData)
            {
                linkWindowRect = new Rect(BDArmorySetup.WindowRectRadar.x - linkRectWidth, BDArmorySetup.WindowRectRadar.y + 16, linkRectWidth,
                    16 + (numberOfAvailableLinks * linkRectEntryHeight));
                LinkRadarWindow();

                BDGUIUtils.UseMouseEventInRect(linkWindowRect);
            }
        }

        //GUI
        //=============================================
        private void WindowRadar(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, BDArmorySetup.WindowRectRadar.width - 18, 30));
            if (GUI.Button(new Rect(BDArmorySetup.WindowRectRadar.width - 18, 2, 16, 16), "X", GUI.skin.button))
            {
                DisableAllRadars();
                return;
            }
            if (!referenceTransform) return;

            //==============================
            GUI.BeginGroup(RadarDisplayRect);

            if (availableRadars.Count == 0) return;
            //bool omnidirectionalDisplay = (radarCount == 1 && linkedRadars[0].omnidirectional);
            float directionalFieldOfView = omniDisplay ? 0 : availableRadars[0].directionalFieldOfView;
            //bool linked = (radarCount > 1);
            Rect scanRect = new Rect(0, 0, RadarDisplayRect.width, RadarDisplayRect.height);
            if (omniDisplay)
            {
                GUI.DrawTexture(scanRect, omniBgTexture, ScaleMode.StretchToFill, true);

                if (vessel.LandedOrSplashed)
                {
                    GUI.Label(RadarDisplayRect, "  N", radarTopStyle);
                }

                // Range Display and control
                DisplayRange();

                //my ship direction icon
                float directionSize = 16;
                Vector3 projectedVesselFwd = Vector3.ProjectOnPlane(vessel.ReferenceTransform.up, referenceTransform.up);
                float dAngle = Vector3.Angle(projectedVesselFwd, referenceTransform.forward);
                if (referenceTransform.InverseTransformVector(vessel.ReferenceTransform.up).x < 0)
                {
                    dAngle = -dAngle;
                }
                GUIUtility.RotateAroundPivot(dAngle, scanRect.center);
                GUI.DrawTexture(
                    new Rect(scanRect.center.x - (directionSize / 2), scanRect.center.y - (directionSize / 2),
                        directionSize, directionSize), BDArmorySetup.Instance.directionTriangleIcon,
                    ScaleMode.StretchToFill, true);
                GUI.matrix = Matrix4x4.identity;

                for (int i = 0; i < rCount; i++)
                {
                    bool canScan = availableRadars[i].canScan;
                    bool canTrackWhileScan = availableRadars[i].canTrackWhileScan;
                    bool islocked = availableRadars[i].locked;
                    float currentAngle = availableRadars[i].currentAngle;

                    float radarAngle = VectorUtils.SignedAngle(projectedVesselFwd,
                        Vector3.ProjectOnPlane(availableRadars[i].transform.up, referenceTransform.up),
                        referenceTransform.right);

                    if (!canScan || availableRadars[i].vessel != vessel) continue;
                    if ((!islocked || canTrackWhileScan))
                    {
                        if (!availableRadars[i].omnidirectional)
                        {
                            currentAngle += radarAngle + dAngle;
                        }
                        else if (!vessel.Landed)
                        {
                            Vector3 north = VectorUtils.GetNorthVector(referenceTransform.position, vessel.mainBody);
                            float angleFromNorth = VectorUtils.SignedAngle(north, projectedVesselFwd,
                                Vector3.Cross(north, vessel.upAxis));
                            currentAngle += angleFromNorth;
                        }

                        GUIUtility.RotateAroundPivot(currentAngle, new Vector2((RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE) / 2, (RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE) / 2));
                        if (availableRadars[i].omnidirectional && radarCount == 1)
                        {
                            GUI.DrawTexture(scanRect, scanTexture, ScaleMode.StretchToFill, true);
                        }
                        else
                        {
                            BDGUIUtils.DrawRectangle(
                                new Rect(scanRect.x + (scanRect.width / 2) - 1, scanRect.y, 2, scanRect.height / 2),
                                new Color(0, 1, 0, 0.35f));
                        }
                        GUI.matrix = Matrix4x4.identity;
                    }

                    //if linked and directional, draw FOV lines
                    if (availableRadars[i].omnidirectional) continue;
                    float fovAngle = availableRadars[i].directionalFieldOfView / 2;
                    float lineWidth = 2;
                    Rect verticalLineRect = new Rect(scanRect.center.x - (lineWidth / 2), 0, lineWidth,
                      scanRect.center.y);
                    GUIUtility.RotateAroundPivot(dAngle + fovAngle + radarAngle, scanRect.center);
                    BDGUIUtils.DrawRectangle(verticalLineRect, new Color(0, 1, 0, 0.6f));
                    GUI.matrix = Matrix4x4.identity;
                    GUIUtility.RotateAroundPivot(dAngle - fovAngle + radarAngle, scanRect.center);
                    BDGUIUtils.DrawRectangle(verticalLineRect, new Color(0, 1, 0, 0.4f));
                    GUI.matrix = Matrix4x4.identity;
                }
            }
            else
            {
                GUI.DrawTexture(scanRect, radialBgTexture, ScaleMode.StretchToFill, true);

                DisplayRange();

                for (int i = 0; i < rCount; i++)
                {
                    bool canScan = availableRadars[i].canScan;
                    bool islocked = availableRadars[i].locked;
                    //float lockScanAngle = linkedRadars[i].lockScanAngle;
                    float currentAngle = availableRadars[i].currentAngle;
                    if (!canScan) continue;
                    float indicatorAngle = currentAngle; //locked ? lockScanAngle : currentAngle;
                    Vector2 scanIndicatorPos =
                        RadarUtils.WorldToRadarRadial(
                            referenceTransform.position +
                            (Quaternion.AngleAxis(indicatorAngle, referenceTransform.up) * referenceTransform.forward),
                            referenceTransform, scanRect, 5000, directionalFieldOfView / 2);
                    GUI.DrawTexture(new Rect(scanIndicatorPos.x - 7, scanIndicatorPos.y - 10, 14, 20),
                        BDArmorySetup.Instance.greenDiamondTexture, ScaleMode.StretchToFill, true);

                    if (!islocked || !availableRadars[i].canTrackWhileScan) continue;
                    Vector2 leftPos =
                        RadarUtils.WorldToRadarRadial(
                            referenceTransform.position +
                            (Quaternion.AngleAxis(availableRadars[i].leftLimit, referenceTransform.up) *
                             referenceTransform.forward), referenceTransform, scanRect, 5000,
                            directionalFieldOfView / 2);
                    Vector2 rightPos =
                        RadarUtils.WorldToRadarRadial(
                            referenceTransform.position +
                            (Quaternion.AngleAxis(availableRadars[i].rightLimit, referenceTransform.up) *
                             referenceTransform.forward), referenceTransform, scanRect, 5000,
                            directionalFieldOfView / 2);
                    float barWidth = 2;
                    float barHeight = 15;
                    Color origColor = GUI.color;
                    GUI.color = Color.green;
                    GUI.DrawTexture(new Rect(leftPos.x - barWidth, leftPos.y - barHeight, barWidth, barHeight),
                        Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                    GUI.DrawTexture(new Rect(rightPos.x, rightPos.y - barHeight, barWidth, barHeight),
                        Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                    GUI.color = origColor;
                }
            }

            //selector
            if (showSelector)
            {
                float selectorSize = 18;
                Rect selectorRect = new Rect(selectorPos.x - (selectorSize / 2), selectorPos.y - (selectorSize / 2),
                    selectorSize, selectorSize);
                Rect sLeftRect = new Rect(selectorRect.x, selectorRect.y, selectorSize / 6, selectorRect.height);
                Rect sRightRect = new Rect(selectorRect.x + selectorRect.width - (selectorSize / 6), selectorRect.y,
                    selectorSize / 6, selectorRect.height);
                BDGUIUtils.DrawRectangle(sLeftRect, Color.green);
                BDGUIUtils.DrawRectangle(sRightRect, Color.green);
            }

            //missile data
            if (LastMissile && LastMissile.TargetAcquired)
            {
                Rect missileDataRect = new Rect(5, scanRect.height - 65, scanRect.width - 5, 60);
                string missileDataString = LastMissile.GetShortName();
                missileDataString += "\nT-" + LastMissile.TimeToImpact.ToString("0");

                if (LastMissile.ActiveRadar && Mathf.Round(Time.time * 3) % 2 == 0)
                {
                    missileDataString += "\nACTIVE";
                }
                GUI.Label(missileDataRect, missileDataString, distanceStyle);
            }

            //roll indicator
            if (!vessel.Landed)
            {
                Vector3 localUp = vessel.ReferenceTransform.InverseTransformDirection(referenceTransform.up);
                localUp = Vector3.ProjectOnPlane(localUp, Vector3.up).normalized;
                float rollAngle = -Misc.Misc.SignedAngle(-Vector3.forward, localUp, Vector3.right);
                GUIUtility.RotateAroundPivot(rollAngle, scanRect.center);
                GUI.DrawTexture(scanRect, rollIndicatorTexture, ScaleMode.StretchToFill, true);
                GUI.matrix = Matrix4x4.identity;
            }

            if (noData)
            {
                GUI.Label(RadarDisplayRect, "NO DATA\n", lockStyle);
            }
            else
            {
                DrawDisplayedContacts();
            }

            GUI.EndGroup();

            // Show Control Button group
            DisplayRadarControls();

            // Resizing code block.
            RADARresizeRect =
                new Rect(BDArmorySetup.WindowRectRadar.width - 18, BDArmorySetup.WindowRectRadar.height - 19, 16, 16);
            GUI.DrawTexture(RADARresizeRect, Misc.Misc.resizeTexture, ScaleMode.StretchToFill, true);
            if (Event.current.type == EventType.MouseDown && RADARresizeRect.Contains(Event.current.mousePosition))
            {
                resizingWindow = true;
            }

            if (Event.current.type == EventType.Repaint && resizingWindow)
            {
                if (Mouse.delta.x != 0 || Mouse.delta.y != 0)
                {
                    float diff = Mouse.delta.x + Mouse.delta.y;
                    UpdateRadarScale(diff);
                    BDArmorySetup.ResizeRadarWindow(BDArmorySettings.RADAR_WINDOW_SCALE);
                }
            }
            // End Resizing code.

            BDGUIUtils.RepositionWindow(ref BDArmorySetup.WindowRectRadar);
        }

        internal static void UpdateRadarScale(float diff)
        {
            float scaleDiff = ((diff / (BDArmorySetup.WindowRectRadar.width + BDArmorySetup.WindowRectRadar.height)) * 100 * .01f);
            BDArmorySettings.RADAR_WINDOW_SCALE += Mathf.Abs(scaleDiff) > .01f ? scaleDiff : scaleDiff > 0 ? .01f : -.01f;
            BDArmorySettings.RADAR_WINDOW_SCALE = Mathf.Clamp(BDArmorySettings.RADAR_WINDOW_SCALE,
                BDArmorySettings.RADAR_WINDOW_SCALE_MIN,
                BDArmorySettings.RADAR_WINDOW_SCALE_MAX);
        }

        private void DisplayRange()
        {
            // Range Display and control
            if (rangeIndex >= rIncrements.Length) DecreaseRange();

            if (GUI.Button(new Rect(5, 5, 16, 16), "-", GUI.skin.button))
            {
                DecreaseRange();
            }

            GUI.Label(new Rect(25, 5, 60, 24), (rIncrements[rangeIndex] / 1000).ToString("0") + "km", distanceStyle);
            if (GUI.Button(new Rect(70, 5, 16, 16), "+", GUI.skin.button))
            {
                IncreaseRange();
            }
        }

        private void DisplayRadarControls()
        {
            float buttonHeight = 25;
            float line = HeaderSize + BorderSize / 2;
            float startX = BorderSize + RadarDisplayRect.width;

            //Set up button positions depending on window scale.
            Rect dataLinkRect = new Rect(startX, line, ControlsWidth, buttonHeight);
            line += buttonHeight + Gap;
            Rect lockModeCycleRect = new Rect(startX, line, ControlsWidth, buttonHeight);
            line += buttonHeight + Gap;
            Rect slaveRect = new Rect(startX, line, ControlsWidth, buttonHeight);
            line += buttonHeight + Gap;
            Rect unlockRect = new Rect(startX, line, ControlsWidth, buttonHeight);
            line += buttonHeight + Gap;
            Rect unlockAllRect = new Rect(startX, line, ControlsWidth, buttonHeight);

            if (canReceiveRadarData)
            {
                if (GUI.Button(dataLinkRect, "Data Link", linkWindowOpen ? GUI.skin.box : GUI.skin.button))
                {
                    if (linkWindowOpen)
                    {
                        CloseLinkRadarWindow();
                    }
                    else
                    {
                        OpenLinkRadarWindow();
                    }
                }
            }
            else
            {
                Color oCol = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.35f);
                GUI.Box(dataLinkRect, "Link N/A", GUI.skin.button);
                GUI.color = oCol;
            }

            if (locked)
            {
                if (GUI.Button(lockModeCycleRect, "Cycle Lock", GUI.skin.button))
                {
                    CycleActiveLock();
                }
            }
            else if (!omniDisplay) //SCAN MODE SELECTOR
            {
                if (!locked)
                {
                    string boresightToggle = availableRadars[0].boresightScan ? "Scan" : "Boresight";
                    if (GUI.Button(lockModeCycleRect, boresightToggle, GUI.skin.button))
                    {
                        availableRadars[0].boresightScan = !availableRadars[0].boresightScan;
                    }
                }
            }

            //slave button
            if (GUI.Button(slaveRect, slaveTurrets ? "Unslave Turrets" : "Slave Turrets",
                slaveTurrets ? GUI.skin.box : GUI.skin.button))
            {
                if (slaveTurrets)
                {
                    UnslaveTurrets();
                }
                else
                {
                    SlaveTurrets();
                }
            }

            //unlocking
            if (locked)
            {
                if (GUI.Button(unlockRect, "Unlock", GUI.skin.button))
                {
                    UnlockCurrentTarget();
                }

                if (GUI.Button(unlockAllRect, "Unlock All", GUI.skin.button))
                {
                    UnlockAllTargets();
                }
            }
        }

        private void LinkRadarWindow()
        {
            GUI.Box(linkWindowRect, string.Empty, GUI.skin.window);

            numberOfAvailableLinks = 0;

            GUI.BeginGroup(linkWindowRect);

            if (GUI.Button(new Rect(8, 8, 100, linkRectEntryHeight), "Refresh", GUI.skin.button))
            {
                RefreshAvailableLinks();
            }
            numberOfAvailableLinks += 1.25f;

            List<VesselRadarData>.Enumerator v = availableExternalVRDs.GetEnumerator();
            while (v.MoveNext())
            {
                if (v.Current == null) continue;
                if (!v.Current.vessel || !v.Current.vessel.loaded) continue;
                bool linked = externalVRDs.Contains(v.Current);
                GUIStyle style = linked ? BDArmorySetup.BDGuiSkin.box : GUI.skin.button;
                if (
                    GUI.Button(
                        new Rect(8, 8 + (linkRectEntryHeight * numberOfAvailableLinks), linkRectWidth - 16,
                            linkRectEntryHeight), v.Current.vessel.vesselName, style))
                {
                    if (linked)
                    {
                        //UnlinkRadar(v);
                        UnlinkVRD(v.Current);
                    }
                    else
                    {
                        //LinkToRadar(v);
                        LinkVRD(v.Current);
                    }
                }
                numberOfAvailableLinks++;
            }
            v.Dispose();

            GUI.EndGroup();
        }

        public void RemoveDataFromRadar(ModuleRadar radar)
        {
            displayedTargets.RemoveAll(t => t.detectedByRadar == radar);
            UpdateLockedTargets();
        }

        private void UnlinkVRD(VesselRadarData vrd)
        {
            Debug.Log("[BDArmory]: Unlinking VRD: " + vrd.vessel.vesselName);
            externalVRDs.Remove(vrd);

            List<ModuleRadar> radarsToUnlink = new List<ModuleRadar>();

            List<ModuleRadar>.Enumerator mra = availableRadars.GetEnumerator();
            while (mra.MoveNext())
            {
                if (mra.Current == null) continue;
                if (mra.Current.vesselRadarData == vrd)
                {
                    radarsToUnlink.Add(mra.Current);
                }
            }
            mra.Dispose();

            List<ModuleRadar>.Enumerator mr = radarsToUnlink.GetEnumerator();
            while (mr.MoveNext())
            {
                if (mr.Current == null) continue;
                Debug.Log("[BDArmory]:  - Unlinking radar: " + mr.Current.radarName);
                UnlinkRadar(mr.Current);
            }
            mr.Dispose();

            SaveExternalVRDVessels();
        }

        private void UnlinkRadar(ModuleRadar mr)
        {
            if (mr && mr.vessel)
            {
                RemoveRadar(mr);
                externalRadars.Remove(mr);
                mr.RemoveExternalVRD(this);

                bool noMoreExternalRadar = true;
                List<ModuleRadar>.Enumerator rad = externalRadars.GetEnumerator();
                while (rad.MoveNext())
                {
                    if (rad.Current == null) continue;
                    if (rad.Current.vessel != mr.vessel) continue;
                    noMoreExternalRadar = false;
                    break;
                }
                rad.Dispose();

                if (noMoreExternalRadar)
                {
                    externalVRDs.Remove(mr.vesselRadarData);
                }
            }
            else
            {
                externalRadars.RemoveAll(r => r == null);
            }
        }

        private void RemoveEmptyVRDs()
        {
            externalVRDs.RemoveAll(vrd => vrd == null);
            List<VesselRadarData> vrdsToRemove = new List<VesselRadarData>();
            List<VesselRadarData>.Enumerator vrda = externalVRDs.GetEnumerator();
            while (vrda.MoveNext())
            {
                if (vrda.Current == null) continue;
                if (vrda.Current.rCount == 0)
                {
                    vrdsToRemove.Add(vrda.Current);
                }
            }
            vrda.Dispose();

            List<VesselRadarData>.Enumerator vrdr = vrdsToRemove.GetEnumerator();
            while (vrdr.MoveNext())
            {
                if (vrdr.Current == null) continue;
                externalVRDs.Remove(vrdr.Current);
            }
            vrdr.Dispose();
        }

        public void UnlinkDisabledRadar(ModuleRadar mr)
        {
            RemoveRadar(mr);
            externalRadars.Remove(mr);
            SaveExternalVRDVessels();
        }

        public void BeginWaitForUnloadedLinkedRadar(ModuleRadar mr, string vesselID)
        {
            UnlinkDisabledRadar(mr);

            if (waitingForVessels.Contains(vesselID))
            {
                return;
            }

            waitingForVessels.Add(vesselID);
            SaveExternalVRDVessels();
            StartCoroutine(RecoverUnloadedLinkedVesselRoutine(vesselID));
        }

        private List<string> waitingForVessels;

        private IEnumerator RecoverUnloadedLinkedVesselRoutine(string vesselID)
        {
            while (true)
            {
                List<Vessel>.Enumerator v = BDATargetManager.LoadedVessels.GetEnumerator();
                while (v.MoveNext())
                {
                    if (v.Current == null || !v.Current.loaded || v.Current == vessel) continue;
                    if (v.Current.id.ToString() != vesselID) continue;
                    VesselRadarData vrd = v.Current.gameObject.GetComponent<VesselRadarData>();
                    if (!vrd) continue;
                    waitingForVessels.Remove(vesselID);
                    StartCoroutine(LinkVRDWhenReady(vrd));
                    yield break;
                }
                v.Dispose();

                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator LinkVRDWhenReady(VesselRadarData vrd)
        {
            while (!vrd.radarsReady || vrd.vessel.packed || vrd.radarCount < 1)
            {
                yield return null;
            }
            LinkVRD(vrd);
            Debug.Log("[BDArmory]: Radar data link recovered: Local - " + vessel.vesselName + ", External - " +
                      vrd.vessel.vesselName);
        }

        public void UnlinkAllExternalRadars()
        {
            externalRadars.RemoveAll(r => r == null);
            List<ModuleRadar>.Enumerator eRad = externalRadars.GetEnumerator();
            while (eRad.MoveNext())
            {
                if (eRad.Current == null) continue;
                eRad.Current.RemoveExternalVRD(this);
            }
            eRad.Dispose();
            externalRadars.Clear();

            externalVRDs.Clear();

            availableRadars.RemoveAll(r => r == null);
            availableRadars.RemoveAll(r => r.vessel != vessel);
            rCount = availableRadars.Count;

            RefreshAvailableLinks();
        }

        private void OpenLinkRadarWindow()
        {
            RefreshAvailableLinks();
            linkWindowOpen = true;
        }

        private void CloseLinkRadarWindow()
        {
            linkWindowOpen = false;
        }

        private void RefreshAvailableLinks()
        {
            if (!HighLogic.LoadedSceneIsFlight || !weaponManager || (FlightGlobals.Vessels == null) || (!FlightGlobals.ready))
            {
                return;
            }

            availableExternalVRDs = new List<VesselRadarData>();
            List<Vessel>.Enumerator v = FlightGlobals.Vessels.GetEnumerator();
            while (v.MoveNext())
            {
                if (v.Current == null || !v.Current.loaded || vessel == null || v.Current == vessel) continue;

                BDTeam team = null;
                List<MissileFire>.Enumerator mf = v.Current.FindPartModulesImplementing<MissileFire>().GetEnumerator();
                while (mf.MoveNext())
                {
                    if (mf.Current == null) continue;
                    team = mf.Current.Team;
                    break;
                }
                mf.Dispose();

                if (team != weaponManager.Team) continue;
                VesselRadarData vrd = v.Current.gameObject.GetComponent<VesselRadarData>();
                if (vrd && vrd.radarCount > 0)
                {
                    availableExternalVRDs.Add(vrd);
                }
            }
            v.Dispose();
        }

        public void LinkVRD(VesselRadarData vrd)
        {
            if (!externalVRDs.Contains(vrd))
            {
                externalVRDs.Add(vrd);
            }

            List<ModuleRadar>.Enumerator mr = vrd.availableRadars.GetEnumerator();
            while (mr.MoveNext())
            {
                if (mr.Current == null) continue;
                LinkToRadar(mr.Current);
            }
            mr.Dispose();
            SaveExternalVRDVessels();
        }

        public void LinkToRadar(ModuleRadar mr)
        {
            if (!mr)
            {
                return;
            }

            if (externalRadars.Contains(mr))
            {
                return;
            }

            externalRadars.Add(mr);
            AddRadar(mr);

            mr.AddExternalVRD(this);
        }

        public void AddRadarContact(ModuleRadar radar, TargetSignatureData contactData, bool _locked)
        {
            bool addContact = true;

            RadarDisplayData rData = new RadarDisplayData();
            rData.vessel = contactData.vessel;

            if (rData.vessel == vessel) return;

            if (rData.vessel.altitude < -20 && radar.rwrThreatType != (int)RadarWarningReceiver.RWRThreatTypes.Sonar) addContact = false; // Normal Radar Should not detect Underwater vessels
            if (!rData.vessel.LandedOrSplashed && radar.rwrThreatType == (int)RadarWarningReceiver.RWRThreatTypes.Sonar) addContact = false; //Sonar should not detect Aircraft
            if (rData.vessel.altitude < 0 && radar.rwrThreatType == (int)RadarWarningReceiver.RWRThreatTypes.Sonar && vessel.Splashed) addContact = true; //Sonar only detects underwater vessels // Sonar should only work when in the water
            if (!vessel.Splashed && radar.rwrThreatType == (int)RadarWarningReceiver.RWRThreatTypes.Sonar) addContact = false; // Sonar should only work when in the water
            if (rData.vessel.Landed && radar.rwrThreatType == (int)RadarWarningReceiver.RWRThreatTypes.Sonar) addContact = false; //Sonar should not detect land vessels

            if (addContact == false) return;

            rData.signalPersistTime = radar.signalPersistTime;
            rData.detectedByRadar = radar;
            rData.locked = _locked;
            rData.targetData = contactData;
            rData.pingPosition = UpdatedPingPosition(contactData.position, radar);

            if (_locked)
            {
                radar.UpdateLockedTargetInfo(contactData);
            }

            bool dontOverwrite = false;

            int replaceIndex = -1;
            for (int i = 0; i < displayedTargets.Count; i++)
            {
                if (displayedTargets[i].vessel == rData.vessel)
                {
                    if (displayedTargets[i].locked && !_locked)
                    {
                        dontOverwrite = true;
                        break;
                    }

                    replaceIndex = i;
                    break;
                }
            }

            if (replaceIndex >= 0)
            {
                displayedTargets[replaceIndex] = rData;
                //UpdateLockedTargets();
                return;
            }
            else if (dontOverwrite)
            {
                //UpdateLockedTargets();
                return;
            }
            else
            {
                displayedTargets.Add(rData);
                UpdateLockedTargets();
                return;
            }
        }

        public void TargetNext()
        {
            // activeLockedTargetIndex is the index to the list of locked targets.
            // It contains the index of the displayedTargets.  We are really concerned with the displayed targets here,
            // but we need to keep the locked targets index list current.
            int displayedTargetIndex;
            if (!locked)
            {
                // No locked targets, get the first target in the list of displayed targets.
                if (displayedTargets.Count == 0) return;
                displayedTargetIndex = 0;
                TryLockTarget(displayedTargets[displayedTargetIndex]);
                lockedTargetIndexes.Add(displayedTargetIndex);
                UpdateLockedTargets();
                return;
            }
            // We have locked target(s)  Lets see if we can select the next one in the list (if it exists)
            displayedTargetIndex = lockedTargetIndexes[activeLockedTargetIndex];
            // Lets store the displayed target that is ative
            ModuleRadar rad = displayedTargets[displayedTargetIndex].detectedByRadar;
            if (lockedTargetIndexes.Count > 1)
            {
                // We have more than one locked target.  Switch to the next locked target.
                if (activeLockedTargetIndex < lockedTargetIndexes.Count - 1)
                    activeLockedTargetIndex++;
                else
                {
                    activeLockedTargetIndex = 0;
                }
                UpdateLockedTargets();
            }
            else
            {
                // If we have only one target we are done.
                if (displayedTargets.Count <= 1) return;
                // We have more targets to work with so attempt lock on the next available displayed target.
                if (displayedTargetIndex < displayedTargets.Count - 1)
                {
                    displayedTargetIndex++;
                }
                else
                {
                    displayedTargetIndex = 0;
                }

                TryLockTarget(displayedTargets[displayedTargetIndex]);
                if (!displayedTargets[displayedTargetIndex].detectedByRadar) return;
                // We have a good lock.  Lets update the indexes and locks
                lockedTargetIndexes.Add(displayedTargetIndex);
                rad.UnlockTargetAt(rad.currentLockIndex);
                UpdateLockedTargets();
            }
        }

        public void TargetPrev()
        {
            // activeLockedTargetIndex is the index to the list of locked targets.
            // It contains the index of the displayedTargets.  We are really concerned with the displayed targets here,
            // but we need to keep the locked targets index list current.
            int displayedTargetIndex;
            if (!locked)
            {
                // No locked targets, get the last target in the list of displayed targets.
                if (displayedTargets.Count == 0) return;
                displayedTargetIndex = displayedTargets.Count - 1;
                TryLockTarget(displayedTargets[displayedTargetIndex]);
                lockedTargetIndexes.Add(displayedTargetIndex);
                UpdateLockedTargets();
                return;
            }
            // We have locked target(s)  Lets see if we can select the previous one in the list (if it exists)
            displayedTargetIndex = lockedTargetIndexes[activeLockedTargetIndex];
            // Lets store the displayed target that is ative
            ModuleRadar rad = displayedTargets[displayedTargetIndex].detectedByRadar;
            if (lockedTargetIndexes.Count > 1)
            {
                // We have more than one locked target.  switch to the previous locked target.
                if (activeLockedTargetIndex > 0)
                    activeLockedTargetIndex--;
                else
                {
                    activeLockedTargetIndex = lockedTargetIndexes.Count - 1;
                }
                UpdateLockedTargets();
            }
            else
            {
                // If we have only one target we are done.
                if (displayedTargets.Count <= 1) return;
                // We have more targets to work with so attempt lock on the pevious available displayed target.
                if (displayedTargetIndex > 0)
                {
                    displayedTargetIndex--;
                }
                else
                {
                    displayedTargetIndex = displayedTargets.Count - 1;
                }

                TryLockTarget(displayedTargets[displayedTargetIndex]);
                if (!displayedTargets[displayedTargetIndex].detectedByRadar) return;
                // We got a good lock.  Lets update the indexes and locks
                lockedTargetIndexes.Add(displayedTargetIndex);
                rad.UnlockTargetAt(rad.currentLockIndex);
                UpdateLockedTargets();
            }
        }

        public void UnlockAllTargetsOfRadar(ModuleRadar radar)
        {
            //radar.UnlockTarget();
            displayedTargets.RemoveAll(t => t.detectedByRadar == radar);
            UpdateLockedTargets();
        }

        public void RemoveVesselFromTargets(Vessel _vessel)
        {
            displayedTargets.RemoveAll(t => t.vessel == _vessel);
            UpdateLockedTargets();
        }

        public void UnlockAllTargets()
        {
            List<ModuleRadar>.Enumerator radar = weaponManager.radars.GetEnumerator();
            while (radar.MoveNext())
            {
                if (radar.Current == null) continue;
                radar.Current.UnlockAllTargets();
            }
            radar.Dispose();
        }

        public void UnlockCurrentTarget()
        {
            if (!locked) return;

            ModuleRadar rad = displayedTargets[lockedTargetIndexes[activeLockedTargetIndex]].detectedByRadar;
            rad.UnlockTargetAt(rad.currentLockIndex);
        }

        private void CleanDisplayedContacts()
        {
            int count = displayedTargets.Count;
            displayedTargets.RemoveAll(t => t.targetData.age > t.signalPersistTime * 2);
            if (count != displayedTargets.Count)
            {
                UpdateLockedTargets();
            }
        }

        private Vector2 UpdatedPingPosition(Vector3 worldPosition, ModuleRadar radar)
        {
            if (rangeIndex < 0 || rangeIndex > rIncrements.Length - 1) rangeIndex = rIncrements.Length - 1;
            if (omniDisplay)
            {
                return RadarUtils.WorldToRadar(worldPosition, referenceTransform, RadarDisplayRect, rIncrements[rangeIndex]);
            }
            else
            {
                return RadarUtils.WorldToRadarRadial(worldPosition, referenceTransform, RadarDisplayRect,
                    rIncrements[rangeIndex], radar.directionalFieldOfView / 2);
            }
        }

        private bool pingPositionsDirty = true;

        private void DrawDisplayedContacts()
        {
            float myAlt = (float)vessel.altitude;

            bool drewLockLabel = false;
            float lockIconSize = 24 * BDArmorySettings.RADAR_WINDOW_SCALE;

            bool lockDirty = false;

            for (int i = 0; i < displayedTargets.Count; i++)
            {
                if (displayedTargets[i].locked && locked)
                {
                    TargetSignatureData lockedTarget = displayedTargets[i].targetData;
                    //LOCKED GUI
                    Vector2 pingPosition;
                    if (omniDisplay)
                    {
                        pingPosition = RadarUtils.WorldToRadar(lockedTarget.position, referenceTransform, RadarDisplayRect,
                            rIncrements[rangeIndex]);
                    }
                    else
                    {
                        pingPosition = RadarUtils.WorldToRadarRadial(lockedTarget.position, referenceTransform,
                            RadarDisplayRect, rIncrements[rangeIndex],
                            displayedTargets[i].detectedByRadar.directionalFieldOfView / 2);
                    }

                    //BDGUIUtils.DrawRectangle(new Rect(pingPosition.x-(4),pingPosition.y-(4),8, 8), Color.green);
                    float vAngle = Vector3.Angle(Vector3.ProjectOnPlane(lockedTarget.velocity, referenceTransform.up),
                        referenceTransform.forward);
                    if (referenceTransform.InverseTransformVector(lockedTarget.velocity).x < 0)
                    {
                        vAngle = -vAngle;
                    }
                    GUIUtility.RotateAroundPivot(vAngle, pingPosition);
                    Rect pingRect = new Rect(pingPosition.x - (lockIconSize / 2), pingPosition.y - (lockIconSize / 2),
                        lockIconSize, lockIconSize);

                    Texture2D txtr = (i == lockedTargetIndexes[activeLockedTargetIndex]) ? lockIconActive : lockIcon;
                    GUI.DrawTexture(pingRect, txtr, ScaleMode.StretchToFill, true);
                    GUI.matrix = Matrix4x4.identity;
                    GUI.Label(new Rect(pingPosition.x + (lockIconSize * 0.35f) + 2, pingPosition.y, 100, 24),
                        (lockedTarget.altitude / 1000).ToString("0"), distanceStyle);

                    if (!drewLockLabel)
                    {
                        GUI.Label(RadarDisplayRect, "-LOCK-\n", lockStyle);
                        drewLockLabel = true;

                        if (slaveTurrets)
                        {
                            GUI.Label(RadarDisplayRect, "TURRETS\n\n", lockStyle);
                        }
                    }

                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                    {
                        GUI.Label(new Rect(pingPosition.x + (pingSize.x / 2), pingPosition.y, 100, 24),
                            lockedTarget.signalStrength.ToString("0.0"));
                    }

                    if (GUI.Button(pingRect, GUIContent.none, GUIStyle.none) &&
                        Time.time - guiInputTime > guiInputCooldown)
                    {
                        guiInputTime = Time.time;
                        if (i == lockedTargetIndexes[activeLockedTargetIndex])
                        {
                            //UnlockTarget(displayedTargets[i].detectedByRadar);
                            //displayedTargets[i].detectedByRadar.UnlockTargetAtPosition(displayedTargets[i].targetData.position);
                            displayedTargets[i].detectedByRadar.UnlockTargetVessel(displayedTargets[i].vessel);
                            UpdateLockedTargets();
                            lockDirty = true;
                        }
                        else
                        {
                            for (int x = 0; x < lockedTargetIndexes.Count; x++)
                            {
                                if (i == lockedTargetIndexes[x])
                                {
                                    activeLockedTargetIndex = x;
                                    break;
                                }
                            }

                            displayedTargets[i].detectedByRadar.SetActiveLock(displayedTargets[i].targetData);

                            UpdateLockedTargets();
                        }
                    }

                    //DLZ
                    if (!lockDirty)
                    {
                        int lTarInd = lockedTargetIndexes[activeLockedTargetIndex];

                        if (i == lTarInd && weaponManager && weaponManager.selectedWeapon != null)
                        {
                            if (weaponManager.selectedWeapon.GetWeaponClass() == WeaponClasses.Missile || weaponManager.selectedWeapon.GetWeaponClass() == WeaponClasses.SLW)
                            {
                                MissileBase currMissile = weaponManager.CurrentMissile;
                                if (currMissile.TargetingMode == MissileBase.TargetingModes.Radar || currMissile.TargetingMode == MissileBase.TargetingModes.Heat)
                                {
                                    MissileLaunchParams dlz = MissileLaunchParams.GetDynamicLaunchParams(currMissile, lockedTarget.velocity, lockedTarget.predictedPosition);
                                    float rangeToPixels = (1 / rIncrements[rangeIndex]) * RadarDisplayRect.height;
                                    float dlzWidth = 12;
                                    float lineWidth = 2;
                                    float dlzX = RadarDisplayRect.width - dlzWidth - lineWidth;

                                    BDGUIUtils.DrawRectangle(new Rect(dlzX, 0, dlzWidth, RadarDisplayRect.height), Color.black);

                                    Rect maxRangeVertLineRect = new Rect(RadarDisplayRect.width - lineWidth,
                                        Mathf.Clamp(RadarDisplayRect.height - (dlz.maxLaunchRange * rangeToPixels), 0,
                                            RadarDisplayRect.height), lineWidth,
                                        Mathf.Clamp(dlz.maxLaunchRange * rangeToPixels, 0, RadarDisplayRect.height));
                                    BDGUIUtils.DrawRectangle(maxRangeVertLineRect, Color.green);

                                    Rect maxRangeTickRect = new Rect(dlzX, maxRangeVertLineRect.y, dlzWidth, lineWidth);
                                    BDGUIUtils.DrawRectangle(maxRangeTickRect, Color.green);

                                    Rect minRangeTickRect = new Rect(dlzX,
                                        Mathf.Clamp(RadarDisplayRect.height - (dlz.minLaunchRange * rangeToPixels), 0,
                                            RadarDisplayRect.height), dlzWidth, lineWidth);
                                    BDGUIUtils.DrawRectangle(minRangeTickRect, Color.green);

                                    Rect rTrTickRect = new Rect(dlzX,
                                        Mathf.Clamp(RadarDisplayRect.height - (dlz.rangeTr * rangeToPixels), 0, RadarDisplayRect.height),
                                        dlzWidth, lineWidth);
                                    BDGUIUtils.DrawRectangle(rTrTickRect, Color.green);

                                    Rect noEscapeLineRect = new Rect(dlzX, rTrTickRect.y, lineWidth,
                                        minRangeTickRect.y - rTrTickRect.y);
                                    BDGUIUtils.DrawRectangle(noEscapeLineRect, Color.green);

                                    float targetDistIconSize = 16 * BDArmorySettings.RADAR_WINDOW_SCALE;
                                    float targetDistY;
                                    if (!omniDisplay)
                                    {
                                        targetDistY = pingPosition.y - (targetDistIconSize / 2);
                                    }
                                    else
                                    {
                                        targetDistY = RadarDisplayRect.height -
                                                      (Vector3.Distance(lockedTarget.predictedPosition,
                                                           referenceTransform.position) * rangeToPixels) -
                                                      (targetDistIconSize / 2);
                                    }

                                    Rect targetDistanceRect = new Rect(dlzX - (targetDistIconSize / 2), targetDistY,
                                        targetDistIconSize, targetDistIconSize);
                                    GUIUtility.RotateAroundPivot(90, targetDistanceRect.center);
                                    GUI.DrawTexture(targetDistanceRect, BDArmorySetup.Instance.directionTriangleIcon,
                                        ScaleMode.StretchToFill, true);
                                    GUI.matrix = Matrix4x4.identity;
                                }
                            }
                        }
                    }
                }
                else
                {
                    float minusAlpha =
                    (Mathf.Clamp01((Time.time - displayedTargets[i].targetData.timeAcquired) /
                                   displayedTargets[i].signalPersistTime) * 2) - 1;

                    //jamming
                    // NEW: evaluation via radarutils!
                    bool jammed = false;
                    float distanceToTarget = (this.vessel.transform.position - displayedTargets[i].targetData.position).sqrMagnitude;
                    float jamDistance = RadarUtils.GetVesselECMJammingDistance(displayedTargets[i].targetData.vessel);
                    if (displayedTargets[i].targetData.vesselJammer && jamDistance * jamDistance > distanceToTarget)
                    {
                        jammed = true;
                    }

                    if (pingPositionsDirty)
                    {
                        //displayedTargets[i].pingPosition = UpdatedPingPosition(displayedTargets[i].targetData.position, displayedTargets[i].detectedByRadar);
                        RadarDisplayData newData = new RadarDisplayData();
                        newData.detectedByRadar = displayedTargets[i].detectedByRadar;
                        newData.locked = displayedTargets[i].locked;
                        newData.pingPosition = UpdatedPingPosition(displayedTargets[i].targetData.position,
                            displayedTargets[i].detectedByRadar);
                        newData.signalPersistTime = displayedTargets[i].signalPersistTime;
                        newData.targetData = displayedTargets[i].targetData;
                        newData.vessel = displayedTargets[i].vessel;
                        displayedTargets[i] = newData;
                    }
                    Vector2 pingPosition = displayedTargets[i].pingPosition;

                    Rect pingRect;
                    //draw missiles and debris as dots
                    if ((displayedTargets[i].targetData.targetInfo &&
                         displayedTargets[i].targetData.targetInfo.isMissile) ||
                        displayedTargets[i].targetData.Team == null)
                    {
                        float mDotSize = 6;
                        pingRect = new Rect(pingPosition.x - (mDotSize / 2), pingPosition.y - (mDotSize / 2), mDotSize,
                            mDotSize);
                        Color origGUIColor = GUI.color;
                        GUI.color = Color.white - new Color(0, 0, 0, minusAlpha);
                        GUI.DrawTexture(pingRect, BDArmorySetup.Instance.greenDotTexture, ScaleMode.StretchToFill,
                            true);
                        GUI.color = origGUIColor;
                    }
                    //draw contacts with direction indicator
                    else if (!jammed && (displayedTargets[i].detectedByRadar.showDirectionWhileScan) &&
                             displayedTargets[i].targetData.velocity.sqrMagnitude > 100)
                    {
                        pingRect = new Rect(pingPosition.x - (lockIconSize / 2), pingPosition.y - (lockIconSize / 2),
                            lockIconSize, lockIconSize);
                        float vAngle =
                            Vector3.Angle(
                                Vector3.ProjectOnPlane(displayedTargets[i].targetData.velocity, referenceTransform.up),
                                referenceTransform.forward);
                        if (referenceTransform.InverseTransformVector(displayedTargets[i].targetData.velocity).x < 0)
                        {
                            vAngle = -vAngle;
                        }
                        GUIUtility.RotateAroundPivot(vAngle, pingPosition);
                        Color origGUIColor = GUI.color;
                        GUI.color = Color.white - new Color(0, 0, 0, minusAlpha);
                        if (weaponManager &&
                            weaponManager.Team.IsFriendly(displayedTargets[i].targetData.Team))
                        {
                            GUI.DrawTexture(pingRect, friendlyContactIcon, ScaleMode.StretchToFill, true);
                        }
                        else
                        {
                            GUI.DrawTexture(pingRect, radarContactIcon, ScaleMode.StretchToFill, true);
                        }

                        GUI.matrix = Matrix4x4.identity;
                        GUI.Label(new Rect(pingPosition.x + (lockIconSize * 0.35f) + 2, pingPosition.y, 100, 24),
                            (displayedTargets[i].targetData.altitude / 1000).ToString("0"), distanceStyle);
                        GUI.color = origGUIColor;
                    }
                    else //draw contacts as rectangles
                    {
                        int drawCount = jammed ? 4 : 1;
                        pingRect = new Rect(pingPosition.x - (pingSize.x / 2), pingPosition.y - (pingSize.y / 2), pingSize.x,
                            pingSize.y);
                        for (int d = 0; d < drawCount; d++)
                        {
                            Rect jammedRect = new Rect(pingRect);
                            Vector3 contactPosition = displayedTargets[i].targetData.position;
                            if (jammed)
                            {
                                //jamming
                                Vector3 jammedPosition = transform.position +
                                                         ((displayedTargets[i].targetData.position - transform.position)
                                                              .normalized *
                                                          Random.Range(100, rIncrements[rangeIndex]));
                                float bearingVariation =
                                    Mathf.Clamp(
                                        Mathf.Pow(32000, 2) /
                                        (displayedTargets[i].targetData.position - transform.position).sqrMagnitude, 0,
                                        80);
                                jammedPosition = transform.position +
                                                 (Quaternion.AngleAxis(
                                                      Random.Range(-bearingVariation, bearingVariation),
                                                      referenceTransform.up) * (jammedPosition - transform.position));
                                if (omniDisplay)
                                {
                                    pingPosition = RadarUtils.WorldToRadar(jammedPosition, referenceTransform, RadarDisplayRect,
                                        rIncrements[rangeIndex]);
                                }
                                else
                                {
                                    pingPosition = RadarUtils.WorldToRadarRadial(jammedPosition, referenceTransform,
                                        RadarDisplayRect, rIncrements[rangeIndex],
                                        displayedTargets[i].detectedByRadar.directionalFieldOfView / 2);
                                }

                                jammedRect = new Rect(pingPosition.x - (pingSize.x / 2),
                                    pingPosition.y - (pingSize.y / 2) - (pingSize.y / 3), pingSize.x, pingSize.y / 3);
                                contactPosition = jammedPosition;
                            }

                            Color iconColor = Color.green;
                            float contactAlt = displayedTargets[i].targetData.altitude;
                            if (!omniDisplay && !jammed)
                            {
                                if (contactAlt - myAlt > 1000)
                                {
                                    iconColor = new Color(0, 0.6f, 1f, 1);
                                }
                                else if (contactAlt - myAlt < -1000)
                                {
                                    iconColor = new Color(1f, 0.68f, 0, 1);
                                }
                            }

                            if (omniDisplay)
                            {
                                Vector3 localPos = referenceTransform.InverseTransformPoint(contactPosition);
                                localPos.y = 0;
                                float angleToContact = Vector3.Angle(localPos, Vector3.forward);
                                if (localPos.x < 0) angleToContact = -angleToContact;
                                GUIUtility.RotateAroundPivot(angleToContact, pingPosition);
                            }

                            if (jammed ||
                                !weaponManager.Team.IsFriendly(displayedTargets[i].targetData.Team))
                            {
                                BDGUIUtils.DrawRectangle(jammedRect, iconColor - new Color(0, 0, 0, minusAlpha));
                            }
                            else
                            {
                                float friendlySize = 12;
                                Rect friendlyRect = new Rect(pingPosition.x - (friendlySize / 2),
                                    pingPosition.y - (friendlySize / 2), friendlySize, friendlySize);
                                Color origGuiColor = GUI.color;
                                GUI.color = iconColor - new Color(0, 0, 0, minusAlpha);
                                GUI.DrawTexture(friendlyRect, BDArmorySetup.Instance.greenDotTexture,
                                    ScaleMode.StretchToFill, true);
                                GUI.color = origGuiColor;
                            }

                            GUI.matrix = Matrix4x4.identity;
                        }
                    }

                    if (GUI.Button(pingRect, GUIContent.none, GUIStyle.none) &&
                        Time.time - guiInputTime > guiInputCooldown)
                    {
                        guiInputTime = Time.time;
                        TryLockTarget(displayedTargets[i]);
                    }

                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                    {
                        GUI.Label(new Rect(pingPosition.x + (pingSize.x / 2), pingPosition.y, 100, 24),
                            displayedTargets[i].targetData.signalStrength.ToString("0.0"));
                    }
                }
            }
            pingPositionsDirty = false;
        }

        private bool omniDisplay
        {
            get { return (radarCount > 1 || (radarCount == 1 && availableRadars[0].omnidirectional)); }
        }

        private void UpdateInputs()
        {
            if (!vessel.isActiveVessel)
            {
                return;
            }

            if (BDInputUtils.GetKey(BDInputSettingsFields.RADAR_SLEW_RIGHT))
            {
                ShowSelector();
                SlewSelector(Vector2.right);
            }
            else if (BDInputUtils.GetKey(BDInputSettingsFields.RADAR_SLEW_LEFT))
            {
                ShowSelector();
                SlewSelector(-Vector2.right);
            }

            if (BDInputUtils.GetKey(BDInputSettingsFields.RADAR_SLEW_UP))
            {
                ShowSelector();
                SlewSelector(-Vector2.up);
            }
            else if (BDInputUtils.GetKey(BDInputSettingsFields.RADAR_SLEW_DOWN))
            {
                ShowSelector();
                SlewSelector(Vector2.up);
            }

            if (BDInputUtils.GetKeyDown(BDInputSettingsFields.RADAR_LOCK))
            {
                if (showSelector)
                {
                    TryLockViaSelector();
                }
                ShowSelector();
            }

            if (BDInputUtils.GetKeyDown(BDInputSettingsFields.RADAR_CYCLE_LOCK))
            {
                if (locked)
                {
                    CycleActiveLock();
                }
            }

            if (BDInputUtils.GetKeyDown(BDInputSettingsFields.RADAR_SCAN_MODE))
            {
                if (!locked && radarCount > 0 && !omniDisplay)
                {
                    availableRadars[0].boresightScan = !availableRadars[0].boresightScan;
                }
            }

            if (BDInputUtils.GetKeyDown(BDInputSettingsFields.RADAR_TURRETS))
            {
                if (slaveTurrets)
                {
                    UnslaveTurrets();
                }
                else
                {
                    SlaveTurrets();
                }
            }

            if (BDInputUtils.GetKeyDown(BDInputSettingsFields.RADAR_RANGE_UP))
            {
                IncreaseRange();
            }
            else if (BDInputUtils.GetKeyDown(BDInputSettingsFields.RADAR_RANGE_DN))
            {
                DecreaseRange();
            }

            if (BDInputUtils.GetKeyDown(BDInputSettingsFields.RADAR_TARGET_NEXT))
            {
                TargetNext();
            }
            else if (BDInputUtils.GetKeyDown(BDInputSettingsFields.RADAR_TARGET_PREV))
            {
                TargetPrev();
            }
        }

        private void TryLockViaSelector()
        {
            bool found = false;
            Vector3 closestPos = Vector3.zero;
            float closestSqrMag = float.MaxValue;
            for (int i = 0; i < displayedTargets.Count; i++)
            {
                float sqrMag = (displayedTargets[i].pingPosition - selectorPos).sqrMagnitude;
                if (sqrMag < closestSqrMag)
                {
                    if (sqrMag < Mathf.Pow(20, 2))
                    {
                        closestPos = displayedTargets[i].targetData.predictedPosition;
                        found = true;
                    }
                }
            }

            if (found)
            {
                TryLockTarget(closestPos);
            }
            else if (closestSqrMag > Mathf.Pow(40, 2))
            {
                UnlockCurrentTarget();
            }
        }

        private void SlewSelector(Vector2 direction)
        {
            float rate = 150;
            selectorPos += direction * rate * Time.deltaTime;

            if (!omniDisplay)
            {
                if (selectorPos.y > RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE * 0.975f)
                {
                    if (rangeIndex > 0)
                    {
                        DecreaseRange();
                        selectorPos.y = RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE * 0.75f;
                    }
                }
                else if (selectorPos.y < RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE * 0.025f)
                {
                    if (rangeIndex < rIncrements.Length - 1)
                    {
                        IncreaseRange();
                        selectorPos.y = RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE * 0.25f;
                    }
                }
            }

            selectorPos.y = Mathf.Clamp(selectorPos.y, 10, RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE - 10);
            selectorPos.x = Mathf.Clamp(selectorPos.x, 10, RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE - 10);
        }

        private void ShowSelector()
        {
            if (!showSelector)
            {
                showSelector = true;
                selectorPos = new Vector2(RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE / 2, RadarScreenSize * BDArmorySettings.RADAR_WINDOW_SCALE / 2);
            }
        }
    }
}
