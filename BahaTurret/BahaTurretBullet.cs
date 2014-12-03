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
			
			/*
			lineMat = new Material(Shader.Find("KSP/Particles/Alpha Blended"));
			lineMat.mainTexture = GameDatabase.Instance.GetTexture("BDArmory/Textures/bullet", false);
			*/
			
			rigidbody.useGravity = false;
			
			//Vector.SetCamera3D(FlightCamera.fetch.mainCamera);
			
		}
		
		void FixedUpdate()
		{
			if(bulletDrop && FlightGlobals.RefFrameIsRotating)
			{
				rigidbody.velocity += FlightGlobals.getGeeForceAtPosition(transform.position) * Time.fixedDeltaTime;
			}
			
			
			if(tracerLength == 0)
			{
				bulletTrail.SetPosition(0, transform.position+(rigidbody.velocity * Time.fixedDeltaTime)-(FlightGlobals.ActiveVessel.rigidbody.velocity*Time.fixedDeltaTime));
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
			
			
			if(Vector3.Distance(transform.position, FlightGlobals.ActiveVessel.transform.position) > maxDistance)
			{
				GameObject.Destroy(gameObject);
				return;
			}
			
			if(Time.time - startTime > 0.01f)
			{
				light.intensity = 0;	
				float dist = (currPosition-prevPosition).magnitude;
				Ray ray = new Ray(prevPosition, currPosition-prevPosition);
				RaycastHit hit;
				if(Physics.Raycast(ray, out hit, dist, 557057))
				{
					bool penetrated = false;
					//hitting a vessel Part
					Part hitPart =  null;
					try{
						hitPart = Part.FromGO(hit.rigidbody.gameObject);
					}catch(NullReferenceException){}
					
					if(hitPart!=null && !hitPart.partInfo.name.Contains("Strut"))
					{
						float destroyChance = (rigidbody.mass/hitPart.crashTolerance) * (rigidbody.velocity-hit.rigidbody.velocity).magnitude * BDArmorySettings.DMG_MULTIPLIER;
						if(BDArmorySettings.INSTAKILL)
						{
							destroyChance = 100;	
						}
						Debug.Log ("Hit! chance of destroy: "+destroyChance);
						if(UnityEngine.Random.Range (0f,100f)<destroyChance)
						{
							penetrated = true;
							if(hitPart.vessel != sourceVessel) hitPart.temperature = hitPart.maxTemp + 100;
						}
					}
					
					
					//hitting a Building
					DestructibleBuilding hitBuilding = null;
					try{
						hitBuilding = hit.collider.gameObject.GetComponentUpwards<DestructibleBuilding>();
					}
					catch(NullReferenceException){}
					if(hitBuilding!=null && hitBuilding.IsIntact)
					{
						float damageToBuilding = rigidbody.mass * rigidbody.velocity.sqrMagnitude * 0.010f;
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
		
		/*
		public void LateUpdate()
		{
			Vector.DestroyLine(ref bulletVectorLine);
			
			Vector3[] pointsArray = new Vector3[2];
			pointsArray[1] = transform.position;
			if(tracerLength == 0)
			{
				pointsArray[0] = transform.position+(rigidbody.velocity * Time.fixedDeltaTime * 0.95f)-(FlightGlobals.ActiveVessel.rigidbody.velocity*Time.fixedDeltaTime);
			}
			else
			{
				pointsArray[0] = transform.position + ((rigidbody.velocity-sourceOriginalV).normalized * tracerLength);
			}
			
			bulletVectorLine = new VectorLine("bulletVectorLine", pointsArray, lineMat, 50*tracerStartWidth, LineType.Continuous);
			
			if(fadeColor)
			{
				FadeColor ();	
			}
			Vector.SetColor(bulletVectorLine, currentColor);;
			Vector.DrawLine3D(bulletVectorLine);	
		}
		*/
		
		void FadeColor()
		{
			Vector4 currentColorV = new Vector4(currentColor.r, currentColor.g, currentColor.b, currentColor.a);
			Vector4 endColorV = new Vector4(projectileColor.r, projectileColor.g, projectileColor.b, projectileColor.a);
			float delta = (Vector4.Distance(currentColorV, endColorV)/0.15f) * TimeWarp.fixedDeltaTime;
			Vector4 finalColorV = Vector4.MoveTowards(currentColor, endColorV, delta);
			currentColor = new Color(finalColorV.x, finalColorV.y, finalColorV.z, finalColorV.w);
		}
	}
}

