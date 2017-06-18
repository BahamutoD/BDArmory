using System.Collections.Generic;
using UnityEngine;

namespace BDArmory.Misc
{
    public class WMTurretGroup : MonoBehaviour
    {
        public enum TargetTypes
        {
            Air,
            Ground,
            Missiles,
            All
        }

        public List<ModuleWeapon> weapons;

        public bool guardMode = false;
        public TargetTypes targetType = TargetTypes.All;


        public void StartFiringOnTarget(Vessel targetVessel, float burstLength)
        {
            List<ModuleWeapon>.Enumerator weapon = weapons.GetEnumerator();
            while (weapon.MoveNext())
            {
                if (weapon.Current == null) continue;
                weapon.Current.legacyTargetVessel = targetVessel;
                weapon.Current.autoFireTimer = Time.time;
                weapon.Current.autoFireLength = burstLength;
            }
            weapon.Dispose();
        }

        public void ForceStopFiring()
        {
            List<ModuleWeapon>.Enumerator weapon = weapons.GetEnumerator();
            while (weapon.MoveNext())
            {
                if (weapon.Current == null) continue;
                weapon.Current.autoFire = false;
                weapon.Current.legacyTargetVessel = null;
            }
            weapon.Dispose();
        }
    }
}