using UnityEngine;

namespace BDArmory.Core.Module
{
    public class BDArmor : PartModule
    {
        [KSPField(isPersistant = true)]
        public float ArmorThickness = 0f;
        
        public override void OnStart(StartState state)
        {
            base.OnAwake();
            part.force_activate();
        }

    }
}
