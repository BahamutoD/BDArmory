using System.Collections.Generic;
using BDArmory.Core.Extension;
using BDArmory.Core.Module;
using UnityEngine;

namespace BDArmory.Armor
{
    public class BDArmor_ : PartModule
    {
        //static BDArmor instance;
        //public static BDArmor Instance => instance;

        [KSPField(isPersistant = true)]
        public float ArmorThickness = 0f;

        //[KSPField]
        //public string explModelPath = "BDArmory/Models/explosion/explosionLarge";

        //[KSPField]
        //public string explSoundPath = "BDArmory/Sounds/explode1";

        //[KSPField]
        //public string explodeMode = "Never";

        //public ArmorUtils.ExplodeMode _explodeMode { get; private set; } = ArmorUtils.ExplodeMode.Never;

        public override void OnStart(StartState state)
        {
            base.OnAwake();
            part.force_activate();
            //instance = this;

            //switch (explodeMode)
            //{
            //    case "Always":
            //        _explodeMode = ArmorUtils.ExplodeMode.Always;
            //        break;
            //    case "Dynamic":
            //        _explodeMode = ArmorUtils.ExplodeMode.Dynamic;
            //        break;
            //    case "Never":
            //        _explodeMode = ArmorUtils.ExplodeMode.Never;
            //        break;
            //}

        }

        public void CreateExplosion(Part part)
        {
            float explodeScale = 0;
            IEnumerator<PartResource> resources = part.Resources.GetEnumerator();
            while (resources.MoveNext())
            {
                if (resources.Current == null) continue;
                switch (resources.Current.resourceName)
                {
                    case "LiquidFuel":
                        explodeScale += (float)resources.Current.amount;
                        break;
                    case "Oxidizer":
                        explodeScale += (float)resources.Current.amount;
                        break;
                }
            }
            resources.Dispose();
            explodeScale /= 100;
            part.explode();
           // ExplosionFX.CreateExplosion(part.partTransform.position, explodeScale * blastRadius, explodeScale * blastPower * 2,
           //    explodeScale * blastHeat, part.vessel, FlightGlobals.upAxis, explModelPath, explSoundPath);
        }


    }
}