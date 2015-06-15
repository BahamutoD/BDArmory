using UnityEngine;
using System;
using System.Collections.Generic;


namespace BahaTurret
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class HitManager : MonoBehaviour
    {
        private static readonly List<Action<Part>> hitHooks = new List<Action<Part>> ();
        private static readonly List<Action<ExplosionObject>> explosionHooks = new List<Action<ExplosionObject>> ();
        private static readonly List<Action<BulletObject>> bulletHooks = new List<Action<BulletObject>> ();

        public HitManager ()
        {
            
        }

        public static void RegisterHitHook(Action<Part> hitHook)
        {
            if (!hitHooks.Contains (hitHook))
            {
                hitHooks.Add (hitHook);
            }
        }

        public static void RegisterExplosionHook(Action<ExplosionObject> explosionHook)
        {
            if (!explosionHooks.Contains (explosionHook))
            {
                explosionHooks.Add (explosionHook);
            }
        }

        public static void RegisterBulletHook(Action<BulletObject> bulletHook)
        {
            if (!bulletHooks.Contains (bulletHook))
            {
                bulletHooks.Add (bulletHook);
            }
        }

        public static void FireHitHooks(Part hitPart)
        {
            //Fire hitHooks
            foreach (Action<Part> hitHook in hitHooks) {
                hitHook (hitPart);
            }
        }

        public static void FireExplosionHooks(ExplosionObject explosion)
        {
            foreach (Action<ExplosionObject> explosionHook in explosionHooks)
            {
                explosionHook (explosion);
            }
        }

        public static void FireBulletHooks(BulletObject bullet)
        {
            foreach (Action<BulletObject> bulletHook in bulletHooks)
            {
                bulletHook (bullet);
            }
        }
    }

    public class ExplosionObject
    {
        public Vector3 position;
        public float raduis;
        public float power;
        public Vessel sourceVessel;
        public Vector3 direction;
        public string explModelPath;
        public string soundPath;

        public ExplosionObject(Vector3 positionVal, float radiusVal, float powerVal, Vessel sourceVesselVal, Vector3 directionVal, string explModelPathVal, string soundPathVal)
        {
            position = positionVal;
            raduis = radiusVal;
            power = powerVal;
            sourceVessel = sourceVesselVal;
            direction = directionVal;
            explModelPath = explModelPathVal;
            soundPath = soundPathVal;
        }

    }

    public class BulletObject
    {
        public Vector3 position;
        public Vector3 normalDirection;
        public bool ricochet;

        public BulletObject(Vector3 positionVal, Vector3 normalDirectionVal, bool ricochetVal)
        {
            position = positionVal;
            normalDirection = normalDirectionVal;
            ricochet = ricochetVal;
        }
    }
}

