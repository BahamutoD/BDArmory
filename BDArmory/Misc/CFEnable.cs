


//Class by NathanKell
//http://forum.kerbalspaceprogram.com/threads/76499-0-23-5-CrossFeedEnabler-v1-4-13-14

namespace BDArmory.Misc
{
    public class CFEnable : PartModule
    {
        // belt-and-suspenders: do this everywhere and everywhen.
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (part.parent != null && part.parent.fuelLookupTargets != null)
            {
                if (!part.parent.fuelLookupTargets.Contains(part))
                    part.parent.fuelLookupTargets.Add(part);
                if (!part.fuelLookupTargets.Contains(part.parent))
                    part.fuelLookupTargets.Add(part.parent);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (part.parent != null && part.parent.fuelLookupTargets != null)
            {
                if (!part.parent.fuelLookupTargets.Contains(part))
                    part.parent.fuelLookupTargets.Add(part);
                if (!part.fuelLookupTargets.Contains(part.parent))
                    part.fuelLookupTargets.Add(part.parent);
            }
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            if (part.parent != null && part.parent.fuelLookupTargets != null)
            {
                if (!part.parent.fuelLookupTargets.Contains(part))
                    part.parent.fuelLookupTargets.Add(part);
                if (!part.fuelLookupTargets.Contains(part.parent))
                    part.fuelLookupTargets.Add(part.parent);
            }
        }

        public void OnDestroy()
        {
            if (part.parent != null && part.parent.fuelLookupTargets != null)
            {
                if (part.parent.fuelLookupTargets.Contains(part))
                    part.parent.fuelLookupTargets.Remove(part);
            }
        }
    }
}