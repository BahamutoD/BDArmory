using System.Linq;

namespace BDArmory.Modules
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
            vessel.OnFlyByWire -= ThrottleControl;
            vessel.OnFlyByWire += ThrottleControl;

            var wmPart = part.FindModuleImplementing<MissileFire>();

            if (wmPart != null)
            {
                if (wmPart.guardMode)
                {
                    wmPart.guardMode = false;
                }

                if (wmPart.AI.pilotEnabled)
                {
                    wmPart.AI.TogglePilot();
                }
            }

            foreach (var p in vessel.parts)
            {
                PartResource r = p.Resources.Where(pr => pr.resourceName == "ElectricCharge").FirstOrDefault();
                if (r != null)
                {
                    p.RequestResource("ElectricCharge", r.amount);
                }

                var camera = p.FindModuleImplementing<ModuleTargetingCamera>();
                var radar = p.FindModuleImplementing<ModuleRadar>();
                var spaceRadar = p.FindModuleImplementing<ModuleSpaceRadar>();

                if (radar != null)
                {
                    if (radar.radarEnabled)
                    {
                        radar.DisableRadar();
                    }
                }

                if (spaceRadar != null)
                {
                    if (spaceRadar.radarEnabled)
                    {
                        spaceRadar.DisableRadar();
                    }
                }

                if (camera != null)
                {
                    if (camera.cameraEnabled)
                    {
                        camera.DisableCamera();
                    }
                }

                if (setup)
                {
                    setup = false;

                    var command = p.FindModuleImplementing<ModuleCommand>();
                    var weapon = p.FindModuleImplementing<ModuleWeapon>();
                    var turret = p.FindModuleImplementing<ModuleTurret>();

                    if (turret != null)
                    {
                        turret.maxPitch = 0;
                        turret.yawRange = 0;
                        turret.yawRangeLimit = 0;
                    }

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

                    if (command != null)
                    {
                        minCrew = command.minimumCrew;
                        command.minimumCrew = 100;
                    }

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
                    }
                }
            }
        }
    }
}