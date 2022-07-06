using System;
using System.Collections.Generic;
using BDArmory.Core;
using BDArmory.Modules;
using UnityEngine;

namespace BDArmory.Misc
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class BDAEditorTools : MonoBehaviour
    {
        private static readonly List<AvailablePart> radars = new List<AvailablePart>();

        public const string Manufacturer = "Bahamuto Dynamics";

        void Awake()
        {
            radars.Clear();
            var availableParts = new List<AvailablePart>();
            var count = PartLoader.LoadedPartsList.Count;
            for (int i = 0; i < count; ++i)
            {
                var avPart = PartLoader.LoadedPartsList[i];
                if (!avPart.partPrefab) continue;
                if (avPart.manufacturer == Manufacturer)
                {
                    availableParts.Add(avPart);
                }
                if (avPart.partPrefab.GetComponent<ModuleRadar>() != null)
                {
                    radars.Add(avPart);
                }
            }

            print(Manufacturer + "  Filter Count: " + availableParts.Count);
            if (availableParts.Count > 0)
                GameEvents.onGUIEditorToolbarReady.Add(CheckDump);
        }

        void CheckDump()
        {
            // dump parts to .CSV list
            if (BDArmorySettings.DRAW_DEBUG_LABELS)
                dumpParts();
        }

        public static List<ModuleRadar> getRadars()
        {
            List<ModuleRadar> results = new List<ModuleRadar>(150);

            foreach (var item in radars)
            {
                var radar = item.partPrefab.GetComponent<ModuleRadar>();
                if (radar != null && (radar.canScan || radar.canLock))
                    results.Add(radar);
            }

            return results;
        }

        void dumpParts()
        {
            String gunName = "bda_weapons_list.csv";
            String missileName = "bda_missile_list.csv";
            String radarName = "bda_radar_list.csv";
            String jammerName = "bda_jammer_list.csv";
            ModuleWeapon weapon = null;
            MissileLauncher missile = null;
            ModuleRadar radar = null;
            ModuleECMJammer jammer = null;

            // 1. create the file
            var fileguns = KSP.IO.TextWriter.CreateForType<BDAEditorTools>(gunName);
            var filemissiles = KSP.IO.TextWriter.CreateForType<BDAEditorTools>(missileName);
            var fileradars = KSP.IO.TextWriter.CreateForType<BDAEditorTools>(radarName);
            var filejammers = KSP.IO.TextWriter.CreateForType<BDAEditorTools>(jammerName);

            // 2. write header
            fileguns.WriteLine("NAME;TITLE;AUTHOR;MANUFACTURER;PART_MASS;PART_COST;PART_CRASHTOLERANCE;PART_MAXTEMP;WEAPON_RPM;WEAPON_DEVIATION;WEAPON_MAXEFFECTIVEDISTANCE;WEAPON_TYPE;WEAPON_BULLETTYPE;WEAPON_AMMONAME;WEAPON_BULLETMASS;WEAPON_BULLET_VELOCITY;WEAPON_MAXHEAT;WEAPON_HEATPERSHOT;WEAPON_HEATLOSS;CANNON_SHELLPOWER;CANNON_SHELLHEAT;CANNON_SHELLRADIUS;CANNON_AIRDETONATION");
            filemissiles.WriteLine("NAME;TITLE;AUTHOR;MANUFACTURER;PART_MASS;PART_COST;PART_CRASHTOLERANCE;PART_MAXTEMP;" +
                                    "MISSILE_THRUST;MISSILE_BOOSTTIME;MISSILE_CRUISETHRUST;MISSILE_CRUISETIME;MISSILE_MAXTURNRATEDPS;MISSILE_BLASTPOWER;MISSILE_BLASTHEAT;MISSILE_BLASTRADIUS;MISSILE_GUIDANCEACTIVE;MISSILE_HOMINGTYPE;MISSILE_TARGETINGTYPE;MISSILE_MINLAUNCHSPEED;MISSILE_MINSTATICLAUNCHRANGE;MISSILE_MAXSTATICLAUNCHRANGE;MISSILE_OPTIMUMAIRSPEED;" +
                                    "CRUISE_TERMINALMANEUVERING; CRUISE_TERMINALGUIDANCETYPE; CRUISE_TERMINALGUIDANCEDISTANCE;" +
                                    "RADAR_ACTIVERADARRANGE;RADAR_RADARLOAL;" +
                                    "TRACK_MAXOFFBORESIGHT;TRACK_LOCKEDSENSORFOV;" +
                                    "HEAT_HEATTHRESHOLD;" +
                                    "LASER_BEAMCORRECTIONFACTOR; LASER_BEAMCORRECTIONDAMPING"
                                    );
            fileradars.WriteLine("NAME;TITLE;AUTHOR;MANUFACTURER;PART_MASS;PART_COST;PART_CRASHTOLERANCE;PART_MAXTEMP;radar_name;rwrThreatType;omnidirectional;directionalFieldOfView;boresightFOV;" +
                                 "scanRotationSpeed;lockRotationSpeed;lockRotationAngle;showDirectionWhileScan;multiLockFOV;lockAttemptFOV;canScan;canLock;canTrackWhileScan;canRecieveRadarData;" +
                                 "maxLocks;radarGroundClutterFactor;radarDetectionCurve;radarLockTrackCurve"
                                  );
            filejammers.WriteLine("NAME;TITLE;AUTHOR;MANUFACTURER;PART_MASS;PART_COST;PART_CRASHTOLERANCE;PART_MAXTEMP;alwaysOn;rcsReduction;rcsReducationFactor;lockbreaker;lockbreak_strength;jammerStrength");

            Debug.Log("Dumping parts...");

            // 3. iterate weapons and write out fields
            foreach (var item in PartLoader.LoadedPartsList)
            {
                weapon = null;
                missile = null;
                radar = null;
                jammer = null;
                weapon = item.partPrefab.GetComponent<ModuleWeapon>();
                missile = item.partPrefab.GetComponent<MissileLauncher>();
                radar = item.partPrefab.GetComponent<ModuleRadar>();
                jammer = item.partPrefab.GetComponent<ModuleECMJammer>();

                if (weapon != null)
                {
                    fileguns.WriteLine(
                        item.name + ";" + item.title + ";" + item.author + ";" + item.manufacturer + ";" + item.partPrefab.mass + ";" + item.cost + ";" + item.partPrefab.crashTolerance + ";" + item.partPrefab.maxTemp + ";" +
                        weapon.roundsPerMinute + ";" + weapon.maxDeviation + ";" + weapon.maxEffectiveDistance + ";" + weapon.weaponType + ";" + weapon.bulletType + ";" + weapon.ammoName + ";" + weapon.bulletMass + ";" + weapon.bulletVelocity + ";" +
                        weapon.maxHeat + ";" + weapon.heatPerShot + ";" + weapon.heatLoss + ";" + weapon.cannonShellPower + ";" + weapon.cannonShellHeat + ";" + weapon.cannonShellRadius + ";" + weapon.airDetonation
                        );
                }

                if (missile != null)
                {
                    filemissiles.WriteLine(
                        item.name + ";" + item.title + ";" + item.author + ";" + item.manufacturer + ";" + item.partPrefab.mass + ";" + item.cost + ";" + item.partPrefab.crashTolerance + ";" + item.partPrefab.maxTemp + ";" +
                                    missile.thrust + ";" + missile.boostTime + ";" + missile.cruiseThrust + ";" + missile.cruiseTime + ";" + missile.maxTurnRateDPS + ";" + missile.blastPower + ";" + missile.blastHeat + ";" + missile.blastRadius + ";" + missile.guidanceActive + ";" + missile.homingType + ";" + missile.targetingType + ";" + missile.minLaunchSpeed + ";" + missile.minStaticLaunchRange + ";" + missile.maxStaticLaunchRange + ";" + missile.optimumAirspeed + ";" +
                                    missile.terminalManeuvering + ";" + missile.terminalGuidanceType + ";" + missile.terminalGuidanceDistance + ";" +
                                    missile.activeRadarRange + ";" + missile.radarLOAL + ";" +
                                    missile.maxOffBoresight + ";" + missile.lockedSensorFOV + ";" +
                                    missile.heatThreshold + ";" +
                                    missile.beamCorrectionFactor + ";" + missile.beamCorrectionDamping
                        );
                }

                if (radar != null)
                {
                    fileradars.WriteLine(
                        item.name + ";" + item.title + ";" + item.author + ";" + item.manufacturer + ";" + item.partPrefab.mass + ";" + item.cost + ";" + item.partPrefab.crashTolerance + ";" + item.partPrefab.maxTemp + ";" +
                        radar.radarName + ";" + radar.getRWRType(radar.rwrThreatType) + ";" + radar.omnidirectional + ";" + radar.directionalFieldOfView + ";" + radar.boresightFOV + ";" + radar.scanRotationSpeed + ";" + radar.lockRotationSpeed + ";" +
                        radar.lockRotationAngle + ";" + radar.showDirectionWhileScan + ";" + radar.multiLockFOV + ";" + radar.lockAttemptFOV + ";" +
                        radar.canScan + ";" + radar.canLock + ";" + radar.canTrackWhileScan + ";" + radar.canRecieveRadarData + ";" +
                        radar.maxLocks + ";" + radar.radarGroundClutterFactor + ";" +
                        radar.radarDetectionCurve.Evaluate(radar.radarMaxDistanceDetect) + "@" + radar.radarMaxDistanceDetect + ";" +
                        radar.radarLockTrackCurve.Evaluate(radar.radarMaxDistanceLockTrack) + "@" + radar.radarMaxDistanceLockTrack
                        );
                }

                if (jammer != null)
                {
                    filejammers.WriteLine(
                        item.name + ";" + item.title + ";" + item.author + ";" + item.manufacturer + ";" + item.partPrefab.mass + ";" + item.cost + ";" + item.partPrefab.crashTolerance + ";" + item.partPrefab.maxTemp + ";" +
                        jammer.alwaysOn + ";" + jammer.rcsReduction + ";" + jammer.rcsReductionFactor + ";" + jammer.lockBreaker + ";" + jammer.lockBreakerStrength + ";" + jammer.jammerStrength
                        );
                }
            }

            // 4. close file
            fileguns.Close();
            filemissiles.Close();
            fileradars.Close();
            filejammers.Close();
            Debug.Log("...dumping parts complete.");
        }
    }
}
