using System.Collections.Generic;
using BDArmory.UI;
using BDArmory.Misc;
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

        public static void SetupShellPool(float caliber,float penetrationfactor)
        {
            GameObject templateShell_small;
            GameObject templateShell_large;
            if (caliber >= 90f)
            {
                templateShell_large =
                    Instantiate(GameDatabase.Instance.GetModel("BDArmory/Models/bulletDecal/BulletDecal2"));

                templateShell_large.SetActive(false);
                if (decalPool_large == null) decalPool_large = ObjectPool.CreateObjectPool(templateShell_large, 250, true, true);
            }
            else
            {
                templateShell_small = 
                    Instantiate(GameDatabase.Instance.GetModel("BDArmory/Models/bulletDecal/BulletDecal1"));
                templateShell_small.SetActive(false);
                if (decalPool_small == null) decalPool_small = ObjectPool.CreateObjectPool(templateShell_small, 250, true, true);
            }             
                          
        }         

        public static void SpawnDecal(RaycastHit hit,Part hitPart, float caliber, float pentrationfactor)
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
            if (pentrationfactor > 1)
            {
                GameObject decalBack = decalPool_.GetPooledObject();
                if (decalBack != null && hitPart != null)
                {
                    decalBack.transform.SetParent(hitPart.transform);
                    decalBack.transform.position = hit.point + new Vector3(-0.25f, 0f, 0f);
                    decalBack.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                    decalBack.SetActive(true);
                }
            }
        }


        void Start()
        {
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
            if (Time.time - startTime > 0.03f)
            {
                IEnumerator<KSPParticleEmitter> pe = pEmitters.AsEnumerable().GetEnumerator();
                while (pe.MoveNext())
                {
                    if (pe.Current == null) continue;
                    pe.Current.emit = false;
                }
                pe.Dispose();
            }

            if (Time.time - startTime > 2f)
            {
                Destroy(gameObject);
            }
        }

        public static void CreateBulletHit(Part hitPart,Vector3 position, RaycastHit hit, Vector3 normalDirection,
                                            bool ricochet,float caliber,float penetrationfactor)
        {
            
            if (decalPool_large == null || decalPool_small == null) SetupShellPool(caliber,penetrationfactor);
            GameObject go;

            if (caliber <= 30)
            {
                go = GameDatabase.Instance.GetModel("BDArmory/Models/bulletHit/bulletHit");
            }
            else
            {
                go = GameDatabase.Instance.GetModel("BDArmory/FX/PenFX");
            }

            if(caliber !=0) SpawnDecal(hit,hitPart,caliber,penetrationfactor); //No bullet decals for laser or ricochet

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
                    pe.Current.force = (4.49f*FlightGlobals.getGeeForceAtPosition(position));
                }
                else if (pe.Current.gameObject.name == "smoke")
                {
                    pe.Current.force = (1.49f*FlightGlobals.getGeeForceAtPosition(position));
                }
            }
            pe.Dispose();
        }
    }
}