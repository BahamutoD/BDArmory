using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BDArmory.Core.Behaviour
{

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class MaxDamageSetup : MonoBehaviour
    {
        private int maxDamageMultiplier = 1000;
        void Start()
        {
            GameEvents.onVesselCreate.Add(UpdateMaxDamage);
            GameEvents.onVesselLoaded.Add(UpdateMaxDamage);    
        }

        private void UpdateMaxDamage(Vessel vessel)
        {
            List<Part>.Enumerator parts = vessel.parts.GetEnumerator();
            while (parts.MoveNext())
            {
                if (parts.Current == null) continue;
                parts.Current.maxTemp = Mathf.Clamp(parts.Current.mass * maxDamageMultiplier,3000,float.MaxValue);
            }
            parts.Dispose();
        }

        void OnDestroy()
        {
            GameEvents.onVesselCreate.Add(UpdateMaxDamage);
            GameEvents.onVesselLoaded.Add(UpdateMaxDamage);
        }
    }
}
