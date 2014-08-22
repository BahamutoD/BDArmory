using System;
using UnityEngine;

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

