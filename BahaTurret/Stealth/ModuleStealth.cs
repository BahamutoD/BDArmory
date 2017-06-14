using UnityEngine;

namespace BahaTurret
{
    public class ModuleStealth : PartModule
    {
        [KSPField]
        public float stealthStrength = 1;

        public static bool stealthEnabled;

        [KSPAction("Toggle Stealth")]
        public void AGEnable(KSPActionParam param)
        {
            stealthEnabled = !stealthEnabled;
        }
        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiName = "Toggle Stealth")]
        public void ToggleStealth()
        {
            stealthEnabled = !stealthEnabled;
        }
    }
}
