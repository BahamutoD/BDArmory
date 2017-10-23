using System.Collections.Generic;
using System.Linq;
using BDArmory.Core.Extension;
using BDArmory.Misc;
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

        public Queue<BlastHitEvent> ExplosionEvents = new Queue<BlastHitEvent>();

        public static List<Part> IgnoreParts = new List<Part>();

        public static List<DestructibleBuilding> IgnoreBuildings = new List<DestructibleBuilding>();

        internal static readonly float ExplosionVelocity = 343f;

        public float Heat { get; set; }

        private bool _ready = false;

        private void Start()
        {
            StartTime = Time.time;
            MaxTime = (Range / ExplosionVelocity)*3f;
            CalculateBlastEvents();
            PEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();
            IEnumerator<KSPParticleEmitter> pe = PEmitters.AsEnumerable().GetEnumerator();
            while (pe.MoveNext())
            {
                if (pe.Current == null) continue;
                EffectBehaviour.AddParticleEmitter(pe.Current);
                pe.Current.emit = true;
            }
            pe.Dispose();

            LightFx = gameObject.AddComponent<Light>();
            LightFx.color = Misc.Misc.ParseColor255("255,238,184,255");
            LightFx.intensity = 8;
            LightFx.range = Range*3f;
            LightFx.shadows = LightShadows.None;

            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                Debug.Log(
                    "[BDArmory]:Explosion started BlastRadius: {" + Range+ "} StartTime: {"+ StartTime + "}, Duration: {" + MaxTime + "}");
            }
        }

        private void CalculateBlastEvents()
        {  
            var temporalEventList = new List<BlastHitEvent>();

            temporalEventList.AddRange(ProcessingPartsInRange());
            temporalEventList.AddRange(ProcessingBuildingsInRange());

            //Let's convert this temperal list on a ordered queue
            using (var enuEvents = temporalEventList.OrderBy(e => e.TimeToImpact).GetEnumerator())
            {
                while (enuEvents.MoveNext())
                {
                    if(enuEvents.Current == null) continue;

                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                    {
                        Debug.Log(
                            "[BDArmory]:Enqueueing");
                    }

                    ExplosionEvents.Enqueue(enuEvents.Current);
                }
            }
        }

        private List<BlastHitEvent> ProcessingBuildingsInRange()
        {
            List<BlastHitEvent> result = new List<BlastHitEvent>();
            List<DestructibleBuilding>.Enumerator bldg = BDATargetManager.LoadedBuildings.GetEnumerator();
            while (bldg.MoveNext())
            {
                if (bldg.Current == null) continue;
                var distance = (bldg.Current.transform.position - Position).magnitude;
                if (distance >= Range * 1000) continue;

                Ray ray = new Ray(Position, bldg.Current.transform.position - Position);
                RaycastHit rayHit;
                if (Physics.Raycast(ray, out rayHit, Range, 557057))
                {
                    DestructibleBuilding building = rayHit.collider.GetComponentInParent<DestructibleBuilding>();

                    // Is not a direct hit, because we are hitting a different part
                    if (building != null && building.Equals(bldg.Current))
                    {
                        // use the more accurate distance
                        distance = (rayHit.point - ray.origin).magnitude;
                        result.Add(new BuildingBlastHitEvent() { Distance = distance, Building = bldg.Current, TimeToImpact = distance / ExplosionVelocity });

                    }
                }
                
            }
            bldg.Dispose();

            return result;
        }

        private List<BlastHitEvent> ProcessingPartsInRange()
        {
            List<BlastHitEvent> result = new List<BlastHitEvent>();
            List<Vessel>.Enumerator v = BDATargetManager.LoadedVessels.GetEnumerator();
            while (v.MoveNext())
            {
                if (v.Current == null) continue;
                if (!v.Current.loaded || v.Current.packed || (v.Current.CoM - Position).magnitude >= Range * 4) continue;
                List<Part>.Enumerator p = v.Current.parts.GetEnumerator();
                while (p.MoveNext())
                {
                    if (p.Current == null) continue;
                    var distance = ((p.Current.transform.position + p.Current.Rigidbody.velocity * Time.fixedDeltaTime) - Position).magnitude;
                    if (distance >= Range) continue;

                    result.Add(new PartBlastHitEvent() { Distance = distance, Part = p.Current, TimeToImpact = distance / ExplosionVelocity});
                }
                p.Dispose();
            }
            v.Dispose();

            return result;
        }

        public void Update()
        {

            if (Time.time - StartTime > 0.2f)
            {
                IEnumerator<KSPParticleEmitter> pe = PEmitters.AsEnumerable().GetEnumerator();
                while (pe.MoveNext())
                {
                    if (pe.Current == null) continue;
                    pe.Current.emit = false;
                }
                pe.Dispose();
            }

            if (ExplosionEvents.Count == 0 )
            {
                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                {
                    Debug.Log(
                        "[BDArmory]:Explosion Finished");
                }
                Destroy(gameObject);
                return;
            }

            LightFx.intensity -= (Range*12f/20f) * Time.deltaTime;

           
        }

        public void FixedUpdate()
        {
            while (ExplosionEvents.Count > 0 && ExplosionEvents.Peek().TimeToImpact <= TimeIndex)
            {
                BlastHitEvent eventToExecute = ExplosionEvents.Dequeue();

                var partBlastHitEvent = eventToExecute as PartBlastHitEvent;
                if (partBlastHitEvent != null)
                {
                    ExecutePartBlastEvent(partBlastHitEvent);
                }
                else
                {
                    ExecuteBuildingBlastEvent((BuildingBlastHitEvent) eventToExecute);
                }
            }   
        }

        private void ExecuteBuildingBlastEvent(BuildingBlastHitEvent eventToExecute)
        {
            //buildings
            DestructibleBuilding building = eventToExecute.Building;
            if (building)
            {
                var distanceFactor = Mathf.Clamp01((Range - eventToExecute.Distance) / Range);
                float damageToBuilding = (BDArmorySettings.DMG_MULTIPLIER / 100) * BDArmorySettings.EXP_HEAT_MOD * 0.00645f *
                                         Power * distanceFactor;
                if (damageToBuilding > building.impactMomentumThreshold / 10)
                {
                    building.AddDamage(damageToBuilding);
                }
                if (building.Damage > building.impactMomentumThreshold)
                {
                    building.Demolish();
                }
                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                {
                      Debug.Log("[BDArmory]:== Explosion hit destructible building! Damage: " +
                              (damageToBuilding).ToString("0.00") + ", total Damage: " + building.Damage);
                }   
            }
        }

        private void ExecutePartBlastEvent(PartBlastHitEvent eventToExecute)
        {
            Part part = eventToExecute.Part;
            if (part == null) return;
            if (part.physicalSignificance != Part.PhysicalSignificance.FULL) return;

            // 1. Normal forward explosive event
            Ray partRay = new Ray(Position, part.transform.position - Position);
            RaycastHit rayHit;
            if (Physics.Raycast(partRay, out rayHit, Range, 557057))
            {
                if (!((Vector3.Angle(partRay.direction, transform.forward)) < 90) && !IsMissile) { return; } // clamp explosion to forward of the hitpoint for bullets

                Part partHit = rayHit.collider.GetComponentInParent<Part>();

                // Is a direct hit, because we are hitting the expected part
                if (partHit != null && partHit.Equals(part))
                {
                    // use the more accurate distance

                    var realDistance = (rayHit.point - partRay.origin).magnitude;

                    //Apply damage
                    Rigidbody rb = part.GetComponent<Rigidbody>();

                    if (rb == null || !rb) return;

                    var distanceFactor = Mathf.Clamp01((Range - realDistance) / Range);

                    var force = Power * distanceFactor * BDArmorySettings.EXP_IMP_MOD;

                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                    {
                        Debug.Log(
                            "[BDArmory]:Executing blast event Part: {" + part.name + "}, " +
                            "Distance Factor: {" + distanceFactor + "}," +
                            "TimeIndex: {" + TimeIndex + "}," +
                            " TimePlanned: {" + eventToExecute.TimeToImpact + "}," +
                            " NegativePressure: {" + eventToExecute.IsNegativePressure + "}");

                    }
                    if (!eventToExecute.IsNegativePressure)
                    {
                        rb.AddForceAtPosition(
                            (part.transform.position - Position) * force,
                            eventToExecute.HitPoint, ForceMode.Impulse);
                        if (Heat <= 0) Heat = Power;

                        part.AddDamage_Explosive(Heat, BDArmorySettings.DMG_MULTIPLIER, BDArmorySettings.EXP_HEAT_MOD,
                                                 distanceFactor, Caliber, IsMissile);

                        // 2. Add Reverse Negative Event
                        ExplosionEvents.Enqueue(new PartBlastHitEvent() { Distance = Range - realDistance, Part = part, TimeToImpact = 2 * (Range / ExplosionVelocity) + (Range - realDistance) / ExplosionVelocity, IsNegativePressure = true });
                    }
                    else
                    {
                        rb.AddForceAtPosition(
                            (Position - part.transform.position) * force * 0.125f,
                            part.transform.position, ForceMode.Impulse);
                    }
                }
            }
        }
        

        public static void CreateExplosion(Vector3 position, float radius, float power, float heat, string explModelPath, string soundPath, bool isMissile = true, float caliber = 0)
        {
            var go = GameDatabase.Instance.GetModel(explModelPath);
            var soundClip = GameDatabase.Instance.GetAudioClip(soundPath);

            Quaternion rotation = Quaternion.LookRotation(VectorUtils.GetUpDirection(position));
            GameObject newExplosion = (GameObject) Instantiate(go, position, rotation);
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
            eFx.Heat = heat;

            if (power <= 5)
            {
                eFx.AudioSource.minDistance = 4f;
                eFx.AudioSource.maxDistance = 3000;
                eFx.AudioSource.priority = 9999;
            }
            newExplosion.SetActive(true);
            IEnumerator<KSPParticleEmitter> pe = newExplosion.GetComponentsInChildren<KSPParticleEmitter>().Cast<KSPParticleEmitter>()
                .GetEnumerator();
            while (pe.MoveNext())
            {
                if (pe.Current == null) continue;
                pe.Current.emit = true;

            }
            pe.Dispose();
        }
    }

    public abstract class BlastHitEvent
    {
        public float Distance { get; set; }
        public float TimeToImpact { get; set; }
        public bool IsNegativePressure { get; set; }
    }

    internal class PartBlastHitEvent : BlastHitEvent
    {
        public Part Part { get; set; }
        public Vector3 HitPoint { get; set; }
    }

    internal class BuildingBlastHitEvent : BlastHitEvent
    {
        public DestructibleBuilding Building { get; set; }
    }
}

