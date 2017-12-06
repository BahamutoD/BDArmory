using System.Collections.Generic;
using UnityEngine;

namespace BDArmory.FX
{
    public class DecalEmitters : MonoBehaviour
    {
        //private float timeNoFlames;
        //private Vessel LastVesselLoaded = null;
        public static List<GameObject> FlameObjects = new List<GameObject>();
        public List<Vessel> vesselsAllowed = new List<Vessel>();

        public void Start()
        {
         
        }

        public static void AttachFlames(RaycastHit hit, Part hitPart)
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

            foreach (var pe in flameObject.GetComponentsInChildren<KSPParticleEmitter>())
            {
                if (!pe.useWorldSpace) continue;

                var gpe = pe.gameObject.AddComponent<DecalGaplessParticleEmitter>();
                //gpe.Part = hitPart.Target;
                gpe.Emit = true;
            }
        }

    }
}
