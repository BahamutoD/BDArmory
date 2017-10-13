using System.Collections.Generic;
using System.Linq;
using BDArmory.Core.Extension;
using BDArmory.Misc;
using BDArmory.Parts;
using BDArmory.UI;
using UnityEngine;

namespace BDArmory.FX
{
    public class ExplosionFx : MonoBehaviour
    {
        public KSPParticleEmitter[] PEmitters { get; set; }

        public Light LightFx { get; set; }

        public float StartTime { get; set; }

        public AudioClip ExSound { get; set; }
        public AudioSource AudioSource { get; set; }
        private float MaxTime { get; set; }
        public float Range { get; set; }
        public float Caliber { get; set; }

        public bool IsMissile { get; set; }

        public float Power { get; set; }

        public Vector3 Position { get; set; }

        public float TimeDetonation { get; set; }
        public float TimeIndex => Time.time - StartTime;

        public List<BlastHitEvent> ExplosionEvents = new List<BlastHitEvent>();
        public static List<Part> IgnoreParts = new List<Part>();

        public static List<DestructibleBuilding> IgnoreBuildings = new List<DestructibleBuilding>();

        internal static readonly float ExplosionVelocity = 400f;

        private bool SoundPlayed { get; set; }

        private void Start()
        {
            StartTime = Time.time;
            MaxTime = Range / ExplosionVelocity;
            CalculateBlastEvents();

            PEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();
            IEnumerator<KSPParticleEmitter> pe = PEmitters.AsEnumerable().GetEnumerator();
            while (pe.MoveNext())
            {
                if (pe.Current == null) continue;
               EffectBehaviour.AddParticleEmitter(pe.Current);
                
                pe.Current.emit = true;
                pe.Current.maxEnergy = MaxTime;
            }
            pe.Dispose();

            LightFx = gameObject.AddComponent<Light>();
            LightFx.color = Misc.Misc.ParseColor255("255,238,184,255");
            LightFx.intensity = 8;
            LightFx.range = Range*3f;
            LightFx.shadows = LightShadows.None;

          
        }

        private void CalculateBlastEvents()
        {  
            ProcessingPartsInRange();
            ProcessingBuildingsInRange();
        }

        private void ProcessingBuildingsInRange()
        {
            List<DestructibleBuilding>.Enumerator bldg = BDATargetManager.LoadedBuildings.GetEnumerator();
            while (bldg.MoveNext())
            {
                if (bldg.Current == null) continue;
                var distance = (bldg.Current.transform.position - Position).magnitude;
                if (distance >= Range * 1000) continue;

                //Is Direct Hit?
                bool isDirectHit = false;

                Ray partRay = new Ray(Position, bldg.Current.transform.position - Position);
                RaycastHit rayHit;
                if (Physics.Raycast(partRay, out rayHit, Range, 557057))
                {
                    DestructibleBuilding building = rayHit.collider.GetComponentInParent<DestructibleBuilding>();

                    // Is not a direct hit, because we are hitting a different part
                    if (building != null && !building.Equals(bldg.Current))
                    {
                        // use the more accurate distance
                        distance = (rayHit.point - partRay.origin).magnitude;
                        isDirectHit = true;
                    }
                }
                ExplosionEvents.Add(new BuildingBlastHitEvent() { Distance = distance, IsDirectHit = isDirectHit, Building = bldg.Current, TimeToImpact = distance / ExplosionVelocity });
            }
            bldg.Dispose();
        }

        private void ProcessingPartsInRange()
        {
            List<Vessel>.Enumerator v = BDATargetManager.LoadedVessels.GetEnumerator();
            while (v.MoveNext())
            {
                if (v.Current == null) continue;
                if (!v.Current.loaded || v.Current.packed || (v.Current.CoM - Position).magnitude >= Range * 4) continue;
                List<Part>.Enumerator p = v.Current.parts.GetEnumerator();
                while (p.MoveNext())
                {
                    if (p.Current == null) continue;
                    var distance = (p.Current.transform.position - Position).magnitude;
                    if (distance >= Range) continue;

                    // 1. Normal forward explosive event

                        //Is Direct Hit?
                        bool isDirectHit = false;

                        Ray partRay = new Ray(Position, p.Current.transform.position - Position);
                        RaycastHit rayHit;
                        if (Physics.Raycast(partRay, out rayHit, Range, 557057))
                        {
                            Part part = rayHit.collider.GetComponentInParent<Part>();

                            // Is not a direct hit, because we are hitting a different part
                            if (part != null && !part.Equals(p.Current))
                            {
                                // use the more accurate distance
                                distance = (rayHit.point - partRay.origin).magnitude;
                                isDirectHit = true;
                            }
                        }
                    ExplosionEvents.Add(new PartBlastHitEvent() {Distance = distance,IsDirectHit = isDirectHit,Part = p.Current, TimeToImpact = distance/ExplosionVelocity});

                    // 2. Add Reverse Sucking Event
                    ExplosionEvents.Add(new PartBlastHitEvent() { Distance = Range - distance, IsDirectHit = false, Part = p.Current, TimeToImpact = (2*Range-distance)/ExplosionVelocity, IsNegativePressure = true});

                }
                p.Dispose();
            }
            v.Dispose();
        }

        public void Update()
        {
            LightFx.intensity = Mathf.Clamp01(MaxTime * 0.25f - TimeIndex);

            if (!SoundPlayed)
            {
                if (Vector3.Distance(FlightGlobals.ActiveVessel.CoM, Position) > TimeIndex * ExplosionVelocity)
                {
                    AudioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
                    AudioSource.PlayOneShot(ExSound);
                    SoundPlayed = true;
                }
            }
          
            if (!(TimeIndex > MaxTime)) return;

            IEnumerator<KSPParticleEmitter> pe = PEmitters.AsEnumerable().GetEnumerator();
            while (pe.MoveNext())
            {
                if (pe.Current == null) continue;
                pe.Current.emit = false;
            }
            pe.Dispose();

            Destroy(gameObject);
        }

        public void FixedUpdate()
        {
            
        }

        public static void CreateExplosion(Vector3 position, float radius, float power, float heat, string explModelPath, string soundPath, bool isMissile, float caliber = 0)
        {
            var go = GameDatabase.Instance.GetModel(explModelPath);
            var soundClip = GameDatabase.Instance.GetAudioClip(soundPath);

            Quaternion rotation = Quaternion.LookRotation(VectorUtils.GetUpDirection(position));
            GameObject newExplosion = (GameObject) Instantiate(go, position, rotation);
            newExplosion.SetActive(true);
            ExplosionFx eFx = newExplosion.AddComponent<ExplosionFx>();
            eFx.ExSound = soundClip;
            eFx.AudioSource = newExplosion.AddComponent<AudioSource>();
            eFx.AudioSource.minDistance = 200;
            eFx.AudioSource.maxDistance = 5500;
            eFx.AudioSource.spatialBlend = 1;
            eFx.Range = radius;
            eFx.Position = position;
            eFx.Power = power;
            eFx.IsMissile = isMissile;
            eFx.Caliber = caliber;

            if (power <= 5)
            {
                eFx.AudioSource.minDistance = 4f;
                eFx.AudioSource.maxDistance = 3000;
                eFx.AudioSource.priority = 9999;
            }

            //IEnumerator<KSPParticleEmitter> pe = newExplosion.GetComponentsInChildren<KSPParticleEmitter>().Cast<KSPParticleEmitter>()
            //    .GetEnumerator();
            //while (pe.MoveNext())
            //{
            //    if (pe.Current == null) continue;
            //    pe.Current.emit = true;
                
            //}
            //pe.Dispose();

            //DoExplosionDamage(position, power, heat, radius, sourceVessel,isMissile, eFx, caliber);
        }

  //      public static void DoExplosionDamage(Vector3 position, float power, float heat, float maxDistance, Vessel sourceVessel, bool isMissile, ExplosionFx expInstance, float caliber = 0)
		//{
		//	if(BDArmorySettings.DRAW_DEBUG_LABELS) Debug.Log("[BDArmory]:======= Doing explosion sphere =========");
		//	IgnoreParts.Clear();
		//	IgnoreBuildings.Clear();

          
  //          // this replaces 2 passes through the vessels list and 2 passes through the parts lists with a single pass, and eliminates boxing and unboxing performed by linq and foreach loops.  Should be faster, with better gc
  //          List<Vessel>.Enumerator v = BDATargetManager.LoadedVessels.GetEnumerator();
		//    while (v.MoveNext())
		//    {
		//        if (v.Current == null) continue;
  //              if (!v.Current.loaded || v.Current.packed || (v.Current.CoM - position).magnitude >= maxDistance * 4) continue;
		//        List<Part>.Enumerator p = v.Current.parts.GetEnumerator();
		//        while (p.MoveNext())
		//        {
		//            if (p.Current == null) continue;
		//            var distance = (p.Current.transform.position - position).magnitude;

  //                  if ((p.Current.transform.position - position).magnitude >= maxDistance) continue;

  //                  expInstance.ExplosionEvents.Add(new PartExplosionEvent()
  //                  {
  //                      Part = p.Current,
  //                      TimeToImpact = (distance / ExplosionVelocity)
  //                  });

		//        }
  //              p.Dispose();
		//    }
  //          v.Dispose();

		//    List<DestructibleBuilding>.Enumerator bldg = BDATargetManager.LoadedBuildings.GetEnumerator();
		//	while(bldg.MoveNext())
		//	{
		//		if(bldg.Current == null) continue;
		//		if((bldg.Current.transform.position - position).magnitude < maxDistance * 1000)
		//		{
		//			DoExplosionRay(new Ray(position, bldg.Current.transform.position - position), power, heat, maxDistance, isMissile, ref IgnoreParts, ref IgnoreBuildings,null,caliber);
		//		}
		//	}
  //          bldg.Dispose();
		//}

        public static void DoExplosionRay(Ray ray, float power, float heat, float maxDistance,bool isMissile,
                                          ref List<Part> ignoreParts, ref List<DestructibleBuilding> ignoreBldgs,
                                          Vessel sourceVessel = null,
                                          float caliber = 0)
        {
            RaycastHit rayHit;
            if (Physics.Raycast(ray, out rayHit, maxDistance, 557057))
            {
                float sqrDist = (rayHit.point - ray.origin).sqrMagnitude;
                float sqrMaxDist = maxDistance * maxDistance;
                float distanceFactor = Mathf.Clamp01((sqrMaxDist - sqrDist) / sqrMaxDist);
                
                //parts
                Part part = rayHit.collider.GetComponentInParent<Part>();
               
                if (part != null && part)
                {
                    //bool hasArmor_ = part.HasArmor();
                    //double armorMass_ = part.GetArmorMass();

                    Vessel missileSource = null;
                    if (sourceVessel != null)
                    {
                        MissileBase ml = part.FindModuleImplementing<MissileBase>();
                        if (ml) missileSource = ml.SourceVessel;                       
                    }

                    if (!ignoreParts.Contains(part) && part.physicalSignificance == Part.PhysicalSignificance.FULL &&
                        (!sourceVessel || sourceVessel != missileSource))
                    {
                        ignoreParts.Add(part);
                        Rigidbody rb = part.GetComponent<Rigidbody>();
                        if (rb)
                        {
                            //Adding Force to hit - blast pressure
                            rb.AddForceAtPosition(ray.direction * power * distanceFactor * BDArmorySettings.EXP_IMP_MOD,
                                rayHit.point, ForceMode.Impulse);
                        }

                        if (heat < 0) heat = power;

                        #region Code moved to partextensions
                        //////////////////////////////////////////////////////////
                        //Damage pipeline for missiles then explosive bullets
                        //////////////////////////////////////////////////////////

                        //float heatDamage = (BDArmorySettings.DMG_MULTIPLIER / 100) *
                        //                   BDArmorySettings.EXP_HEAT_MOD * 
                        //                   heat *
                        //                   (distanceFactor / part.crashTolerance);
                        //float armorReduction = 0;

                        //////////////////////////////////////////////////////////
                        //Missiles
                        //////////////////////////////////////////////////////////
                        //if (isMissile)
                        //{
                        //    if (hasArmor_)
                        //    {
                        //        //TODO: figure out how much to nerf armor for missile hit                                
                        //        armorReduction = heatDamage / 8;
                        //    }                            
                        //}

                        //////////////////////////////////////////////////////////
                        //Explosive Bullets
                        //////////////////////////////////////////////////////////

                        //if (!isMissile)
                        //{
                        //    if (hasArmor_)
                        //    {
                        //        if(caliber < 50) heatDamage = heatDamage * heat / 100; //penalty for low-mid caliber HE rounds hitting armor panels
                        //        armorReduction = heatDamage / 16;
                        //    }

                        //}

                        //////////////////////////////////////////////////////////

                        //float excessHeat = Mathf.Max(0, (float)(part.temperature + heatDamage - part.maxTemp));

                        //if (excessHeat > 0 && part.parent)
                        //   {
                        //      part.parent.AddDamage(excessHeat);
                        //   }
                        #endregion

                        //////////////////////////////////////////////////////////
                        // Apply Damage
                        //////////////////////////////////////////////////////////

                        //part.AddDamage(heatDamage,caliber,isMissile);
                        part.AddDamage_Explosive(heat, BDArmorySettings.DMG_MULTIPLIER, BDArmorySettings.EXP_HEAT_MOD, distanceFactor, caliber, isMissile);

                        #region code moved to partextensions
                        //if (armorReduction != 0) part.ReduceArmor(armorReduction);

                        //if (BDArmorySettings.DRAW_DEBUG_LABELS)
                        //    Debug.Log("[BDArmory]:====== Explosion ray hit part! Damage: ");
                        #endregion

                        return;
                    }
                }

                //buildings
                DestructibleBuilding building = rayHit.collider.GetComponentInParent<DestructibleBuilding>();
                if (building && !ignoreBldgs.Contains(building))
                {
                    ignoreBldgs.Add(building);
                    float damageToBuilding = (BDArmorySettings.DMG_MULTIPLIER / 100) * BDArmorySettings.EXP_HEAT_MOD * 0.00645f *
                                             power * distanceFactor;
                    if (damageToBuilding > building.impactMomentumThreshold / 10) building.AddDamage(damageToBuilding);
                    if (building.Damage > building.impactMomentumThreshold) building.Demolish();
                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                        Debug.Log("[BDArmory]:== Explosion hit destructible building! Damage: " +
                                  (damageToBuilding).ToString("0.00") + ", total Damage: " + building.Damage);
                }
            }
        }
    }

    public abstract class BlastHitEvent
    {
        public float Distance { get; set; }
        public float TimeToImpact { get; set; }
        public bool IsDirectHit { get; set; }
        public bool IsNegativePressure { get; set; }
    }

    internal class PartBlastHitEvent : BlastHitEvent
    {
        public Part Part { get; set; }    
    }

    internal class BuildingBlastHitEvent : BlastHitEvent
    {
        public DestructibleBuilding Building { get; set; }
    }
}

