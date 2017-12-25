using System.Collections.Generic;
using BDArmory.Core;
using BDArmory.Misc;
using BDArmory.Core.Extension;
using UniLinq;
using UnityEngine;

namespace BDArmory.FX
{
    public class BulletHitFX : MonoBehaviour
    {
        KSPParticleEmitter[] pEmitters;
        AudioSource audioSource;
        AudioClip hitSound;
        public Vector3 normal;
        float startTime;
        public bool ricochet;
        public float caliber;

        public GameObject bulletHoleDecalPrefab;
        public static ObjectPool decalPool_small;
        public static ObjectPool decalPool_large;
        public static int maxPoolSize = 200;
        public static Dictionary<Vessel,List<float>> PartsOnFire = new Dictionary<Vessel, List<float>>(); 

        public static int MaxFiresPerVessel = 3;
        public static float FireLifeTimeInSeconds = 5f;
        
        private bool disabled = false;

        public static void SetupShellPool()
        {

            GameObject templateShell_large;
            templateShell_large =
                    Instantiate(GameDatabase.Instance.GetModel("BDArmory/Models/bulletDecal/BulletDecal2"));
            templateShell_large.SetActive(false);
            if (decalPool_large == null)
                decalPool_large = ObjectPool.CreateObjectPool(templateShell_large, maxPoolSize, true, true);

            GameObject templateShell_small;
            templateShell_small =
                Instantiate(GameDatabase.Instance.GetModel("BDArmory/Models/bulletDecal/BulletDecal1"));
            templateShell_small.SetActive(false);
            if (decalPool_small == null)
                decalPool_small = ObjectPool.CreateObjectPool(templateShell_small, maxPoolSize, true, true);
            
        }

        public static void SpawnDecal(RaycastHit hit,Part hitPart, float caliber, float penetrationfactor)
        {
            ObjectPool decalPool_;

            if (caliber >= 90f)
            {
                decalPool_ = decalPool_large;
            }
            else
            {
                decalPool_ = decalPool_small;
            }            
            
            //front hit
            GameObject decalFront = decalPool_.GetPooledObject();
            if (decalFront != null && hitPart != null)
            {
                decalFront.transform.SetParent(hitPart.transform);
                decalFront.transform.position = hit.point + new Vector3(0.25f, 0f, 0f);                               
                decalFront.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                decalFront.SetActive(true);

            }
            //back hole if fully penetrated
            if (penetrationfactor >= 1)
            {
                GameObject decalBack = decalPool_.GetPooledObject();
                if (decalBack != null && hitPart != null)
                {
                    decalBack.transform.SetParent(hitPart.transform);
                    decalBack.transform.position = hit.point + new Vector3(-0.25f, 0f, 0f);
                    decalBack.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                    decalBack.SetActive(true);
                }

                if (CanFlamesBeAttached(hitPart))
                {
                    AttachFlames(hit, hitPart, caliber);
                }
            }
        }
        
        private static bool CanFlamesBeAttached(Part hitPart)
        {
            if (!hitPart.HasFuel())
                return false;            

            if (hitPart.vessel.LandedOrSplashed)
            {
                MaxFiresPerVessel = BDArmorySettings.MAX_FIRES_PER_VESSEL;
                FireLifeTimeInSeconds = BDArmorySettings.FIRELIFETIME_IN_SECONDS;
            }

            if (PartsOnFire.ContainsKey(hitPart.vessel) && PartsOnFire[hitPart.vessel].Count >= MaxFiresPerVessel)
            {
                var firesOnVessel = PartsOnFire[hitPart.vessel];

                firesOnVessel.Where(x => (Time.time - x) > FireLifeTimeInSeconds).Select(x => firesOnVessel.Remove(x));
                return false;
            }

            if (!PartsOnFire.ContainsKey(hitPart.vessel))
            {
                List<float> firesList = new List<float> {Time.time};

                PartsOnFire.Add(hitPart.vessel, firesList);
            }
            else
            {
               PartsOnFire[hitPart.vessel].Add(Time.time);
            }

            return true;
        }

        void Start()
        {
            if (decalPool_large == null || decalPool_small == null)
                SetupShellPool();

            startTime = Time.time;
            pEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();

            IEnumerator<KSPParticleEmitter> pe = pEmitters.AsEnumerable().GetEnumerator();
            while (pe.MoveNext())
            {
                if (pe.Current == null) continue;
                EffectBehaviour.AddParticleEmitter(pe.Current);
            }

            pe.Dispose();

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.minDistance = 1;
            audioSource.maxDistance = 50;
            audioSource.spatialBlend = 1;
            audioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;

            int random = Random.Range(1, 3);

            if (ricochet)
            {
                if (caliber <= 30)
                {
                    string path = "BDArmory/Sounds/ricochet" + random;
                    hitSound = GameDatabase.Instance.GetAudioClip(path);
                }
                else
                {
                    string path = "BDArmory/Sounds/Artillery_Shot";
                    hitSound = GameDatabase.Instance.GetAudioClip(path);
                }
            }
            else
            {
                if (caliber <= 30)
                {
                    string path = "BDArmory/Sounds/bulletHit" + random;
                    hitSound = GameDatabase.Instance.GetAudioClip(path);
                }
                else
                {
                    string path = "BDArmory/Sounds/Artillery_Shot";
                    hitSound = GameDatabase.Instance.GetAudioClip(path);
                }
            }

            audioSource.PlayOneShot(hitSound);
        }

        void Update()
        {
            using (new PerformanceLogger("BulletHitFX.Update"))
            {
                if (!disabled && Time.time - startTime > 0.03f)
                {
                    IEnumerator<KSPParticleEmitter> pe = pEmitters.AsEnumerable().GetEnumerator();
                    while (pe.MoveNext())
                    {
                        if (pe.Current == null) continue;
                        pe.Current.emit = false;
                    }
                    pe.Dispose();
                    disabled = true;
                }
                if (Time.time - startTime > 2f)
                {
                    Destroy(gameObject);
                }
            }
        }

        public static void CreateBulletHit(Part hitPart,Vector3 position, RaycastHit hit, Vector3 normalDirection,
                                            bool ricochet,float caliber,float penetrationfactor)
        {
            
            if (decalPool_large == null || decalPool_small == null)
                SetupShellPool();

            GameObject go;

            if (caliber <= 30)
            {
                go = GameDatabase.Instance.GetModel("BDArmory/Models/bulletHit/bulletHit");
            }
            else
            {
                go = GameDatabase.Instance.GetModel("BDArmory/FX/PenFX");
            }

            if(caliber !=0 && !hitPart.IgnoreDecal())
            {
                SpawnDecal(hit,hitPart,caliber,penetrationfactor); //No bullet decals for laser or ricochet
            }

            GameObject newExplosion =
                (GameObject) Instantiate(go, position, Quaternion.LookRotation(normalDirection));
            newExplosion.SetActive(true);
            newExplosion.AddComponent<BulletHitFX>();
            newExplosion.GetComponent<BulletHitFX>().ricochet = ricochet;
            newExplosion.GetComponent<BulletHitFX>().caliber = caliber;
            IEnumerator<KSPParticleEmitter> pe = newExplosion.GetComponentsInChildren<KSPParticleEmitter>().Cast<KSPParticleEmitter>().GetEnumerator();
            while (pe.MoveNext())
            {
                if (pe.Current == null) continue;
                pe.Current.emit = true;
              
                if (pe.Current.gameObject.name == "sparks")
                {
                    pe.Current.force = (4.49f * FlightGlobals.getGeeForceAtPosition(position));
                }
                else if (pe.Current.gameObject.name == "smoke")
                {
                    pe.Current.force = (1.49f * FlightGlobals.getGeeForceAtPosition(position));
                }
            }
            pe.Dispose();
        }


        public static void AttachFlames(RaycastHit hit, Part hitPart, float caliber)
        {
            var modelUrl = "BDArmory/FX/FlameEffect2/model";

            var flameObject =
                (GameObject)
                Instantiate(
                    GameDatabase.Instance.GetModel(modelUrl),
                    hit.point + new Vector3(0.25f, 0f, 0f),
                    Quaternion.identity);

            flameObject.SetActive(true);
            flameObject.transform.SetParent(hitPart.transform);
            flameObject.AddComponent<DecalEmitterScript>();

            if(hitPart.vessel.LandedOrSplashed && hitPart.GetFireFX() && caliber >= 100f)
            {
                DecalEmitterScript.shrinkRateFlame = 0.25f;
                DecalEmitterScript.shrinkRateSmoke = 0.125f;
            }             

            foreach (var pe in flameObject.GetComponentsInChildren<KSPParticleEmitter>())
            {
                if (!pe.useWorldSpace) continue;
                var gpe = pe.gameObject.AddComponent<DecalGaplessParticleEmitter>();
                gpe.Emit = true;
            }
        }

        public static void AttachFlames(Vector3 contactPoint, Part hitPart)
        {
            if (!CanFlamesBeAttached(hitPart)) return;

            var modelUrl = "BDArmory/FX/FlameEffect2/model";

            var flameObject =
                (GameObject)
                Instantiate(
                    GameDatabase.Instance.GetModel(modelUrl),
                    contactPoint,
                    Quaternion.identity);

            flameObject.SetActive(true);
            flameObject.transform.SetParent(hitPart.transform);
            flameObject.AddComponent<DecalEmitterScript>();

            DecalEmitterScript.shrinkRateFlame = 0.125f;
            DecalEmitterScript.shrinkRateSmoke = 0.125f;

            foreach (var pe in flameObject.GetComponentsInChildren<KSPParticleEmitter>())
            {
                if (!pe.useWorldSpace) continue;
                var gpe = pe.gameObject.AddComponent<DecalGaplessParticleEmitter>();                
                gpe.Emit = true;
            }
        }
    }
}