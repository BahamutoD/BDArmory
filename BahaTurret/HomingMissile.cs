using System;
using UnityEngine;

namespace BahaTurret
{
	public class HomingMissile : MonoBehaviour
	{
		public Vessel target = null;
		public Vessel sourceVessel;
		
		public float thrust = 15;
		public float cruiseThrust = 5;
		public float dropTime = 0.4f;
		public float boostTime = 2.2f;
		public bool guidanceActive = true;
		public float maxTurnRateDPS = 15;
		
		private float startTime;
		bool exploded = false;
		bool checkMiss = false;
		
		private float prevDistance = -1;
		private AudioSource audioSource;
		//LineRenderer LR;
		
		Vector3 thrustDirection;
		
		KSPParticleEmitter[] pEmitters;
		
		//collision raycasting
		public Vector3 prevPosition;
		public Vector3 currPosition;
		
		
		//private bool launched = false;
		
		
		void Start()
		{
			thrustDirection = transform.forward;
			
			startTime = Time.time;
			boostTime += dropTime;
			audioSource = gameObject.AddComponent<AudioSource>();
			AudioClip clip = GameDatabase.Instance.GetAudioClip("BDArmory/Parts/aim-120/sounds/rocketLoop");
			audioSource.minDistance = 1;
			audioSource.maxDistance = 1000;
			audioSource.clip = clip;
			audioSource.loop = true;
			
			pEmitters = gameObject.GetComponentsInChildren<KSPParticleEmitter>();
			
			
			
			rigidbody.AddRelativeForce(Vector3.down * 2, ForceMode.VelocityChange);
			
			if(!FlightGlobals.RefFrameIsRotating)
			{
				rigidbody.useGravity = false;
			}
			
			
			
			
			//LR = gameObject.AddComponent<LineRenderer>();
			//LR.SetVertexCount(2);
			//LR.SetWidth(0.1f, 0.1f);
			
			
		}
		
		
		
		void FixedUpdate()
		{
			
			
			
			float timeIndex = Time.time-startTime;
			
			if(timeIndex > 20)
			{
				GameObject.Destroy(this.gameObject);
				return;
				
			}
			
			if(timeIndex > dropTime && timeIndex < boostTime)  //after drop, boost thrust
			{
				if(!audioSource.isPlaying)
				{
					audioSource.Play();	
				}
				//rigidbody.AddRelativeForce(thrustDirection * thrust);
				rigidbody.AddForce(thrustDirection * thrust);
				rigidbody.useGravity = false;
				
				prevPosition =	transform.position + rigidbody.velocity*Time.fixedDeltaTime;
				
			}
			
			if(timeIndex > boostTime) //cruise thrust
			{
				//rigidbody.AddRelativeForce(thrustDirection * cruiseThrust);	
				rigidbody.AddForce(thrustDirection * cruiseThrust);
				
			}
			
			
			if(timeIndex > dropTime)  //all thrusting
			{
				foreach(KSPParticleEmitter pe in pEmitters)
				{
					pe.worldVelocity = rigidbody.velocity/3f;
					pe.EmitParticle();
				}
				
				foreach(Light light in gameObject.GetComponentsInChildren<Light>())
				{
					light.intensity = 1.5f;	
				}
				
				//homing v4
				if(target!=null && guidanceActive && timeIndex > dropTime+0.8f)
				{
					try{
						
						Vector3 targetPosition = target.transform.position;
						float targetDistance = (targetPosition-transform.position).magnitude;
						if(prevDistance == -1)
						{
							prevDistance = targetDistance;
						}
						
						//increaseTurnRate on approach
						float turnRateDPS = Mathf.Clamp((3/timeIndex-dropTime)*maxTurnRateDPS, 0, maxTurnRateDPS);
						if(targetDistance<400)
						{
							turnRateDPS = Mathf.Clamp (turnRateDPS+0.2f, 0, 45);	
						}
						
						float radiansDelta = turnRateDPS*Mathf.Deg2Rad*Time.fixedDeltaTime;
						
						rigidbody.velocity = Vector3.RotateTowards(rigidbody.velocity, targetPosition-transform.position, radiansDelta, 0);
						
						//model transform. visual only
						transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(rigidbody.velocity), turnRateDPS*Time.fixedDeltaTime);
						//
						
						if(targetDistance < 100)
						{
							checkMiss = true;	
						}
						
						if((timeIndex > 5+dropTime || checkMiss) && prevDistance-targetDistance < 0) //and moving away from target??
						{
							guidanceActive = false;	
							Debug.Log ("Missile has overshot!");
						}
						
						prevDistance = targetDistance;
						
					}
					catch(NullReferenceException)
					{
						Debug.Log ("NRE: Missile guidance fail. Target out of range or unloaded");
						guidanceActive = false;
					}
				
					
					
				}
				
				
				//collision raycasting
			
				currPosition = transform.position + rigidbody.velocity*Time.fixedDeltaTime;
				float dist = (currPosition-prevPosition).magnitude;
				
				//LR.SetPosition(0, prevPosition);
				//LR.SetPosition(1, currPosition);
				
				Ray ray = new Ray(prevPosition, currPosition-prevPosition);
				RaycastHit hit;
				Vector3 adjustPos = Vector3.zero;
				
				if(Physics.Raycast(ray, out hit, dist, 557057))
				{
					Vessel hitVessel = null;
					try
					{
						hitVessel = Part.FromGO(hit.transform.gameObject).vessel;
						adjustPos = this.rigidbody.velocity * Time.fixedDeltaTime;
					}catch(NullReferenceException){}
					
					if((hitVessel==null || hitVessel!=sourceVessel) && hit.transform.gameObject!=transform.gameObject)
					{
						if(!exploded)
						{
							Debug.Log ("Missile collided with "+hit.transform.gameObject.name);
							rigidbody.AddExplosionForce(6000, hit.point+adjustPos, 500, 1, ForceMode.Impulse);
							Part dummyPart = new Part();
							FXMonger.Explode(dummyPart, hit.point+adjustPos, 5);                                  
							GameObject.Destroy(this.gameObject, Time.fixedDeltaTime);
							exploded = true;
						}
					}
				}
				prevPosition = currPosition;
			}
					
		}
		
	}
}

