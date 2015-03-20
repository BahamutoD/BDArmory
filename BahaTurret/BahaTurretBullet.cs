using System;
using UnityEngine;

namespace BahaTurret
{
	public class BahaTurretBullet : MonoBehaviour
	{
		public float startTime;
		public Vessel sourceVessel;
		public Color lightColor = Misc.ParseColor255("255, 235, 145, 255");
		public Color projectileColor;
		
		public string bulletTexturePath;
		
		public bool fadeColor;
		public Color startColor;
		Color currentColor;
		
		public bool bulletDrop = true;
		
		public float tracerStartWidth = 1;
		public float tracerEndWidth = 1;
		public float tracerLength = 0;
		
		public float initialSpeed;
		
		
		public Vector3 prevPosition;
		public Vector3 currPosition;
		
		LineRenderer bulletTrail;
	//	VectorLine bulletVectorLine;
		//Material lineMat;
		
		
		Vector3 sourceOriginalV;
		bool hasBounced = false;
		
		float maxDistance;
		
		void Start()
		{
			float maxLimit = Mathf.Clamp(BDArmorySettings.MAX_BULLET_RANGE, 0, 8000);
			maxDistance = Mathf.Clamp(BDArmorySettings.PHYSICS_RANGE, 2500, maxLimit);
			projectileColor.a = projectileColor.a/2;
			startColor.a = startColor.a/2;
			
			currentColor = projectileColor;
			if(fadeColor)	
			{
				currentColor = startColor;
			}
			startTime = Time.time;
			prevPosition = gameObject.transform.position;
			
			sourceOriginalV = sourceVessel.rigidbody.velocity;
			
			Light light = gameObject.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = lightColor;
			light.range = 8;
			light.intensity = 1;
			
			
			bulletTrail = gameObject.AddComponent<LineRenderer>();
			bulletTrail.SetVertexCount(2);
			bulletTrail.SetPosition(0, transform.position);
			bulletTrail.SetPosition(1, transform.position);
			float width = tracerStartWidth * Vector3.Distance(transform.position, FlightCamera.fetch.mainCamera.transform.position)/50;
			bulletTrail.SetWidth(width, width);
			bulletTrail.material = new Material(Shader.Find("KSP/Particles/Alpha Blended"));
			bulletTrail.material.mainTexture = GameDatabase.Instance.GetTexture(bulletTexturePath, false);
			bulletTrail.material.SetColor("_TintColor", currentColor);
			
			
			
			rigidbody.useGravity = false;
			
		
			
		}
		
		void FixedUpdate()
		{
			if(bulletDrop && FlightGlobals.RefFrameIsRotating)
			{
				rigidbody.velocity += FlightGlobals.getGeeForceAtPosition(transform.position) * TimeWarp.fixedDeltaTime;
			}
			
			
			if(tracerLength == 0)
			{
				bulletTrail.SetPosition(0, transform.position+(rigidbody.velocity * TimeWarp.fixedDeltaTime)-(FlightGlobals.ActiveVessel.rigidbody.velocity*TimeWarp.fixedDeltaTime));
			}
			else
			{
				bulletTrail.SetPosition(0, transform.position + ((rigidbody.velocity-sourceOriginalV).normalized * tracerLength));	
			}
			if(fadeColor)
			{
				FadeColor();
				bulletTrail.material.SetColor("_TintColor", currentColor);
			}
			
			float fov = FlightCamera.fetch.mainCamera.fieldOfView;
			float width1 = (fov/60) * tracerStartWidth * Mathf.Clamp(Vector3.Distance(transform.position, FlightCamera.fetch.mainCamera.transform.position),0,3000)/50;
			float width2 = (fov/60) * tracerEndWidth * Mathf.Clamp(Vector3.Distance(transform.position, FlightCamera.fetch.mainCamera.transform.position),0,3000)/50;
			
			bulletTrail.SetWidth(width1, width2);
			
			bulletTrail.SetPosition(1, transform.position);
			
			
			
			currPosition = gameObject.transform.position;
			
			if((currPosition-FlightGlobals.ActiveVessel.transform.position).sqrMagnitude > maxDistance*maxDistance)
			{
				GameObject.Destroy(gameObject);
				return;
			}
			
			if(Time.time - startTime > 0.01f)
			{
				light.intensity = 0;	
				float dist = initialSpeed*TimeWarp.fixedDeltaTime;
				
				Ray ray = new Ray(prevPosition, currPosition-prevPosition);
				RaycastHit hit;
				if(Physics.Raycast(ray, out hit, dist, 557057))
				{
					bool penetrated = false;
					
					//hitting a vessel Part
					
					//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    /////////////////////////////////////////////////[panzer1b] HEAT BASED DAMAGE CODE START//////////////////////////////////////////////////////////////
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

					Part hitPart =  null;   //determine when bullet collides with a target
					try{
						hitPart = Part.FromGO(hit.rigidbody.gameObject);
					}catch(NullReferenceException){}
					
					if(hitPart!=null && !hitPart.partInfo.name.Contains("Strut"))   //when a part is hit, execute damage code (ignores struts to keep those from being abused as armor)
					{
                        float ricochetChance = 30;  //chance to bounce off, potentially doing more damage to another part in the trajectory
						float heatDamage = (rigidbody.mass/hitPart.crashTolerance) * initialSpeed * 50 * BDArmorySettings.DMG_MULTIPLIER;   //how much heat damage will be applied based on bullet mass, velocity, and part's impact tolerance
						if(BDArmorySettings.INSTAKILL)  //instakill support, will be removed once mod becomes officially MP
						{
                            heatDamage = hitPart.maxTemp + 100; //make heat damage equal to the part's max temperture, effectively instakilling any part it hits
						}
                        if (BDArmorySettings.DRAW_DEBUG_LINES) Debug.Log("Hit! damage applied: " + heatDamage); //debugging stuff

                        if (hitPart.mass <= 0.01)   //if part mass is below 0.01, instakill it and do minor collateral (anti-exploit and to keep people from abusing near massless or massless crap as armor)
                        {
                            if (hitPart.vessel != sourceVessel) hitPart.temperature += hitPart.maxTemp + 500;  //make heat damage equal to the part's max temperture, and add 500 extra heat damage which should do minor collateral to teh surrounding parts
                        }
                        else    //apply damage normally if no special case present
                        {
                            if (hitPart.vessel != sourceVessel) hitPart.temperature += heatDamage;  //apply heat damage to the hit part.
                        }
						if(UnityEngine.Random.Range (0f,100f)>ricochetChance)   //dont ricochet if the random range is higher then the ricochet chance
                        {
                            penetrated = true;  //disable ricochet if true
						}
					}

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    /////////////////////////////////////////////////[panzer1b] HEAT BASED DAMAGE CODE END////////////////////////////////////////////////////////////////
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
					

					//hitting a Building
					DestructibleBuilding hitBuilding = null;
					try{
						hitBuilding = hit.collider.gameObject.GetComponentUpwards<DestructibleBuilding>();
					}
					catch(NullReferenceException){}
					if(hitBuilding!=null && hitBuilding.IsIntact)
					{
						float damageToBuilding = rigidbody.mass * initialSpeed * BDArmorySettings.DMG_MULTIPLIER/120;
						hitBuilding.AddDamage(damageToBuilding);
						if(hitBuilding.Damage > hitBuilding.impactMomentumThreshold)
						{
							penetrated = true;
							hitBuilding.Demolish();
						}
						if(BDArmorySettings.DRAW_DEBUG_LINES) Debug.Log("bullet hit destructible building! Damage: "+(damageToBuilding).ToString("0.00")+ ", total Damage: "+hitBuilding.Damage);
					}
					
					if(hitPart == null || (hitPart!=null && hitPart.vessel!=sourceVessel))
					{
						
						
						//ricochet
						float reflectRandom = UnityEngine.Random.Range(-300f, 90f);
						float hitAngle = Vector3.Angle(rigidbody.velocity, -hit.normal);
						bool ricochetHit = false;
						if(!penetrated && reflectRandom > 90-hitAngle && !hasBounced)
						{
							ricochetHit = true;
							transform.position = hit.point;
							rigidbody.velocity = Vector3.Reflect(rigidbody.velocity, hit.normal);
							rigidbody.velocity = hitAngle/150 * rigidbody.velocity * 0.75f;
							
							Vector3 randomDirection = UnityEngine.Random.rotation * Vector3.one;
							
							rigidbody.velocity = Vector3.RotateTowards(rigidbody.velocity, randomDirection, UnityEngine.Random.Range(0f,5f)*Mathf.Deg2Rad, 0);
						}
						
						if(ricochetHit && !hasBounced)
						{
							hasBounced = true;
							if(BDArmorySettings.BULLET_HITS)
							{
								BulletHitFX.CreateBulletHit(hit.point, hit.normal, true);
							}	
						}
						else
						{
							if(BDArmorySettings.BULLET_HITS)
							{
								BulletHitFX.CreateBulletHit(hit.point, hit.normal, false);
							}
							GameObject.Destroy(gameObject); //destroy bullet on collision
						}
					}
				}
			}
			prevPosition = currPosition;
		}
		
	
	
		
		void FadeColor()
		{
			Vector4 currentColorV = new Vector4(currentColor.r, currentColor.g, currentColor.b, currentColor.a);
			Vector4 endColorV = new Vector4(projectileColor.r, projectileColor.g, projectileColor.b, projectileColor.a);
			//float delta = (Vector4.Distance(currentColorV, endColorV)/0.15f) * TimeWarp.fixedDeltaTime;
			float delta = TimeWarp.fixedDeltaTime;
			Vector4 finalColorV = Vector4.MoveTowards(currentColor, endColorV, delta);
			currentColor = new Color(finalColorV.x, finalColorV.y, finalColorV.z, finalColorV.w);
		}
	}
}

