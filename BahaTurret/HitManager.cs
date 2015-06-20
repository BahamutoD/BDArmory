using UnityEngine;
using System;
using System.Collections.Generic;


namespace BahaTurret
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class HitManager : MonoBehaviour
    {
        //Hook Registry
        private static readonly List<Action<Part>> hitHooks = new List<Action<Part>> ();
        private static readonly List<Action<ExplosionObject>> explosionHooks = new List<Action<ExplosionObject>> ();
        private static readonly List<Action<BulletObject>> bulletHooks = new List<Action<BulletObject>> ();
        private static readonly List<Action<BahaTurretBullet>> tracerHooks = new List<Action<BahaTurretBullet>> ();
        private static readonly List<Action<BahaTurretBullet>> tracerDestroyHooks = new List<Action<BahaTurretBullet>> ();
        private static readonly List<Func<Guid, bool>> allowDamageHooks = new List<Func<Guid, bool>> ();

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

        public static void RegisterTracerHook(Action<BahaTurretBullet> tracerHook)
        {
            if (!tracerHooks.Contains (tracerHook))
            {
                tracerHooks.Add (tracerHook);
            }
        }

        public static void RegisterTracerDestroyHook(Action<BahaTurretBullet> tracerDestroyHook)
        {
            if (!tracerDestroyHooks.Contains (tracerDestroyHook))
            {
                tracerDestroyHooks.Add (tracerDestroyHook);
            }
        }

        public static void RegisterAllowDamageHook(System.Func<Guid, bool> allowDamageHook)
        {
            if (!allowDamageHooks.Contains (allowDamageHook))
            {
                allowDamageHooks.Add (allowDamageHook);
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

        public static void  FireTracerHooks(BahaTurretBullet tracer)
        {
            foreach (Action<BahaTurretBullet> tracerHook in tracerHooks)
            {
                tracerHook (tracer);
            }
        }

        public static void  FireTracerDestroyHooks(BahaTurretBullet tracer)
        {
            foreach (Action<BahaTurretBullet> tracerHook in tracerDestroyHooks)
            {
                tracerHook (tracer);
            }
        }

        public static bool ShouldAllowDamageHooks(Guid vesselID)
        {
            foreach (Func<Guid, bool> allowDamageHook in allowDamageHooks)
            {
                bool result;
                result = allowDamageHook (vesselID);
                if (!result) {
                    return false;
                }
            }
            return true;
        }
    }

    public class ExplosionObject
    {
        public readonly Vector3 position;
        public readonly float raduis;
        public readonly float power;
        public readonly Vessel sourceVessel;
        public readonly Vector3 direction;
        public readonly string explModelPath;
        public readonly string soundPath;

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
        public readonly Vector3 position;
        public readonly Vector3 normalDirection;
        public readonly bool ricochet;

        public BulletObject(Vector3 positionVal, Vector3 normalDirectionVal, bool ricochetVal)
        {
            position = positionVal;
            normalDirection = normalDirectionVal;
            ricochet = ricochetVal;
        }
    }
}

