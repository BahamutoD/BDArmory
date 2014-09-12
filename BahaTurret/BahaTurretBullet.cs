using System;
using UnityEngine;

namespace BahaTurret
{
	public class BahaTurretBullet : MonoBehaviour
	{
		public float startTime;
		public float bulletLifeTime = 10;
		public Vessel sourceVessel;
		public Color lightColor = Misc.ParseColor255("255, 235, 145, 255");
		public Color projectileColor;
		
		public bool bulletDrop = true;
		
		public float tracerStartWidth = 1;
		public float tracerEndWidth = 1;
		public float tracerLength = 0;
		
		
		public Vector3 prevPosition;
		public Vector3 currPosition;
		
		LineRenderer bulletTrail;
		
		Vector3 sourceOriginalV;
		
		void Start()
		{
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
			bulletTrail.SetWidth(tracerStartWidth, tracerEndWidth);
			bulletTrail.material = new Material(Shader.Find("KSP/Particles/Alpha Blended"));
			bulletTrail.material.mainTexture = GameDatabase.Instance.GetTexture("BDArmory/Textures/bullet", false);
			bulletTrail.material.SetColor("_TintColor", projectileColor);
			
			rigidbody.useGravity = false;
			
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
			
			bulletTrail.SetPosition(1, transform.position);
			
			currPosition = gameObject.transform.position;
			if(Time.time - startTime > bulletLifeTime)
			{
				GameObject.Destroy(gameObject);
			}
			
			if(Time.time - startTime > 0.01f)
			{
				light.intensity = 0;	
				float dist = (currPosition-prevPosition).magnitude;
				Ray ray = new Ray(prevPosition, currPosition-prevPosition);
				RaycastHit hit;
				if(Physics.Raycast(ray, out hit, dist, 557057))
				{
					
					Part hitPart =  null;
					try{
						hitPart = Part.FromGO(hit.rigidbody.gameObject);
					}catch(NullReferenceException){}
					
					if(hitPart!=null)
					{
						float destroyChance = (rigidbody.mass/hitPart.crashTolerance) * (rigidbody.velocity-hit.rigidbody.velocity).magnitude * 8000;
						if(BDArmorySettings.INSTAKILL)
						{
							destroyChance = 100;	
						}
						Debug.Log ("Hit! chance of destroy: "+destroyChance);
						if(UnityEngine.Random.Range (0f,100f)<destroyChance)
						{
							if(hitPart.vessel != sourceVessel) hitPart.temperature = hitPart.maxTemp + 100;
						}
					}
					if(hitPart==null || (hitPart!=null && hitPart.vessel!=sourceVessel))
					{
						//hit effects
						if(BDArmorySettings.BULLET_HITS)
						{
							BulletHitFX.CreateBulletHit(hit.point, hit.normal);
						}
						
						GameObject.Destroy(gameObject); //destroy bullet on collision
					}
				}
			}
			prevPosition = currPosition;
		}
		
	}
}

