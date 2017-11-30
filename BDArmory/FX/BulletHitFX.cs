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
        private int maxConcurrentDecals = 10;

        public static Queue<GameObject> decalsInPool;
        public static Queue<GameObject> decalsActiveInWorld;

        public static ObjectPool decalPool;


        public static void SetupShellPool()
        {
            GameObject templateShell =
                Instantiate(GameDatabase.Instance.GetModel("BDArmory/Models/bulletHit/BDAc_BulletHole"));
            templateShell.SetActive(false);            
            if(decalPool == null) decalPool = ObjectPool.CreateObjectPool(templateShell, 100, true, true);
        }

        public void InitializeDecals()
        {
            bulletHoleDecalPrefab = GameDatabase.Instance.GetModel("BDArmory/Models/bulletHit/BDAc_BulletHole");

            decalsInPool = new Queue<GameObject>();
            decalsActiveInWorld = new Queue<GameObject>();

            for (int i = 0; i < maxConcurrentDecals; i++)
            {
                InstantiateDecal();
            }
        }

        public void InstantiateDecal()
        {
            var spawned = Instantiate(bulletHoleDecalPrefab);
            spawned.transform.SetParent(this.transform);

            decalsInPool.Enqueue(spawned);
            spawned.SetActive(false);
        }

        public static void SpawnDecal(RaycastHit hit,Part hitPart)
        {
            //GameObject decal = GetNextAvailableDecal();
            GameObject decal = decalPool.GetPooledObject();
            if (decal != null && hitPart != null)
            {
                decal.transform.SetParent(hitPart.transform);
                decal.transform.position = hit.point;                
                decal.transform.rotation = Quaternion.FromToRotation(-Vector3.forward, hit.normal);
                decal.transform.rotation *= Quaternion.Euler(0, 90f, 0);

                decal.SetActive(true);

                //decalsActiveInWorld.Enqueue(decal);
            }
        }

        public static GameObject GetNextAvailableDecal()
        {
            if (decalsInPool == null)
            {
                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                {
                    Debug.Log("[BDArmory]: Decals broken");
                }
                return null;
            }

            if (decalsInPool.Count > 0)
                return decalsInPool.Dequeue();

            var oldestActiveDecal = decalsActiveInWorld.Dequeue();
            return oldestActiveDecal;
        }

        void Start()
        {
            //InitializeDecals();
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

        public static void CreateBulletHit(Part hitPart,Vector3 position, RaycastHit hit, Vector3 normalDirection, bool ricochet,float caliber = 0)
        {
            if(decalPool == null) SetupShellPool();
            GameObject go;

            if (caliber <= 30)
            {
                go = GameDatabase.Instance.GetModel("BDArmory/Models/bulletHit/bulletHit");
            }
            else
            {
                go = GameDatabase.Instance.GetModel("BDArmory/FX/PenFX");
            }

            SpawnDecal(hit,hitPart);

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