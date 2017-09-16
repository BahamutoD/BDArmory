using System;
using System.Collections.Generic;
using UnityEngine;

namespace BDArmory.Armor
{
    public class BulletInfo
    {
        public float positiveCoefficient { get; private set; }
        public FloatCurve penetration { get; private set; }
        public string name { get; private set; }
        public float caliber { get; private set; }
        public float bulletMass { get; private set; }
        public float bulletVelocity { get; private set; }
        public static BulletInfos bullets;

        public BulletInfo(string name, float caliber, float bulletVelocity, float bulletMass, float positiveCoefficient, FloatCurve penetration)
        {
            this.name = name;
            this.positiveCoefficient = positiveCoefficient;
            this.penetration = penetration;
            this.caliber = caliber;
            this.bulletVelocity = bulletVelocity;
            this.bulletMass = bulletMass;            
        }

        public static void Load()
        {
            try
            {
                bullets = new BulletInfos();
                UrlDir.UrlConfig[] nodes = GameDatabase.Instance.GetConfigs("BULLET");
                for (int i = 0; i < nodes.Length; i++)
                {
                    ConfigNode node = nodes[i].config;
                    FloatCurve penetrationCurve = new FloatCurve();
                    penetrationCurve.Load(node.GetNode("penetration"));

                    bullets.Add(
                        new BulletInfo(
                        node.GetValue("name"),
                        float.Parse(node.GetValue("caliber")),
                        float.Parse(node.GetValue("bulletVelocity")),
                        float.Parse(node.GetValue("bulletMass")),
                        float.Parse(node.GetValue("positiveCoefficient")),
                        penetrationCurve)
                        );
                }
            }
            catch (Exception e)
            {
                Debug.Log("[BDArmory]: Error Loading Bullet Config | " + e.ToString());
            }
        }
    }

    public class BulletInfos : List<BulletInfo>
    {
        public BulletInfo this[string name]
        {
            get { return Find((value) => { return value.name == name; }); }
        }
    }
}