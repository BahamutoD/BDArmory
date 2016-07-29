using System;
using System.Collections.Generic;
using UnityEngine;
using KSP;


//Class by NathanKell
//http://forum.kerbalspaceprogram.com/threads/76499-0-23-5-CrossFeedEnabler-v1-4-13-14

namespace BahaTurret
{
    public class CFEnable : PartModule
    {
        // belt-and-suspenders: do this everywhere and everywhen.
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (part.parent != null && part.parent.fuelLookupTargets != null)
            {
                if (!part.parent.fuelLookupTargets.Contains(this.part))
                    part.parent.fuelLookupTargets.Add(this.part);
                if (!this.part.fuelLookupTargets.Contains(part.parent))
                    part.fuelLookupTargets.Add(part.parent);
			}
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (part.parent != null && part.parent.fuelLookupTargets != null)
            {
                if (!part.parent.fuelLookupTargets.Contains(this.part))
                    part.parent.fuelLookupTargets.Add(this.part);
                if (!this.part.fuelLookupTargets.Contains(part.parent))
                    part.fuelLookupTargets.Add(part.parent);
            }
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            if (part.parent != null && part.parent.fuelLookupTargets != null)
            {
                if (!part.parent.fuelLookupTargets.Contains(this.part))
                    part.parent.fuelLookupTargets.Add(this.part);
                if (!this.part.fuelLookupTargets.Contains(part.parent))
                    part.fuelLookupTargets.Add(part.parent);
            }
        }

        public void OnDestroy()
        {
            if (part.parent != null && part.parent.fuelLookupTargets != null)
            {
                if (part.parent.fuelLookupTargets.Contains(this.part))
                    part.parent.fuelLookupTargets.Remove(this.part);
            }
        }
    }
}