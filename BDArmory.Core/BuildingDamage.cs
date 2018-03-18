using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BDArmory.Core
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class BuildingDamage : ScenarioDestructibles
    {
        public override void OnAwake()
        {
            Debug.Log("[BDArmory]: Modifying Buildings");

            foreach (KeyValuePair<string, ProtoDestructible> bldg in protoDestructibles)
            {
                DestructibleBuilding building = bldg.Value.dBuildingRefs[0];
                building.damageDecay = 600f;
                building.impactMomentumThreshold *= 150;
            }

        }

    }
}
