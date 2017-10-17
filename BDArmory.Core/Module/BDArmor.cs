using UnityEngine;

namespace BDArmory.Core.Module
{
    public class BDArmor : PartModule
    {
        static BDArmor instance;
        public static BDArmor Instance => instance;

        [KSPField(isPersistant = true)]
        public float ArmorThickness = 0f;

        [KSPField]
        public string explModelPath = "BDArmory/Models/explosion/explosionLarge";

        [KSPField]
        public string explSoundPath = "BDArmory/Sounds/explode1";

        [KSPField]
        public string explodeMode = "Never";

        public ArmorUtils.ExplodeMode _explodeMode { get; private set; } = ArmorUtils.ExplodeMode.Never;

        public override void OnStart(StartState state)
        {
            base.OnAwake();
            part.force_activate();

            //instance = this;

            switch (explodeMode)
            {
                case "Always":
                    _explodeMode = ArmorUtils.ExplodeMode.Always;
                    break;
                case "Dynamic":
                    _explodeMode = ArmorUtils.ExplodeMode.Dynamic;
                    break;
                case "Never":
                    _explodeMode = ArmorUtils.ExplodeMode.Never;
                    break;
            }
        }

    }
}
