using System;
using System.Collections;
using System.Linq;
using BDArmory.Armor;
using BDArmory.Core.Extension;
using BDArmory.Core.Module;
using BDArmory.FX;
using BDArmory.Parts;
using BDArmory.Shaders;
using BDArmory.UI;
using UnityEngine;
using System.Collections.Generic;

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
        public bool hasDetonated = false;
        public bool hasRichocheted = false;

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

        void OnDestory()
        {
            StopCoroutine(FrameDelayedRoutine());
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

                            if (hitPart?.vessel == sourceVessel) return;  //avoid autohit;                     

                            float hitAngle = Vector3.Angle(currentVelocity, -hit.normal);

                            if (CheckGroundHit(hitPart, hit) || CheckBuildingHit(hit))
                            {
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
 
                            //Standard Pipeline Damage, Armor and Explosives
                            
                            impactVelocity = currentVelocity.magnitude;
                            float anglemultiplier = (float)Math.Cos(Math.PI * hitAngle / 180.0);

                            CalculateDragAnalyticEstimate();

                            if (RicochetOnPart(hitPart, hit, hitAngle, impactVelocity)){ hasRichocheted = true; }

                            var penetrationFactor = CalculateArmorPenetration(hitPart, anglemultiplier, hit);

                            if (penetrationFactor > 1) //fully penetrated, not explosive, continue ballistic damage
                            {
                                hasPenetrated = true;
                                //CheckPartForExplosion(hitPart); //cannot re-enable until we serially do hits otherwise everything the ray hits may explode on penetration simultaneousely                             
                                ApplyDamage(hitPart, 1, penetrationFactor);
                                penTicker += 1;
                            }
                            else
                            {
                                hasPenetrated = false;               
                                // explosive bullets that get stopped by armor will explode                                    
                                ApplyDamage(hitPart, penetrationFactor, penetrationFactor);
                                ExplosiveDetonation(hitPart, hit, ray);
                            }                           

                            /////////////////////////////////////////////////////////////////////////////////
                            //Flak Explosion (air detonation/proximity fuse) or penetrated after a few ticks
                            /////////////////////////////////////////////////////////////////////////////////

                            if ((explosive && airDetonation && distanceFromStart > detonationRange) || (penTicker >= 2 && explosive))
                            {
                                //detonate
                                ExplosionFX.CreateExplosion(hit.point, radius, blastPower, blastHeat, sourceVessel,
                                    currentVelocity.normalized, explModelPath, explSoundPath, false, caliber);
                                hasDetonated = true;
                                return;
                            }

                            //bullet should not go any further if moving too slowly after hit
                            //smaller caliber rounds would be too deformed to do any further damage
                            if (currentVelocity.magnitude <= 30 || (caliber < 30 && hasPenetrated))
                            {
                                if (BDArmorySettings.DRAW_DEBUG_LABELS)
                                {
                                    Debug.Log("[BDArmory]: Bullet Velocity too low, stopping");
                                }
                                KillBullet();
                                return;
                            }
                            if (!hasPenetrated || hasRichocheted) break;
                            //end While
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

        private void ApplyDamage(Part hitPart, float multiplier, float penetrationfactor)
        {
            //hitting a vessel Part
            //No struts, they cause weird bugs :) -BahamutoD
            if(hitPart == null) return;
            if (hitPart.partInfo.name.Contains("Strut")) return;

            hitPart.AddDamage_Ballistic(mass, caliber, multiplier, penetrationfactor, BDArmorySettings.DMG_MULTIPLIER, bulletDmgMult, impactVelocity);
            
            #region Code Moved To PartExtensions
            // if (hitPart.HasArmor()) return; - Why would we not do damage if armor??

            //Basic kinetic formula. 
            //double heatDamage = ((0.5f * (mass * Math.Pow(impactVelocity, 2))) *
            //                        BDArmorySettings.DMG_MULTIPLIER
            //                        * 0.0055f); //dmg mult is 100 baseline, so this constant adjusted accordingly

            //Now, we know exactly how well the bullet was stopped by the armor. 
            //This value will be below 1 when it is stopped by the armor.
            //That means that we should not apply all the damage to the part that stopped by the bullet
            //Also we are not considering hear the angle of penetration , because we already did on the armor penetration calculations.

            //as armor is decreased level of damage should increase exponentially
            //double armorMass_ = hitPart.GetArmorMass();
            //double armorPCT_ = hitPart.GetArmorPercentage();

            //heatDamage = (heatDamage * multiplier) * Math.Max(1f, armorPCT_);

            //double damage_d = ((float)Math.Log10(Mathf.Clamp(hitPart.GetArmorPercentage() * 100, 1f,100f)) + 5f) * heatDamage * multiplier;
            //double damage_d = (Mathf.Clamp((float)Math.Log10(armorPCT_),10f,100f) + 5f) * heatDamage;
            //float damage_f = (float)damage_d;

            //if (caliber <= 30f && armorMass_ >= 100d) heatDamage *= 0.0625f; //penalty for low caliber rounds,not if armor is very low

            /////////////////////////////////////////////
            //if (BDArmorySettings.DRAW_DEBUG_LABELS)
            //{
            //    //Debug.Log("[BDArmory]: Damage Applied: " + (int) heatDamage);
            //    Debug.Log("[BDArmory]: mass: " + mass + " caliber: " + caliber + " velocity: " + currentVelocity.magnitude + " multiplier: " + multiplier + " penetrationfactor: " + penetrationfactor);
            //}

            //if (hitPart.vessel != sourceVessel)
            //{
            //    hitPart.AddDamage((float) heatDamage,multiplier,caliber,false);
            //}
            #endregion
        }

        private void CalculateDragNumericalIntegration()
        {
            if (!bulletDrop) return;
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
            if (!bulletDrop) return;
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
                Debug.Log("[BDArmory]: Armor penetration = " + penetration + " | Thickness = " + thickness);
            }

            bool fullyPenetrated = penetration > thickness; //check whether bullet penetrates the plate

            if (BDArmorySettings.BULLET_HITS)
            {
                BulletHitFX.CreateBulletHit(hit.point, hit.normal, !fullyPenetrated);
            }
                        
            double massToReduce = Math.PI * Math.Pow((caliber*0.001) / 2,2) * (penetration);
            //double massToReduce = 0.5f * mass * Math.Pow(impactVelocity, 2) * Mathf.Clamp01(penetrationFactor);
            //massToReduce *= 0.125;

            if (fullyPenetrated)
            {
                //lower velocity on penetrating armor plate
                //does not affect low impact parts so that rounds can go through entire tank easily              

                currentVelocity = currentVelocity * (float)Math.Sqrt(thickness / penetration);
                if (penTicker > 0) currentVelocity *= 0.85f; //signifincanly reduce velocity on subsequent penetrations

                //updating impact velocity
                impactVelocity = currentVelocity.magnitude;
                CalculateDragAnalyticEstimate();

                flightTimeElapsed -= Time.deltaTime;
                prevPosition = transform.position;
            }
            else
            {                
                massToReduce *= 0.0625;                

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
            float penetration = 0; //penetration of 0 for legacy support
            if (caliber > 10) //use the "krupp" penetration formula for anything larger then HMGs
            {
                penetration = (float)(16f * impactVelocity * Math.Sqrt(mass/1000) / Math.Sqrt(caliber));
            }
            //if (apBulletDmg != 0) penetration += apBulletDmg;

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

        bool RicochetOnPart(Part p, RaycastHit hit, float angleFromNormal, float impactVel)
        {
            float hitTolerance = p.crashTolerance;
            //15 degrees should virtually guarantee a ricochet, but 75 degrees should nearly always be fine
            float chance = (((angleFromNormal - 5)/75)*(hitTolerance/150))*100/Mathf.Clamp01(impactVel/600);
            float random = UnityEngine.Random.Range(0f, 100f);
            if (BDArmorySettings.DRAW_DEBUG_LABELS) Debug.Log("[BDArmory]: Ricochet chance: "+chance);
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
            if (reflectRandom > 90 - hitAngle)
            {
                return true;
            }
  
             return false;                        

        }

        public void DoRicochet(Part p, RaycastHit hit,float hitAngle)
        {
            //ricochet            
            if (BDArmorySettings.BULLET_HITS)
            {
                BulletHitFX.CreateBulletHit(hit.point, hit.normal, true);
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
            if (!hitPart.FindModuleImplementing<BDArmor>()) return;

            switch (BDArmor.Instance._explodeMode)
            {
                case ArmorUtils.ExplodeMode.Always:
                    CreateExplosion(hitPart);
                    break;
                case ArmorUtils.ExplodeMode.Dynamic:
                    float probability = CalculateExplosionProbability(hitPart);
                    if (probability > 0.1f)
                        CreateExplosion(hitPart);
                    break;
                case ArmorUtils.ExplodeMode.Never:
                    break;
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
            
            if (explosive)
                    probability += 0.1f;

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
            part.explode();
            ExplosionFX.CreateExplosion(part.partTransform.position, explodeScale * radius, explodeScale * blastPower * 2,
                explodeScale * blastHeat, part.vessel, FlightGlobals.upAxis, explModelPath, explSoundPath, false);
        }

    }
}