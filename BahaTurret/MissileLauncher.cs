using System;
using UnityEngine;

namespace BahaTurret
{
	public class MissileLauncher : PartModule
	{
		public string team;
		
		public float timeStart = -1;
		public float timeIndex = 0;
		
		[KSPField(isPersistant = false)]
		public float thrust = 30;
		[KSPField(isPersistant = false)]
		public float cruiseThrust = 3;
		[KSPField(isPersistant = false)]
		public float dropTime = 0.4f;
		[KSPField(isPersistant = false)]
		public float boostTime = 2.2f;
		[KSPField(isPersistant = false)]
		public float cruiseTime = 45;
		[KSPField(isPersistant = false)]
		public bool guidanceActive = true;
		
		
		[KSPField(isPersistant = false)]
		public float blastRadius = 150;
		[KSPField(isPersistant = false)]
		public float blastPower = 25;
		[KSPField(isPersistant = false)]
		public float maxTurnRateDPS = 20;
		
		[KSPField(isPersistant = false)]
		public string audioClipPath = "";
		
		[KSPField(isPersistant = false)]
		public int explosionSize = 1;
		
		
		bool checkMiss = false;
		bool hasExploded = false;
		private float prevDistance = -1;
		private AudioSource audioSource;
		KSPParticleEmitter[] pEmitters;
		
		private Vector3 initialObtVelocity;
		
		public GameObject target = null;
		public Vessel targetVessel = null;
		public bool hasFired = false;
		Vector3 targetPosition;
		
		LineRenderer LR;
		bool debug = false;
		float cmTimer;
		
		bool hasUpdatedPhysRange = false;
		
		
		public override void OnStart (PartModule.StartState state)
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				pEmitters = part.FindModelComponents<KSPParticleEmitter>();
				audioSource = gameObject.AddComponent<AudioSource>();
				audioSource.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME);
				audioSource.minDistance = 1;
				audioSource.maxDistance = 1000;
				audioSource.loop = true;
				audioSource.dopplerLevel = 0.3f;
				if(audioClipPath!="")
				{
					
					AudioClip clip = GameDatabase.Instance.GetAudioClip(audioClipPath);
					audioSource.clip = clip;

				}
				
				
				
				cmTimer = Time.time;
				
				part.force_activate();
				part.OnJustAboutToBeDestroyed += new Callback(Detonate);
				
				if(debug)
				{
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
					LR.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
					LR.material.SetColor("_EmissiveColor", Color.red);
				}
				
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
				foreach(var wpm in vessel.FindPartModulesImplementing<MissileFire>())
				{
					team = wpm.team;	
				}
				
				audioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/deployClick"));
				part.force_activate();
				Vessel sourceVessel = vessel;
				try{
					target = vessel.targetObject.GetVessel().gameObject;
				}catch(NullReferenceException){}
				part.decouple(50);
				
				
				if(timeStart == -1) timeStart = Time.time;
				hasFired = true;
			}
		}
		
		public void FireMissileOnTarget(Vessel v)
		{
			if(!hasFired)
			{
				foreach(var wpm in vessel.FindPartModulesImplementing<MissileFire>())
				{
					team = wpm.team;	
				}
				
				audioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/deployClick"));
				part.force_activate();
				Vessel sourceVessel = vessel;
				target = v.gameObject;
				part.decouple(5);
				
				if(timeStart == -1) timeStart = Time.time;
				hasFired = true;
			}
		}
		
	
		
		
		
		public override void OnFixedUpdate()
		{
			if(hasExploded)
			{
				part.explode();
			}
			
			
			if(hasFired)
			{
				if(!hasUpdatedPhysRange)
				{
					BDArmorySettings.ApplyPhysRange();
					hasUpdatedPhysRange = true;
					vessel.vesselName = part.partInfo.title + " (fired)";
				}
				rigidbody.isKinematic = false;
				vessel.vesselType = VesselType.Probe;
				if(!vessel.loaded) vessel.Load();
				vessel.ResumeTarget();
				
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
					if(audioClipPath!="" && !audioSource.isPlaying)	
					{
						audioSource.Play();	
					}
					foreach(Light light in gameObject.GetComponentsInChildren<Light>())
					{
						light.intensity = 1.5f;	
					}
					
					foreach(KSPParticleEmitter pe in pEmitters)
					{
						if(vessel.atmDensity > 0)
						{
							pe.emit = true;
						}
						else
						{
							if(pe.useWorldSpace) pe.emit = false;
							else pe.emit = true;
						}
						
					}
					
					
				}
				else
				{
					if(audioClipPath != "" && audioSource.isPlaying)
					{
						audioSource.Stop ();
					}
					foreach(Light light in gameObject.GetComponentsInChildren<Light>())
					{
						light.intensity = 0;	
					}
				}
				if(timeIndex > dropTime + boostTime + cruiseTime)
				{
					foreach(KSPParticleEmitter pe in pEmitters)
					{
						pe.maxEmission = 0;
						pe.minEmission = 0;
					}	
				}
				
				
				
				
				if(timeIndex > dropTime) //guidance
				{
					part.crashTolerance = 1;
					
					//guidance and attitude stabilisation scales to atmospheric density.
					float atmosMultiplier = Mathf.Clamp01 (2.5f*(float)FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(transform.position))); 
					
					//model transform attitude stabilisation 
					transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vessel.srf_velocity, transform.up), atmosMultiplier * (0.5f*(timeIndex-dropTime)) * 50*Time.fixedDeltaTime);
					if(!FlightGlobals.RefFrameIsRotating && timeIndex - dropTime > 0.5f && FlightGlobals.ActiveVessel!=vessel)
					{
						transform.rotation = Quaternion.Lerp (transform.rotation, Quaternion.LookRotation(rigidbody.velocity), atmosMultiplier/2.5f);
						
					}
					//
					
					
					
					if(target!=null && guidanceActive && timeIndex - dropTime > 0.5f)
					{
						WarnTarget();
						
						if(Vector3.Distance(target.transform.position, transform.position) < BDArmorySettings.PHYSICS_RANGE){
							/*
							if(!FlightGlobals.RefFrameIsRotating && FlightGlobals.ActiveVessel==vessel)
							{
								FlightGlobals.ForceSetActiveVessel(target);
							}
							*/
							targetPosition = target.transform.position;
							LookForCountermeasure();
							
							float targetDistance = (targetPosition-transform.position).magnitude;
							if(prevDistance == -1)
							{
								prevDistance = targetDistance;
							}
							
							if(targetDistance > 10) //guide towards where the target is going to be
							{
								targetPosition = targetPosition + target.rigidbody.velocity*(1/((target.rigidbody.velocity-vessel.rigidbody.velocity).magnitude/targetDistance));
								if(!FlightGlobals.RefFrameIsRotating)
								{
									targetPosition = target.transform.position + target.rigidbody.velocity*(1/((target.rigidbody.velocity-rigidbody.velocity).magnitude/targetDistance));
								}
							}
							
							//increaseTurnRate after launch
							float turnRateDPS = atmosMultiplier * Mathf.Clamp(((timeIndex-dropTime)/boostTime)*maxTurnRateDPS, 0, maxTurnRateDPS);
							//decrease turn rate after thrust cuts out
							if(timeIndex > dropTime+boostTime+cruiseTime)
							{
								turnRateDPS = atmosMultiplier * Mathf.Clamp(maxTurnRateDPS - ((timeIndex-dropTime-boostTime-cruiseTime)*2), 1, maxTurnRateDPS);	
							}
							
							float radiansDelta = turnRateDPS*Mathf.Deg2Rad*Time.fixedDeltaTime;
							
							rigidbody.velocity = Vector3.RotateTowards(rigidbody.velocity, targetPosition-transform.position, radiansDelta, 0);
							
							if(targetDistance < 500)
							{
								checkMiss = true;	
							}
							
							if(checkMiss && prevDistance-targetDistance < 0) //and moving away from target??
							{
								//proximity detonation
								if(targetDistance < 15) part.explode();
								else
								{
									guidanceActive = false;
								}
								
							}
							
							prevDistance = targetDistance;
							
							if(debug)
							{
								LR.SetPosition(0, transform.position);
								LR.SetPosition(1, targetPosition);
								Debug.Log ("Turn rate: "+turnRateDPS.ToString("0.00"));
							}
							
							
						}
						else
						{
							Debug.Log ("Missile guidance fail. Target out of range or unloaded");
							guidanceActive = false;
						}
					
					}
				}
			}
		}
		
		
		
		public void Detonate()
		{
			if(BDACameraTools.lastProjectileFired==this.part) BDACameraTools.lastProjectileFired = null;
			//Debug.Log ("===========Missile detonating============");
			if(!hasExploded)
			{
				hasExploded = true;
				
				ExplosionFX.CreateExplosion(transform.position, explosionSize, blastRadius, blastPower);
			}

		}
		
		public static bool CheckIfMissile(Part p)
		{
			
			if(p.FindModulesImplementing<MissileLauncher>().Count > 0)
			{
				return true;
			}
			else return false;
			
		}
		
		void LookForCountermeasure()
		{
			foreach(GameObject flare in BDArmorySettings.Flares)
			{
				if(flare!=null)
				{
					float angle = Vector3.Angle(transform.forward, flare.transform.position-transform.position);
					if(angle < 20 && Vector3.Distance(flare.transform.position, transform.position) < 2000)
					{
						//Debug.Log ("CMflare detected");
						targetPosition = flare.transform.position;
						return;
					}
				}
			}
			
			
		}
		
		void WarnTarget()
		{
			if(targetVessel == null)
			{
				if(FlightGlobals.ActiveVessel.gameObject == target) targetVessel = FlightGlobals.ActiveVessel;	
			}
			
			if(targetVessel!=null)
			{
				foreach(var wpm in targetVessel.FindPartModulesImplementing<MissileFire>())
				{
					wpm.MissileWarning();
					break;
				}
			}
		}
		
		
	}
}

