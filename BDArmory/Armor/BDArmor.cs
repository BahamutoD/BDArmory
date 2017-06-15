using System;
using System.Collections.Generic;
using System.Linq;
using BDArmory.FX;
using UnityEngine;

namespace BDArmory.Armor
{
    public class BDArmor
    {
        public float EquivalentThickness { get; private set; }
        public ExplodeMode explodeMode { get; private set; }
        public float blastRadius { get; private set; }
        public float blastPower { get; private set; }
        public float blastHeat { get; private set; }
        public float outerArmorThickness { get; private set; }
        public string explModelPath { get; private set; }
        public string explSoundPath { get; private set; }

        public BDArmor(ConfigNode configNode)
        {
            if (configNode.HasValue("EquivalentThickness"))
                EquivalentThickness = float.Parse(configNode.GetValue("EquivalentThickness"));
            else
                EquivalentThickness = 1;

            if (configNode.HasValue("ExplodeMode"))
                explodeMode = (ExplodeMode) Enum.Parse(typeof(ExplodeMode), configNode.GetValue("ExplodeMode"));
            else
                explodeMode = ExplodeMode.Never;

            if (configNode.HasValue("blastRadius"))
                blastRadius = float.Parse(configNode.GetValue("blastRadius"));
            else
                blastRadius = 1;

            if (configNode.HasValue("blastPower"))
                blastPower = float.Parse(configNode.GetValue("blastPower"));
            else
                blastPower = 1;

            if (configNode.HasValue("blastHeat"))
                blastHeat = float.Parse(configNode.GetValue("blastHeat"));
            else
                blastHeat = 1;

            if (configNode.HasValue("outerArmorThickness"))
                outerArmorThickness = float.Parse(configNode.GetValue("outerArmorThickness"));
            else
                outerArmorThickness = float.MaxValue;

            if (configNode.HasValue("explModelPath"))
                explModelPath = configNode.GetValue("explModelPath");
            else
                explModelPath = "BDArmory/Models/explosion/explosionLarge";

            if (configNode.HasValue("explSoundPath"))
                explSoundPath = configNode.GetValue("explSoundPath");
            else
                explSoundPath = "BDArmory/Sounds/explode1";
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
            ExplosionFX.CreateExplosion(part.partTransform.position, explodeScale*blastRadius, explodeScale*blastPower*2,
                explodeScale*blastHeat, part.vessel, FlightGlobals.upAxis, explModelPath, explSoundPath);
        }

        public static BDArmor GetArmor(Collider collider, Part hitPart)
        {
            if (!hitPart)
                return null;
            List<ConfigNode>.Enumerator nodes = hitPart.partInfo.partConfig.GetNodes("BDARMOR").ToList().GetEnumerator();
            while (nodes.MoveNext())
            {
                if (nodes.Current == null) continue;
                Transform transform;
                if (nodes.Current.HasValue("ArmorRootTransform"))
                    transform = hitPart.FindModelTransform(nodes.Current.GetValue("ArmorRootTransform"));
                else
                    transform = hitPart.partTransform;
                if (!transform)
                {
                    Debug.LogError("[BDArmory]: Armor Transform not found!");
                    return null;
                }
                if (collider.transform == transform || collider.transform.IsChildOf(transform))
                {
                    return new BDArmor(nodes.Current);
                }
            }
            nodes.Dispose();
            return null;
        }

        public enum ExplodeMode
        {
            Always,
            Dynamic,
            Never
        }
    }
}