using System;
using UnityEngine;

namespace BahaTurret
{
	public class MissileLauncher : PartModule
	{
		public bool team;
		
		public float timeStart = -1;
		public float timeIndex = 0;
		
		[KSPField(isPersistant = false)]
		public float thrust = 30;
		[KSPField(isPersistant = false)]
		public float cruiseThrust = 3;
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Drop Time"),
        	UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
		public float dropTime = 0.4f;
		[KSPField(isPersistant = false)]
		public float boostTime = 2.2f;
		[KSPField(isPersistant = false)]
		public float cruiseTime = 45;
		[KSPField(isPersistant = false)]
		public bool guidanceActive = true;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Decouple Speed"),
        	UI_FloatRange(minValue = 0f, maxValue = 8f, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
		public float decoupleSpeed = 0;
		
		
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
		
		[KSPField(isPersistant = false)]
		public bool hasRCS = false;
		KSPParticleEmitter upRCS;
		KSPParticleEmitter downRCS;
		KSPParticleEmitter leftRCS;
		KSPParticleEmitter rightRCS;
		KSPParticleEmitter forwardRCS;
		float rcsAudioMinInterval = 0.06f;
		float rcsAudioTimer = 0;
		
		public Vessel sourceVessel = null;
		bool checkMiss = false;
		bool hasExploded = false;
		bool targetInView = false;
		private float prevDistance = -1;
		private AudioSource audioSource;
		public AudioSource sfAudioSource;
		KSPParticleEmitter[] pEmitters;
		
		private Vector3 initialObtVelocity;
		
		public GameObject target = null;
		public Vessel targetVessel = null;
		public bool hasFired = false;
		Vector3 targetPosition;
		
		bool startedEngine = false;
		
		LineRenderer LR;
		float cmTimer;
		
		//deploy animation
		[KSPField(isPersistant = false)]
		public string deployAnimationName = "";
		
		[KSPField(isPersistant = false)]
		public float deployedDrag = 0.02f;
		
		[KSPField(isPersistant = false)]
		public float deployTime = 0.2f;
		
		public bool deployed;
		public float deployedTime;
		
		AnimationState[] deployStates;
		
		bool hasPlayedFlyby = false;
		
		
		
		public override void OnStart (PartModule.StartState state)
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				pEmitters = part.FindModelComponents<KSPParticleEmitter>();
				audioSource = gameObject.AddComponent<AudioSource>();
				audioSource.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME);
				audioSource.minDistance = 1;
				audioSource.maxDistance = 2000;
				audioSource.loop = true;
				audioSource.dopplerLevel = 0.1f;
				audioSource.pitch = 1.6f;
				audioSource.priority = 255;
			
				if(audioClipPath!="")
				{
					audioSource.clip = GameDatabase.Instance.GetAudioClip(audioClipPath);
				}
				
				sfAudioSource = gameObject.AddComponent<AudioSource>();
				sfAudioSource.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME);
				sfAudioSource.minDistance = 1;
				sfAudioSource.maxDistance = 2000;
				sfAudioSource.dopplerLevel = 0;
				sfAudioSource.priority = 230;
				
				
				cmTimer = Time.time;
				
				part.force_activate();
				part.OnJustAboutToBeDestroyed += new Callback(Detonate);
				
			
				
				
				foreach(var pe in pEmitters)	
				{
					if(hasRCS)
					{
						if(pe.gameObject.name == "rcsUp") upRCS = pe;
						else if(pe.gameObject.name == "rcsDown") downRCS = pe;
						else if(pe.gameObject.name == "rcsLeft") leftRCS = pe;
						else if(pe.gameObject.name == "rcsRight") rightRCS = pe;
						else if(pe.gameObject.name == "rcsForward") forwardRCS = pe;
					}
					
					if(!pe.gameObject.name.Contains("rcs") && !pe.useWorldSpace)
					{
						pe.sizeGrow = 99999;
					}
				}
				
				if(hasRCS) KillRCS();
			}
			
			if(part.partInfo.title.Contains("Bomb"))
			{
				Fields["dropTime"].guiActive = false;
				Fields["dropTime"].guiActiveEditor = false;
			}
			
			if(deployAnimationName != "")
			{
				deployStates = Misc.SetUpAnimation(deployAnimationName, part);
			}
			else
			{
				deployedDrag = part.maximum_drag;	
			}
		}
		
		[KSPAction("Fire Missile")]
		public void AGFire(KSPActionParam param)
		{
			FireMissile();	
			if(BDArmorySettings.Instance.wpnMgr!=null) BDArmorySettings.Instance.wpnMgr.UpdateList();
		}
		
		[KSPEvent(guiActive = true, guiName = "Fire Missile", active = true)]
		public void GuiFire()
		{
			FireMissile();	
			if(BDArmorySettings.Instance.wpnMgr!=null) BDArmorySettings.Instance.wpnMgr.UpdateList();
		}
		
		
		public void FireMissile()
		{
			if(!hasFired)
			{
				foreach(var wpm in vessel.FindPartModulesImplementing<MissileFire>())
				{
					team = wpm.team;	
				}
				
				sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/deployClick"));
				
				sourceVessel = vessel;
				
				if(vessel.targetObject!=null)
				{
					try
					{
						target = vessel.targetObject.GetVessel().gameObject;
					}
					catch(NullReferenceException)
					{
						vessel.targetObject = null;
						target = null;
					}
					
				}
				
				part.decouple(0);
				
				part.force_activate();
				
				vessel.rigidbody.velocity += decoupleSpeed * -part.transform.up;
				
				BDArmorySettings.Instance.ApplyPhysRange();
				vessel.vesselName = part.partInfo.title + " (fired)";
				vessel.vesselType = VesselType.Probe;
				
				if(timeStart == -1) timeStart = Time.time;
				hasFired = true;
				
				
			}
		}
		
		public void FireMissileOnTarget(Vessel v)
		{
			if(!hasFired)
			{
				FireMissile();
				target = v.gameObject;
			}
		}
		
	
		
		
		
		public override void OnFixedUpdate()
		{
			
			if(hasFired && !hasExploded && part!=null)
			{
				
				//flybyaudio
				float mCamDistance = Vector3.Distance(FlightCamera.fetch.mainCamera.transform.position, transform.position);
				float mCamRelV = Vector3.Distance(FlightGlobals.ActiveVessel.srf_velocity, vessel.srf_velocity);
				if(!hasPlayedFlyby && FlightGlobals.ActiveVessel != vessel && mCamDistance < 400 && mCamRelV > 343  && mCamRelV < 800 && Vector3.Angle(rigidbody.velocity, FlightGlobals.ActiveVessel.transform.position-transform.position)<60)
				{
					Debug.LogWarning ("mCamRelV: "+mCamRelV);
					sfAudioSource.PlayOneShot (GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/missileFlyby"));	
					hasPlayedFlyby = true;
				}
				
				
				rigidbody.isKinematic = false;
				if(!vessel.loaded) vessel.Load();
				
				
				timeIndex = Time.time - timeStart;
				
				//deploy stuff
				if(deployAnimationName != "" && timeIndex > deployTime && !deployed)
				{
					deployed = true;
					deployedTime = Time.time;
				}
				
				if(deployed)
				{
					foreach(var anim in deployStates)
					{
						anim.speed = 1;
						part.maximum_drag = deployedDrag;
						part.minimum_drag = deployedDrag;
					}	
				}
				
				if(timeIndex < dropTime) //drop phase
				{
					//transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(rigidbody.velocity), .05f);
					initialObtVelocity = vessel.obt_velocity;
				}
				
				if(timeIndex > dropTime && timeIndex < boostTime) //boost phase
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
					rigidbody.AddRelativeForce(thrust * Vector3.forward);
					if(hasRCS) forwardRCS.emit = true;
					if(!startedEngine && thrust > 0)
					{
						startedEngine = true;
						sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/launch"));
					}
					
				}
				
				if(timeIndex > dropTime + boostTime && timeIndex < dropTime + boostTime + cruiseTime) //cruise phase
				{
					if(!hasRCS) rigidbody.AddRelativeForce(cruiseThrust * Vector3.forward);
					else
					{
						forwardRCS.emit = false;
						audioSource.Stop();
					}
				}
				
				if(timeIndex > dropTime && timeIndex < dropTime + boostTime + cruiseTime) //all thrust
				{
					
					if(timeIndex > dropTime + .1f) audioSource.pitch = Mathf.Lerp(audioSource.pitch, 1f, 0.2f);
					
					
					
					if(!hasRCS)
					{
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
					
					foreach(KSPParticleEmitter pe in pEmitters)
					{
						if(!pe.gameObject.name.Contains("rcs") && !pe.useWorldSpace)
						{
							pe.sizeGrow = Mathf.Lerp(pe.sizeGrow, 1, 0.3f);
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
				if(timeIndex > dropTime + boostTime + cruiseTime && !hasRCS)
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
					if(!hasRCS)
					{
						transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vessel.srf_velocity, transform.up), atmosMultiplier * (0.5f*(timeIndex-dropTime)) * 50*Time.fixedDeltaTime);
					}
					else
					{
						transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vessel.srf_velocity, transform.up), (0.5f*(timeIndex-dropTime)) * 50*Time.fixedDeltaTime);
					}
					
					if(!FlightGlobals.RefFrameIsRotating && timeIndex - dropTime > 0.5f && FlightGlobals.ActiveVessel!=vessel)
					{
						if(!hasRCS)
						{
							transform.rotation = Quaternion.Lerp (transform.rotation, Quaternion.LookRotation(rigidbody.velocity), atmosMultiplier/2.5f);
						}
						
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
							targetPosition = target.transform.position + target.rigidbody.velocity*Time.fixedDeltaTime;
							
							
							//target CoM
							bool targetIsVessel = false;
							Vessel tVessel = null;
							Part p = null;
							Vector3 targetCoMPos;
							try
							{
								p = Part.FromGO(target);	
							}
							catch(NullReferenceException){}
							if(p!=null)
							{
								targetIsVessel = true;
								tVessel = p.vessel;
								targetCoMPos = p.vessel.findWorldCenterOfMass();
								targetPosition = targetCoMPos+target.rigidbody.velocity*Time.fixedDeltaTime;
							}
							float targetViewAngle = Vector3.Angle (transform.forward, targetPosition-transform.position);
							targetInView = (targetViewAngle < 20);
							LookForCountermeasure();
							
							float targetDistance = Vector3.Distance(targetPosition, transform.position+rigidbody.velocity*Time.fixedDeltaTime);
							if(prevDistance == -1)
							{
								prevDistance = targetDistance;
							}
							
							if(targetDistance > 10 && targetInView) //guide towards where the target is going to be
							{
								targetPosition = targetPosition + target.rigidbody.velocity*(1/((target.rigidbody.velocity-vessel.rigidbody.velocity).magnitude/targetDistance));
								
							}
							
							//increaseTurnRate after launch
							float turnRateDPS = Mathf.Clamp(((timeIndex-dropTime)/boostTime)*maxTurnRateDPS, 0, maxTurnRateDPS);
							if(!hasRCS) turnRateDPS *= atmosMultiplier;
							
							//decrease turn rate after thrust cuts out
							if(timeIndex > dropTime+boostTime+cruiseTime)
							{
								turnRateDPS = atmosMultiplier * Mathf.Clamp(maxTurnRateDPS - ((timeIndex-dropTime-boostTime-cruiseTime)*1.4f), 1, maxTurnRateDPS);	
								if(hasRCS) turnRateDPS = 0;
							}
							
							if(hasRCS)
							{
								
								if(turnRateDPS > 0) DoRCS();
								else KillRCS();
							}
							
							float radiansDelta = turnRateDPS*Mathf.Deg2Rad*Time.fixedDeltaTime;
							
							if(hasRCS) transform.rotation = Quaternion.RotateTowards (transform.rotation, Quaternion.LookRotation(rigidbody.velocity, transform.up), turnRateDPS);
							
							rigidbody.velocity = Vector3.RotateTowards(rigidbody.velocity, targetPosition-transform.position, radiansDelta, 0);
							
							//proximity detonation
							if(((targetIsVessel && !tVessel.Landed) || !targetIsVessel) && targetDistance < blastRadius/3) 
							{
								//Debug.Log("Proximity detonating! Target Distance: "+Vector3.Distance(target.transform.position, transform.position)+", blast radius: "+blastRadius);
								Detonate();
								return;
							}
							
							if(targetDistance < 200)
							{
								checkMiss = true;	
							}
							
							//kill guidance if missile has missed
							if(checkMiss && prevDistance-targetDistance < 0) 
							{
									guidanceActive = false;
									if(hasRCS) KillRCS();
							}
							
							prevDistance = targetDistance;
							
							if(BDArmorySettings.DRAW_DEBUG_LINES)
							{
								if(!gameObject.GetComponent<LineRenderer>())
								{
									LR = gameObject.AddComponent<LineRenderer>();
									LR.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
									LR.material.SetColor("_EmissiveColor", Color.red);
								}else
								{
									LR = gameObject.GetComponent<LineRenderer>();
								}
								LR.SetVertexCount(2);
								LR.SetPosition(0, transform.position+rigidbody.velocity*Time.fixedDeltaTime);
								LR.SetPosition(1, targetPosition);
								//Debug.Log ("Turn rate: "+turnRateDPS.ToString("0.00"));
							}
							
							
						}
						else
						{
							Debug.Log ("Missile guidance fail. Target out of range or unloaded");
							guidanceActive = false;
							if(hasRCS) KillRCS();
						}
					
					}
					
					if(hasRCS && !guidanceActive)
					{
						KillRCS();	
					}
				}
			}
		}
		
		
		
		public void Detonate()
		{
			if(!hasExploded)
			{
				hasExploded = true;
				if(part!=null) part.temperature = part.maxTemp + 100;
				Vector3 position = transform.position+rigidbody.velocity*Time.fixedDeltaTime;
				if(sourceVessel==null) sourceVessel = vessel;
				ExplosionFX.CreateExplosion(position, explosionSize, blastRadius, blastPower, sourceVessel, transform.forward);
				
			}

		}
		
		public static bool CheckIfMissile(Part p)
		{
			
			if(p.GetComponent<MissileLauncher>())
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
					float flareAcquireMaxRange = 2500;
					float chanceFactor = BDArmorySettings.FLARE_CHANCE_FACTOR;
					float chance = Mathf.Clamp(chanceFactor-(Vector3.Distance(flare.transform.position, transform.position)/(flareAcquireMaxRange/chanceFactor)), 0, chanceFactor);
					chance -= UnityEngine.Random.Range(0f, chance);
					bool chancePass = (flare.GetComponent<CMFlare>().acquireDice < chance);
					float angle = Vector3.Angle(transform.forward, flare.transform.position-transform.position);
					if(angle < 45 && Vector3.Distance(flare.transform.position, transform.position) < 2500 && chancePass && targetInView)
					{
						Debug.Log ("=Missile deflected via flare=");
						//target = flare;
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
		
		void DoRCS()
		{
			Quaternion velRotation = Quaternion.LookRotation(rigidbody.velocity, transform.up);
			Vector3 offset = Quaternion.Inverse(velRotation) * (targetPosition-transform.position);
			
			bool playRcsAudio = (Time.time-rcsAudioTimer > rcsAudioMinInterval);
			
			
			if(offset.x > 1)
			{
				if(!rightRCS.emit && playRcsAudio) 
				{
					sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/popThrust"));
					rcsAudioTimer = Time.time;
				}
				
				rightRCS.emit = true;
				leftRCS.emit = false;
			}
			else if(offset.x < -1)
			{
				if(!leftRCS.emit && playRcsAudio)
				{
					sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/popThrust"));
					rcsAudioTimer = Time.time;
				}
				rightRCS.emit = false;
				leftRCS.emit = true;
			}
			else
			{
				rightRCS.emit = false;
				leftRCS.emit = false;
			}
			
			if(offset.y > 1)
			{
				if(!upRCS.emit && playRcsAudio) 
				{
					sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/popThrust"));
					rcsAudioTimer = Time.time;
				}
				upRCS.emit = true;
				downRCS.emit = false;
			}
			else if(offset.y < -1)
			{
				if(!downRCS.emit && playRcsAudio) 
				{
					sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/popThrust"));
					rcsAudioTimer = Time.time;
				}
				upRCS.emit = false;
				downRCS.emit = true;
			}
			else
			{
				upRCS.emit = false;
				downRCS.emit = false;
			}
			
			//Debug.Log ("offset: "+offset.x.ToString("0.000")+", "+offset.y.ToString("0.000"));
		}
		
		void KillRCS()
		{
			upRCS.emit = false;
			downRCS.emit = false;
			leftRCS.emit = false;
			rightRCS.emit = false;
		}
		
		
		
		
	}
}

