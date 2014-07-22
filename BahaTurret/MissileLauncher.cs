using System;
using UnityEngine;

namespace BahaTurret
{
	public class MissileLauncher : PartModule
	{
		
		public float timeStart = -1;
		public float timeIndex = 0;
		public float thrust = 25;
		public float cruiseThrust = 2;
		public float dropTime = 0.4f;
		public float boostTime = 2.2f;
		public float cruiseTime = 45;
		public bool guidanceActive = true;
		public bool hasFired = false;
		
		[KSPField(isPersistant = false)]
		public float blastRadius = 150;
		[KSPField(isPersistant = false)]
		public float blastPower = 25;
		
		public float maxTurnRateDPS = 50;
		bool checkMiss = false;
		private float prevDistance = -1;
		private AudioSource audioSource;
		KSPParticleEmitter[] pEmitters;
		
		private Vector3 initialObtVelocity;
		
		public Vessel target = null;
		
		//collision raycasting
		public Vector3 prevPosition;
		public Vector3 currPosition;
		
		//LineRenderer LR;
		
		
		public override void OnStart (PartModule.StartState state)
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				pEmitters = part.FindModelComponents<KSPParticleEmitter>();
				audioSource = gameObject.AddComponent<AudioSource>();
				AudioClip clip = GameDatabase.Instance.GetAudioClip("BDArmory/Parts/aim-120/sounds/rocketLoop");
				audioSource.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME);
				audioSource.minDistance = 1;
				audioSource.maxDistance = 1000;
				audioSource.clip = clip;
				audioSource.loop = true;
				//part.PhysicsSignificance = 1;
				part.force_activate();
				part.OnJustAboutToBeDestroyed += new Callback(Detonate);
				
				/*
				if(!gameObject.GetComponent<LineRenderer>())
				{
					LR = gameObject.AddComponent<LineRenderer>();
				}else
				{
					LR = gameObject.GetComponent<LineRenderer>();
				}
				LR.SetVertexCount(2);
				LR.SetPosition(0, transform.position);
				LR.SetPosition(1, transform.position);
				*/
			}
			
			
		}
		
		
		
		
		[KSPAction("Fire Missile")]
		public void AGFire(KSPActionParam param)
		{
			FireMissile();	
		}
		
		[KSPEvent(guiActive = true, guiName = "Fire Missile", active = true)]
		public void GuiFire()
		{
			FireMissile();	
		}
		
		
		public void FireMissile()
		{
			if(!hasFired)
			{
				part.force_activate();
				Vessel sourceVessel = vessel;
				try{
					target = vessel.targetObject.GetVessel();
				}catch(NullReferenceException){}
				part.decouple(2);
				part.rigidbody.velocity = sourceVessel.rigidbody.velocity;
				part.rigidbody.AddRelativeForce(Vector3.down * 2, ForceMode.VelocityChange);
				
				if(timeStart == -1) timeStart = Time.time;
				hasFired = true;
				
				
			}
			
		}
		
	
		
		
		
		public override void OnFixedUpdate()
		{
			
			if(hasFired)
			{
				timeIndex = Time.time - timeStart;
				
				if(timeIndex < dropTime) //drop phase
				{
					//transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(rigidbody.velocity), .05f);
					initialObtVelocity = vessel.obt_velocity;
				}
				
				if(timeIndex > dropTime && timeIndex < boostTime) //boost phase
				{
					rigidbody.AddRelativeForce(thrust * Vector3.forward);
					
				}
				
				if(timeIndex > dropTime + boostTime && timeIndex < dropTime + boostTime + cruiseTime) //cruise phase
				{
					rigidbody.AddRelativeForce(cruiseThrust * Vector3.forward);
				}
				
				if(timeIndex > dropTime && timeIndex < dropTime + boostTime + cruiseTime) //all thrust
				{
					//light, sound & particle fx
					if(!audioSource.isPlaying)	
					{
						audioSource.Play();	
					}
					foreach(Light light in gameObject.GetComponentsInChildren<Light>())
					{
						light.intensity = 1.5f;	
					}
					foreach(KSPParticleEmitter pe in pEmitters)
					{
						pe.EmitParticle();	
					}
					
				}
				else
				{
					if(audioSource.isPlaying)
					{
						audioSource.Stop ();
					}
					foreach(Light light in gameObject.GetComponentsInChildren<Light>())
					{
						light.intensity = 0;	
					}
				}
				
				
				
				
				if(timeIndex > dropTime) //guidance
				{
					//model transform. always points prograde
					transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vessel.srf_velocity, transform.up), 50*Time.fixedDeltaTime);
					if(!FlightGlobals.RefFrameIsRotating)
					{
						transform.rotation = Quaternion.LookRotation(rigidbody.velocity);
					}
					//
					
					
					/*
					if (vessel.vesselType == VesselType.Debris)
					{
						vessel.vesselType = VesselType.Ship;	
					}
					*/
					
					if(target!=null && guidanceActive && timeIndex - dropTime > 0.5f)
					{
						try{
							
							Vector3 targetPosition = target.transform.position;
							float targetDistance = (targetPosition-transform.position).magnitude;
							if(prevDistance == -1)
							{
								prevDistance = targetDistance;
							}
							
							
							
							if(targetDistance > 100) //guide towards where the target is going to be
							{
								targetPosition = target.transform.position + target.srf_velocity*(1/((target.rigidbody.velocity-rigidbody.velocity).magnitude/targetDistance));
							}
							
							/*
							LR.SetPosition(0, transform.position);
							LR.SetPosition(1, targetPosition);
							*/
							
							//increaseTurnRate on approach
							float turnRateDPS = Mathf.Clamp((3/timeIndex-dropTime-.05f)*maxTurnRateDPS, 0, maxTurnRateDPS);
							if(targetDistance<1000)
							{
								turnRateDPS = Mathf.Clamp (turnRateDPS+11f, 0, 90);	
							}
							
							float radiansDelta = turnRateDPS*Mathf.Deg2Rad*Time.fixedDeltaTime;
							
							rigidbody.velocity = Vector3.RotateTowards(rigidbody.velocity, targetPosition-transform.position, radiansDelta, 0);
							
							if(target.checkLanded() && targetDistance > (vessel.srfSpeed*12.7f)+target.altitude && vessel.verticalSpeed < 0 && vessel.altitude-target.altitude < 1000)// && vessel.altitude < 500)  //prevent from dropping to land
							{
								rigidbody.AddForce(FlightGlobals.upAxis * rigidbody.mass * (FlightGlobals.getGeeForceAtPosition(transform.position).magnitude + 20f));
							}
							if(targetDistance < 100)
							{
								checkMiss = true;	
							}
							
							if(checkMiss && prevDistance-targetDistance < 0) //and moving away from target??
							{
								//proximity detonation
								if(targetDistance < 30) part.explode();
								//guidanceActive = false;	
								//Debug.Log ("Missile has overshot!");
							}
							
							prevDistance = targetDistance;
							
							
							
						}
						catch(NullReferenceException)
						{
							Debug.Log ("NRE: Missile guidance fail. Target out of range or unloaded");
							guidanceActive = false;
						}
					
						
						
					}
					
					
				}
				
			}
		}
		
		
		public void Detonate()
		{
			//Debug.Log ("===========Missile detonating============");
			
			Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius, 557057);
			foreach(Collider col in colliders)
			{
				Rigidbody rb = null;
				try
				{
					rb = col.gameObject.GetComponentUpwards<Rigidbody>();
				}catch(NullReferenceException){}
				if(rb!=null)
				{
					rb.AddExplosionForce(blastPower, transform.position, blastRadius, 0, ForceMode.Impulse);
					ExplosionFX.CreateExplosion(transform.position);
					//FXMonger.Explode(this.part, rb.transform.position, 5); 
				}
			}
			
			                                 
		}
		
	}
}

