using System;
using UnityEngine;
using System.Collections.Generic;

namespace BahaTurret
{
	public class RocketLauncher : PartModule
	{
		public bool hasRocket = true;
		
		[KSPField(isPersistant = false)]
		public string rocketType;
		
		[KSPField(isPersistant = false)]
		public string rocketModelPath;
		
		[KSPField(isPersistant = false)]
		public float rocketMass;
		
		[KSPField(isPersistant = false)]
		public float thrust;
		
		[KSPField(isPersistant = false)]
		public float thrustTime;
		
		[KSPField(isPersistant = false)]
		public float blastRadius;
		
		[KSPField(isPersistant = false)]
		public float blastForce;
		
		[KSPField(isPersistant = false)]
		public float rippleRPM;
		
		public bool drawAimer = false;
		
		Vector3 rocketPrediction = Vector3.zero;
		Texture2D aimerTexture;
		
		
		
		
		
		
		
		[KSPAction("Fire")]
		public void AGFire(KSPActionParam param)
		{
			FireRocket();	
		}
		
		[KSPEvent(guiActive = true, guiName = "Fire", active = true)]
		public void GuiFire()
		{
			FireRocket();	
		}
		
		
		public override void OnStart (PartModule.StartState state)
		{
			
			part.force_activate();
			
			aimerTexture = GameDatabase.Instance.GetTexture("BDArmory/Textures/grayCircle", false);
			
		}
		
		public override void OnFixedUpdate ()
		{
			SimulateTrajectory();
		}
		
		public void FireRocket()
		{
			if(part.RequestResource(rocketType, 1) >= 1)
			{
				GameObject rocketObj = GameDatabase.Instance.GetModel(rocketModelPath);
				rocketObj = (GameObject) Instantiate(rocketObj, transform.position+(rigidbody.velocity*Time.fixedDeltaTime), transform.rotation);
				rocketObj.transform.rotation = part.transform.rotation;
				rocketObj.transform.localScale = part.rescaleFactor * Vector3.one;
				Rocket rocket = rocketObj.AddComponent<Rocket>();
				//rocket.startVelocity = rigidbody.velocity;
				rocket.mass = rocketMass;
				rocket.blastForce = blastForce;
				rocket.blastRadius = blastRadius;
				rocket.thrust = thrust;
				rocket.thrustTime = thrustTime;
				if(vessel.targetObject!=null) rocket.targetVessel = vessel.targetObject.GetVessel();
				rocket.sourceVessel = vessel;
				rocketObj.SetActive(true);
				rocketObj.transform.SetParent(transform);
				
			}
		}
		
		
		void SimulateTrajectory()
		{
			if(BDArmorySettings.AIM_ASSIST && drawAimer && vessel.isActiveVessel)
			{
				float gAccel = (float) FlightGlobals.getGeeForceAtPosition(transform.position).magnitude;
				float simTime = 0;
				Transform fireTransform = part.transform;
				Vector3 pointingDirection = fireTransform.forward;
				Vector3 simVelocity = rigidbody.velocity;
				Vector3 simCurrPos = fireTransform.position + (rigidbody.velocity*Time.fixedDeltaTime);
				Vector3 simPrevPos = fireTransform.position + (rigidbody.velocity*Time.fixedDeltaTime);
				Vector3 simStartPos = fireTransform.position + (rigidbody.velocity*Time.fixedDeltaTime);
				bool simulating = true;
				
				List<Vector3> pointPositions = new List<Vector3>();
				pointPositions.Add(simCurrPos);
				
				while(simulating)
				{
					
					RaycastHit hit;
					simVelocity -= gAccel*FlightGlobals.getUpAxis()*Time.fixedDeltaTime;
					if(simTime > 0.04f && simTime < thrustTime)
					{
						pointingDirection = Vector3.RotateTowards(pointingDirection, simVelocity, (0.5f*(simTime)) * 50*Time.fixedDeltaTime * Mathf.Deg2Rad, 0);
						simVelocity += thrust/rocketMass * Time.fixedDeltaTime * pointingDirection;
					}
					simCurrPos += simVelocity * Time.fixedDeltaTime;
					pointPositions.Add(simCurrPos);
					if(simTime > 0.1f && Physics.Raycast(simPrevPos,simCurrPos-simPrevPos, out hit, Vector3.Distance(simPrevPos,simCurrPos), 557057))
					{
						rocketPrediction = hit.point;
						simulating = false;
						break;
					}
					
					
					simPrevPos = simCurrPos;
					
					if(Vector3.Distance(simStartPos,simCurrPos)>2500)
					{
						rocketPrediction = simStartPos + (simCurrPos-simStartPos).normalized*2500;
						simulating = false;
					}
					simTime += Time.fixedDeltaTime;
				}
				
				Debug.Log ("Rocket simulation frames: "+pointPositions.Count);
				
				if(BDArmorySettings.DRAW_DEBUG_LINES)
				{
					Vector3[] pointsArray = pointPositions.ToArray();
					if(gameObject.GetComponent<LineRenderer>()==null)
					{
						LineRenderer lr = gameObject.AddComponent<LineRenderer>();
						lr.SetWidth(.1f, .1f);
						lr.SetVertexCount(pointsArray.Length);
						for(int i = 0; i<pointsArray.Length; i++)
						{
							lr.SetPosition(i, pointsArray[i]);	
						}
					}
					else
					{
						LineRenderer lr = gameObject.GetComponent<LineRenderer>();
						lr.enabled = true;
						lr.SetVertexCount(pointsArray.Length);
						for(int i = 0; i<pointsArray.Length; i++)
						{
							lr.SetPosition(i, pointsArray[i]);	
						}	
					}
				}
				else
				{
					if(gameObject.GetComponent<LineRenderer>()!=null)
					{
						gameObject.GetComponent<LineRenderer>().enabled = false;	
					}
				}
			}
				
		}
		
		void OnGUI()
		{
			if(drawAimer && vessel.isActiveVessel)
			{
				float size = 30;
				
				Vector3 aimPosition;
				//if(BDArmorySettings.AIM_ASSIST) 
				aimPosition = Camera.main.WorldToViewportPoint(rocketPrediction);
				//else aimPosition = Camera.main.WorldToViewportPoint(pointingAtPosition);
				
				Rect drawRect = new Rect(aimPosition.x*Screen.width-(0.5f*size), (1-aimPosition.y)*Screen.height-(0.5f*size), size, size);
				float cameraAngle = Vector3.Angle(Camera.main.transform.forward, rocketPrediction-Camera.main.transform.position);
				if(cameraAngle<90) GUI.DrawTexture(drawRect, aimerTexture);
				
				
				
			}
			
		}
		
		
	}
	
	public class Rocket : MonoBehaviour
	{
		public Vessel targetVessel = null;
		public Vessel sourceVessel;
		public Vector3 startVelocity;
		public float mass;
		public float thrust;
		public float thrustTime;
		public float blastRadius;
		public float blastForce;
		float startTime;
		AudioSource audioSource;
		
		Vector3 prevPosition;
		Vector3 currPosition;
		
		float stayTime = 0.04f;
		float lifeTime = 10;
		
		void Start()
		{
			prevPosition = transform.position;
			currPosition = transform.position;
			startTime = Time.time;
			gameObject.AddComponent<Rigidbody>();
			rigidbody.mass = mass;
			rigidbody.Sleep();
			//rigidbody.velocity = startVelocity;
			if(!FlightGlobals.RefFrameIsRotating) rigidbody.useGravity = false;
			
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.loop = true;
			audioSource.minDistance = 1;
			audioSource.maxDistance = 1000;
			audioSource.dopplerLevel = 0.02f;
			audioSource.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME);
			audioSource.clip = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/rocketLoop");
		}
		
		void FixedUpdate()
		{
			if(!audioSource.isPlaying)
			{
				audioSource.Play ();	
			}
			
			//model transform. always points prograde
			transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(rigidbody.velocity, transform.up), (0.5f*(Time.time-startTime)) * 50*Time.fixedDeltaTime);
			if(!FlightGlobals.RefFrameIsRotating && Time.time-startTime > 0.5f)
			{
				transform.rotation = Quaternion.LookRotation(rigidbody.velocity);
				
			}
			//
			if(Time.time - startTime < stayTime && transform.parent!=null)
			{
				transform.rotation = transform.parent.rotation;		
				transform.position = transform.parent.position+(transform.parent.rigidbody.velocity*Time.fixedDeltaTime);
			}
			
			if(Time.time - startTime < thrustTime && Time.time-startTime > stayTime)
			{
				float random = UnityEngine.Random.Range(-.2f,.2f);
				float random2 = UnityEngine.Random.Range(-.2f,.2f);
				rigidbody.AddForce((thrust * transform.forward) + (random * transform.right) + (random2 * transform.up));
			}
			
			if(Time.time-startTime > stayTime && transform.parent!=null)
			{
				startVelocity = transform.parent.rigidbody.velocity;
				transform.parent = null;	
				rigidbody.WakeUp();
				rigidbody.velocity = startVelocity;
			}

			if(Time.time - startTime > 0.1f+stayTime)
			{
				currPosition = transform.position;
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
						Debug.Log ("Hit part: "+hitPart.name+", chance of destroy: "+destroyChance);
						if(UnityEngine.Random.Range (0f,100f)<destroyChance)
						{
							if(hitPart.vessel != sourceVessel) hitPart.explode();
						}
					}
					if(hitPart==null || (hitPart!=null && hitPart.vessel!=sourceVessel))
					{
						Detonate(hit.point);
					}
				}
			}
			prevPosition = currPosition;
			
			if(Time.time - startTime > lifeTime)
			{
				Detonate (transform.position);	
			}
			
			//proxy detonation
			if(targetVessel!=null && Vector3.Distance(transform.position, targetVessel.transform.position)< 0.5f*blastRadius)
			{
				Detonate(transform.position);	
			}
			
		}
		
		void Detonate(Vector3 pos)
		{
			ExplosionFX.CreateExplosion(pos, 1, blastRadius, blastForce);
			GameObject.Destroy(gameObject); //destroy bullet on collision
		}
		
		
		
		
		
		
		
		
	}
}

