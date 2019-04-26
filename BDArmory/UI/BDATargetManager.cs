using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BDArmory.Core;
using BDArmory.Core.Extension;
using BDArmory.CounterMeasure;
using BDArmory.Misc;
using BDArmory.Modules;
using BDArmory.Parts;
using BDArmory.Radar;
using BDArmory.Targeting;
using KSP.UI.Screens;
using UnityEngine;

namespace BDArmory.UI
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class BDATargetManager : MonoBehaviour
    {
        private static Dictionary<BDTeam, List<TargetInfo>> TargetDatabase;
        private static Dictionary<BDTeam, List<GPSTargetInfo>> GPSTargets;
        public static List<ModuleTargetingCamera> ActiveLasers;
        public static List<IBDWeapon> FiredMissiles;
        public static List<DestructibleBuilding> LoadedBuildings;
        public static List<Vessel> LoadedVessels;
        public static BDATargetManager Instance;

        private StringBuilder debugString = new StringBuilder();
        private float updateTimer = 0;

        public static bool hasAddedButton;

        void Awake()
        {
            GameEvents.onGameStateLoad.Add(LoadGPSTargets);
            GameEvents.onGameStateSave.Add(SaveGPSTargets);
            LoadedBuildings = new List<DestructibleBuilding>();
            DestructibleBuilding.OnLoaded.Add(AddBuilding);
            LoadedVessels = new List<Vessel>();
            GameEvents.onVesselLoaded.Add(AddVessel);
            GameEvents.onVesselGoOnRails.Add(RemoveVessel);
            GameEvents.onVesselGoOffRails.Add(AddVessel);
            GameEvents.onVesselCreate.Add(AddVessel);
            GameEvents.onVesselDestroy.Add(CleanVesselList);

            Instance = this;
        }

        void OnDestroy()
        {
            if (GameEvents.onGameStateLoad != null && GameEvents.onGameStateSave != null)
            {
                GameEvents.onGameStateLoad.Remove(LoadGPSTargets);
                GameEvents.onGameStateSave.Remove(SaveGPSTargets);
            }

            GPSTargets = new Dictionary<BDTeam, List<GPSTargetInfo>>();

            GameEvents.onVesselLoaded.Remove(AddVessel);
            GameEvents.onVesselGoOnRails.Remove(RemoveVessel);
            GameEvents.onVesselGoOffRails.Remove(AddVessel);
            GameEvents.onVesselCreate.Remove(AddVessel);
            GameEvents.onVesselDestroy.Remove(CleanVesselList);
        }

        void Start()
        {
            //legacy targetDatabase
            TargetDatabase = new Dictionary<BDTeam, List<TargetInfo>>();
            StartCoroutine(CleanDatabaseRoutine());

            if (GPSTargets == null)
            {
                GPSTargets = new Dictionary<BDTeam, List<GPSTargetInfo>>();
            }

            //Laser points
            ActiveLasers = new List<ModuleTargetingCamera>();

            FiredMissiles = new List<IBDWeapon>();

            //AddToolbarButton();
            StartCoroutine(ToolbarButtonRoutine());
        }

        public static List<GPSTargetInfo> GPSTargetList(BDTeam team)
        {
            if (team == null)
                throw new ArgumentNullException("team");
            if (GPSTargets.TryGetValue(team, out List<GPSTargetInfo> database))
                return database;
            var newList = new List<GPSTargetInfo>();
            GPSTargets.Add(team, newList);
            return newList;
        }

        void AddBuilding(DestructibleBuilding b)
        {
            if (!LoadedBuildings.Contains(b))
            {
                LoadedBuildings.Add(b);
            }

            LoadedBuildings.RemoveAll(x => x == null);
        }

        void AddVessel(Vessel v)
        {
            if (!LoadedVessels.Contains(v))
            {
                LoadedVessels.Add(v);
            }
            CleanVesselList(v);
        }

        void RemoveVessel(Vessel v)
        {
            if (v != null)
            {
                LoadedVessels.Remove(v);
            }
            CleanVesselList(v);
        }

        void CleanVesselList(Vessel v)
        {
            LoadedVessels.RemoveAll(ves => ves == null);
            LoadedVessels.RemoveAll(ves => ves.loaded == false);
        }

        void AddToolbarButton()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (!hasAddedButton)
                {
                    Texture buttonTexture = GameDatabase.Instance.GetTexture(BDArmorySetup.textureDir + "icon", false);
                    ApplicationLauncher.Instance.AddModApplication(ShowToolbarGUI, HideToolbarGUI, Dummy, Dummy, Dummy, Dummy, ApplicationLauncher.AppScenes.FLIGHT, buttonTexture);
                    hasAddedButton = true;
                }
            }
        }

        IEnumerator ToolbarButtonRoutine()
        {
            if (hasAddedButton) yield break;
            if (!HighLogic.LoadedSceneIsFlight) yield break;
            while (!ApplicationLauncher.Ready)
            {
                yield return null;
            }

            AddToolbarButton();
        }

        public void ShowToolbarGUI()
        {
            BDArmorySetup.windowBDAToolBarEnabled = true;
        }

        public void HideToolbarGUI()
        {
            BDArmorySetup.windowBDAToolBarEnabled = false;
        }

        void Dummy()
        { }

        void Update()
        {
            if (BDArmorySettings.DRAW_DEBUG_LABELS && FlightGlobals.ready)
            {
                updateTimer -= Time.fixedDeltaTime;
                if (updateTimer < 0)
                {
                    UpdateDebugLabels();
                    updateTimer = 0.5f;    //next update in half a sec only
                }
            }
        }

        public static void RegisterLaserPoint(ModuleTargetingCamera cam)
        {
            if (ActiveLasers.Contains(cam))
            {
                return;
            }
            else
            {
                ActiveLasers.Add(cam);
            }
        }

        ///// <summary>
        ///// Gets the laser target painter with the least angle off boresight. Set the missileBase as the reference missilePosition.
        ///// </summary>
        ///// <returns>The laser target painter.</returns>
        ///// <param name="referenceTransform">Reference missilePosition.</param>
        ///// <param name="maxBoreSight">Max bore sight.</param>
        //public static ModuleTargetingCamera GetLaserTarget(MissileLauncher ml, bool parentOnly)
        //{
        //          return GetModuleTargeting(parentOnly, ml.transform.forward, ml.transform.position, ml.maxOffBoresight, ml.vessel, ml.SourceVessel);
        //      }

        //      public static ModuleTargetingCamera GetLaserTarget(BDModularGuidance ml, bool parentOnly)
        //      {
        //          float maxOffBoresight = 45;

        //          return GetModuleTargeting(parentOnly, ml.MissileReferenceTransform.forward, ml.MissileReferenceTransform.position, maxOffBoresight,ml.vessel,ml.SourceVessel);
        //      }

        /// <summary>
        /// Gets the laser target painter with the least angle off boresight. Set the missileBase as the reference missilePosition.
        /// </summary>
        /// <returns>The laser target painter.</returns>
        public static ModuleTargetingCamera GetLaserTarget(MissileBase ml, bool parentOnly)
        {
            return GetModuleTargeting(parentOnly, ml.GetForwardTransform(), ml.MissileReferenceTransform.position, ml.maxOffBoresight, ml.vessel, ml.SourceVessel);
        }

        private static ModuleTargetingCamera GetModuleTargeting(bool parentOnly, Vector3 missilePosition, Vector3 position, float maxOffBoresight, Vessel vessel, Vessel sourceVessel)
        {
            ModuleTargetingCamera finalCam = null;
            float smallestAngle = 360;
            List<ModuleTargetingCamera>.Enumerator cam = ActiveLasers.GetEnumerator();
            while (cam.MoveNext())
            {
                if (cam.Current == null) continue;
                if (parentOnly && !(cam.Current.vessel == vessel || cam.Current.vessel == sourceVessel)) continue;
                if (!cam.Current.cameraEnabled || !cam.Current.groundStabilized || !cam.Current.surfaceDetected ||
                    cam.Current.gimbalLimitReached) continue;

                float angle = Vector3.Angle(missilePosition, cam.Current.groundTargetPosition - position);
                if (!(angle < maxOffBoresight) || !(angle < smallestAngle) ||
                    !CanSeePosition(cam.Current.groundTargetPosition, vessel.transform.position,
                        (vessel.transform.position + missilePosition))) continue;

                smallestAngle = angle;
                finalCam = cam.Current;
            }
            cam.Dispose();
            return finalCam;
        }

        public static bool CanSeePosition(Vector3 groundTargetPosition, Vector3 vesselPosition, Vector3 missilePosition)
        {
            if ((groundTargetPosition - vesselPosition).sqrMagnitude < Mathf.Pow(20, 2))
            {
                return false;
            }

            float dist = BDArmorySettings.MAX_GUARD_VISUAL_RANGE; //replaced constant 10km with actual configured visual range
            Ray ray = new Ray(missilePosition, groundTargetPosition - missilePosition);
            ray.origin += 10 * ray.direction;
            RaycastHit rayHit;
            if (Physics.Raycast(ray, out rayHit, dist, 557057))
            {
                if ((rayHit.point - groundTargetPosition).sqrMagnitude < 200)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The the heat signature of a vessel (for Heat/IR targeting).
        /// Returns the heat of the hottest part of the vessel
        /// </summary>
        /// <param name="v">Vessel</param>
        /// <returns>Heat signature value</returns>
        public static float GetVesselHeatSignature(Vessel v)
        {
            float heatScore = 0f;

            List<Part>.Enumerator part = v.Parts.GetEnumerator();
            while (part.MoveNext())
            {
                if (!part.Current) continue;

                float thisScore = (float)(part.Current.thermalInternalFluxPrevious + part.Current.skinTemperature);
                heatScore = Mathf.Max(heatScore, thisScore);
            }

            return heatScore;
        }

        /// <summary>
        /// Find a flare within acceptable thermal range that will "decoy" for the passed heatsignature
        /// </summary>
        public static TargetSignatureData GetFlareTarget(Ray ray, float scanRadius, float highpassThreshold, bool allAspect, float heatSignature)
        {
            TargetSignatureData flareTarget = TargetSignatureData.noTarget;

            List<CMFlare>.Enumerator flare = BDArmorySetup.Flares.GetEnumerator();
            while (flare.MoveNext())
            {
                if (!flare.Current) continue;

                float angle = Vector3.Angle(flare.Current.transform.position - ray.origin, ray.direction);
                if (angle < scanRadius)
                {
                    float score = flare.Current.thermal * Mathf.Clamp01(15 / angle);

                    score *= Mathf.Pow(1400, 2) / Mathf.Clamp((flare.Current.transform.position - ray.origin).sqrMagnitude, 90000, 36000000);
                    score *= Mathf.Clamp(Vector3.Angle(flare.Current.transform.position - ray.origin, -VectorUtils.GetUpDirection(ray.origin)) / 90, 0.5f, 1.5f);

                    // check acceptable range:
                    // flare cannot be too cool, but also not too bright
                    if ((score > heatSignature * 0.9) && (score < heatSignature * 1.15))
                    {
                        flareTarget = new TargetSignatureData(flare.Current, score);
                    }
                }
            }

            return flareTarget;
        }

        public static TargetSignatureData GetHeatTarget(Ray ray, float scanRadius, float highpassThreshold, bool allAspect, MissileFire mf = null, bool favorGroundTargets = false)
        {
            float minMass = 0.05f;  //otherwise the RAMs have trouble shooting down incoming missiles
            TargetSignatureData finalData = TargetSignatureData.noTarget;
            float finalScore = 0;

            foreach (Vessel vessel in LoadedVessels)
            {
                if (!vessel || !vessel.loaded)
                {
                    continue;
                }

                if (favorGroundTargets && !vessel.LandedOrSplashed) // for AGM heat guidance
                    continue;

                TargetInfo tInfo = vessel.gameObject.GetComponent<TargetInfo>();
                // If no weaponManager or no target or the target is not a missile with engines on..??? and the target weighs less than 50kg, abort.
                if (mf == null ||
                    !tInfo ||
                    !(mf && tInfo.isMissile && (tInfo.MissileBaseModule.MissileState == MissileBase.MissileStates.Boost || tInfo.MissileBaseModule.MissileState == MissileBase.MissileStates.Cruise)))
                {
                    if (vessel.GetTotalMass() < minMass)
                    {
                        continue;
                    }
                }

                // Abort if target is friendly.
                if (mf != null && tInfo != null)
                {
                    if (mf.Team.IsFriendly(tInfo.Team))
                    {
                        continue;
                    }
                }
                float angle = Vector3.Angle(vessel.CoM - ray.origin, ray.direction);
                if (angle < scanRadius)
                {
                    if (RadarUtils.TerrainCheck(ray.origin, vessel.transform.position))
                        continue;

                    if (!allAspect)
                    {
                        if (!Misc.Misc.CheckSightLineExactDistance(ray.origin, vessel.CoM + vessel.Velocity(), Vector3.Distance(vessel.CoM, ray.origin), 5, 5))
                            continue;
                    }

                    float score = GetVesselHeatSignature(vessel) * Mathf.Clamp01(15 / angle);
                    score *= Mathf.Pow(1400, 2) / Mathf.Clamp((vessel.CoM - ray.origin).sqrMagnitude, 90000, 36000000);

                    if (vessel.LandedOrSplashed && !favorGroundTargets)
                    {
                        score /= 4;
                    }

                    score *= Mathf.Clamp(Vector3.Angle(vessel.transform.position - ray.origin, -VectorUtils.GetUpDirection(ray.origin)) / 90, 0.5f, 1.5f);

                    if (score > finalScore)
                    {
                        finalScore = score;
                        finalData = new TargetSignatureData(vessel, score);
                    }
                }
            }

            // see if there are flares decoying us:
            TargetSignatureData flareData = GetFlareTarget(ray, scanRadius, highpassThreshold, allAspect, finalScore);

            if (finalScore < highpassThreshold)
            {
                finalData = TargetSignatureData.noTarget;
            }

            // return matching flare
            if (!flareData.Equals(TargetSignatureData.noTarget))
                return flareData;

            //else return the target:
            return finalData;
        }

        void UpdateDebugLabels()
        {
            debugString.Length = 0;

            using (var team = TargetDatabase.GetEnumerator())
                while (team.MoveNext())
                {
                    debugString.Append($"Team {team.Current.Key} targets:");
                    debugString.Append(Environment.NewLine);
                    foreach (TargetInfo targetInfo in team.Current.Value)
                    {
                        if (targetInfo)
                        {
                            if (!targetInfo.Vessel)
                            {
                                debugString.Append($"- A target with no vessel reference.");
                                debugString.Append(Environment.NewLine);
                            }
                            else
                            {
                                debugString.Append($"- {targetInfo.Vessel.vesselName} Engaged by {targetInfo.TotalEngaging()}");
                                debugString.Append(Environment.NewLine);
                            }
                        }
                        else
                        {
                            debugString.Append($"- null target info.");
                            debugString.Append(Environment.NewLine);
                        }
                    }
                }

            debugString.Append(Environment.NewLine);
            debugString.Append($"Heat Signature: {GetVesselHeatSignature(FlightGlobals.ActiveVessel):#####}");
            debugString.Append(Environment.NewLine);

            debugString.Append($"Radar Signature: " + RadarUtils.GetVesselRadarSignature(FlightGlobals.ActiveVessel).radarModifiedSignature);
            debugString.Append(Environment.NewLine);

            debugString.Append($"Chaff multiplier: " + RadarUtils.GetVesselChaffFactor(FlightGlobals.ActiveVessel));
            debugString.Append(Environment.NewLine);

            debugString.Append($"ECM Jammer Strength: " + FlightGlobals.ActiveVessel.gameObject.GetComponent<VesselECMJInfo>()?.jammerStrength);
            debugString.Append(Environment.NewLine);

            debugString.Append($"ECM Lockbreak Strength: " + FlightGlobals.ActiveVessel.gameObject.GetComponent<VesselECMJInfo>()?.lockBreakStrength);
            debugString.Append(Environment.NewLine);
        }

        public void SaveGPSTargets(ConfigNode saveNode = null)
        {
            string saveTitle = HighLogic.CurrentGame.Title;
            Debug.Log("[BDArmory]: Save title: " + saveTitle);
            ConfigNode fileNode = ConfigNode.Load("GameData/BDArmory/gpsTargets.cfg");
            if (fileNode == null)
            {
                fileNode = new ConfigNode();
                fileNode.AddNode("BDARMORY");
                fileNode.Save("GameData/BDArmory/gpsTargets.cfg");
            }

            if (fileNode != null && fileNode.HasNode("BDARMORY"))
            {
                ConfigNode node = fileNode.GetNode("BDARMORY");

                if (GPSTargets == null || !FlightGlobals.ready)
                {
                    return;
                }

                ConfigNode gpsNode = null;
                if (node.HasNode("BDAGPSTargets"))
                {
                    foreach (ConfigNode n in node.GetNodes("BDAGPSTargets"))
                    {
                        if (n.GetValue("SaveGame") == saveTitle)
                        {
                            gpsNode = n;
                            break;
                        }
                    }

                    if (gpsNode == null)
                    {
                        gpsNode = node.AddNode("BDAGPSTargets");
                        gpsNode.AddValue("SaveGame", saveTitle);
                    }
                }
                else
                {
                    gpsNode = node.AddNode("BDAGPSTargets");
                    gpsNode.AddValue("SaveGame", saveTitle);
                }

                bool foundTargets = false;
                using (var kvp = GPSTargets.GetEnumerator())
                    while (kvp.MoveNext())
                        if (kvp.Current.Value.Count > 0)
                        {
                            foundTargets = true;
                            break;
                        }
                if (!foundTargets)
                    return;

                string targetString = GPSListToString();
                gpsNode.SetValue("Targets", targetString, true);
                fileNode.Save("GameData/BDArmory/gpsTargets.cfg");
                Debug.Log("[BDArmory]: ==== Saved BDA GPS Targets ====");
            }
        }

        void LoadGPSTargets(ConfigNode saveNode)
        {
            ConfigNode fileNode = ConfigNode.Load("GameData/BDArmory/gpsTargets.cfg");
            string saveTitle = HighLogic.CurrentGame.Title;

            if (fileNode != null && fileNode.HasNode("BDARMORY"))
            {
                ConfigNode node = fileNode.GetNode("BDARMORY");

                foreach (ConfigNode gpsNode in node.GetNodes("BDAGPSTargets"))
                {
                    if (gpsNode.HasValue("SaveGame") && gpsNode.GetValue("SaveGame") == saveTitle)
                    {
                        if (gpsNode.HasValue("Targets"))
                        {
                            string targetString = gpsNode.GetValue("Targets");
                            if (targetString == string.Empty)
                            {
                                Debug.Log("[BDArmory]: ==== BDA GPS Target string was empty! ====");
                                return;
                            }
                            StringToGPSList(targetString);
                            Debug.Log("[BDArmory]: ==== Loaded BDA GPS Targets ====");
                        }
                        else
                        {
                            Debug.Log("[BDArmory]: ==== No BDA GPS Targets value found! ====");
                        }
                    }
                }
            }
        }

        // Because Unity's JsonConvert is a featureless pita.
        [Serializable]
        public class SerializableGPSData
        {
            public List<string> Team = new List<string>();
            public List<string> Data = new List<string>();

            public SerializableGPSData(Dictionary<BDTeam, List<GPSTargetInfo>> data)
            {
                using (var kvp = data.GetEnumerator())
                    while (kvp.MoveNext())
                    {
                        Team.Add(kvp.Current.Key.Name);
                        Data.Add(JsonUtility.ToJson(new SerializableGPSList(kvp.Current.Value)));
                    }
            }

            public Dictionary<BDTeam, List<GPSTargetInfo>> Load()
            {
                var value = new Dictionary<BDTeam, List<GPSTargetInfo>>();
                for (int i = 0; i < Team.Count; ++i)
                    value.Add(BDTeam.Get(Team[i]), JsonUtility.FromJson<SerializableGPSList>(Data[i]).Load());
                return value;
            }
        }

        [Serializable]
        public class SerializableGPSList
        {
            public List<string> Data = new List<string>();

            public SerializableGPSList(List<GPSTargetInfo> data)
            {
                using (var gps = data.GetEnumerator())
                    while (gps.MoveNext())
                        Data.Add(JsonUtility.ToJson(gps.Current));
            }

            public List<GPSTargetInfo> Load()
            {
                var value = new List<GPSTargetInfo>();
                using (var json = Data.GetEnumerator())
                    while (json.MoveNext())
                        value.Add(JsonUtility.FromJson<GPSTargetInfo>(json.Current));
                return value;
            }
        }

        //format: very mangled json :(
        private string GPSListToString()
        {
            return Misc.Misc.JsonCompat(JsonUtility.ToJson(new SerializableGPSData(GPSTargets)));
        }

        private void StringToGPSList(string listString)
        {
            try
            {
                GPSTargets = JsonUtility.FromJson<SerializableGPSData>(Misc.Misc.JsonDecompat(listString)).Load();

                Debug.Log("[BDArmory]: Loaded GPS Targets.");
            }
            catch { }
        }

        IEnumerator CleanDatabaseRoutine()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(5);

                using (var team = TargetDatabase.GetEnumerator())
                    while (team.MoveNext())
                    {
                        team.Current.Value.RemoveAll(target => target == null);
                        team.Current.Value.RemoveAll(target => target.Team == team.Current.Key);
                        team.Current.Value.RemoveAll(target => !target.isThreat);
                    }
            }
        }

        void RemoveTarget(TargetInfo target, BDTeam team)
        {
            TargetDatabase[team].Remove(target);
        }

        public static void RemoveTarget(TargetInfo target)
        {
            using (var db = TargetDatabase.GetEnumerator())
                while (db.MoveNext())
                    db.Current.Value.Remove(target);
        }

        public static void ReportVessel(Vessel v, MissileFire reporter)
        {
            if (!v) return;
            if (!reporter) return;

            TargetInfo info = v.gameObject.GetComponent<TargetInfo>();
            if (!info)
            {
                List<MissileFire>.Enumerator mf = v.FindPartModulesImplementing<MissileFire>().GetEnumerator();
                while (mf.MoveNext())
                {
                    if (mf.Current == null) continue;
                    if (reporter.Team.IsEnemy(mf.Current.Team))
                    {
                        info = v.gameObject.AddComponent<TargetInfo>();
                        info.detectedTime[reporter.Team] = Time.time;
                        break;
                    }
                }
                mf.Dispose();

                List<MissileBase>.Enumerator ml = v.FindPartModulesImplementing<MissileBase>().GetEnumerator();
                while (ml.MoveNext())
                {
                    if (ml.Current == null) continue;
                    if (ml.Current.HasFired)
                    {
                        if (reporter.Team.IsEnemy(ml.Current.Team))
                        {
                            info = v.gameObject.AddComponent<TargetInfo>();
                            info.detectedTime[reporter.Team] = Time.time;
                            break;
                        }
                    }
                }
                ml.Dispose();
            }

            // add target to database
            if (info && reporter.Team.IsEnemy(info.Team))
            {
                AddTarget(info, reporter.Team);
                info.detectedTime[reporter.Team] = Time.time;
            }
        }

        public static void AddTarget(TargetInfo target, BDTeam reportingTeam)
        {
            if (target.Team == null) return;
            if (!BDATargetManager.TargetList(reportingTeam).Contains(target))
            {
                BDATargetManager.TargetList(reportingTeam).Add(target);
            }
        }

        public static List<TargetInfo> TargetList(BDTeam team)
        {
            if (TargetDatabase.TryGetValue(team, out List<TargetInfo> database))
                return database;
            var newList = new List<TargetInfo>();
            TargetDatabase.Add(team, newList);
            return newList;
        }

        public static void ClearDatabase()
        {
            using (var teamDB = TargetDatabase.GetEnumerator())
                while (teamDB.MoveNext())
                {
                    using (var targetList = teamDB.Current.Value.GetEnumerator())
                        while (targetList.MoveNext())
                            targetList.Current.detectedTime.Clear();
                    teamDB.Current.Value.Clear();
                }
        }

        public static TargetInfo GetAirToAirTarget(MissileFire mf)
        {
            TargetInfo finalTarget = null;

            float finalTargetSuitability = 0;        //this will determine how suitable the target is, based on where it is located relative to the targeting vessel and how far it is

            List<TargetInfo>.Enumerator target = TargetList(mf.Team).GetEnumerator();
            while (target.MoveNext())
            {
                if (target.Current == null) continue;
                if (target.Current.NumFriendliesEngaging(mf.Team) >= 2) continue;
                if (target.Current && target.Current.Vessel && target.Current.isFlying && !target.Current.isMissile && target.Current.isThreat)
                {
                    Vector3 targetRelPos = target.Current.Vessel.vesselTransform.position - mf.vessel.vesselTransform.position;
                    float targetSuitability = Vector3.Dot(targetRelPos.normalized, mf.vessel.ReferenceTransform.up);       //prefer targets ahead to those behind
                    targetSuitability += 500 / (targetRelPos.magnitude + 100);

                    if (finalTarget == null || (target.Current.NumFriendliesEngaging(mf.Team) < finalTarget.NumFriendliesEngaging(mf.Team)) || targetSuitability > finalTargetSuitability + finalTarget.NumFriendliesEngaging(mf.Team))
                    {
                        finalTarget = target.Current;
                        finalTargetSuitability = targetSuitability;
                    }
                }
            }

            return finalTarget;
        }

        //this will search for an AA target that is immediately in front of the AI during an extend when it would otherwise be helpless
        public static TargetInfo GetAirToAirTargetAbortExtend(MissileFire mf, float maxDistance, float cosAngleCheck)
        {
            TargetInfo finalTarget = null;

            float finalTargetSuitability = 0;    //this will determine how suitable the target is, based on where it is located relative to the targeting vessel and how far it is

            List<TargetInfo>.Enumerator target = TargetList(mf.Team).GetEnumerator();
            while (target.MoveNext())
            {
                if (target.Current == null || !target.Current.Vessel || target.Current.isLandedOrSurfaceSplashed || target.Current.isMissile || !target.Current.isThreat) continue;
                Vector3 targetRelPos = target.Current.Vessel.vesselTransform.position - mf.vessel.vesselTransform.position;

                float distance, dot;
                distance = targetRelPos.magnitude;
                dot = Vector3.Dot(targetRelPos.normalized, mf.vessel.ReferenceTransform.up);

                if (distance > maxDistance || cosAngleCheck > dot)
                    continue;

                float targetSuitability = dot;       //prefer targets ahead to those behind
                targetSuitability += 500 / (distance + 100);        //same suitability check as above

                if (finalTarget != null && !(targetSuitability > finalTargetSuitability)) continue;
                //just pick the most suitable one
                finalTarget = target.Current;
                finalTargetSuitability = targetSuitability;
            }
            target.Dispose();
            return finalTarget;
        }

        //returns the nearest friendly target
        public static TargetInfo GetClosestFriendly(MissileFire mf)
        {
            TargetInfo finalTarget = null;

            List<TargetInfo>.Enumerator target = TargetList(mf.Team).GetEnumerator();
            while (target.MoveNext())
            {
                if (target.Current == null || !target.Current.Vessel || target.Current.weaponManager == mf) continue;
                if (finalTarget == null || (target.Current.IsCloser(finalTarget, mf)))
                {
                    finalTarget = target.Current;
                }
            }
            target.Dispose();
            return finalTarget;
        }

        //returns the target that owns this weapon manager
        public static TargetInfo GetTargetFromWeaponManager(MissileFire mf)
        {
            List<TargetInfo>.Enumerator target = TargetList(mf.Team).GetEnumerator();
            while (target.MoveNext())
            {
                if (target.Current == null) continue;
                if (target.Current.Vessel && target.Current.weaponManager == mf)
                {
                    return target.Current;
                }
            }
            target.Dispose();
            return null;
        }

        public static TargetInfo GetClosestTarget(MissileFire mf)
        {
            TargetInfo finalTarget = null;

            List<TargetInfo>.Enumerator target = TargetList(mf.Team).GetEnumerator();
            while (target.MoveNext())
            {
                if (target.Current == null) continue;
                if (target.Current && target.Current.Vessel && mf.CanSeeTarget(target.Current) && !target.Current.isMissile)
                {
                    if (finalTarget == null || (target.Current.IsCloser(finalTarget, mf)))
                    {
                        finalTarget = target.Current;
                    }
                }
            }
            target.Dispose();
            return finalTarget;
        }

        public static List<TargetInfo> GetAllTargetsExcluding(List<TargetInfo> excluding, MissileFire mf)
        {
            List<TargetInfo> finalTargets = new List<TargetInfo>();

            List<TargetInfo>.Enumerator target = TargetList(mf.Team).GetEnumerator();
            while (target.MoveNext())
            {
                if (target.Current == null) continue;
                if (target.Current && target.Current.Vessel && mf.CanSeeTarget(target.Current) && !excluding.Contains(target.Current))
                {
                    finalTargets.Add(target.Current);
                }
            }
            target.Dispose();
            return finalTargets;
        }

        public static TargetInfo GetLeastEngagedTarget(MissileFire mf)
        {
            TargetInfo finalTarget = null;

            List<TargetInfo>.Enumerator target = TargetList(mf.Team).GetEnumerator();
            while (target.MoveNext())
            {
                if (target.Current == null) continue;
                if (target.Current && target.Current.Vessel && mf.CanSeeTarget(target.Current) && !target.Current.isMissile && target.Current.isThreat)
                {
                    if (finalTarget == null || target.Current.NumFriendliesEngaging(mf.Team) < finalTarget.NumFriendliesEngaging(mf.Team))
                    {
                        finalTarget = target.Current;
                    }
                }
            }
            target.Dispose();
            return finalTarget;
        }

        public static TargetInfo GetMissileTarget(MissileFire mf, bool targetingMeOnly = false)
        {
            TargetInfo finalTarget = null;

            List<TargetInfo>.Enumerator target = TargetList(mf.Team).GetEnumerator();
            while (target.MoveNext())
            {
                if (target.Current == null) continue;
                if (target.Current && target.Current.Vessel && target.Current.isMissile && target.Current.isThreat && mf.CanSeeTarget(target.Current))
                {
                    if (target.Current.MissileBaseModule)
                    {
                        if (targetingMeOnly)
                        {
                            if (Vector3.SqrMagnitude(target.Current.MissileBaseModule.TargetPosition - mf.vessel.CoM) > 60 * 60)
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (BDArmorySettings.DRAW_DEBUG_LABELS)
                            Debug.LogWarning("checking target missile -  doesn't have missile module");
                    }

                    if (((finalTarget == null && target.Current.NumFriendliesEngaging(mf.Team) < 2) || (finalTarget != null && target.Current.NumFriendliesEngaging(mf.Team) < finalTarget.NumFriendliesEngaging(mf.Team))))
                    {
                        finalTarget = target.Current;
                    }
                }
            }
            target.Dispose();
            return finalTarget;
        }

        public static TargetInfo GetUnengagedMissileTarget(MissileFire mf)
        {
            List<TargetInfo>.Enumerator target = TargetList(mf.Team).GetEnumerator();
            while (target.MoveNext())
            {
                if (target.Current == null) continue;
                if (target.Current && target.Current.Vessel && mf.CanSeeTarget(target.Current) && target.Current.isMissile && target.Current.isThreat)
                {
                    if (target.Current.NumFriendliesEngaging(mf.Team) == 0)
                    {
                        return target.Current;
                    }
                }
            }
            target.Dispose();
            return null;
        }

        public static TargetInfo GetClosestMissileTarget(MissileFire mf)
        {
            TargetInfo finalTarget = null;

            List<TargetInfo>.Enumerator target = TargetList(mf.Team).GetEnumerator();
            while (target.MoveNext())
            {
                if (target.Current == null) continue;
                if (target.Current && target.Current.Vessel && mf.CanSeeTarget(target.Current) && target.Current.isMissile)
                {
                    bool isHostile = false;
                    if (target.Current.isThreat)
                    {
                        isHostile = true;
                    }

                    if (isHostile && (finalTarget == null || target.Current.IsCloser(finalTarget, mf)))
                    {
                        finalTarget = target.Current;
                    }
                }
            }
            target.Dispose();
            return finalTarget;
        }

        //checks to see if a friendly is too close to the gun trajectory to fire them
        public static bool CheckSafeToFireGuns(MissileFire weaponManager, Vector3 aimDirection, float safeDistance, float cosUnsafeAngle)
        {
            if (weaponManager == null) return false;
            if (weaponManager.vessel == null) return false;

            using (var friendlyTarget = FlightGlobals.Vessels.GetEnumerator())
                while (friendlyTarget.MoveNext())
                {
                    if (friendlyTarget.Current == null || friendlyTarget.Current == weaponManager.vessel)
                        continue;
                    var wms = friendlyTarget.Current.FindPartModuleImplementing<MissileFire>();
                    if (wms != null && wms.Team != weaponManager.Team)
                        continue;
                    Vector3 targetDistance = friendlyTarget.Current.CoM - weaponManager.vessel.CoM;
                    float friendlyPosDot = Vector3.Dot(targetDistance, aimDirection);
                    if (friendlyPosDot <= 0)
                        continue;
                    float friendlyDistance = targetDistance.magnitude;
                    float friendlyPosDotNorm = friendlyPosDot / friendlyDistance;       //scale down the dot to be a 0-1 so we can check it againts cosUnsafeAngle

                    if (friendlyDistance < safeDistance && cosUnsafeAngle < friendlyPosDotNorm)           //if it's too close and it's within the Unsafe Angle, don't fire
                        return false;
                }
            return true;
        }

        void OnGUI()
        {
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                GUI.Label(new Rect(600, 100, 600, 600), debugString.ToString());
            }
        }
    }
}
