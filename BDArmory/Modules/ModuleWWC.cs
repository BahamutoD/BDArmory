using System.Collections.Generic;

namespace BDArmory.Modules
{
    public class ModuleWWC : PartModule
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Deactivation Depth"),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0f, maxValue = -200f, stepIncrement = 1f)]
        public float CutOffDepth = -5;

        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight && vessel.altitude < CutOffDepth)
            {
                DisableWeapons();
            }
        }

        private void DisableWeapons()
        {
            List<ModuleTurret> turrets = new List<ModuleTurret>(200);
            foreach (Part p in vessel.Parts)
            {
                turrets.AddRange(p.FindModulesImplementing<ModuleTurret>());
            }
            foreach (ModuleTurret turret in turrets)
            {
                turret.maxPitch = 0;
                turret.yawRange = 0;
                turret.pitchSpeedDPS = 0;
            }

            List<ModuleWeapon> weapons = new List<ModuleWeapon>(200);
            foreach (Part p in vessel.Parts)
            {
                weapons.AddRange(p.FindModulesImplementing<ModuleWeapon>());
            }
            foreach (ModuleWeapon weapon in weapons)
            {
                weapon.engageGround = false;
                weapon.engageAir = false;
                weapon.engageMissile = false;
                weapon.engageSLW = false;
                weapon.engageRangeMin = 0;
            }
        }
    }
}
