using System;
using System.Collections;
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
        //

        Vector3 startPosition;
        public bool airDetonation = false;
        public float detonationRange = 3500;
        float randomWidthScale = 1;
        LineRenderer bulletTrail;
        Vector3 sourceOriginalV;
        bool hasBounced;
        public float maxDistance;
        Light lightFlash;
        bool wasInitiated;
        public Vector3 currentVelocity;
        public float mass;
        public float ballisticCoefficient;
        public float flightTimeElapsed;
        bool collisionEnabled;
        public static Shader bulletShader;
        public static bool shaderInitialized;

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

            hasBounced = false;
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

        void FixedUpdate()
        {
            float distanceFromStart = Vector3.Distance(transform.position, startPosition);
            if (!gameObject.activeInHierarchy)
            {
                return;
            }
            flightTimeElapsed += TimeWarp.fixedDeltaTime; //calculate flight time for drag purposes

            if (bulletDrop && FlightGlobals.RefFrameIsRotating)
            {
                currentVelocity += FlightGlobals.getGeeForceAtPosition(transform.position)*TimeWarp.fixedDeltaTime;
            }
            if (dragType == BulletDragTypes.NumericalIntegration)
            {
                Vector3 dragAcc = currentVelocity*currentVelocity.magnitude*
                                  (float)
                                  FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(transform.position),
                                      FlightGlobals.getExternalTemperature(transform.position));
                dragAcc *= 0.5f;
                dragAcc /= ballisticCoefficient;

                currentVelocity -= dragAcc*TimeWarp.fixedDeltaTime;
                //numerical integration; using Euler is silly, but let's go with it anyway
            }

            if (tracerLength == 0)
            {
                bulletTrail.SetPosition(0,
                    transform.position +
                    (currentVelocity*tracerDeltaFactor*TimeWarp.fixedDeltaTime/TimeWarp.CurrentRate) -
                    (FlightGlobals.ActiveVessel.Velocity() * TimeWarp.fixedDeltaTime));
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

            if (distanceFromStart > maxDistance)
            {
                KillBullet();
                return;
            }

            if (collisionEnabled)
            {
                float dist = initialSpeed*TimeWarp.fixedDeltaTime;

                Ray ray = new Ray(prevPosition, currPosition - prevPosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, dist, 557057))
                {
                    bool penetrated = true;
                    bool armor = false;
                    Part hitPart = null;

                    try
                    {
                        hitPart = hit.collider.gameObject.GetComponentInParent<Part>();                        
                    }
                    catch (NullReferenceException)
                    {
                        return;
                    }

                    if ((hitPart == null) || hitPart.FindModuleImplementing<BDArmor>() == null)
                    {
                        armor = BDArmor.GetArmor(hit.collider, hitPart);
                        if (BDArmorySettings.DRAW_DEBUG_LABELS && armor) Debug.Log("[BDArmory]: Armor Hit");
                    }

                    //BDArmor armor = BDArmor.GetArmor(hit.collider, hitPart);

                    ArmorPenetration.BulletPenetrationData armorData = new ArmorPenetration.BulletPenetrationData(ray, hit);
                    ArmorPenetration.DoPenetrationRay(armorData, bullet.positiveCoefficient);
                    float penetration = bullet.penetration.Evaluate(distanceFromStart)/1000;
                    bool fulllyPenetrated = penetration*leftPenetration >
                                           ((armor) ? 1f : BDArmor.Instance.EquivalentThickness) * armorData.armorThickness;
                    Vector3 finalDirect = Vector3.Lerp(ray.direction, -hit.normal, bullet.positiveCoefficient);
                    
                    if (fulllyPenetrated)
                    {
                        currentVelocity = finalDirect*currentVelocity.magnitude*leftPenetration;
                    }
                    else
                    {
                        currPosition = hit.point;
                        bulletTrail.SetPosition(1, currPosition);
                    }

                    float hitAngle = Vector3.Angle(currentVelocity, -hit.normal);

                    ///////////////////////////////////////////////////////////////////////
                    // Bullet Damage Start - ballistic
                    ///////////////////////////////////////////////////////////////////////

                    //dont do bullet damage if it is explosive
                    if (bulletType != PooledBulletTypes.Explosive) 
                    {
                        float impactVelocity = currentVelocity.magnitude;
                        if (dragType == BulletDragTypes.AnalyticEstimate)
                        {
                            float analyticDragVelAdjustment =
                                (float)
                                FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPosition),
                                    FlightGlobals.getExternalTemperature(currPosition));
                            analyticDragVelAdjustment *= flightTimeElapsed*initialSpeed;
                            analyticDragVelAdjustment += 2*ballisticCoefficient;

                            analyticDragVelAdjustment = 2*ballisticCoefficient*initialSpeed/analyticDragVelAdjustment;
                            //velocity as a function of time under the assumption of a projectile only acted upon by drag with a constant drag area

                            analyticDragVelAdjustment = analyticDragVelAdjustment - initialSpeed;
                            //since the above was velocity as a function of time, but we need a difference in drag, subtract the initial velocity
                            //the above number should be negative...
                            impactVelocity += analyticDragVelAdjustment; //so add it to the impact velocity

                            if (impactVelocity < 0)
                            {
                                impactVelocity = 0;
                                //clamp the velocity to > 0, since it could drop below 0 if the bullet is fired upwards
                            }
                            //Debug.Log("flight time: " + flightTimeElapsed + " BC: " + ballisticCoefficient + "\ninit speed: " + initialSpeed + " vel diff: " + analyticDragVelAdjustment);
                        }

                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        /////////////////////////////////////////////////[panzer1b] HEAT BASED DAMAGE CODE START//////////////////////////////////////////////////////////////
                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        //hitting a vessel Part
                        if (hitPart != null) //see if it will ricochet of the part
                        {
                            penetrated = !RicochetOnPart(hitPart, hitAngle, impactVelocity);
                        }
                        else //see if it will ricochet off scenery
                        {
                            float reflectRandom = UnityEngine.Random.Range(-75f, 90f);
                            if (reflectRandom > 90 - hitAngle)
                            {
                                penetrated = false;
                            }
                        }
                        
                        if (hitPart != null && !hitPart.partInfo.name.Contains("Strut")) //when a part is hit, execute damage code (ignores struts to keep those from being abused as armor)(no, because they caused weird bugs :) -BahamutoD)
                        {
                            //TODO: Review damage formula - is this the correct method of calculation?
                            float heatDamage = (mass/(hitPart.crashTolerance*hitPart.mass))*
                                               (impactVelocity * impactVelocity) * // was impactVelocity * ImpactVelocity
                                               BDArmorySettings.DMG_MULTIPLIER * // global damage multiplier
                                               bulletDmgMult // individual bullet modifier, default 1
                                               ;
                            //how much heat damage will be applied based on bullet mass, velocity, and part's impact tolerance and mass
                            if (!penetrated)
                            {
                                heatDamage = heatDamage/2; //TODO: Review Damage for penetrated
                            }
                            if (!fulllyPenetrated)
                            {
                                heatDamage = heatDamage/3; //TODO: Review Damage for fully penetrated
                            }

                            //if (BDArmorySettings.INSTAKILL)
                            //    //instakill support, will be removed once mod becomes officially MP
                            //{
                            //    heatDamage = (float) hitPart.maxTemp + 100;
                            //    //make heat damage equal to the part's max temperture, effectively instakilling any part it hits
                            //}

                            if (BDArmorySettings.DRAW_DEBUG_LABELS)
                                Debug.Log("[BDArmory]: Hit! damage applied: " + heatDamage + " | Penetrated:" + penetrated + " | Fully Penetrated:" + fulllyPenetrated);

                            if (hitPart.vessel != sourceVessel)
                            {
                                hitPart.AddDamage(heatDamage);
                            }

                            float overKillHeatDamage = (float) (hitPart.temperature - hitPart.maxTemp);

                            if (overKillHeatDamage > 0)
                            //if the part is destroyed by overheating, we want to add the remaining heat to attached parts.  This prevents using tiny parts as armor
                            {
                                overKillHeatDamage *= hitPart.crashTolerance; //reset to raw damage
                                float numConnectedParts = hitPart.children.Count;
                                if (hitPart.parent != null)
                                {
                                    numConnectedParts++;
                                    overKillHeatDamage /= numConnectedParts;
                                    hitPart.parent.AddDamage(overKillHeatDamage/
                                                                  (hitPart.parent.crashTolerance*hitPart.parent.mass));

                                    for (int i = 0; i < hitPart.children.Count; i++)
                                    {
                                        hitPart.children[i].AddDamage(overKillHeatDamage/
                                                                           hitPart.children[i].crashTolerance);
                                    }
                                }
                                else
                                {
                                    overKillHeatDamage /= numConnectedParts;
                                    for (int i = 0; i < hitPart.children.Count; i++)
                                    {
                                        hitPart.children[i].AddDamage(overKillHeatDamage/
                                                                           hitPart.children[i].crashTolerance);
                                    }
                                }
                            }
                        }

                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        /////////////////////////////////////////////////[panzer1b] HEAT BASED DAMAGE CODE END////////////////////////////////////////////////////////////////
                        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        #region Bullet damage for buildings
                        
                        //hitting a Building
                        DestructibleBuilding hitBuilding = null;
                        try
                        {
                            hitBuilding = hit.collider.gameObject.GetComponentUpwards<DestructibleBuilding>();
                        }
                        catch (NullReferenceException)
                        {
                        }
                        if (hitBuilding != null && hitBuilding.IsIntact)
                        {
                            float damageToBuilding = mass*initialSpeed*initialSpeed*BDArmorySettings.DMG_MULTIPLIER/
                                                     12000;
                            if (!penetrated)
                            {
                                damageToBuilding = damageToBuilding/8;
                            }
                            hitBuilding.AddDamage(damageToBuilding);
                            if (hitBuilding.Damage > hitBuilding.impactMomentumThreshold)
                            {
                                hitBuilding.Demolish();
                            }
                            if (BDArmorySettings.DRAW_DEBUG_LABELS)
                                Debug.Log("[BDArmory]: bullet hit destructible building! Damage: " +
                                          (damageToBuilding).ToString("0.00") + ", total Damage: " + hitBuilding.Damage);
                        }
                    }
                    
                    #endregion

                    ///////////////////////////////////////////////////////////////////////
                    //Bullet Damage End - ballistic
                    ///////////////////////////////////////////////////////////////////////

                    if (hitPart == null || (hitPart != null && hitPart.vessel != sourceVessel))
                    {
                        if (!penetrated && !hasBounced && !fulllyPenetrated)
                        {
                            //ricochet
                            hasBounced = true;
                            if (BDArmorySettings.BULLET_HITS)
                            {
                                BulletHitFX.CreateBulletHit(hit.point, hit.normal, true);
                            }

                            tracerStartWidth /= 2;
                            tracerEndWidth /= 2;

                            transform.position = hit.point;
                            currentVelocity = Vector3.Reflect(currentVelocity, hit.normal);
                            currentVelocity = (hitAngle/150)*currentVelocity*0.65f;

                            Vector3 randomDirection = UnityEngine.Random.rotation*Vector3.one;

                            currentVelocity = Vector3.RotateTowards(currentVelocity, randomDirection,
                                UnityEngine.Random.Range(0f, 5f)*Mathf.Deg2Rad, 0);
                        }
                        else
                        {
                            ///////////////////////////////////////////////////////////////////////
                            // Bullet Damage Start - explosive
                            ///////////////////////////////////////////////////////////////////////

                            if (bulletType == PooledBulletTypes.Explosive)
                            {
                                ExplosionFX.CreateExplosion(hit.point - (ray.direction*0.1f), radius, blastPower,
                                    blastHeat, sourceVessel, currentVelocity.normalized, explModelPath, explSoundPath);
                            }
                            else if (BDArmorySettings.BULLET_HITS)
                            {
                                BulletHitFX.CreateBulletHit(hit.point, hit.normal, false);
                            }

                            if (armor &&
                                (penetration * leftPenetration > BDArmor.Instance.outerArmorThickness/1000 * BDArmor.Instance.EquivalentThickness ||
                                 fulllyPenetrated))
                            {
                                switch (BDArmor.Instance._explodeMode)
                                {
                                    case ArmorUtils.ExplodeMode.Always:
                                        BDArmor.Instance.CreateExplosion(hitPart);
                                        break;
                                    case ArmorUtils.ExplodeMode.Dynamic:
                                        float probability = CalculateExplosionProbability(hitPart);
                                        if (probability > 0.1f)
                                            BDArmor.Instance.CreateExplosion(hitPart);
                                        break;
                                    case ArmorUtils.ExplodeMode.Never:
                                        break;
                                }
                            }
                            if (fulllyPenetrated)
                            {
                                leftPenetration -= armorData.armorThickness/penetration;
                                transform.position = armorData.hitResultOut.point;
                                flightTimeElapsed -= Time.fixedDeltaTime;
                                prevPosition = transform.position;
                                FixedUpdate();
                                return;
                            }
                            else
                            {
                                KillBullet();                                
                                return;
                            }
                        }
                    }
                }
            }

            if (bulletType == PooledBulletTypes.Explosive && airDetonation && distanceFromStart > detonationRange)
            {
                //detonate
                ExplosionFX.CreateExplosion(transform.position, radius, blastPower, blastHeat, sourceVessel,
                    currentVelocity.normalized, explModelPath, explSoundPath);
                KillBullet();
                return;
            }

            prevPosition = currPosition;

            //move bullet            
            transform.position += currentVelocity*Time.fixedDeltaTime;
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
        }

        void FadeColor()
        {
            Vector4 currentColorV = new Vector4(currentColor.r, currentColor.g, currentColor.b, currentColor.a);
            Vector4 endColorV = new Vector4(projectileColor.r, projectileColor.g, projectileColor.b, projectileColor.a);
            //float delta = (Vector4.Distance(currentColorV, endColorV)/0.15f) * TimeWarp.fixedDeltaTime;
            float delta = TimeWarp.fixedDeltaTime;
            Vector4 finalColorV = Vector4.MoveTowards(currentColor, endColorV, delta);
            currentColor = new Color(finalColorV.x, finalColorV.y, finalColorV.z, Mathf.Clamp(finalColorV.w, 0.25f, 1f));
        }

        bool RicochetOnPart(Part p, float angleFromNormal, float impactVel)
        {
            float hitTolerance = p.crashTolerance;
            //15 degrees should virtually guarantee a ricochet, but 75 degrees should nearly always be fine
            float chance = (((angleFromNormal - 5)/75)*(hitTolerance/150))*100/Mathf.Clamp01(impactVel/600);
            float random = UnityEngine.Random.Range(0f, 100f);
            //if (BDArmorySettings.DRAW_DEBUG_LABELS) Debug.Log("[BDArmory]: Ricochet chance: "+chance);
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
            if (bulletType == PooledBulletTypes.Explosive)
                probability += 0.1f;
            return probability;
        }
    }
}