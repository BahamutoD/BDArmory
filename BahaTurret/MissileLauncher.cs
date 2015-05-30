using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{

	
	public class MissileLauncher : PartModule
	{
		public enum MissileStates{Idle, Drop, Boost, Cruise, PostThrust}

		public bool team;
		
		public float timeStart = -1;
		public float timeIndex = 0;

		//aero
		[KSPField(isPersistant = false)]
		public bool aero = false;
		[KSPField(isPersistant = false)]
		public float liftArea = 0.015f;
		[KSPField(isPersistant = false)]
		public float steerMult = 0.5f;
		Vector3 aeroTorque = Vector3.zero;

		[KSPField(isPersistant = false)]
		public float maxTorque = 90;
		//

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
		[KSPField(isPersistant = false)]
		public float maxOffBoresight = 45;
		
		[KSPField(isPersistant = false)]
		public float maxAoA = 35;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Decouple Speed"),
        	UI_FloatRange(minValue = 0f, maxValue = 10f, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
		public float decoupleSpeed = 0;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Direction: "), 
			UI_Toggle(disabledText = "Lateral", enabledText = "Forward")]
		public bool decoupleForward = false;
		
		[KSPField(isPersistant = false)]
		public string homingType = "AAM";
		[KSPField(isPersistant = false)]
		public float optimumAirspeed = 220;
		
		[KSPField(isPersistant = false)]
		public float blastRadius = 150;
		[KSPField(isPersistant = false)]
		public float blastPower = 25;
		[KSPField(isPersistant = false)]
		public float maxTurnRateDPS = 20;
		
		[KSPField(isPersistant = false)]
		public string audioClipPath = string.Empty;

		AudioClip thrustAudio;

		[KSPField(isPersistant = false)]
		public string boostClipPath = string.Empty;

		AudioClip boostAudio;
		
		[KSPField(isPersistant = false)]
		public bool isSeismicCharge = false;
		
		[KSPField(isPersistant = false)]
		public float rndAngVel = 0;
		
		[KSPField(isPersistant = false)]
		public bool isTimed = false;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Detonation Time"),
        	UI_FloatRange(minValue = 2f, maxValue = 30f, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
		public float detonationTime = 2;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cruise Altitude"),
		 UI_FloatRange(minValue = 50f, maxValue = 1500f, stepIncrement = 10f, scene = UI_Scene.All)]
		public float cruiseAltitude = 300;

		[KSPField(isPersistant = false)]
		public string rotationTransformName = string.Empty;
		Transform rotationTransform;

		[KSPField(isPersistant = false)]
		public bool terminalManeuvering = false;
		
		
		
		[KSPField(isPersistant = false)]
		public string explModelPath = "BDArmory/Models/explosion/explosion";
		
		public string explSoundPath = "BDArmory/Sounds/explode1";
			
		
		[KSPField(isPersistant = false)]
		public bool spoolEngine = false;
		
		[KSPField(isPersistant = false)]
		public bool hasRCS = false;
		[KSPField(isPersistant = false)]
		public float rcsThrust = 1;
		float rcsRVelThreshold = 0.13f;
		KSPParticleEmitter upRCS;
		KSPParticleEmitter downRCS;
		KSPParticleEmitter leftRCS;
		KSPParticleEmitter rightRCS;
		KSPParticleEmitter forwardRCS;
		float rcsAudioMinInterval = 0.2f;

		public Vessel sourceVessel = null;
		bool checkMiss = false;
		bool hasExploded = false;
		bool targetInView = false;
		private float prevDistance = -1;
		private AudioSource audioSource;
		public AudioSource sfAudioSource;
		List<KSPParticleEmitter> pEmitters;
		List<BDAGaplessParticleEmitter> gaplessEmitters;
		
		//private Vector3 initialObtVelocity;
		
		public GameObject target = null;
		public Vessel targetVessel = null;
		public MissileFire targetMf = null;
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

		[KSPField(isPersistant = false)]
		public bool useSimpleDrag = false;

		[KSPField(isPersistant = false)]
		public Vector3 simpleCoD = new Vector3(0,0,-1);

		[KSPField(isPersistant = false)]
		public float agmDescentRatio = 1.45f;
		
		float currentThrust = 0;
		
		public bool deployed;
		public float deployedTime;
		
		AnimationState[] deployStates;
		
		bool hasPlayedFlyby = false;
		
		Quaternion previousRotation;
		
		float debugTurnRate = 0;
		string debugString = "";
		
		Vector3 randomOffset = Vector3.zero;


		[KSPField(isPersistant = false)]
		public string boostTransformName = string.Empty;
		List<KSPParticleEmitter> boostEmitters;

		
		public MissileStates MissileState = MissileStates.Idle;
		
		public override void OnStart (PartModule.StartState state)
		{
			gaplessEmitters = new List<BDAGaplessParticleEmitter>();
			pEmitters = new List<KSPParticleEmitter>();
			boostEmitters = new List<KSPParticleEmitter>();

			if(isTimed)
			{
				Fields["detonationTime"].guiActive = true;
				Fields["detonationTime"].guiActiveEditor = true;
			}
			else
			{
				Fields["detonationTime"].guiActive = false;
				Fields["detonationTime"].guiActiveEditor = false;
			}
			
			if(HighLogic.LoadedSceneIsFlight)
			{
				foreach(var emitter in part.FindModelComponents<KSPParticleEmitter>())
				{
					if(emitter.useWorldSpace)
					{
						BDAGaplessParticleEmitter gaplessEmitter = emitter.gameObject.AddComponent<BDAGaplessParticleEmitter>();	
						gaplessEmitter.part = part;
						gaplessEmitters.Add (gaplessEmitter);
					}
					else
					{
						if(emitter.transform.name != boostTransformName)
						{
							pEmitters.Add(emitter);	
						}
						else
						{
							boostEmitters.Add(emitter);
						}
					}
				}
				//pEmitters = part.FindModelComponents<KSPParticleEmitter>();
				
				audioSource = gameObject.AddComponent<AudioSource>();
				audioSource.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME)+0.1f;
				audioSource.minDistance = 1;
				audioSource.maxDistance = 1000;
				audioSource.loop = true;
				audioSource.pitch = 1f;
				audioSource.priority = 255;
				
				previousRotation = transform.rotation;
			
				if(audioClipPath!=string.Empty)
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
				
				if(hasRCS)
				{
					SetupRCS();
					KillRCS();
				}

				if(rotationTransformName!=string.Empty)
				{
					rotationTransform = part.FindModelTransform(rotationTransformName);
				}

				if(audioClipPath != string.Empty)
				{
					thrustAudio = GameDatabase.Instance.GetAudioClip(audioClipPath);
				}

				if(boostClipPath != string.Empty)
				{
					boostAudio = GameDatabase.Instance.GetAudioClip(boostClipPath);
				}

			}

			if(homingType != "Cruise")
			{
				Fields["cruiseAltitude"].guiActive = false;
				Fields["cruiseAltitude"].guiActiveEditor = false;
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
				if(GetComponentInChildren<KSPParticleEmitter>())
				{
					BDArmorySettings.numberOfParticleEmitters++;
				}
				
				foreach(var wpm in vessel.FindPartModulesImplementing<MissileFire>())
				{
					team = wpm.team;	
					break;
				}
				
				sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/deployClick"));
				
				sourceVessel = vessel;
				
				if(vessel.targetObject!=null && target == null)
				{
					try
					{
						target = vessel.targetObject.GetVessel().gameObject;
						targetVessel = vessel.targetObject.GetVessel();
					}
					catch(NullReferenceException)
					{
						vessel.targetObject = null;
						target = null;
					}
				}

				part.decouple(0);
				//BDArmorySettings.Instance.ApplyPhysRange();
				part.force_activate();
				part.Unpack();
				vessel.situation = Vessel.Situations.FLYING;
				rigidbody.isKinematic = false;
				BDArmorySettings.Instance.ApplyNewVesselRanges(vessel);


				//add target info to vessel
				if(targetVessel!=null && !vessel.gameObject.GetComponent<TargetInfo>())
				{
					TargetInfo info = vessel.gameObject.AddComponent<TargetInfo>();
					info.isMissile = true;
					info.missileModule = this;

					foreach(var mf in targetVessel.FindPartModulesImplementing<MissileFire>())
					{
						targetMf = mf;
						break;
					}
				}

				if(decoupleForward)
				{
					vessel.rigidbody.velocity += decoupleSpeed * part.transform.forward;
				}
				else
				{
					vessel.rigidbody.velocity += decoupleSpeed * -part.transform.up;
				}
				
				if(rndAngVel > 0)
				{
					vessel.rigidbody.angularVelocity += UnityEngine.Random.insideUnitSphere.normalized * rndAngVel;	
				}
				

				vessel.vesselName = part.partInfo.title + " (fired)";
				vessel.vesselType = VesselType.Probe;

				
				timeStart = Time.time;
				hasFired = true;
				
				previousRotation = transform.rotation;

				//setting ref transform for navball
				GameObject refObject = new GameObject();
				refObject.transform.rotation = Quaternion.LookRotation(-transform.up, transform.forward);
				refObject.transform.parent = transform;
				part.SetReferenceTransform(refObject.transform);
				vessel.SetReferenceTransform(part);

				MissileState = MissileStates.Drop;
				
			}
		}
		
		public void FireMissileOnTarget(Vessel v)
		{
			if(!hasFired)
			{
				target = v.gameObject;
				targetVessel = v;
				FireMissile();
			}
		}
		
	
		
		
		
		public override void OnFixedUpdate()
		{

			debugString = "";
			if(hasFired && !hasExploded && part!=null)
			{
				rigidbody.isKinematic = false;
				AntiSpin();

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

				//simpleDrag
				if(useSimpleDrag)
				{
					SimpleDrag();
				}

				//flybyaudio
				float mCamDistanceSqr = (FlightCamera.fetch.mainCamera.transform.position-transform.position).sqrMagnitude;
				float mCamRelVSqr = (float)(FlightGlobals.ActiveVessel.srf_velocity-vessel.srf_velocity).sqrMagnitude;
				if(!hasPlayedFlyby 
				   && FlightGlobals.ActiveVessel != vessel 
				   && FlightGlobals.ActiveVessel != sourceVessel 
				   && mCamDistanceSqr < 400*400 && mCamRelVSqr > 300*300  
				   && mCamRelVSqr < 800*800 
				   && Vector3.Angle(rigidbody.velocity, FlightGlobals.ActiveVessel.transform.position-transform.position)<60)
				{
					sfAudioSource.PlayOneShot (GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/missileFlyby"));	
					hasPlayedFlyby = true;
				}
				
				if(vessel.isActiveVessel)
				{
					audioSource.dopplerLevel = 0;
				}
				else
				{
					audioSource.dopplerLevel = 1f;
				}


				
				//Missile State
				timeIndex = Time.time - timeStart;
				if(timeIndex < dropTime)
				{
					MissileState = MissileStates.Drop;
				}
				else if(timeIndex < dropTime + boostTime)
				{
					MissileState = MissileStates.Boost;	
				}
				else if(timeIndex < dropTime+boostTime+cruiseTime)
				{
					MissileState = MissileStates.Cruise;	
				}
				else
				{
					MissileState = MissileStates.PostThrust;	
				}
				

				
				
				if(MissileState == MissileStates.Drop) //drop phase
				{
					//transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(rigidbody.velocity), .05f);
					//initialObtVelocity = vessel.obt_velocity;
				}
				else if(MissileState == MissileStates.Boost) //boost phase
				{
					//light, sound & particle fx
					if(boostAudio||thrustAudio)	
					{
						if(!PauseMenu.isOpen)
						{
							if(!audioSource.isPlaying)
							{
								if(boostAudio)
								{
									audioSource.clip = boostAudio;
								}
								else if(thrustAudio)
								{
									audioSource.clip = thrustAudio;
								}
								audioSource.Play();	
							}
						}
						else if(audioSource.isPlaying)
						{
							audioSource.Stop();
						}
					}


					foreach(Light light in gameObject.GetComponentsInChildren<Light>())
					{
						light.intensity = 1.5f;	
					}
					if(spoolEngine) 
					{
						currentThrust = Mathf.MoveTowards(currentThrust, thrust, thrust/10);
					}
					else
					{
						currentThrust = thrust;	
					}

					if(boostTransformName != string.Empty)
					{
						foreach(var emitter in boostEmitters)
						{
							emitter.emit = true;
						}
					}

					rigidbody.AddRelativeForce(currentThrust * Vector3.forward);
					if(hasRCS) forwardRCS.emit = true;
					if(!startedEngine && thrust > 0)
					{
						startedEngine = true;
						sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/launch"));
					}
				}
				else if(MissileState == MissileStates.Cruise) //cruise phase
				{
					part.crashTolerance = 1;

					if(thrustAudio)	
					{
						if(!PauseMenu.isOpen)
						{
							if(!audioSource.isPlaying || audioSource.clip!=thrustAudio)
							{
								audioSource.clip = thrustAudio;
								audioSource.Play();	
							}
						}
						else if(audioSource.isPlaying)
						{
							audioSource.Stop();
						}
					}


					if(spoolEngine)
					{
						currentThrust = Mathf.MoveTowards(currentThrust, cruiseThrust, thrust/10);
					}
					else
					{
						currentThrust = cruiseThrust;	
					}
					
					if(!hasRCS)
					{
						rigidbody.AddRelativeForce(cruiseThrust * Vector3.forward);
					}
					else
					{
						forwardRCS.emit = false;
						audioSource.Stop();
					}

					if(boostTransformName != string.Empty)
					{
						foreach(var emitter in boostEmitters)
						{
							emitter.emit = false;
						}
					}
				}
				
				if(MissileState != MissileStates.Idle && MissileState != MissileStates.PostThrust && MissileState != MissileStates.Drop) //all thrust
				{
					if(!hasRCS)
					{
						foreach(KSPParticleEmitter pe in pEmitters)
						{
							pe.emit = true;
						}
						foreach(var gpe in gaplessEmitters)
						{
							if(vessel.atmDensity > 0)
							{
								gpe.emit = true;
							}
							else
							{
								gpe.emit = false;
							}	
						}
					}
					
					foreach(KSPParticleEmitter pe in pEmitters)
					{
						if(!pe.gameObject.name.Contains("rcs") && !pe.useWorldSpace)
						{
							pe.sizeGrow = Mathf.Lerp(pe.sizeGrow, 1, 0.4f);
						}
					}
				}
				else
				{
					if(thrustAudio && audioSource.isPlaying)
					{
						audioSource.volume = Mathf.Lerp(audioSource.volume, 0, 0.1f);
						audioSource.pitch = Mathf.Lerp(audioSource.pitch, 0, 0.1f);
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
						pe.maxEmission = Mathf.FloorToInt(pe.maxEmission * 0.8f);
						pe.minEmission =  Mathf.FloorToInt(pe.minEmission * 0.8f);
					}
					foreach(var gpe in gaplessEmitters)
					{
						gpe.pEmitter.maxSize = Mathf.MoveTowards(gpe.pEmitter.maxSize, 0, 0.005f);
						gpe.pEmitter.minSize = Mathf.MoveTowards(gpe.pEmitter.minSize, 0, 0.008f);
					}
				}
				
				

				if(MissileState != MissileStates.Idle && MissileState != MissileStates.Drop) //guidance
				{
					//guidance and attitude stabilisation scales to atmospheric density. //use part.atmDensity
					float atmosMultiplier = Mathf.Clamp01 (2.5f*(float)FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(transform.position), FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody)); 

					float optimumSpeedFactor = (float)vessel.srfSpeed/(2*optimumAirspeed);
					float controlAuthority = Mathf.Clamp01(atmosMultiplier * (-Mathf.Abs(2*optimumSpeedFactor-1) + 1));
					debugString += "\ncontrolAuthority: "+controlAuthority;

					if(target!=null && guidanceActive)// && timeIndex - dropTime > 0.5f)
					{
						WarnTarget();

						targetPosition = target.transform.position;
						if(targetVessel && targetVessel.loaded && target.rigidbody)
						{
							Vector3 targetCoMPos = targetVessel.findWorldCenterOfMass();
							targetPosition = targetCoMPos+target.rigidbody.velocity*Time.fixedDeltaTime;
						}
					
						float targetViewAngle = Vector3.Angle (transform.forward, target.transform.position-transform.position);
						targetInView = (targetViewAngle < maxOffBoresight);
						LookForCountermeasure();
						
						float targetDistance = Vector3.Distance(targetPosition, transform.position+rigidbody.velocity*Time.fixedDeltaTime);
						if(prevDistance == -1)
						{
							prevDistance = targetDistance;
						}
						
						//increaseTurnRate after launch
						float turnRateDPS = Mathf.Clamp(((timeIndex-dropTime)/boostTime)*maxTurnRateDPS * 25f, 0, maxTurnRateDPS);
						float turnRatePointDPS = turnRateDPS;
						if(!hasRCS)
						{
							turnRateDPS *= controlAuthority;
						}
						
						//decrease turn rate after thrust cuts out
						if(timeIndex > dropTime+boostTime+cruiseTime)
						{
							turnRateDPS = atmosMultiplier * Mathf.Clamp(maxTurnRateDPS - ((timeIndex-dropTime-boostTime-cruiseTime)*0.45f), 1, maxTurnRateDPS);	
							if(hasRCS) 
							{
								turnRateDPS = 0;
							}
						}
						
						if(hasRCS)
						{
							if(turnRateDPS > 0)
							{
								DoRCS();
							}
							else
							{
								KillRCS();
							}
						}
						debugTurnRate = turnRateDPS;
						float radiansDelta = turnRateDPS*Mathf.Deg2Rad*Time.fixedDeltaTime;

						float finalMaxTorque = Mathf.Clamp((timeIndex-dropTime)*30, 0, maxTorque);

						if(homingType == "AAM")
						{
							if(targetDistance > 10 && targetInView && targetVessel) //guide towards where the target is going to be
							{
								targetPosition = MissileGuidance.GetAirToAirTarget(targetPosition, vessel, targetVessel);
							}
							
							float clampedSpeed = Mathf.Clamp((float) vessel.srfSpeed, 1, 1000);
							float limitAoA = Mathf.Clamp(3500/clampedSpeed, 5, maxAoA);
							
							if(targetVessel!=null && targetVessel.LandedOrSplashed)
							{
								if(randomOffset == Vector3.zero)
								{
									randomOffset = UnityEngine.Random.insideUnitSphere;
								}
								targetPosition += randomOffset * blastRadius * 3f;	
							}
								
							if(!aero)
							{
								//legacy guidance/steering
								if(Vector3.Angle (transform.forward, rigidbody.velocity) < limitAoA)
								{
									transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(targetPosition-transform.position, transform.up), 1.3f*turnRateDPS*Time.fixedDeltaTime);
								}
								else
								{
									transform.rotation = Quaternion.RotateTowards (transform.rotation, Quaternion.LookRotation(vessel.srf_velocity, transform.up), 1.3f*turnRateDPS*Time.fixedDeltaTime);
								}
								
								if(!hasRCS)
								{
									rigidbody.velocity = Vector3.RotateTowards(rigidbody.velocity, transform.forward, radiansDelta, 0);
								}
								else
								{
									//rigidbody.velocity = Vector3.RotateTowards(rigidbody.velocity, targetPosition-transform.position, radiansDelta, 0);	
								}
							}
							else
							{
								if(Time.time-timeStart > dropTime+0.5f)
								{
									aeroTorque = MissileGuidance.DoAeroForces(this, targetPosition, liftArea, controlAuthority * steerMult, aeroTorque, finalMaxTorque, limitAoA);
								}
							}
						}
						else if(homingType == "AGM")
						{
							if(targetViewAngle > 50) guidanceActive = false;
							Vector3 agmTarget = Vector3.zero;
							if(targetVessel!=null)
							{
								agmTarget = MissileGuidance.GetAirToGroundTarget(targetPosition, vessel, targetVessel, agmDescentRatio);
							
								float clampedSpeed = Mathf.Clamp((float) vessel.srfSpeed, 1, 1000);
								float limitAoA = Mathf.Clamp(3500/clampedSpeed, 5, maxAoA);
								
								debugString += "\n limitAoA: "+limitAoA.ToString("0.0");

								if(!aero)
								{
									if(Vector3.Angle (transform.forward, rigidbody.velocity) < limitAoA)
									{
										transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(agmTarget-transform.position, transform.up), 3*turnRatePointDPS*Time.fixedDeltaTime);
									}
									else
									{
										transform.rotation = Quaternion.RotateTowards (transform.rotation, Quaternion.LookRotation(vessel.srf_velocity, transform.up), 2*turnRatePointDPS*Time.fixedDeltaTime);
									}
									
									
									rigidbody.velocity = Vector3.RotateTowards(rigidbody.velocity, transform.forward, radiansDelta, 0);
								}
								else
								{
									aeroTorque = MissileGuidance.DoAeroForces(this, agmTarget, liftArea, controlAuthority * steerMult, aeroTorque, finalMaxTorque, maxAoA);
								}
							}
						}
						else if(homingType == "RCS")
						{
							if(targetVessel!=null)
							{
								transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(targetPosition-transform.position, transform.up), turnRateDPS*Time.fixedDeltaTime);
							}
						}
						else if(homingType == "Cruise")
						{
							Vector3 cruiseTarget = Vector3.zero;
							if(targetVessel!=null)
							{
								if(terminalManeuvering && targetDistance < 4500)
								{
									cruiseTarget = MissileGuidance.GetTerminalManeuveringTarget(targetPosition, vessel, targetVessel);
								}
								else
								{
									cruiseTarget = MissileGuidance.GetCruiseTarget(targetPosition, vessel, targetVessel, cruiseAltitude);
								}

								targetPosition = cruiseTarget;
								
								float clampedSpeed = Mathf.Clamp((float) vessel.srfSpeed, 1, 1000);
								float limitAoA = Mathf.Clamp(3500/clampedSpeed, 5, maxAoA);
								
								debugString += "\n limitAoA: "+limitAoA.ToString("0.0");

								Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D());

								//axial rotation
								if(rotationTransform)
								{
									Quaternion originalRotation = transform.rotation;
									Quaternion originalRTrotation = rotationTransform.rotation;
									transform.rotation = Quaternion.LookRotation(transform.forward, upDirection);
									rotationTransform.rotation = originalRTrotation;
									Vector3 lookUpDirection = Misc.ProjectOnPlane(vessel.acceleration, Vector3.zero, transform.forward);
									lookUpDirection = transform.InverseTransformPoint(lookUpDirection + transform.position);

									lookUpDirection = new Vector3(lookUpDirection.x, 0, 0);
									lookUpDirection += 10*Vector3.up;
									//Debug.Log ("lookUpDirection: "+lookUpDirection);
								

									rotationTransform.localRotation = Quaternion.Lerp(rotationTransform.localRotation, Quaternion.LookRotation(Vector3.forward, lookUpDirection), 0.04f);
									Quaternion finalRotation = rotationTransform.rotation;
									transform.rotation = originalRotation;
									rotationTransform.rotation = finalRotation;
								}

								if(!aero)
								{
									//pointing direction
									if(Vector3.Angle (transform.forward, rigidbody.velocity) < limitAoA)
									{
										turnRatePointDPS = Mathf.Clamp(turnRatePointDPS*Vector3.Angle(transform.forward, cruiseTarget-transform.position)/20, 5, turnRatePointDPS);
										transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(cruiseTarget-transform.position, transform.up), 1.5f*turnRatePointDPS*Time.fixedDeltaTime);
									}
									else
									{
										turnRatePointDPS = Mathf.Clamp(turnRatePointDPS*Vector3.Angle(transform.forward, vessel.srf_velocity)/20, 1, turnRatePointDPS);
										transform.rotation = Quaternion.RotateTowards (transform.rotation, Quaternion.LookRotation(vessel.srf_velocity, transform.up), 1*turnRatePointDPS*Time.fixedDeltaTime);
									}

									rigidbody.velocity = Vector3.RotateTowards(rigidbody.velocity, transform.forward, radiansDelta, 0);
								}
								else
								{
									aeroTorque = MissileGuidance.DoAeroForces(this, cruiseTarget, liftArea, controlAuthority * steerMult, aeroTorque, finalMaxTorque, limitAoA); 
								}
								
							}
						}
					
						
						if(targetDistance < 800 || MissileState == MissileStates.PostThrust)
						{
							checkMiss = true;	
						}
						
						//kill guidance if missile has missed
						if(checkMiss && prevDistance-targetDistance < 0) 
						{
							guidanceActive = false;
							targetMf = null;
							if(hasRCS) KillRCS();
							if(targetDistance < blastRadius*0.45) Detonate();
							return;
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
						targetMf = null;
						if(!aero)
						{
							if(!hasRCS && !useSimpleDrag)	
							{
								transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vessel.srf_velocity, transform.up), atmosMultiplier * (0.5f*(timeIndex-dropTime)) * 50*Time.fixedDeltaTime);	
							}
						}
						else
						{
							aeroTorque = MissileGuidance.DoAeroForces(this, transform.position + 20*transform.forward, liftArea, .25f, aeroTorque, maxTorque, maxAoA);
						}
					}
					
					if(hasRCS && !guidanceActive)
					{
						KillRCS();	
					}
				}
				
				//Timed detonation
				if(isTimed && timeIndex > detonationTime)
				{
					Detonate();
				}
				
				
			}
		}
		
		
		
		public void Detonate()
		{
			
			if(isSeismicCharge)
			{
				DetonateSeismicCharge();
			
			}
			else if(!hasExploded && hasFired)
			{
				BDArmorySettings.numberOfParticleEmitters--;
				
				hasExploded = true;
				
				if(targetVessel == null)
				{
					if(target!=null && FlightGlobals.ActiveVessel.gameObject == target)
					{
						targetVessel = FlightGlobals.ActiveVessel;
					}
					else if(target!=null && !BDArmorySettings.Flares.Contains(target))
					{
						targetVessel = Part.FromGO(target).vessel;
					}
					
				}
				
				if(targetVessel!=null)
				{
					foreach(var wpm in targetVessel.FindPartModulesImplementing<MissileFire>())
					{
						wpm.missileIsIncoming = false;
					}
				}
				
				if(part!=null) part.temperature = part.maxTemp + 100;
				Vector3 position = transform.position;//+rigidbody.velocity*Time.fixedDeltaTime;
				if(sourceVessel==null) sourceVessel = vessel;
				ExplosionFX.CreateExplosion(position, blastRadius, blastPower, sourceVessel, transform.forward, explModelPath, explSoundPath);
				
			}

		}
		
		
		
		
		void DetonateSeismicCharge()
		{
			if(!hasExploded && hasFired)
			{
				GameSettings.SHIP_VOLUME = 0;
				GameSettings.MUSIC_VOLUME = 0;
				GameSettings.AMBIENCE_VOLUME = 0;
				
				BDArmorySettings.numberOfParticleEmitters--;
				
				hasExploded = true;
				
				if(targetVessel == null)
				{
					if(target!=null && FlightGlobals.ActiveVessel.gameObject == target)
					{
						targetVessel = FlightGlobals.ActiveVessel;
					}
					else if(target!=null && !BDArmorySettings.Flares.Contains(target))
					{
						targetVessel = Part.FromGO(target).vessel;
					}
					
				}
				
				if(targetVessel!=null)
				{
					foreach(var wpm in targetVessel.FindPartModulesImplementing<MissileFire>())
					{
						wpm.missileIsIncoming = false;
					}
				}
				
				if(part!=null)
				{
					
					part.temperature = part.maxTemp + 100;
				}
				Vector3 position = transform.position+rigidbody.velocity*Time.fixedDeltaTime;
				if(sourceVessel==null) sourceVessel = vessel;
				
				SeismicChargeFX.CreateSeismicExplosion(transform.position-(rigidbody.velocity.normalized*15), UnityEngine.Random.rotation);
				
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
					if(angle < 45 && (flare.transform.position-transform.position).sqrMagnitude < Mathf.Pow(flareAcquireMaxRange, 2) && chancePass && targetInView)
					{
						//Debug.Log ("=Missile deflected via flare=");
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
				if(FlightGlobals.ActiveVessel.gameObject == target)
				{
					targetVessel = FlightGlobals.ActiveVessel;
				}
				else if(target!=null && !BDArmorySettings.Flares.Contains(target))
				{
					targetVessel = Part.FromGO(target).vessel;
				}
				
			}
			
			if(targetVessel!=null)
			{
				foreach(var wpm in targetVessel.FindPartModulesImplementing<MissileFire>())
				{
					wpm.MissileWarning(Vector3.Distance(transform.position, targetVessel.transform.position), this);
					break;
				}
			}
		}


		float[] rcsFiredTimes;
		KSPParticleEmitter[] rcsTransforms;
		void SetupRCS()
		{
			rcsFiredTimes = new float[]{0,0,0,0};
			rcsTransforms = new KSPParticleEmitter[]{upRCS, leftRCS, rightRCS, downRCS};
		}

		void DoRCS()
		{
			for(int i = 0; i < 4; i++)
			{
				Vector3 relV = targetVessel.obt_velocity-vessel.obt_velocity;
				Vector3 localRelV = rcsTransforms[i].transform.InverseTransformPoint(relV + transform.position);


				float giveThrust = Mathf.Clamp(-localRelV.z, 0, rcsThrust);
				rigidbody.AddForce(-giveThrust*rcsTransforms[i].transform.forward);

				if(localRelV.z < -rcsRVelThreshold)
				{
					rcsAudioMinInterval = UnityEngine.Random.Range(0.15f,0.25f);
					if(Time.time-rcsFiredTimes[i] > rcsAudioMinInterval)
					{
						sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/popThrust"));
						rcsTransforms[i].emit = true;
						rcsFiredTimes[i] = Time.time;
					}
				}
				else
				{
					rcsTransforms[i].emit = false;
				}

				//turn off emit
				if(Time.time-rcsFiredTimes[i] > rcsAudioMinInterval*0.75f)
				{
					rcsTransforms[i].emit = false;
				}
			}


		}
		
		void KillRCS()
		{
			upRCS.emit = false;
			downRCS.emit = false;
			leftRCS.emit = false;
			rightRCS.emit = false;
		}

		/*
		void OnGUI()
		{
			if(hasFired)	
			{
				GUI.Label(new Rect(200,200,200,200), debugString);	
			}
		}
		*/

		void AntiSpin()
		{
			Vector3 spin = Vector3.Project(rigidbody.angularVelocity, rigidbody.transform.forward);// * 8 * Time.fixedDeltaTime;
			debugString += "\nlocal angVel: "+transform.InverseTransformDirection(rigidbody.angularVelocity);
			rigidbody.angularVelocity -= spin;
			//rigidbody.maxAngularVelocity = 7;
			rigidbody.angularVelocity -= 0.5f * rigidbody.angularVelocity;
		}
		
		void SimpleDrag()
		{
			float simSpeedSquared = rigidbody.velocity.sqrMagnitude;
			Vector3 currPos = transform.position;
			Vector3 dragForce = (0.008f * rigidbody.mass) * part.minimum_drag * 0.5f * simSpeedSquared * (float) FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPos), FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody) * rigidbody.velocity.normalized;
			rigidbody.AddForceAtPosition(-dragForce, transform.TransformPoint(simpleCoD));

		}
		
	}
}

