using System;
using System.Collections;
using System.Linq;
using BDArmory.Armor;
using BDArmory.Core.Extension;
using BDArmory.Core.Module;
using BDArmory.FX;
using BDArmory.Parts;
using BDArmory.Shaders;
using UnityEngine;
using System.Collections.Generic;
using BDArmory.Core;

namespace BDArmory
{
    public class PooledBullet : MonoBehaviour
    {

        #region Declarations

        public BulletInfo bullet;
        public float leftPenetration;

        public enum PooledBulletTypes
        {
            Standard,
            Explosive
        }

        public enum BulletDragTypes
        {
            None,
            AnalyticEstimate,
            NumericalIntegration
        }

        public PooledBulletTypes bulletType;
        public BulletDragTypes dragType;

        public Vessel sourceVessel;
        public Color lightColor = Misc.Misc.ParseColor255("255, 235, 145, 255");
        public Color projectileColor;
        public string bulletTexturePath;
        public bool fadeColor;
        public Color startColor;
        Color currentColor;
        public bool bulletDrop = true;
        public float tracerStartWidth = 1;
        public float tracerEndWidth = 1;
        public float tracerLength = 0;
        public float tracerDeltaFactor = 1.35f;
        public float tracerLuminance = 1;
        public float initialSpeed;

        public Vector3 currPosition;

        //explosive parameters
        public float radius = 30;
        public float tntMass = 0;
        public float blastPower = 8;
        public float blastHeat = -1;
        public float bulletDmgMult = 1;
        public string explModelPath;
        public string explSoundPath;

        Vector3 startPosition;
        public bool airDetonation = false;
        public bool proximityDetonation = false;
        public float detonationRange = 5f;
        public float defaultDetonationRange = 3500f;
        public float maxAirDetonationRange = 3500f;
        float randomWidthScale = 1;
        LineRenderer bulletTrail;
        public float timeToLiveUntil;
        Light lightFlash;
        bool wasInitiated;
        public Vector3 currentVelocity;
        public float bulletMass;
        public float caliber = 1;
        public float bulletVelocity; //muzzle velocity
        public bool explosive = false;
        public float apBulletMod = 0;
        public float ballisticCoefficient;
        public float flightTimeElapsed;
        public static Shader bulletShader;
        public static bool shaderInitialized;
        private float impactVelocity;
        private float dragVelocity;

        public bool hasPenetrated = false;
        public bool hasDetonated = false;
        public bool hasRichocheted = false;

        public int penTicker = 0;

        public Rigidbody rb;
        #endregion

        private Vector3[] linePositions = new Vector3[2];
        void OnEnable()
        {
            startPosition = transform.position;
            initialSpeed = currentVelocity.magnitude; // this is the velocity used for drag estimations (only), use total velocity, not muzzle velocity

            if (!wasInitiated)
            {
                //projectileColor.a = projectileColor.a/2;
                //startColor.a = startColor.a/2;
            }

            projectileColor.a = Mathf.Clamp(projectileColor.a, 0.25f, 1f);
            startColor.a = Mathf.Clamp(startColor.a, 0.25f, 1f);
            currentColor = projectileColor;
            if (fadeColor)
            {
                currentColor = startColor;
            }

            if (lightFlash == null || !gameObject.GetComponent<Light>())
            {
                lightFlash = gameObject.AddOrGetComponent<Light>();
                lightFlash.type = LightType.Point;
                lightFlash.range = 8;
                lightFlash.intensity = 1;
                lightFlash.color = lightColor;
                lightFlash.enabled = true;
            }

            //tracer setup
            if (bulletTrail == null || !gameObject.GetComponent<LineRenderer>())
            {
                bulletTrail = gameObject.AddOrGetComponent<LineRenderer>();
            }

            if (!wasInitiated)
            {
                bulletTrail.positionCount = linePositions.Length;
            }
            linePositions[0] = transform.position;
            linePositions[1] = transform.position;
            bulletTrail.SetPositions(linePositions);


            if (!shaderInitialized)
            {
                shaderInitialized = true;
                bulletShader = BDAShaderLoader.BulletShader;
            }

            if (!wasInitiated)
            {
                bulletTrail.material = new Material(bulletShader);
                randomWidthScale = UnityEngine.Random.Range(0.5f, 1f);
                gameObject.layer = 15;
            }

            bulletTrail.material.mainTexture = GameDatabase.Instance.GetTexture(bulletTexturePath, false);
            bulletTrail.material.SetColor("_TintColor", currentColor);
            bulletTrail.material.SetFloat("_Lum", tracerLuminance);

            tracerStartWidth *= 2f;
            tracerEndWidth *= 2f;

            leftPenetration = 1;
            wasInitiated = true;
            StartCoroutine(FrameDelayedRoutine());
        }

        void OnDestroy()
        {
            StopCoroutine(FrameDelayedRoutine());
        }

        IEnumerator FrameDelayedRoutine()
        {
            yield return new WaitForFixedUpdate();
            lightFlash.enabled = false;
        }

        void OnWillRenderObject()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }
            Camera currentCam = Camera.current;
            if (TargetingCamera.IsTGPCamera(currentCam))
            {
                UpdateWidth(currentCam, 4);
            }
            else
            {
                UpdateWidth(currentCam, 1);
            }
        }

        void FixedUpdate()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            //floating origin and velocity offloading corrections
            if (!FloatingOrigin.Offset.IsZero() || !Krakensbane.GetFrameVelocity().IsZero())
            {
                transform.position -= FloatingOrigin.OffsetNonKrakensbane;
                startPosition -= FloatingOrigin.OffsetNonKrakensbane;
            }

            float distanceFromStart = Vector3.Distance(transform.position, startPosition);

            //calculate flight time for drag purposes
            flightTimeElapsed += Time.fixedDeltaTime;

            //Drag types currently only affect Impactvelocity 
            //Numerical Integration is currently Broken
            switch (dragType)
            {
                case BulletDragTypes.None:
                    break;
                case BulletDragTypes.AnalyticEstimate:
                    CalculateDragAnalyticEstimate();
                    break;
                case BulletDragTypes.NumericalIntegration:
                    CalculateDragNumericalIntegration();
                    break;
            }

            if (tracerLength == 0)
            {
                // visual tracer velocity is relative to the observer
                linePositions[0] = transform.position +
                                   ((currentVelocity - FlightGlobals.ActiveVessel.Velocity()) * tracerDeltaFactor * 0.45f * Time.fixedDeltaTime);
            }
            else
            {
                linePositions[0] = transform.position + ((currentVelocity - FlightGlobals.ActiveVessel.Velocity()).normalized * tracerLength);
            }

            if (fadeColor)
            {
                FadeColor();
                bulletTrail.material.SetColor("_TintColor", currentColor * tracerLuminance);
            }
            linePositions[1] = transform.position;
         
            bulletTrail.SetPositions(linePositions);
            currPosition = transform.position;

            if (Time.time > timeToLiveUntil) //kill bullet when TTL ends
            {
                KillBullet();
                return;
            }

            // bullet collision block
            {
                //reset our hit variables to default state
                hasPenetrated = true;
                hasDetonated = false;
                hasRichocheted = false;
                penTicker = 0;

                float dist = currentVelocity.magnitude * Time.fixedDeltaTime;
                Ray ray = new Ray(currPosition, currentVelocity);                
                var hits = Physics.RaycastAll(ray, dist, 9076737);
                if (hits.Length > 0)
                {
                    var orderedHits = hits.OrderBy(x => x.distance);

                    using (var hitsEnu = orderedHits.GetEnumerator())
                    {
                        while (hitsEnu.MoveNext())
                        {
                            if (!hasPenetrated || hasRichocheted || hasDetonated) break;

                            RaycastHit hit = hitsEnu.Current;
                            Part hitPart = null;
                            KerbalEVA hitEVA = null;

                            try
                            {
                                hitPart = hit.collider.gameObject.GetComponentInParent<Part>();
                                hitEVA = hit.collider.gameObject.GetComponentUpwards<KerbalEVA>();
                            }
                            catch (NullReferenceException)
                            {
                                Debug.Log("[BDArmory]:NullReferenceException for Ballistic Hit");
                                return;
                            }

                            if (hitEVA != null)
                            {
                                hitPart = hitEVA.part;
                                // relative velocity, separate from the below statement, because the hitpart might be assigned only above
                                if (hitPart?.rb != null)
                                    impactVelocity = (currentVelocity * (1 - dragVelocity / currentVelocity.magnitude)
                                        - (hitPart.rb.velocity + Krakensbane.GetFrameVelocityV3f())).magnitude;
                                else
                                    impactVelocity = currentVelocity.magnitude - dragVelocity;
                                ApplyDamage(hitPart, hit, 1, 1);
                                break;
                            }

                            if (hitPart?.vessel == sourceVessel) continue;  //avoid autohit;                     

                            Vector3 impactVector = currentVelocity;
                            if (hitPart?.rb != null)
                                // using relative velocity vector instead of just bullet velocity
                                // since KSP vessels might move faster than bullets
                                impactVector = (currentVelocity * (1 - dragVelocity / currentVelocity.magnitude) 
                                    - (hitPart.rb.velocity + Krakensbane.GetFrameVelocityV3f()));

                            float hitAngle = Vector3.Angle(impactVector, -hit.normal);

                            if (CheckGroundHit(hitPart, hit))
                            {
                                CheckBuildingHit(hit);
                                if (!RicochetScenery(hitAngle))
                                {
                                    ExplosiveDetonation(hitPart, hit, ray);
                                    KillBullet();
                                }
                                else
                                {
                                    DoRicochet(hitPart, hit, hitAngle);
                                }
                                return;
                            }

                            //Standard Pipeline Hitpoints, Armor and Explosives

                            impactVelocity = impactVector.magnitude;
                            float anglemultiplier = (float)Math.Cos(Math.PI * hitAngle / 180.0);

                            float penetrationFactor = CalculateArmorPenetration(hitPart, anglemultiplier, hit);

                            if (penetrationFactor >= 2)
                            {
                                //its not going to bounce if it goes right through
                                hasRichocheted = false;
                            }
                            else
                            {
                                if (RicochetOnPart(hitPart, hit, hitAngle, impactVelocity))
                                    hasRichocheted = true;
                            }

                            if (penetrationFactor > 1 && !hasRichocheted) //fully penetrated continue ballistic damage
                            {
                                hasPenetrated = true;
                                ApplyDamage(hitPart, hit, 1, penetrationFactor);
                                penTicker += 1;
                                CheckPartForExplosion(hitPart);

                                //Explosive bullets that penetrate should explode shortly after
                                //if penetration is very great, they will have moved on 
                                //checking velocity as they would not be able to come out the other side
                                //if (explosive && penetrationFactor < 3 || currentVelocity.magnitude <= 800f)
                                if (explosive)
                                {
                                    //move bullet            
                                    transform.position += (currentVelocity * Time.fixedDeltaTime) / 3;

                                    ExplosiveDetonation(hitPart, hit, ray);
                                    hasDetonated = true;
                                    KillBullet();
                                }
                            }
                            else if (!hasRichocheted) // explosive bullets that get stopped by armor will explode 
                            {
                                //New method

                                if (hitPart.rb != null)
                                {
                                    float forceAverageMagnitude = impactVelocity * impactVelocity *
                                                          (1f / hit.distance) * (bulletMass - tntMass);

                                    float accelerationMagnitude =
                                        forceAverageMagnitude / (hitPart.vessel.GetTotalMass() * 1000);

                                    hitPart?.rb.AddForceAtPosition(impactVector.normalized * accelerationMagnitude, hit.point, ForceMode.Acceleration);

                                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                                        Debug.Log("[BDArmory]: Force Applied " + Math.Round(accelerationMagnitude, 2) + "| Vessel mass in kgs=" + hitPart.vessel.GetTotalMass() * 1000 + "| bullet effective mass =" + (bulletMass - tntMass));
                                }

                                hasPenetrated = false;
                                ApplyDamage(hitPart, hit, 1, penetrationFactor);
                                ExplosiveDetonation(hitPart, hit, ray);
                                hasDetonated = true;
                                KillBullet();
                            }

                            /////////////////////////////////////////////////////////////////////////////////
                            // penetrated after a few ticks
                            /////////////////////////////////////////////////////////////////////////////////
                             
                            //penetrating explosive
                            //richochets

                            if ((penTicker >= 2 && explosive) || (hasRichocheted && explosive))
                            {
                                //detonate
                                ExplosiveDetonation(hitPart, hit, ray, airDetonation);
                                return;
                            }

                            //bullet should not go any further if moving too slowly after hit
                            //smaller caliber rounds would be too deformed to do any further damage
                            if (currentVelocity.magnitude <= 100 && hasPenetrated)
                            {
                                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                                {
                                    Debug.Log("[BDArmory]: Bullet Velocity too low, stopping");
                                }
                                KillBullet();
                                return;
                            }
                            //we need to stop the loop if the bullet has stopped,richochet or detonated
                            if (!hasPenetrated || hasRichocheted || hasDetonated) break;

                        }//end While
                    }//end enumerator
                }//end if hits
            }// end if collision

            //////////////////////////////////////////////////
            //Flak Explosion (air detonation/proximity fuse)
            //////////////////////////////////////////////////

            if (ProximityAirDetonation(distanceFromStart))
            {
                //detonate
                ExplosionFx.CreateExplosion(currPosition, tntMass, explModelPath, explSoundPath, false, caliber, null, currentVelocity);
                KillBullet();

                return;
            }

            if (bulletDrop)
            {
                // Gravity???
                var gravity_ = FlightGlobals.getGeeForceAtPosition(transform.position);
                //var gravity_ = Physics.gravity;
                currentVelocity += gravity_ * TimeWarp.deltaTime;
            }

            //move bullet
            transform.position += currentVelocity * Time.fixedDeltaTime;
        }

        private bool ProximityAirDetonation(float distanceFromStart)
        {
            bool detonate = false;

            if (distanceFromStart <= 500f) return false;

            if (explosive && airDetonation)
            {
                if(distanceFromStart > maxAirDetonationRange || distanceFromStart > defaultDetonationRange)
                {
                    return detonate = true;
                }

                if (proximityDetonation)
                {
                    using (var hitsEnu = Physics.OverlapSphere(transform.position, detonationRange, 557057).AsEnumerable().GetEnumerator())
                    {
                        while (hitsEnu.MoveNext())
                        {
                            if (hitsEnu.Current == null) continue;

                            try
                            {
                                Part partHit = hitsEnu.Current.GetComponentInParent<Part>();
                                if (partHit?.vessel == sourceVessel) continue;

                                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                                    Debug.Log("[BDArmory]: Bullet proximity sphere hit | Distance overlap = " + detonationRange + "| Part name = " + partHit.name);

                                return detonate = true;
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                    }
                }
            }
            return detonate;
          }

        private void ApplyDamage(Part hitPart, RaycastHit hit, float multiplier, float penetrationfactor)
        {
            //hitting a vessel Part
            //No struts, they cause weird bugs :) -BahamutoD
            if (hitPart == null) return;
            if (hitPart.partInfo.name.Contains("Strut")) return;

            if (BDArmorySettings.BULLET_HITS)
            {
                BulletHitFX.CreateBulletHit(hitPart,hit.point, hit, hit.normal, hasRichocheted, caliber,penetrationfactor);
            }

            hitPart.AddBallisticDamage(bulletMass, caliber, multiplier, penetrationfactor,
                                        bulletDmgMult,impactVelocity, explosive);
        }

        private void CalculateDragNumericalIntegration()
        {
            Vector3 dragAcc = currentVelocity * currentVelocity.magnitude *
                              (float)
                              FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(transform.position),
                                  FlightGlobals.getExternalTemperature(transform.position));
            dragAcc *= 0.5f;
            dragAcc /= ballisticCoefficient;

            currentVelocity -= dragAcc * TimeWarp.deltaTime;
            //numerical integration; using Euler is silly, but let's go with it anyway

        }

        private void CalculateDragAnalyticEstimate()
        {
            float analyticDragVelAdjustment = (float)FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPosition), FlightGlobals.getExternalTemperature(currPosition));
            analyticDragVelAdjustment *= flightTimeElapsed * initialSpeed;
            analyticDragVelAdjustment += 2 * ballisticCoefficient;

            analyticDragVelAdjustment = 2 * ballisticCoefficient * initialSpeed / analyticDragVelAdjustment;
            //velocity as a function of time under the assumption of a projectile only acted upon by drag with a constant drag area

            analyticDragVelAdjustment = analyticDragVelAdjustment - initialSpeed;
            //since the above was velocity as a function of time, but we need a difference in drag, subtract the initial velocity
            //the above number should be negative...
            //impactVelocity += analyticDragVelAdjustment; //so add it to the impact velocity

            dragVelocity = analyticDragVelAdjustment;

        }

        private float CalculateArmorPenetration(Part hitPart, float anglemultiplier, RaycastHit hit)
        {
            ///////////////////////////////////////////////////////////////////////                                 
            // Armor Penetration
            ///////////////////////////////////////////////////////////////////////

            float penetration = CalculatePenetration();

            //TODO: Extract bdarmory settings from this values
            float thickness = CalculateThickness(hitPart, anglemultiplier);
            if (thickness < 1) thickness = 1; //prevent divide by zero or other odd behavior

            var penetrationFactor = penetration / thickness;

            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                Debug.Log("[BDArmory]: Armor penetration = " + penetration + " | Thickness = " + thickness);
            }

            bool fullyPenetrated = penetration > thickness; //check whether bullet penetrates the plate

            double massToReduce = Math.PI * Math.Pow((caliber * 0.001) / 2, 2) * (penetration);

            if (fullyPenetrated)
            {
                //lower velocity on penetrating armor plate
                //does not affect low impact parts so that rounds can go through entire tank easily              
                //If round penetrates easily it should not loose much velocity

                //if (penetrationFactor < 2)
                currentVelocity = currentVelocity * (float)Math.Sqrt(thickness / penetration);
                //signifincanly reduce velocity on subsequent penetrations
                if (penTicker > 0) currentVelocity *= 0.55f;

                //updating impact velocity
                //impactVelocity = currentVelocity.magnitude;

                flightTimeElapsed -= Time.fixedDeltaTime;
            }
            else
            {
                massToReduce *= 0.125f;

                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                {
                    Debug.Log("[BDArmory]: Bullet Stopped by Armor");
                }
            }
            hitPart.ReduceArmor(massToReduce);
            return penetrationFactor;
        }

        private float CalculatePenetration()
        {
            float penetration = 0;
            if (caliber > 10) //use the "krupp" penetration formula for anything larger than HMGs
            {
                penetration = (float)(16f * impactVelocity * Math.Sqrt(bulletMass / 1000) / Math.Sqrt(caliber));
            }

            return penetration;
        }

        private static float CalculateThickness(Part hitPart, float anglemultiplier)
        {
            float thickness = (float)hitPart.GetArmorThickness();
            return Mathf.Max(thickness / anglemultiplier, 1);
        }

        private bool ExplosiveDetonation(Part hitPart, RaycastHit hit, Ray ray, bool airDetonation = false)
        {
            ///////////////////////////////////////////////////////////////////////                                 
            // High Explosive Detonation
            ///////////////////////////////////////////////////////////////////////

            if (hitPart == null || hitPart.vessel != sourceVessel)
            {
                //if bullet hits and is HE, detonate and kill bullet
                if (explosive)
                {
                    if (BDArmorySettings.DRAW_DEBUG_LABELS)
                    {
                        Debug.Log("[BDArmory]: Detonation Triggered | penetration: " + hasPenetrated + " penTick: " + penTicker + " airDet: " + airDetonation);
                    }

                    if (airDetonation)
                    {
                        ExplosionFx.CreateExplosion(hit.point, GetExplosivePower(), explModelPath, explSoundPath, false, caliber);
                    }
                    else
                    {
                        ExplosionFx.CreateExplosion(hit.point - (ray.direction * 0.1f),
                                                    GetExplosivePower(),
                                                    explModelPath, explSoundPath, false, caliber, null, direction: currentVelocity);
                    }

                    KillBullet();
                    hasDetonated = true;
                    return true;
                }
            }
            return false;
        }

        private bool CheckGroundHit(Part hitPart, RaycastHit hit)
        {
            if (hitPart == null)
            {
                if (BDArmorySettings.BULLET_HITS)
                {
                    BulletHitFX.CreateBulletHit(hitPart,hit.point, hit, hit.normal, true, caliber,0);
                }

                return true;
            }
            return false;
        }

        private bool CheckBuildingHit(RaycastHit hit)
        {
            DestructibleBuilding building = null;
            try
            {
                building = hit.collider.gameObject.GetComponentUpwards<DestructibleBuilding>();
                building.damageDecay = 600f;
            }
            catch (Exception) { }

            if (building != null && building.IsIntact)
            {
                float damageToBuilding = ((0.5f * (bulletMass * Mathf.Pow(currentVelocity.magnitude, 2)))
                            * (BDArmorySettings.DMG_MULTIPLIER / 100) * bulletDmgMult
                            * 1e-4f);
                damageToBuilding /= 8f;
                building.AddDamage(damageToBuilding);
                if (building.Damage > building.impactMomentumThreshold * 150)
                {
                    building.Demolish();
                }
                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                    Debug.Log("[BDArmory]: Ballistic hit destructible building! Hitpoints Applied: " + Mathf.Round(damageToBuilding) +
                             ", Building Damage : " + Mathf.Round(building.Damage) +
                             " Building Threshold : " + building.impactMomentumThreshold);


                return true;
            }
            return false;
        }

        public void UpdateWidth(Camera c, float resizeFactor)
        {
            if (c == null)
            {
                return;
            }
            if (bulletTrail == null)
            {
                return;
            }
            if (!gameObject.activeInHierarchy)
            {
                return;
            }


            float fov = c.fieldOfView;
            float factor = (fov / 60) * resizeFactor *
                           Mathf.Clamp(Vector3.Distance(transform.position, c.transform.position), 0, 3000) / 50;
            float width1 = tracerStartWidth * factor * randomWidthScale;
            float width2 = tracerEndWidth * factor * randomWidthScale;

            bulletTrail.SetWidth(width1, width2);
        }

        void KillBullet()
        {
            gameObject.SetActive(false);
        }

        void FadeColor()
        {
            Vector4 endColorV = new Vector4(projectileColor.r, projectileColor.g, projectileColor.b, projectileColor.a);
            float delta = TimeWarp.deltaTime;
            Vector4 finalColorV = Vector4.MoveTowards(currentColor, endColorV, delta);
            currentColor = new Color(finalColorV.x, finalColorV.y, finalColorV.z, Mathf.Clamp(finalColorV.w, 0.25f, 1f));
        }

        bool RicochetOnPart(Part p, RaycastHit hit, float angleFromNormal, float impactVel)
        {
            float hitTolerance = p.crashTolerance;
            //15 degrees should virtually guarantee a ricochet, but 75 degrees should nearly always be fine
            float chance = (((angleFromNormal - 5) / 75) * (hitTolerance / 150)) * 100 / Mathf.Clamp01(impactVel / 600);
            float random = UnityEngine.Random.Range(0f, 100f);
            if (BDArmorySettings.DRAW_DEBUG_LABELS) Debug.Log("[BDArmory]: Ricochet chance: " + chance);
            if (random < chance)
            {
                DoRicochet(p, hit, angleFromNormal);
                return true;
            }
            else
            {
                return false;
            }
        }

        bool RicochetScenery(float hitAngle)
        {
            float reflectRandom = UnityEngine.Random.Range(-75f, 90f);
            if (reflectRandom > 90 - hitAngle && caliber <= 30f)
            {
                return true;
            }

            return false;

        }

        public void DoRicochet(Part p, RaycastHit hit, float hitAngle)
        {
            //ricochet            
            if (BDArmorySettings.BULLET_HITS)
            {
                BulletHitFX.CreateBulletHit(p,hit.point, hit, hit.normal, true,caliber,0);
            }

            tracerStartWidth /= 2;
            tracerEndWidth /= 2;

            transform.position = hit.point;
            currentVelocity = Vector3.Reflect(currentVelocity, hit.normal);
            currentVelocity = (hitAngle / 150) * currentVelocity * 0.65f;

            Vector3 randomDirection = UnityEngine.Random.rotation * Vector3.one;

            currentVelocity = Vector3.RotateTowards(currentVelocity, randomDirection,
                UnityEngine.Random.Range(0f, 5f) * Mathf.Deg2Rad, 0);

        }

        public void CheckPartForExplosion(Part hitPart)
        {
            if (!hitPart.FindModuleImplementing<HitpointTracker>()) return;

            switch (hitPart.GetExplodeMode())
            {
                case "Always":
                    CreateExplosion(hitPart);
                    break;
                case "Dynamic":
                    float probability = CalculateExplosionProbability(hitPart);
                    if (probability >= 3)
                        CreateExplosion(hitPart);
                    break;
                case "Never":
                    break;
            }
        }

        private float CalculateExplosionProbability(Part part)
        {

            ///////////////////////////////////////////////////////////////
            float probability = 0;
            float fuelPct = 0;            
            for (int i = 0; i < part.Resources.Count; i++)
            {
                PartResource current = part.Resources[i];
                switch (current.resourceName)
                {
                    case "LiquidFuel":
                        fuelPct = (float)(current.amount / current.maxAmount);
                        break;
                        //case "Oxidizer":
                        //   probability += (float) (current.amount/current.maxAmount);
                        //    break;
                }
            }

            if (fuelPct > 0 && fuelPct <= 0.60f)
            {
                probability = Core.Utils.BDAMath.RangedProbability(new[] { 50f, 25f, 20f, 5f });
            }
            else
            {
                probability = Core.Utils.BDAMath.RangedProbability(new[] { 50f, 25f, 20f, 2f });
            }

            if (fuelPct == 1f || fuelPct == 0f)
                probability = 0f;

            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                Debug.Log("[BDArmory]: Explosive Probablitliy " + probability);
            }

            return probability;

        }    

        public void CreateExplosion(Part part)
        {
            float explodeScale = 0;
            IEnumerator<PartResource> resources = part.Resources.GetEnumerator();
            while (resources.MoveNext())
            {
                if (resources.Current == null) continue;
                switch (resources.Current.resourceName)
                {
                    case "LiquidFuel":
                        explodeScale += (float)resources.Current.amount;
                        break;
                    case "Oxidizer":
                        explodeScale += (float)resources.Current.amount;
                        break;
                }
            }

            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                Debug.Log("[BDArmory]: Penetration of bullet detonated fuel!");
            }

            resources.Dispose();

            explodeScale /= 100;
            part.explosionPotential = explodeScale;


            PartExploderSystem.AddPartToExplode(part);        
        }

        private float GetExplosivePower()
        {
            return tntMass > 0 ? tntMass : blastPower;
        }

    }
}