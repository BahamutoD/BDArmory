using System.Collections.Generic;
using System.Linq;
using BDArmory.Core.Extension;
using BDArmory.Misc;
using BDArmory.UI;
using UnityEngine;
using System;
using BDArmory.Core.Utils;

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
        public Vector3 Direction { get; set; }
        public Part ExplosivePart { get; set; }

        public float TimeIndex => Time.time - StartTime;

        public Queue<BlastHitEvent> ExplosionEvents = new Queue<BlastHitEvent>();

        public static List<Part> IgnoreParts = new List<Part>();

        public static List<DestructibleBuilding> IgnoreBuildings = new List<DestructibleBuilding>();

        internal static readonly float ExplosionVelocity = 343f;

        private float particlesMaxEnergy;

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
                if (pe.Current.maxEnergy > particlesMaxEnergy)
                {
                    particlesMaxEnergy = pe.Current.maxEnergy;
                }
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
                    "[BDArmory]:Explosion started tntMass: {" + Power + "}  BlastRadius: {" + Range+ "} StartTime: {"+ StartTime + "}, Duration: {" + MaxTime + "}");
            }
        }

        private void CalculateBlastEvents()
        {  
            var temporalEventList = new List<BlastHitEvent>();

            temporalEventList.AddRange(ProcessingBlastSphere());
      

            //Let's convert this temporal list on a ordered queue
            using (var enuEvents = temporalEventList.OrderBy(e => e.TimeToImpact).GetEnumerator())
            {
                while (enuEvents.MoveNext())
                {
                    if(enuEvents.Current == null) continue;

                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                    {
                        Debug.Log(
                            "[BDArmory]: Enqueueing Blast Event");
                    }

                    ExplosionEvents.Enqueue(enuEvents.Current);
                }
            }
        }


        private List<BlastHitEvent> ProcessingBlastSphere()
        {
            List<BlastHitEvent> result = new List<BlastHitEvent>();

            using (var hitCollidersEnu = Physics.OverlapSphere(Position, Range, 557057).AsEnumerable().GetEnumerator())
            {
                while (hitCollidersEnu.MoveNext())
                {
                    if(hitCollidersEnu.Current == null) continue;

                    Part partHit = hitCollidersEnu.Current.GetComponentInParent<Part>();

                    if (partHit != null && partHit.mass > 0)
                    {
                        ProcessPartEvent(partHit, result);
                    }
                    else
                    {
                         DestructibleBuilding building = hitCollidersEnu.Current.GetComponentInParent<DestructibleBuilding>();

                        if (building != null)
                        {
                            ProcessBuildingEvent(building, result);
                        }
                    }
                }
            }
            return result;
        }

        private void ProcessBuildingEvent(DestructibleBuilding building, List<BlastHitEvent> eventList)
        {
            Ray ray = new Ray(Position, building.transform.position - Position);
            RaycastHit rayHit;
            if (Physics.Raycast(ray, out rayHit, Range, 557057))
            {
                //TODO: Maybe we are not hitting building because we are hitting explosive parts. 

                DestructibleBuilding destructibleBuilding = rayHit.collider.GetComponentInParent<DestructibleBuilding>();

                // Is not a direct hit, because we are hitting a different part
                if (destructibleBuilding != null && destructibleBuilding.Equals(building))
                {
                    var distance = Vector3.Distance(Position, rayHit.point);
                    eventList.Add(new BuildingBlastHitEvent() { Distance = Vector3.Distance(Position, rayHit.point), Building = building, TimeToImpact = distance / ExplosionVelocity });
                }
            }
        }

        private void ProcessPartEvent(Part part, List<BlastHitEvent> eventList)
        {
            RaycastHit hit;
           
            if (IsInLineOfSight(part, ExplosivePart, out hit))
            {
                if (IsAngleAllowed(Direction, hit))
                {
                    var realDistance = Vector3.Distance(Position, hit.point);
                    //Adding damage hit
                    eventList.Add(new PartBlastHitEvent()
                    {
                        Distance = realDistance,
                        Part = part,
                        TimeToImpact = realDistance / ExplosionVelocity,
                        HitPoint = hit.point,
                    });
                }
            }               
        }

        private bool IsAngleAllowed(Vector3 direction, RaycastHit hit)
        {
            if (IsMissile || direction == default(Vector3))
            {
                return true;
            }

            return Vector3.Angle(hit.point - Position, direction) < 100f;
        }
        /// <summary>
        /// This method will calculate if there is valid line of sight between the explosion origin and the specific Part
        /// In order to avoid collisions with the same missile part, It will not take into account those parts beloging to same vessel that contains the explosive part
        /// </summary>
        /// <param name="part"></param>
        /// <param name="explosivePart"></param>
        /// <param name="hit"> out property with the actual hit</param>
        /// <returns></returns>
        private bool IsInLineOfSight(Part part, Part explosivePart, out RaycastHit hit)
        {
            Ray partRay = new Ray(Position, part.transform.position - Position);

            var hits = Physics.RaycastAll(partRay, Range, 557057).AsEnumerable();
            using (var hitsEnu = hits.OrderBy(x => x.distance).GetEnumerator())
            {
                while (hitsEnu.MoveNext())
                {
                    Part partHit = hitsEnu.Current.collider.GetComponentInParent<Part>();
                    if (partHit == null) continue;
                    hit = hitsEnu.Current;

                    if (partHit == part)
                    {
                        return true;
                    }
                    if (partHit != part)
                    {
                        // ignoring collsions against the explosive
                        if (explosivePart != null && partHit.vessel == explosivePart.vessel)
                        {
                            continue;
                        }
                        return false;
                    }
                }
            }

            hit = new RaycastHit();
            return false;
        }

        public void Update()
        {
            LightFx.intensity -= 12 * Time.deltaTime;
            if (TimeIndex > 0.2f)
            {
                IEnumerator<KSPParticleEmitter> pe = PEmitters.AsEnumerable().GetEnumerator();
                while (pe.MoveNext())
                {
                    if (pe.Current == null) continue;
                    pe.Current.emit = false;
                }
                pe.Dispose();
            }

            if (ExplosionEvents.Count == 0 && TimeIndex > Math.Max(MaxTime,particlesMaxEnergy))
            {
                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                {
                    Debug.Log(
                        "[BDArmory]:Explosion Finished");
                }
                Destroy(gameObject);
                return;
            }
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
            //TODO: Review if the damage is sensible after so many changes
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
                      Debug.Log("[BDArmory]:== Explosion hit destructible building! Hitpoints: " +
                              (damageToBuilding).ToString("0.00") + ", total Hitpoints: " + building.Damage);
                }   
            }
        }

        private void ExecutePartBlastEvent(PartBlastHitEvent eventToExecute)
        {
            if (eventToExecute.Part == null ||eventToExecute.Part.Rigidbody == null || eventToExecute.Part.vessel == null || eventToExecute.Part.partInfo == null ) return;

            try
            {
                Part part = eventToExecute.Part;
                Rigidbody rb = part.Rigidbody;
                var realDistance = eventToExecute.Distance;
                var partArea = part.GetArea() * 0.50f;
                BlastInfo blastInfo =
                    BlastPhysicsUtils.CalculatePartAcceleration(partArea,
                        part.vessel.totalMass * 1000f, Power, realDistance);

                var distanceFromHitToPartcenter = Math.Max(1, Vector3.Distance(eventToExecute.HitPoint, part.transform.position));
                var explosiveDamage = blastInfo.Pressure * 3f / distanceFromHitToPartcenter;

                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                {
                    Debug.Log(
                        "[BDArmory]: Executing blast event Part: {" + part.name + "}, " +
                        " Acceleration: {" + blastInfo.Acceleration + "}," +
                        " Distance: {" + realDistance + "}," +
                        " Pressure: {" + blastInfo.Pressure + "}," +
                        " distanceFromHitToPartcenter: {" + distanceFromHitToPartcenter + "}," +
                        " ExplosiveDamage: {" + explosiveDamage + "}," +
                        " Surface: {" + partArea + "}," +
                        " Vessel mass: {" + part.vessel.totalMass * 1000f + "}," +
                        " TimeIndex: {" + TimeIndex + "}," +
                        " TimePlanned: {" + eventToExecute.TimeToImpact + "}," +
                        " NegativePressure: {" + eventToExecute.IsNegativePressure + "}");
                }
                if (!eventToExecute.IsNegativePressure)
                {
                    // Add Reverse Negative Event
                    ExplosionEvents.Enqueue(new PartBlastHitEvent() { Distance = Range - realDistance, Part = part, TimeToImpact = 2 * (Range / ExplosionVelocity) + (Range - realDistance) / ExplosionVelocity, IsNegativePressure = true });

                    AddForceAtPosition(rb,
                        (eventToExecute.HitPoint + part.rb.velocity * TimeIndex - Position).normalized * blastInfo.Acceleration *
                        BDArmorySettings.EXP_IMP_MOD,
                        eventToExecute.HitPoint + part.rb.velocity * TimeIndex);

                    part.AddExplosiveDamage(explosiveDamage, BDArmorySettings.DMG_MULTIPLIER, BDArmorySettings.EXP_HEAT_MOD, Caliber, IsMissile);
                }
                else
                {
                    AddForceAtPosition(rb, (Position - part.transform.position).normalized * blastInfo.Acceleration * BDArmorySettings.EXP_IMP_MOD * 0.25f, part.transform.position);
                }
            }
            catch
            {
                // ignored due to depending on previous event an object could be disposed
            }
        }

        public static void CreateExplosion(Vector3 position, float tntMassEquivalent, string explModelPath, string soundPath, bool isMissile = true, float caliber = 0, Part explosivePart = null, Vector3 direction = default(Vector3))
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
            eFx.Range = BlastPhysicsUtils.CalculateBlastRange(tntMassEquivalent);
            eFx.Position = position;
            eFx.Power = tntMassEquivalent;
            eFx.IsMissile = isMissile;
            eFx.Caliber = caliber;
            eFx.ExplosivePart = explosivePart;
            eFx.Direction = direction;

            if (tntMassEquivalent <= 5)
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

        public static void AddForceAtPosition(Rigidbody rb,Vector3 force,Vector3 position, ForceMode mode = ForceMode.Acceleration)
        {
            //////////////////////////////////////////////////////////
            // Add The force to part
            //////////////////////////////////////////////////////////

            rb.AddForceAtPosition(force, position, mode);
            if (BDArmorySettings.DRAW_DEBUG_LABELS)            
                Debug.Log("[BDArmory]: Force Applied | Explosive : " + Math.Round(force.magnitude, 2));
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

