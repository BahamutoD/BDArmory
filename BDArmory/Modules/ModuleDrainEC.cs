using BDArmory.Modules;
using System.Diagnostics;
using System.Linq;

namespace EMP
{
    public class ModuleDrainEC : PartModule
    {
        public bool armed = true;
        private bool setup = true;
        private int minCrew = 0;

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                part.force_activate();
                vessel.OnFlyByWire += ThrottleControl;
                part.OnJustAboutToBeDestroyed += EnableVessel;
            }
            base.OnStart(state);
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (armed)
                {
                    DisableVessel();
                }
                else
                {
                    EnableVessel();
                }
            }
        }

        void ThrottleControl(FlightCtrlState s)
        {
            s.mainThrottle = 0;
        }

        private void EnableVessel()
        {
            foreach (var p in vessel.parts)
            {
                var command = p.FindModuleImplementing<ModuleCommand>();

                if (command != null)
                {
                    command.minimumCrew = minCrew;
                }

                foreach (ModuleEnginesFX engineFX in p.Modules)
                {
                    engineFX.allowRestart = true;
                }

                foreach (ModuleEngines engine in p.Modules)
                {
                    engine.allowRestart = true;
                }
            }

            Destroy(this);
        }

        private void DisableVessel()
        {
            UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Start");
            vessel.OnFlyByWire -= ThrottleControl;
            vessel.OnFlyByWire += ThrottleControl;

            UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Checking Vessel Parts");

            foreach (Part p in vessel.parts)
            {
                UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Draining EC");

                PartResource r = p.Resources.Where(pr => pr.resourceName == "ElectricCharge").FirstOrDefault();
                if (r != null)
                {
                    if (r.amount >= 0)
                    {
                        p.RequestResource("ElectricCharge", r.amount);
                    }
                }

                if (setup)
                {
                    UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Setup");

                    setup = false;

                    var camera = p.FindModuleImplementing<ModuleTargetingCamera>();
                    var radar = p.FindModuleImplementing<ModuleRadar>();
                    var spaceRadar = p.FindModuleImplementing<ModuleSpaceRadar>();
                    UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Seeking Radar");

                    if (radar != null)
                    {
                        if (radar.radarEnabled)
                        {
                            radar.DisableRadar();
                        }
                    }

                    UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Seeking Space Radar");

                    if (spaceRadar != null)
                    {
                        if (spaceRadar.radarEnabled)
                        {
                            spaceRadar.DisableRadar();
                        }
                    }

                    UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Seeking Camera");

                    if (camera != null)
                    {
                        if (camera.cameraEnabled)
                        {
                            camera.DisableCamera();
                        }
                    }

                    var command = p.FindModuleImplementing<ModuleCommand>();
                    var weapon = p.FindModuleImplementing<ModuleWeapon>();
                    var turret = p.FindModuleImplementing<ModuleTurret>();
                    UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Seeking Turret");

                    if (turret != null)
                    {
                        turret.maxPitch = 0;
                        turret.yawRange = 0;
                        turret.yawRangeLimit = 0;
                    }
                    UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Seeking Weapon");

                    if (weapon != null)
                    {
                        weapon.onlyFireInRange = true;
                        weapon.engageRangeMax = 0;
                        weapon.engageRangeMin = 0;
                        weapon.engageSLW = false;
                        weapon.engageMissile = false;
                        weapon.engageGround = false;
                        weapon.engageAir = false;
                    }
                    UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Seeking Command");

                    if (command != null)
                    {
                        minCrew = command.minimumCrew;
                        command.minimumCrew = 100;
                    }
                    /*
                    var wmPart = part.FindModuleImplementing<MissileFire>();
                    UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Find Weapon Manager");

                    if (wmPart != null)
                    {
                        UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Found Weapon Manager");

                        if (wmPart.guardMode)
                        {
                            UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Guard Mode Off");

                            wmPart.guardMode = false;
                        }

                        if (wmPart.AI.pilotEnabled)
                        {
                            UnityEngine.Debug.Log("MODULE DRAIN EC ------------- Disable Vessel Pilot AI Off");

                            wmPart.AI.TogglePilot();
                        }
                    }

                    /*
                    foreach (ModuleEnginesFX engineFX in p.Modules)
                    {
                        engineFX.allowRestart = false;
                        engineFX.Shutdown();
                        engineFX.ShutdownAction(new KSPActionParam(KSPActionGroup.None, KSPActionType.Deactivate));
                    }

                    foreach (ModuleEngines engine in p.Modules)
                    {
                        engine.allowRestart = false;
                        engine.Shutdown();
                        engine.ShutdownAction(new KSPActionParam(KSPActionGroup.None, KSPActionType.Deactivate));
                    }*/
                }
            }
        }
    }
}