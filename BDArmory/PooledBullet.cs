using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BDArmory.Armor;
using BDArmory.Core.Extension;
using BDArmory.FX;
using BDArmory.Parts;
using BDArmory.Shaders;
using BDArmory.UI;
using UnityEngine;

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

        public Vector3 prevPosition;
        public Vector3 currPosition;

        //explosive parameters
        public float radius = 30;
        public float blastPower = 8;
        public float blastHeat = -1;
        public float bulletDmgMult = 1;
        public string explModelPath;
        public string explSoundPath;        

        Vector3 startPosition;
        public bool airDetonation = false;
        public float detonationRange = 3500;
        float randomWidthScale = 1;
        LineRenderer bulletTrail;
        Vector3 sourceOriginalV;
        public float maxDistance;
        Light lightFlash;
        bool wasInitiated;
        public Vector3 currentVelocity;
        public float mass = 5.40133e-5f;
        public float caliber = 0;
        public float bulletVelocity; //muzzle velocity
        public bool explosive = false;
        public float apBulletDmg = 0;
        public float ballisticCoefficient;
        public float flightTimeElapsed;
        bool collisionEnabled;
        public static Shader bulletShader;
        public static bool shaderInitialized;
        private float impactVelocity;

        public bool hasPenetrated = false;
        public int penTicker = 0;
        #endregion

        void OnEnable()
        {
            startPosition = transform.position;
            collisionEnabled = false;

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

            prevPosition = gameObject.transform.position;

            sourceOriginalV = sourceVessel.Velocity();

            if (!lightFlash)
            {
                lightFlash = gameObject.AddComponent<Light>();
                lightFlash.type = LightType.Point;
                lightFlash.range = 8;
                lightFlash.intensity = 1;
            }
            lightFlash.color = lightColor;
            lightFlash.enabled = true;


            //tracer setup
            if (!bulletTrail)
            {
                bulletTrail = gameObject.AddComponent<LineRenderer>();
            }
            if (!wasInitiated)
            {
                bulletTrail.SetVertexCount(2);
            }
            bulletTrail.SetPosition(0, transform.position);
            bulletTrail.SetPosition(1, transform.position);

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

        IEnumerator FrameDelayedRoutine()
        {
            yield return new WaitForFixedUpdate();
            lightFlash.enabled = false;
            collisionEnabled = true;
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

        void Update()
        {
            float distanceFromStart = Vector3.Distance(transform.position, startPosition);
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            flightTimeElapsed += TimeWarp.deltaTime; //calculate flight time for drag purposes

            if (bulletDrop && FlightGlobals.RefFrameIsRotating)
            {
                currentVelocity += FlightGlobals.getGeeForceAtPosition(transform.position)*TimeWarp.deltaTime;
            }

            CalculateDragNumericalIntegration();

            if (tracerLength == 0)
            {
                bulletTrail.SetPosition(0,
                    transform.position +
                    ((currentVelocity*tracerDeltaFactor*TimeWarp.deltaTime / TimeWarp.CurrentRate) -
                    (FlightGlobals.ActiveVessel.Velocity() * TimeWarp.deltaTime)) * 0.25);
            }
            else
            {
                bulletTrail.SetPosition(0,
                    transform.position + ((currentVelocity - sourceOriginalV).normalized*tracerLength));
            }

            if (fadeColor)
            {
                FadeColor();
                bulletTrail.material.SetColor("_TintColor", currentColor*tracerLuminance);
            }

            bulletTrail.SetPosition(1, transform.position);

            currPosition = gameObject.transform.position;

            if (distanceFromStart > maxDistance)//kill bullet if it goes past the max allowed distance
            {
                KillBullet();
                return;
            }

            if (collisionEnabled)
            {
                float dist = currentVelocity.magnitude * TimeWarp.deltaTime;
                Ray ray = new Ray(currPosition, currPosition - prevPosition);
                var hits = Physics.RaycastAll(ray, dist, 557057);        
                if (hits.Length > 0)
                {
                    var orderedHits = hits.OrderBy(x => x.distance);

                    using (var hitsEnu = orderedHits.GetEnumerator())
                    {
                        while (hitsEnu.MoveNext())
                        {
                            RaycastHit hit = hitsEnu.Current;
                            Part hitPart = null;

                            try
                            {
                                hitPart = hit.collider.gameObject.GetComponentInParent<Part>();
                            }
                            catch (NullReferenceException)
                            {
                                Debug.Log("[BDArmory]:NullReferenceException");
                                return;
                            }

                            if (hitPart?.vessel == sourceVessel)
                            {
                                //avoid autohit;
                                return;
                            }

                            if (CheckGroundHit(hitPart, hit))
                            {
                                ExplosiveDetonation(hitPart, hit, ray);
                                KillBullet();
                                return;
                            }

                            if (CheckBuildingHit(hit))
                            {
                                ExplosiveDetonation(hitPart, hit, ray);
                                KillBullet();
                                return;
                            }

                            //Standard Pipeline Damage, Armor and Explosives
                            float hitAngle = Vector3.Angle(currentVelocity, -hit.normal);
                            impactVelocity = currentVelocity.magnitude;
                            float anglemultiplier = (float)Math.Cos(3.14 * hitAngle / 180.0);

                            CalculateDragAnalyticEstimate();

                            var penetrationFactor = CalculateArmorPenetration(hitPart, anglemultiplier, hit);

                            if (penetrationFactor > 1) //fully penetrated, not explosive, continue ballistic damage
                            {
                                ApplyDamage(hitPart, 1, penetrationFactor, caliber);
                                hasPenetrated = true;
                                penTicker += 1;
                            }
                            else
                            {
                                ApplyDamage(hitPart, penetrationFactor * 0.1f, penetrationFactor, caliber);
                                ExplosiveDetonation(hitPart, hit, ray); // explosive bullets that get stopped by armor will explode                        
                                hasPenetrated = false;
                                KillBullet();
                            }

                            ///////////////////////////////////////////////////////////////////////
                            //Flak Explosion (air detonation/proximity fuse) or penetrated after a few ticks
                            ///////////////////////////////////////////////////////////////////////

                            if ((explosive && airDetonation && distanceFromStart > detonationRange) || (penTicker >= 2 && explosive))
                            {
                                //detonate
                                ExplosionFX.CreateExplosion(transform.position, radius, blastPower, blastHeat, sourceVessel,
                                    currentVelocity.normalized, explModelPath, explSoundPath, false, caliber);
                                KillBullet();
                                return;
                            }

                            //bullet should not go any further if moving too slowly after hit
                            //smaller caliber rounds would be too deformed to do any further damage
                            if (currentVelocity.magnitude <= 150 || (caliber < 30 && hasPenetrated))
                            {
                                KillBullet();
                                return;
                            }
                        }
                    } 
                }
            }
   
            ///////////////////////////////////////////////////////////////////////
            //Bullet Translation
            ///////////////////////////////////////////////////////////////////////
            
            prevPosition = currPosition;
            //move bullet            
            transform.position += currentVelocity * Time.deltaTime;
        }

        private void ApplyDamage(Part hitPart, float multiplier, float penetrationfactor,float caliber = 0)
        {
            //hitting a vessel Part
            //No struts, they cause weird bugs :) -BahamutoD
            if(hitPart == null) return;
            if (hitPart.partInfo.name.Contains("Strut")) return;
            // if (hitPart.HasArmor()) return; - Why would we not do damage if armor??
            
            //Basic kinetic formula. 
            double heatDamage = ((0.5f * (mass * Math.Pow(impactVelocity, 2))) *
                                    BDArmorySettings.DMG_MULTIPLIER
                                    * 0.0025); //dmg mult is 100 baseline, so this constant adjusted accordingly

            //Now, we know exactly how well the bullet was stopped by the armor. 
            //This value will be below 1 when it is stopped by the armor.
            //That means that we should not apply all the damage to the part that stopped by the bullet
            //Also we are not considering hear the angle of penetration , because we already did on the armor penetration calculations.

            heatDamage *= multiplier;

            if (BDArmorySettings.DRAW_DEBUG_LABELS)
            {
                Debug.Log("[BDArmory]: Damage Applied: " + (int) heatDamage);
                Debug.Log("[BDArmory]: mass: " + mass + " caliber: " + caliber + " velocity: " + currentVelocity.magnitude + " multiplier: " + multiplier + " penetrationfactor: " + penetrationfactor);
            }

            if (hitPart.vessel != sourceVessel)
            {
                hitPart.AddDamage((float) heatDamage,caliber);
            }
            
        }

        private void CalculateDragNumericalIntegration()
        {
            if (dragType == BulletDragTypes.NumericalIntegration)
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
        }

        private void CalculateDragAnalyticEstimate()
        {
            if (dragType == BulletDragTypes.AnalyticEstimate)
            {
                float analyticDragVelAdjustment =
                    (float)
                    FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPosition),
                        FlightGlobals.getExternalTemperature(currPosition));
                analyticDragVelAdjustment *= flightTimeElapsed * initialSpeed;
                analyticDragVelAdjustment += 2 * ballisticCoefficient;

                analyticDragVelAdjustment = 2 * ballisticCoefficient * initialSpeed / analyticDragVelAdjustment;
                //velocity as a function of time under the assumption of a projectile only acted upon by drag with a constant drag area

                analyticDragVelAdjustment = analyticDragVelAdjustment - initialSpeed;
                //since the above was velocity as a function of time, but we need a difference in drag, subtract the initial velocity
                //the above number should be negative...
                impactVelocity += analyticDragVelAdjustment; //so add it to the impact velocity
            }
        }

        private float CalculateArmorPenetration( Part hitPart, float anglemultiplier, RaycastHit hit)
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
                Debug.Log("[BDArmory]: Armor penetration =" + penetration + " | Thickness = " + thickness);
            }

            bool fullyPenetrated = penetration > thickness; //check whether bullet penetrates the plate

            if (BDArmorySettings.BULLET_HITS)
            {
                BulletHitFX.CreateBulletHit(hit.point, hit.normal, !fullyPenetrated);
            }

            double massToReduce = 0;

            if (fullyPenetrated)
            {
                //lower velocity on penetrating armor plate
                //does not affect low impact parts so that rounds can go through entire tank easily                

                currentVelocity = currentVelocity * (float)Math.Sqrt(thickness / penetration);

                //updating impact velocity
                impactVelocity = currentVelocity.magnitude;
                CalculateDragAnalyticEstimate();

                flightTimeElapsed -= Time.deltaTime;
                prevPosition = transform.position;

                //massToReduce = 0.5f * mass * Math.Pow(impactVelocity, 2) * Mathf.Clamp01(penetrationFactor);
                //massToReduce *= 0.25;
                massToReduce = mass;

                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                {
                    Debug.Log("[BDArmory]: Bullet Penetrated Armor: Armor lost = " + massToReduce);
                }
            }
            else
            {
                //massToReduce = 0.5f * mass * Math.Pow(impactVelocity, 2) * Mathf.Clamp01(penetrationFactor);
                //massToReduce *= 0.125;
                massToReduce = mass * 0.85;

                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                {
                    Debug.Log("[BDArmory]: Bullet Stopped by Armor. Armor lost =" + massToReduce);
                }
            }
            hitPart.ReduceArmor(massToReduce);
            return penetrationFactor;
        }

        private float CalculatePenetration()
        {

            //TODO: Increase penetration for AP using pooled bullet modifiers

            float penetration = 0; //penetration of 0 for legacy support
            if (caliber > 10) //use the "krupp" penetration formula for anything larger then HMGs
            {
                penetration = (float)(16f * impactVelocity * Math.Sqrt(mass) / Math.Sqrt(caliber));
            }

            return penetration;
        }

        private static float CalculateThickness(Part hitPart, float anglemultiplier)
        {
            float thickness = (float)hitPart.GetArmorMass();
            return Mathf.Max(thickness / anglemultiplier, 10) ;
        }

        private bool ExplosiveDetonation(Part hitPart, RaycastHit hit, Ray ray)
        {
            ///////////////////////////////////////////////////////////////////////                                 
            // High Explosive Detonation
            ///////////////////////////////////////////////////////////////////////

            if (hitPart == null || hitPart.vessel != sourceVessel)
            {
                //if bullet hits and is HE, detonate and kill bullet
                if (explosive)
                {
                    ExplosionFX.CreateExplosion(hit.point - (ray.direction * 0.1f), radius, blastPower,
                                                blastHeat, sourceVessel, currentVelocity.normalized,
                                                explModelPath, explSoundPath, false,caliber);
                    KillBullet();
                    return true;
                }
            }
            return false;
        }

        private bool CheckGroundHit(Part hitPart, RaycastHit hit)
        {
            if (hitPart == null) //kill bullet if impacted part isnt defined
            {
                if (BDArmorySettings.BULLET_HITS)
                {
                    BulletHitFX.CreateBulletHit(hit.point, hit.normal, true);
                }
               
                return true;
            }
            return false;
        }

        private bool CheckBuildingHit(RaycastHit hit)
        {
            DestructibleBuilding hitBuilding = null;
            try
            {
                hitBuilding = hit.collider.gameObject.GetComponentUpwards<DestructibleBuilding>();
            }
            catch (Exception) { }

            if (hitBuilding != null && hitBuilding.IsIntact)
            {
                float damageToBuilding = mass * initialSpeed * initialSpeed * BDArmorySettings.DMG_MULTIPLIER /
                                         12000;
                hitBuilding.AddDamage(damageToBuilding);
                if (hitBuilding.Damage > hitBuilding.impactMomentumThreshold)
                {
                    hitBuilding.Demolish();
                }
                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                    Debug.Log("[BDArmory]: bullet hit destructible building! Damage: " +
                              (damageToBuilding).ToString("0.00") + ", total Damage: " + hitBuilding.Damage);

               
                return true;
            }
            return false;
        }

        public void UpdateWidth(Camera c, float resizeFactor)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            float fov = c.fieldOfView;
            float factor = (fov/60)*resizeFactor*
                           Mathf.Clamp(Vector3.Distance(transform.position, c.transform.position), 0, 3000)/50;
            float width1 = tracerStartWidth*factor*randomWidthScale;
            float width2 = tracerEndWidth*factor*randomWidthScale;

            bulletTrail.SetWidth(width1, width2);
        }

        void KillBullet()
        {
            gameObject.SetActive(false);
            hasPenetrated = false;
            penTicker = 0;
        }

        void FadeColor()
        {
            Vector4 endColorV = new Vector4(projectileColor.r, projectileColor.g, projectileColor.b, projectileColor.a);
            float delta = TimeWarp.deltaTime;
            Vector4 finalColorV = Vector4.MoveTowards(currentColor, endColorV, delta);
            currentColor = new Color(finalColorV.x, finalColorV.y, finalColorV.z, Mathf.Clamp(finalColorV.w, 0.25f, 1f));
        }

        bool RicochetOnPart(Part p, float angleFromNormal, float impactVel)
        {
            float hitTolerance = p.crashTolerance;
            //15 degrees should virtually guarantee a ricochet, but 75 degrees should nearly always be fine
            float chance = (((angleFromNormal - 5)/75)*(hitTolerance/150))*100/Mathf.Clamp01(impactVel/600);
            float random = UnityEngine.Random.Range(0f, 100f);
            if (BDArmorySettings.DRAW_DEBUG_LABELS) Debug.Log("[BDArmory]: Ricochet chance: "+chance);
            if (random < chance)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private float CalculateExplosionProbability(Part part)
        {
            float probability = 0;
            for (int i = 0; i < part.Resources.Count; i++)
            {
                PartResource current = part.Resources[i];
                switch (current.resourceName)
                {
                    case "LiquidFuel":
                        probability += (float) (current.amount/current.maxAmount);
                        break;
                    case "Oxidizer":
                        probability += (float) (current.amount/current.maxAmount);
                        break;
                }
            }
            //if (bulletType == PooledBulletTypes.Explosive)
            if (explosive)
                    probability += 0.1f;
            return probability;
        }
    }
}