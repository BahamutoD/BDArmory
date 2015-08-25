using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{

	
	public class MissileLauncher : PartModule, IBDWeapon
	{
		public enum MissileStates{Idle, Drop, Boost, Cruise, PostThrust}


		public enum GuidanceModes{None,AAMLead,AAMPure,AGM,AGMBallistic,Cruise,STS,Bomb,RCS}
		public GuidanceModes guidanceMode;
		[KSPField]
		public string homingType = "AAM";

		[KSPField]
		public string targetingType = "radar";
		public enum TargetingModes{None,Radar,Heat,Laser,GPS,AntiRad}
		public TargetingModes targetingMode;
		public bool team;
		
		public float timeFired = -1;
		public float timeIndex = 0;

		//aero
		[KSPField]
		public bool aero = false;
		[KSPField]
		public float liftArea = 0.015f;
		[KSPField]
		public float steerMult = 0.5f;
		[KSPField]
		public float torqueRampUp = 30f;
		Vector3 aeroTorque = Vector3.zero;
		float controlAuthority = 0;
		float finalMaxTorque = 0;
		[KSPField]
		public float aeroSteerDamping = 0;

		[KSPField]
		public float maxTorque = 90;
		//

		[KSPField]
		public float thrust = 30;
		[KSPField]
		public float cruiseThrust = 3;
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Drop Time"),
        	UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
		public float dropTime = 0.4f;
		[KSPField]
		public float boostTime = 2.2f;
		[KSPField]
		public float cruiseTime = 45;
		[KSPField]
		public bool guidanceActive = true;
		[KSPField]
		public float maxOffBoresight = 45;
		
		[KSPField]
		public float maxAoA = 35;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Decouple Speed"),
        	UI_FloatRange(minValue = 0f, maxValue = 10f, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
		public float decoupleSpeed = 0;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Direction: "), 
			UI_Toggle(disabledText = "Lateral", enabledText = "Forward")]
		public bool decoupleForward = false;
		


		[KSPField]
		public float optimumAirspeed = 220;
		
		[KSPField]
		public float blastRadius = 150;
		[KSPField]
		public float blastPower = 25;
		[KSPField]
		public float maxTurnRateDPS = 20;
		[KSPField]
		public bool proxyDetonate = true;
		
		[KSPField]
		public string audioClipPath = string.Empty;

		AudioClip thrustAudio;

		[KSPField]
		public string boostClipPath = string.Empty;

		AudioClip boostAudio;
		
		[KSPField]
		public bool isSeismicCharge = false;
		
		[KSPField]
		public float rndAngVel = 0;
		
		[KSPField]
		public bool isTimed = false;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Detonation Time"),
        	UI_FloatRange(minValue = 2f, maxValue = 30f, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
		public float detonationTime = 2;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cruise Altitude"),
		 UI_FloatRange(minValue = 30, maxValue = 2500f, stepIncrement = 5f, scene = UI_Scene.All)]
		public float cruiseAltitude = 500;

		[KSPField]
		public string rotationTransformName = string.Empty;
		Transform rotationTransform;

		[KSPField]
		public bool terminalManeuvering = false;
		
		
		
		[KSPField]
		public string explModelPath = "BDArmory/Models/explosion/explosion";
		
		public string explSoundPath = "BDArmory/Sounds/explode1";
			
		
		[KSPField]
		public bool spoolEngine = false;
		
		[KSPField]
		public bool hasRCS = false;
		[KSPField]
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
		private AudioSource audioSource;
		public AudioSource sfAudioSource;
		List<KSPParticleEmitter> pEmitters;
		List<BDAGaplessParticleEmitter> gaplessEmitters;
		
		public MissileFire targetMf = null;
		public bool hasFired = false;

		bool startedEngine = false;
		
		LineRenderer LR;
		float cmTimer;
		
		//deploy animation
		[KSPField]
		public string deployAnimationName = "";
		
		[KSPField]
		public float deployedDrag = 0.02f;
		
		[KSPField]
		public float deployTime = 0.2f;

		[KSPField]
		public bool useSimpleDrag = false;
		[KSPField]
		public float simpleDrag = 0.02f;

		[KSPField]
		public Vector3 simpleCoD = new Vector3(0,0,-1);

		[KSPField]
		public float agmDescentRatio = 1.45f;
		
		float currentThrust = 0;
		
		public bool deployed;
		public float deployedTime;
		
		AnimationState[] deployStates;
		
		bool hasPlayedFlyby = false;
		
		Quaternion previousRotation;
		
		float debugTurnRate = 0;
		string debugString = "";


		List<GameObject> boosters;
		bool decoupleBoosters = false;
		bool hasDecoupledBoosters = false;
		[KSPField]
		public float boosterDecoupleSpeed = 5;
		[KSPField]
		public float boosterMass = 0;

		public float timeToImpact;
		public bool targetAcquired = false;
		public Vector3 targetPosition = Vector3.zero;
		Vector3 targetVelocity = Vector3.zero;
		Vector3 targetAcceleration = Vector3.zero;

		public Vessel legacyTargetVessel;



		[KSPField]
		public string boostTransformName = string.Empty;
		List<KSPParticleEmitter> boostEmitters;

		public MissileStates MissileState = MissileStates.Idle;

		[KSPField]
		public float lockedSensorFOV = 2.5f;


		//laser stuff
		public ModuleTargetingCamera lockedCamera = null;
		Vector3 lastLaserPoint;
		Vector3 laserStartPosition;
		Vector3 startDirection;


		//heat stuff
		public TargetSignatureData heatTarget;
		[KSPField]
		public float heatThreshold = 200;
		[KSPField]
		public bool allAspect = false;



		float lockFailTimer = -1;
		public bool hasMissed = false;

		//radar stuff
		public ModuleRadar radar;
		public TargetSignatureData radarTarget;
		[KSPField]
		public float activeRadarRange = 6000;
		public bool activeRadar = false;
		float lastRWRPing = 0;
		[KSPField]
		public float activeRadarMinThresh = 140;
		[KSPField]
		public bool radarLOAL = false;
		bool radarLOALSearching = false;

		//GPS stuff
		public Vector3d targetGPSCoords;

		//weapon interface
		[KSPField]
		public string missileType = "missile";
		private WeaponClasses weaponClass;
		public WeaponClasses GetWeaponClass()
		{
			return weaponClass;
		}
		void ParseWeaponClass()
		{
			missileType = missileType.ToLower();
			if(missileType == "bomb")
			{
				weaponClass = WeaponClasses.Bomb;
			}
			else
			{
				weaponClass = WeaponClasses.Missile;
			}
		}
		[KSPField]
		public string shortName = string.Empty;
		public string GetShortName()
		{
			return shortName;
		}
		public Part GetPart()
		{
			return part;
		}



		public override void OnStart(PartModule.StartState state)
		{
			ParseWeaponClass();

			if(shortName == string.Empty)
			{
				shortName = part.partInfo.title;
			}

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
				ParseModes();

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
				previousRotation = transform.rotation;

				cmTimer = Time.time;
				
				part.force_activate();
				//part.OnJustAboutToBeDestroyed += new Callback(Detonate);
				
			
				
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

				if(rotationTransformName!=string.Empty)
				{
					rotationTransform = part.FindModelTransform(rotationTransformName);
				}


				boosters = new List<GameObject>();
				foreach(var tf in part.FindModelTransforms("boosterTransform"))
				{
					boosters.Add(tf.gameObject);
					decoupleBoosters = true;
				}



				
				if(hasRCS)
				{
					SetupRCS();
					KillRCS();
				}

				SetupAudio();



			}

			if(guidanceMode != GuidanceModes.Cruise)
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
				deployedDrag = simpleDrag;	
			}
		}

		/*
		void OnCollisionEnter(Collision col)
		{
			if(!hasExploded && hasFired && Time.time - timeFired > 1)
			{
				Detonate();
			}
		}
		*/

		void SetupAudio()
		{
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.minDistance = 1;
			audioSource.maxDistance = 1000;
			audioSource.loop = true;
			audioSource.pitch = 1f;
			audioSource.priority = 255;

			if(audioClipPath!=string.Empty)
			{
				audioSource.clip = GameDatabase.Instance.GetAudioClip(audioClipPath);
			}

			sfAudioSource = gameObject.AddComponent<AudioSource>();
			sfAudioSource.minDistance = 1;
			sfAudioSource.maxDistance = 2000;
			sfAudioSource.dopplerLevel = 0;
			sfAudioSource.priority = 230;



			if(audioClipPath != string.Empty)
			{
				thrustAudio = GameDatabase.Instance.GetAudioClip(audioClipPath);
			}

			if(boostClipPath != string.Empty)
			{
				boostAudio = GameDatabase.Instance.GetAudioClip(boostClipPath);
			}

			UpdateVolume();
			BDArmorySettings.OnVolumeChange += UpdateVolume;
		}

		void UpdateVolume()
		{
			if(audioSource)
			{
				audioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
			}
			if(sfAudioSource)
			{
				sfAudioSource.volume = BDArmorySettings.BDARMORY_WEAPONS_VOLUME;
			}
		}

		void OnDestroy()
		{
			BDArmorySettings.OnVolumeChange -= UpdateVolume;
		}
		
		[KSPAction("Fire Missile")]
		public void AGFire(KSPActionParam param)
		{
			if(BDArmorySettings.Instance.ActiveWeaponManager != null && BDArmorySettings.Instance.ActiveWeaponManager.vessel == vessel) BDArmorySettings.Instance.ActiveWeaponManager.SendTargetDataToMissile(this);
			FireMissile();	
			if(BDArmorySettings.Instance.ActiveWeaponManager!=null) BDArmorySettings.Instance.ActiveWeaponManager.UpdateList();
		}
		
		[KSPEvent(guiActive = true, guiName = "Fire Missile", active = true)]
		public void GuiFire()
		{
			if(BDArmorySettings.Instance.ActiveWeaponManager != null && BDArmorySettings.Instance.ActiveWeaponManager.vessel == vessel) BDArmorySettings.Instance.ActiveWeaponManager.SendTargetDataToMissile(this);
			FireMissile();	
			if(BDArmorySettings.Instance.ActiveWeaponManager!=null) BDArmorySettings.Instance.ActiveWeaponManager.UpdateList();
		}

		[KSPEvent(guiActive = true, guiActiveEditor = false, active = true, guiName = "Jettison")]
		public void Jettison()
		{
			part.decouple(0);
			if(BDArmorySettings.Instance.ActiveWeaponManager!=null) BDArmorySettings.Instance.ActiveWeaponManager.UpdateList();
		}
		
		
		public void FireMissile()
		{
			if(!hasFired)
			{
				

				hasFired = true;

				GameEvents.onPartDie.Add(PartDie);

				BDATargetManager.FiredMissiles.Add(this);

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



				//TARGETING
				targetPosition = transform.position + (transform.forward * 5000); //set initial target position so if no target update, missile will count a miss if it nears this point or is flying post-thrust
				startDirection = transform.forward;
				if(BDArmorySettings.ALLOW_LEGACY_TARGETING)
				{
					if(vessel.targetObject!=null && vessel.targetObject.GetVessel()!=null)
					{
						legacyTargetVessel = vessel.targetObject.GetVessel();

						foreach(var mf in legacyTargetVessel.FindPartModulesImplementing<MissileFire>())
						{
							targetMf = mf;
							break;
						}

						if(targetingMode == TargetingModes.Heat)
						{
							heatTarget = new TargetSignatureData(legacyTargetVessel, 9999);
						}
					}
				}
				if(targetingMode == TargetingModes.Laser)
				{
					laserStartPosition = transform.position;
					if(lockedCamera)
					{
						targetAcquired = true;
						targetPosition = lastLaserPoint = lockedCamera.groundTargetPosition;
					}
				}
				else if(targetingMode == TargetingModes.AntiRad && targetAcquired)
				{
					RadarWarningReceiver.OnRadarPing += ReceiveRadarPing;
				}

				part.decouple(0);
				part.force_activate();
				part.Unpack();
				vessel.situation = Vessel.Situations.FLYING;
				rigidbody.isKinematic = false;
				BDArmorySettings.Instance.ApplyNewVesselRanges(vessel);
				part.bodyLiftMultiplier = 0;
				part.dragModel = Part.DragModel.NONE;

				//add target info to vessel
				TargetInfo info = vessel.gameObject.AddComponent<TargetInfo>();
				info.team = BDATargetManager.BoolToTeam(team);
				info.isMissile = true;
				info.missileModule = this;

				StartCoroutine(DecoupleRoutine());
				
				if(rndAngVel > 0)
				{
					part.rb.angularVelocity += UnityEngine.Random.insideUnitSphere.normalized * rndAngVel;	
				}
				

				vessel.vesselName = GetShortName();
				vessel.vesselType = VesselType.Probe;

				
				timeFired = Time.time;

				
				previousRotation = transform.rotation;

				//setting ref transform for navball
				GameObject refObject = new GameObject();
				refObject.transform.rotation = Quaternion.LookRotation(-transform.up, transform.forward);
				refObject.transform.parent = transform;
				part.SetReferenceTransform(refObject.transform);
				vessel.SetReferenceTransform(part);

				MissileState = MissileStates.Drop;

				part.crashTolerance = 9999;



				
			}
		}

		IEnumerator DecoupleRoutine()
		{
			yield return new WaitForFixedUpdate();
			if(decoupleForward)
			{
				part.rb.velocity += decoupleSpeed * part.transform.forward;
			}
			else
			{
				part.rb.velocity += decoupleSpeed * -part.transform.up;
			}

			//Misc.RemoveFARModule(part);
		}

		/// <summary>
		/// Fires the missile on target vessel.  Used by AI currently.
		/// </summary>
		/// <param name="v">V.</param>
		public void FireMissileOnTarget(Vessel v)
		{
			if(!hasFired)
			{
				legacyTargetVessel = v;

				FireMissile();
			}
		}
		
		void OnDisable()
		{
			if(targetingMode == TargetingModes.AntiRad)
			{
				RadarWarningReceiver.OnRadarPing -= ReceiveRadarPing;
			}
		}
		
		
		public override void OnFixedUpdate()
		{

			debugString = "";
			if(hasFired && !hasExploded && part!=null)
			{
				part.rb.isKinematic = false;
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
					&& Vector3.Angle(vessel.srf_velocity, FlightGlobals.ActiveVessel.transform.position-transform.position)<60)
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


				if(guidanceActive)
				{
					if(BDArmorySettings.ALLOW_LEGACY_TARGETING && legacyTargetVessel)
					{
						UpdateLegacyTarget();
					}

					if(targetingMode == TargetingModes.Heat)
					{
						UpdateHeatTarget();
					}
					else if(targetingMode == TargetingModes.Radar)
					{
						UpdateRadarTarget();
					}
					else if(targetingMode == TargetingModes.Laser)
					{
						UpdateLaserTarget();
					}
					else if(targetingMode == TargetingModes.GPS)
					{
						UpdateGPSTarget();
					}
					else if(targetingMode == TargetingModes.AntiRad)
					{
						UpdateAntiRadiationTarget();
					}
				}

				
				//Missile State
				timeIndex = Time.time - timeFired;
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
				
				if(timeIndex > 0.5f)
				{
					part.crashTolerance = 1;
				}
				
				
				if(MissileState == MissileStates.Drop) //drop phase
				{
				}
				else if(MissileState == MissileStates.Boost) //boost phase
				{
					//light, sound & particle fx
					if(boostAudio||thrustAudio)	
					{
						if(!BDArmorySettings.GameIsPaused)
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

					part.rb.AddRelativeForce(currentThrust * Vector3.forward);
					if(hasRCS) forwardRCS.emit = true;
					if(!startedEngine && thrust > 0)
					{
						startedEngine = true;
						sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/launch"));
						RadarWarningReceiver.WarnMissileLaunch(transform.position, transform.forward);
					}
				}
				else if(MissileState == MissileStates.Cruise) //cruise phase
				{
					part.crashTolerance = 1;

					if(thrustAudio)	
					{
						if(!BDArmorySettings.GameIsPaused)
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
						part.rb.AddRelativeForce(cruiseThrust * Vector3.forward);
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
							if(!emitter) continue;
							emitter.emit = false;
						}
					}

					if(decoupleBoosters && !hasDecoupledBoosters)
					{
						hasDecoupledBoosters = true;
						part.mass -= boosterMass;
						foreach(var booster in boosters)
						{
							if(!booster) continue;
							(booster.AddComponent<DecoupledBooster>()).DecoupleBooster(part.rb.velocity, boosterDecoupleSpeed);
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
								gpe.pEmitter.worldVelocity = 2*ParticleTurbulence.Turbulence;
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
						gpe.pEmitter.worldVelocity = 2*ParticleTurbulence.Turbulence;
					}
				}
				
				

				if(MissileState != MissileStates.Idle && MissileState != MissileStates.Drop) //guidance
				{
					//guidance and attitude stabilisation scales to atmospheric density. //use part.atmDensity
					float atmosMultiplier = Mathf.Clamp01 (2.5f*(float)FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(transform.position), FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody)); 

					if(vessel.srfSpeed < optimumAirspeed)
					{
						float optimumSpeedFactor = (float)vessel.srfSpeed / (2 * optimumAirspeed);
						controlAuthority = Mathf.Clamp01(atmosMultiplier * (-Mathf.Abs(2 * optimumSpeedFactor - 1) + 1));
					}
					else
					{
						controlAuthority = Mathf.Clamp01(atmosMultiplier);
					}
					debugString += "\ncontrolAuthority: "+controlAuthority;

					if(guidanceActive)// && timeIndex - dropTime > 0.5f)
					{
						WarnTarget();
						Vector3 targetPosition = Vector3.zero;

						if(legacyTargetVessel && legacyTargetVessel.loaded)
						{
							Vector3 targetCoMPos = legacyTargetVessel.findWorldCenterOfMass();
							targetPosition = targetCoMPos+legacyTargetVessel.rb_velocity*Time.fixedDeltaTime;
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

						finalMaxTorque = Mathf.Clamp((timeIndex-dropTime)*torqueRampUp, 0, maxTorque); //ramp up torque

						if(guidanceMode == GuidanceModes.AAMLead)
						{
							AAMGuidance();
						}
						else if(guidanceMode == GuidanceModes.AGM)
						{
							AGMGuidance();
						}
						else if(guidanceMode == GuidanceModes.AGMBallistic)
						{
							AGMBallisticGuidance();
						}
						else if(guidanceMode == GuidanceModes.RCS)
						{
							if(legacyTargetVessel!=null)
							{
								transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(targetPosition-transform.position, transform.up), turnRateDPS*Time.fixedDeltaTime);
							}
						}
						else if(guidanceMode == GuidanceModes.Cruise)
						{
							CruiseGuidance();
						}
					

					}
					else
					{
						CheckMiss();
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
							aeroTorque = MissileGuidance.DoAeroForces(this, transform.position + (20*vessel.srf_velocity), liftArea, .25f, aeroTorque, maxTorque, maxAoA);
						}


					}

					if(aero && aeroSteerDamping > 0)
					{
						//part.rb.angularDrag = aeroSteerDamping;
					}
					
					if(hasRCS && !guidanceActive)
					{
						KillRCS();	
					}
				}
				
				//Timed detonation
				if(isTimed && timeIndex > detonationTime)
				{
					part.temperature = part.maxTemp+100;;
				}
				
				
			}
		}

		void CruiseGuidance()
		{
			Vector3 cruiseTarget = Vector3.zero;
			float distance = Vector3.Distance(targetPosition, transform.position);

			if(terminalManeuvering && distance < 4500)
			{
				cruiseTarget = MissileGuidance.GetTerminalManeuveringTarget(targetPosition, vessel, cruiseAltitude);
				debugString += "\nTerminal Maneuvers";
			}
			else
			{
				float agmThreshDist = 2500;
				if(distance <agmThreshDist)
				{
					cruiseTarget = MissileGuidance.GetAirToGroundTarget(targetPosition, vessel, agmDescentRatio);
					debugString += "\nDescending On Target";
				}
				else
				{
					cruiseTarget = MissileGuidance.GetCruiseTarget(targetPosition, vessel, cruiseAltitude);
					debugString += "\nCruising";
				}
			}
					
			float clampedSpeed = Mathf.Clamp((float)vessel.srfSpeed, 1, 1000);
			float limitAoA = Mathf.Clamp(3500 / clampedSpeed, 5, maxAoA);

			//debugString += "\n limitAoA: "+limitAoA.ToString("0.0");

			Vector3 upDirection = VectorUtils.GetUpDirection(transform.position);

			//axial rotation
			if(rotationTransform)
			{
				Quaternion originalRotation = transform.rotation;
				Quaternion originalRTrotation = rotationTransform.rotation;
				transform.rotation = Quaternion.LookRotation(transform.forward, upDirection);
				rotationTransform.rotation = originalRTrotation;
				Vector3 accel = vessel.acceleration;
				Vector3 tDir = (cruiseTarget - transform.position).normalized * 20;
				Vector3 lookUpDirection = Vector3.ProjectOnPlane(tDir, transform.forward);
				lookUpDirection = transform.InverseTransformPoint(lookUpDirection + transform.position);

				lookUpDirection = new Vector3(lookUpDirection.x, 0, 0);
				lookUpDirection += 10 * Vector3.up;
				//Debug.Log ("lookUpDirection: "+lookUpDirection);


				rotationTransform.localRotation = Quaternion.Lerp(rotationTransform.localRotation, Quaternion.LookRotation(Vector3.forward, lookUpDirection), 0.04f);
				Quaternion finalRotation = rotationTransform.rotation;
				transform.rotation = originalRotation;
				rotationTransform.rotation = finalRotation;
			}

			aeroTorque = MissileGuidance.DoAeroForces(this, cruiseTarget, liftArea, controlAuthority * steerMult, aeroTorque, finalMaxTorque, limitAoA); 
			CheckMiss();

			debugString += "\nRadarAlt: " + MissileGuidance.GetRadarAltitude(vessel);
		}

		void AAMGuidance()
		{
			Vector3 aamTarget;
			if(targetAcquired)
			{
				DrawDebugLine(transform.position+(part.rb.velocity*Time.fixedDeltaTime), targetPosition);

				aamTarget = MissileGuidance.GetAirToAirTarget(targetPosition, targetVelocity, targetAcceleration, vessel, out timeToImpact);
				if(Vector3.Angle(aamTarget-transform.position, transform.forward) > maxOffBoresight*0.75f)
				{
					aamTarget = targetPosition;
				}

				//proxy detonation
				if(proxyDetonate && ((targetPosition+(targetVelocity*Time.fixedDeltaTime))-(transform.position)).sqrMagnitude < Mathf.Pow(blastRadius*0.5f,2))
				{
					part.temperature = part.maxTemp + 100;
				}
			}
			else
			{
				aamTarget = transform.position + (20*vessel.srf_velocity.normalized);
			}


			if(Time.time-timeFired > dropTime+0.25f)
			{
				aeroTorque = MissileGuidance.DoAeroForces(this, aamTarget, liftArea, controlAuthority * steerMult, aeroTorque, finalMaxTorque, maxAoA);
			}

			CheckMiss();
		}

		void AGMGuidance()
		{
			if(targetingMode != TargetingModes.GPS)
			{
				if(targetAcquired)
				{
					//lose lock if seeker reaches gimbal limit
					float targetViewAngle = Vector3.Angle(transform.forward, targetPosition - transform.position);
				
					if(targetViewAngle > maxOffBoresight)
					{
						Debug.Log("AGM Missile guidance failed - target out of view");
						guidanceActive = false;
					}
					CheckMiss();
				}
				else
				{
					if(targetingMode == TargetingModes.Laser)
					{
						//keep going straight until found laser point
						targetPosition = laserStartPosition + (20000 * startDirection);
					}
				}
			}

			Vector3 agmTarget = MissileGuidance.GetAirToGroundTarget(targetPosition, vessel, agmDescentRatio);

			aeroTorque = MissileGuidance.DoAeroForces(this, agmTarget, liftArea, controlAuthority * steerMult, aeroTorque, finalMaxTorque, maxAoA);
		}

		void AGMBallisticGuidance()
		{
			Vector3 agmTarget;
			bool validSolution = MissileGuidance.GetBallisticGuidanceTarget(targetPosition, vessel, true, out agmTarget);
			if(!validSolution || Vector3.Angle(targetPosition - transform.position, agmTarget - transform.position) > Mathf.Clamp(maxOffBoresight, 0, 65))
			{
				Vector3 dToTarget = targetPosition - transform.position;
				Vector3 direction = Quaternion.AngleAxis(maxOffBoresight * 0.9f, Vector3.Cross(dToTarget, VectorUtils.GetUpDirection(transform.position))) * dToTarget;
				agmTarget = transform.position + direction;
			}

			aeroTorque = MissileGuidance.DoAeroForces(this, agmTarget, liftArea, controlAuthority * steerMult, aeroTorque, finalMaxTorque, maxAoA);
		}

		void UpdateGPSTarget()
		{
			if(targetAcquired)
			{
				targetPosition = VectorUtils.GetWorldSurfacePostion(targetGPSCoords, vessel.mainBody);
				targetVelocity = Vector3.zero;
				targetAcceleration = Vector3.zero;
			}
			else
			{
				guidanceActive = false;
			}
		}

		void UpdateLaserTarget()
		{
			if(targetAcquired)
			{
				if(lockedCamera && lockedCamera.groundStabilized && !lockedCamera.gimbalLimitReached && lockedCamera.surfaceDetected) //active laser target
				{
					targetPosition = lockedCamera.groundTargetPosition;
					targetVelocity = (targetPosition - lastLaserPoint) / Time.fixedDeltaTime;
					targetAcceleration = Vector3.zero;
					lastLaserPoint = targetPosition;
				}
				else //lost active laser target, home on last known position
				{
					if(CMSmoke.RaycastSmoke(new Ray(transform.position, lastLaserPoint - transform.position)))
					{
						//Debug.Log("Laser missile affected by smoke countermeasure");
						float angle = VectorUtils.FullRangePerlinNoise(0.75f * Time.time, 10) * BDArmorySettings.SMOKE_DEFLECTION_FACTOR;
						targetPosition = VectorUtils.RotatePointAround(lastLaserPoint, transform.position, VectorUtils.GetUpDirection(transform.position), angle);
						targetVelocity = Vector3.zero;
						targetAcceleration = Vector3.zero;
						lastLaserPoint = targetPosition;
					}
					else
					{
						targetPosition = lastLaserPoint;
					}
				}
			}
			else
			{
				ModuleTargetingCamera foundCam = null;
				foundCam = BDATargetManager.GetLaserTarget(this);
				if(foundCam != null && foundCam.cameraEnabled && foundCam.groundStabilized && CanSeePosition(foundCam.groundTargetPosition))
				{
					Debug.Log("Laser guided missile actively found laser point. Enabling guidance.");
					lockedCamera = foundCam;
					targetAcquired = true;
				}
			}
		}

		void UpdateLegacyTarget()
		{
			if(legacyTargetVessel)
			{
				maxOffBoresight = 90;
				
				if(targetingMode == TargetingModes.Radar)
				{
					activeRadarRange = 20000;
					targetAcquired = true;
					radarTarget = new TargetSignatureData(legacyTargetVessel, 500);
					return;
				}
				else if(targetingMode == TargetingModes.Heat)
				{
					targetAcquired = true;
					heatTarget = new TargetSignatureData(legacyTargetVessel, 500);
					return;
				}

				if(targetingMode != TargetingModes.GPS || targetAcquired)
				{
					targetAcquired = true;
					targetPosition = legacyTargetVessel.CoM + (legacyTargetVessel.rb_velocity * Time.fixedDeltaTime);
					targetGPSCoords = VectorUtils.WorldPositionToGeoCoords(targetPosition, vessel.mainBody);
					targetVelocity = legacyTargetVessel.srf_velocity;
					targetAcceleration = legacyTargetVessel.acceleration;
					lastLaserPoint = targetPosition;
					lockFailTimer = 0;
				}
			}
		}

		void UpdateAntiRadiationTarget()
		{
			if(!targetAcquired)
			{
				guidanceActive = false;
				return;
			}

			if(FlightGlobals.ready)
			{
				if(lockFailTimer < 0)
				{
					lockFailTimer = 0;
				}
				lockFailTimer += Time.fixedDeltaTime;
			}

			if(lockFailTimer > 8)
			{
				guidanceActive = false;
				targetAcquired = false;
			}
			else
			{
				targetPosition = VectorUtils.GetWorldSurfacePostion(targetGPSCoords, vessel.mainBody);
			}
		}

		void ReceiveRadarPing(Vessel v, Vector3 source, RadarWarningReceiver.RWRThreatTypes type, float persistTime)
		{
			if(targetingMode == TargetingModes.AntiRad && targetAcquired && v == vessel)
			{
				if((source - VectorUtils.GetWorldSurfacePostion(targetGPSCoords, vessel.mainBody)).sqrMagnitude < Mathf.Pow(50, 2)
					&& Vector3.Angle(source-transform.position, transform.forward) < maxOffBoresight)
				{
					targetAcquired = true;
					targetPosition = source;
					targetGPSCoords = VectorUtils.WorldPositionToGeoCoords(targetPosition, vessel.mainBody);
					targetVelocity = Vector3.zero;
					targetAcceleration = Vector3.zero;
					lockFailTimer = 0;
				}
			}
		}
		
		int snapshotTicker;
		int locksCount = 0;
		TargetSignatureData[] scannedTargets;
		void UpdateRadarTarget()
		{
			targetAcquired = false;

			float angleToTarget = Vector3.Angle(radarTarget.position-transform.position,transform.forward);
			if(radarTarget.exists)
			{
				if(!activeRadar && ((radarTarget.predictedPosition - transform.position).sqrMagnitude > Mathf.Pow(activeRadarRange, 2) || angleToTarget > maxOffBoresight * 0.75f))
				{
					if(radar
					   && radar.lockedTarget.exists
					   && (radarTarget.predictedPosition - radar.lockedTarget.predictedPosition).sqrMagnitude < Mathf.Pow(100, 2)
					   )
					{
						targetAcquired = true;
						radarTarget = radar.lockedTarget;
						targetPosition = radarTarget.predictedPosition;
						targetVelocity = radarTarget.velocity;
						targetAcceleration = radarTarget.acceleration;
						//radarTarget.signalStrength = 
						return;
					}
					else
					{
						Debug.Log("Radar guidance failed. Out of range and no data feed.");
						radarTarget = TargetSignatureData.noTarget;
						legacyTargetVessel = null;
						return;
					}
				}
				else
				{
					radar = null;

					if(angleToTarget > maxOffBoresight)
					{
						Debug.Log("Radar guidance failed.  Target is out of active seeker gimbal limits.");
						radarTarget = TargetSignatureData.noTarget;
						legacyTargetVessel = null;
						return;
					}
					else
					{
						if(scannedTargets == null) scannedTargets = new TargetSignatureData[5];
						TargetSignatureData.ResetTSDArray(ref scannedTargets);
						Ray ray = new Ray(transform.position, radarTarget.predictedPosition - transform.position);
						bool pingRWR = Time.time - lastRWRPing > 0.4f;
						if(pingRWR) lastRWRPing = Time.time;
						bool radarSnapshot = (snapshotTicker > 20);
						if(radarSnapshot)
						{
							snapshotTicker = 0;
						}
						else
						{
							snapshotTicker++;
						}
						RadarUtils.ScanInDirection(ray, lockedSensorFOV, activeRadarMinThresh, ref scannedTargets, 0.4f, pingRWR, RadarWarningReceiver.RWRThreatTypes.MissileLock, radarSnapshot);
						float sqrThresh = radarLOALSearching ? Mathf.Pow(500, 2) : Mathf.Pow(40, 2);

						if(radarLOAL && radarLOALSearching && !radarSnapshot)
						{
							//only scan on snapshot interval
						}
						else
						{
							for(int i = 0; i < scannedTargets.Length; i++)
							{
								if(scannedTargets[i].exists && (scannedTargets[i].predictedPosition - radarTarget.predictedPosition).sqrMagnitude < sqrThresh)
								{
									radarTarget = scannedTargets[i];
									targetAcquired = true;
									radarLOALSearching = false;
									targetPosition = radarTarget.predictedPosition + (radarTarget.velocity * Time.fixedDeltaTime);
									targetVelocity = radarTarget.velocity;
									targetAcceleration = radarTarget.acceleration;

									if(!activeRadar && Time.time - timeFired > 1)
									{
										if(locksCount == 0)
										{
											RadarWarningReceiver.PingRWR(ray, lockedSensorFOV, RadarWarningReceiver.RWRThreatTypes.MissileLaunch, 2f);
											Debug.Log("Pitbull! Radar missile has gone active.  Radar sig strength: " + radarTarget.signalStrength.ToString("0.0"));
										}
										else if(locksCount > 2)
										{
											guidanceActive = false;
											checkMiss = true;
											if(BDArmorySettings.DRAW_DEBUG_LABELS)
											{
												Debug.Log("Radar missile reached max re-lock attempts.");
											}
										}
										locksCount++;
									}
									activeRadar = true;
									return;
								}
							}
						}

						if(radarLOAL)
						{
							radarLOALSearching = true;
							targetAcquired = true;
							targetPosition = radarTarget.predictedPosition + (radarTarget.velocity * Time.fixedDeltaTime);
							targetVelocity = radarTarget.velocity;
							targetAcceleration = Vector3.zero;
							activeRadar = false;
						}
						else
						{
							radarTarget = TargetSignatureData.noTarget;
						}

					}
				}
			}
			else if(radarLOAL && radarLOALSearching)
			{
				if(scannedTargets == null) scannedTargets = new TargetSignatureData[5];
				TargetSignatureData.ResetTSDArray(ref scannedTargets);
				Ray ray = new Ray(transform.position, transform.forward);
				bool pingRWR = Time.time - lastRWRPing > 0.4f;
				if(pingRWR) lastRWRPing = Time.time;
				bool radarSnapshot = (snapshotTicker > 6);
				if(radarSnapshot)
				{
					snapshotTicker = 0;
				}
				else
				{
					snapshotTicker++;
				}
				RadarUtils.ScanInDirection(ray, lockedSensorFOV*3, activeRadarMinThresh*2, ref scannedTargets, 0.4f, pingRWR, RadarWarningReceiver.RWRThreatTypes.MissileLock, radarSnapshot);
				float sqrThresh = Mathf.Pow(300, 2);

				float smallestAngle = 360;
				TargetSignatureData lockedTarget = TargetSignatureData.noTarget;

				for(int i = 0; i < scannedTargets.Length; i++)
				{
					if(scannedTargets[i].exists && (scannedTargets[i].predictedPosition - radarTarget.predictedPosition).sqrMagnitude < sqrThresh)
					{
						float angle = Vector3.Angle(scannedTargets[i].predictedPosition - transform.position, transform.forward);
						if(angle < smallestAngle)
						{
							lockedTarget = scannedTargets[i];
							smallestAngle = angle;
						}


						activeRadar = true;
						return;
					}
				}

				if(lockedTarget.exists)
				{
					radarTarget = lockedTarget;
					targetAcquired = true;
					radarLOALSearching = false;
					targetPosition = radarTarget.predictedPosition + (radarTarget.velocity * Time.fixedDeltaTime);
					targetVelocity = radarTarget.velocity;
					targetAcceleration = radarTarget.acceleration;

					if(!activeRadar && Time.time - timeFired > 1)
					{
						RadarWarningReceiver.PingRWR(new Ray(transform.position, radarTarget.predictedPosition - transform.position), lockedSensorFOV, RadarWarningReceiver.RWRThreatTypes.MissileLaunch, 2f);
						Debug.Log("Pitbull! Radar missile has gone active.  Radar sig strength: " + radarTarget.signalStrength.ToString("0.0"));
					}
					return;
				}
				else
				{
					targetAcquired = true;
					targetPosition = transform.position + (startDirection * 500);
					targetVelocity = Vector3.zero;
					targetAcceleration = Vector3.zero;
					radarLOALSearching = true;
					return;
				}
			}

			if(!radarTarget.exists)
			{
				legacyTargetVessel = null;
			}
		}

		void UpdateHeatTarget()
		{
			targetAcquired = false;

			if(lockFailTimer > 1)
			{
				legacyTargetVessel = null;

				return;
			}
			
			if(heatTarget.exists && lockFailTimer < 0)
			{
				lockFailTimer = 0;
			}
			if(lockFailTimer >= 0)
			{
				Ray lookRay = new Ray(transform.position, heatTarget.position+(heatTarget.velocity*Time.fixedDeltaTime)-transform.position);
				heatTarget = BDATargetManager.GetHeatTarget(lookRay, lockedSensorFOV/2, heatThreshold, allAspect);
				
				if(heatTarget.exists)
				{
					targetAcquired = true;
					targetPosition = heatTarget.position+(heatTarget.velocity*Time.fixedDeltaTime);
					targetVelocity = heatTarget.velocity;
					targetAcceleration = heatTarget.acceleration;
					lockFailTimer = 0;
				}
				else
				{
					if(FlightGlobals.ready)
					{
						lockFailTimer += Time.fixedDeltaTime;
					}
				}
			}


		}

		void CheckMiss()
		{
			float sqrDist = ((targetPosition+(targetVelocity*Time.fixedDeltaTime))-(transform.position+(part.rb.velocity*Time.fixedDeltaTime))).sqrMagnitude;
			if(sqrDist < 160000 || (MissileState == MissileStates.PostThrust && (guidanceMode == GuidanceModes.AAMLead || guidanceMode == GuidanceModes.AAMPure)))
			{
				checkMiss = true;	
			}
			
			//kill guidance if missile has missed
			if(!hasMissed && checkMiss && 
				Vector3.Dot(targetPosition-transform.position,transform.forward)<0) 
			{
				Debug.Log ("Missile CheckMiss showed miss");
				hasMissed = true;
				guidanceActive = false;
				targetMf = null;
				if(hasRCS) KillRCS();
				if(sqrDist < Mathf.Pow(blastRadius * 0.5f, 2)) part.temperature = part.maxTemp + 100;

				isTimed = true;
				detonationTime = Time.time - timeFired + 1.5f;
				return;
			}
		}

		void RayDetonator()
		{
			Vector3 lineStart = transform.position;
			Vector3 lineEnd = transform.position + part.rb.velocity;
			RaycastHit rayHit;
			if(Physics.Linecast(lineStart, lineEnd, out rayHit, 557057))
			{
				if(rayHit.collider.attachedRigidbody && rayHit.collider.attachedRigidbody != part.rb)
				{
					part.temperature = part.temperature + 100;
				}
			}
			
		}

	

		void DrawDebugLine(Vector3 start, Vector3 end)
		{
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
				LR.SetPosition(0, start);
				LR.SetPosition(1, end);
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

				
				if(legacyTargetVessel!=null)
				{
					foreach(var wpm in legacyTargetVessel.FindPartModulesImplementing<MissileFire>())
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


		void PartDie(Part p)
		{
			if(p == part)
			{
				Detonate();
				BDATargetManager.FiredMissiles.Remove(this);
				GameEvents.onPartDie.Remove(PartDie);
			}
		}



		public bool CanSeePosition(Vector3 pos)
		{
			if((pos-transform.position).sqrMagnitude < Mathf.Pow(20,2))
			{
				return false;
			}

			float dist = 10000;
			Ray ray = new Ray(transform.position, pos-transform.position);
			RaycastHit rayHit;
			if(Physics.Raycast(ray, out rayHit, dist, 557057))
			{
				if((rayHit.point-pos).sqrMagnitude < 200)
				{
					return true;
				}
				else
				{
					return false;
				}
			}

			return true;
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

				/*
				if(targetVessel == null)
				{
					if(target!=null && FlightGlobals.ActiveVessel.gameObject == target)
					{
						targetVessel = FlightGlobals.ActiveVessel;
					}
					else if(target!=null && !BDArmorySettings.Flares.Contains(t => t.gameObject == target))
					{
						targetVessel = Part.FromGO(target).vessel;
					}
					
				}
				*/
				if(legacyTargetVessel!=null)
				{
					foreach(var wpm in legacyTargetVessel.FindPartModulesImplementing<MissileFire>())
					{
						wpm.missileIsIncoming = false;
					}
				}
				
				if(part!=null)
				{
					
					part.temperature = part.maxTemp + 100;
				}
				Vector3 position = transform.position+part.rb.velocity*Time.fixedDeltaTime;
				if(sourceVessel==null) sourceVessel = vessel;
				
				SeismicChargeFX.CreateSeismicExplosion(transform.position-(part.rb.velocity.normalized*15), UnityEngine.Random.rotation);
				
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
			

		
		void WarnTarget()
		{
			if(legacyTargetVessel == null)
			{
				return;
				/*
				if(FlightGlobals.ActiveVessel.gameObject == target)
				{
					targetVessel = FlightGlobals.ActiveVessel;
				}
				else if(target!=null && !BDArmorySettings.Flares.Contains(target))
				{
					targetVessel = Part.FromGO(target).vessel;
				}
				*/
				
			}
			
			if(legacyTargetVessel!=null)
			{
				foreach(var wpm in legacyTargetVessel.FindPartModulesImplementing<MissileFire>())
				{
					wpm.MissileWarning(Vector3.Distance(transform.position, legacyTargetVessel.transform.position), this);
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
				Vector3 relV = legacyTargetVessel.obt_velocity-vessel.obt_velocity;
				Vector3 localRelV = rcsTransforms[i].transform.InverseTransformPoint(relV + transform.position);


				float giveThrust = Mathf.Clamp(-localRelV.z, 0, rcsThrust);
				part.rb.AddForce(-giveThrust*rcsTransforms[i].transform.forward);

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


		void OnGUI()
		{
			if(hasFired && BDArmorySettings.DRAW_DEBUG_LABELS)	
			{
				GUI.Label(new Rect(200,200,200,200), debugString);	
			}
		}


		void AntiSpin()
		{
			Vector3 spin = Vector3.Project(part.rb.angularVelocity, part.rb.transform.forward);// * 8 * Time.fixedDeltaTime;
			part.rb.angularVelocity -= spin;
			//rigidbody.maxAngularVelocity = 7;
			part.rb.angularVelocity -= 0.5f * part.rb.angularVelocity;
		}
		
		void SimpleDrag()
		{
			part.dragModel = Part.DragModel.NONE;
			float simSpeedSquared = (float)vessel.srf_velocity.sqrMagnitude;
			Vector3 currPos = transform.position;
			float drag = deployed ? deployedDrag : simpleDrag;
			Vector3 dragForce = (0.008f * part.rb.mass) * drag * 0.5f * simSpeedSquared * (float) FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPos), FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody) * vessel.srf_velocity.normalized;
			part.rb.AddForceAtPosition(-dragForce, transform.TransformPoint(simpleCoD));

		}

		void ParseModes()
		{
			homingType = homingType.ToLower();
			switch(homingType)
			{
			case "aam":
				guidanceMode = GuidanceModes.AAMLead;
				break;
			case "aamlead":
				guidanceMode = GuidanceModes.AAMLead;
				break;
			case "aampure":
				guidanceMode = GuidanceModes.AAMPure;
				break;
			case "agm":
				guidanceMode = GuidanceModes.AGM;
				break;
			case "agmballistic":
				guidanceMode = GuidanceModes.AGMBallistic;
				break;
			case "cruise":
				guidanceMode = GuidanceModes.Cruise;
				break;
			case "sts":
				guidanceMode = GuidanceModes.STS;
				break;
			case "rcs":
				guidanceMode = GuidanceModes.RCS;
				break;
			default:
				guidanceMode = GuidanceModes.None;
				break;
			}

			targetingType = targetingType.ToLower();
			switch(targetingType)
			{
			case "radar":
				targetingMode = TargetingModes.Radar;
				break;
			case "heat":
				targetingMode = TargetingModes.Heat;
				break;
			case "laser":
				targetingMode = TargetingModes.Laser;
				break;
			case "gps":
				targetingMode = TargetingModes.GPS;
				maxOffBoresight = 360;
				break;
			case "antirad":
				targetingMode = TargetingModes.AntiRad;
				break;
			default:
				targetingMode = TargetingModes.None;
				break;
			}
		}
		
	}
}

