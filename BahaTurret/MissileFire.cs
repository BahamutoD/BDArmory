using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class MissileFire : PartModule
	{
		//weapons
		private List<IBDWeapon> weaponTypes = new List<IBDWeapon>();
		public IBDWeapon[] weaponArray;
		
		[KSPField(guiActiveEditor = false, isPersistant = true, guiActive = false)]
		public int weaponIndex = 0;
		
		ScreenMessage selectionMessage;
		//ScreenMessage armedMessage;
		string selectionText = "";
		Transform cameraTransform;
		Part lastFiredSym = null;
		
		float startTime;
		
		float rippleTimer;
		[KSPField(isPersistant = true)]
		public float rippleRPM = 650;
		float triggerTimer = 0;
		
		//public float triggerHoldTime = 0.3f;

		[KSPField(isPersistant = true)]
		public bool rippleFire = false;
		
		public bool hasSingleFired = false;
		
		
		//
		
		
		//bomb aimer
		Part bombPart = null;
		Vector3 bombAimerPosition = Vector3.zero;
		Texture2D bombAimerTexture = GameDatabase.Instance.GetTexture("BDArmory/Textures/grayCircle", false);
		bool showBombAimer = false;
		//
		
		
		//targeting
		private List<Vessel> loadedVessels = new List<Vessel>();
		float targetListTimer;
		
		
		//rocket aimer handling
		RocketLauncher nextRocket = null;
		
		
		//sounds
		AudioSource audioSource;
		public AudioSource warningAudioSource;
		AudioSource targetingAudioSource;
		AudioClip clickSound = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/click");
		AudioClip warningSound = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/warning");
		AudioClip armOnSound = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/armOn");
		AudioClip armOffSound = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/armOff");

		//missile warning
		public bool missileIsIncoming = false;
		float incomingMissileDistance = 2500;
		
		
		//guard mode vars
		float targetScanTimer = 0;
		Vessel guardTarget = null;
		public TargetInfo currentTarget;

		//AIPilot
		public BDModulePilotAI pilotAI = null;

		//targeting pods
		public List<ModuleTargetingCamera> targetingPods = new List<ModuleTargetingCamera>();

		//radar
		public ModuleRadar radar;
		public List<ModuleRadar> radars = new List<ModuleRadar>();

		//jammers
		public List<ModuleECMJammer> jammers = new List<ModuleECMJammer>();

		//RWR
		public RadarWarningReceiver rwr;

		//GPS
		public Vector3d designatedGPSCoords = Vector3d.zero;

		//current weapon ref
		MissileLauncher currMiss;
		public MissileLauncher currentMissile
		{
			get
			{
				if(selectedWeapon != null && (selectedWeapon.GetWeaponClass() == WeaponClasses.Missile || selectedWeapon.GetWeaponClass() == WeaponClasses.Bomb))
				{
					if(currMiss && currMiss.part == selectedWeapon.GetPart())
					{
						return currMiss;
					}
					else
					{
						currMiss = selectedWeapon.GetPart().FindModuleImplementing<MissileLauncher>();
						return currMiss;
					}
				}
				else
				{
					currMiss = null;
					return null;
				}
			}
		}

		public ModuleWeapon currentGun
		{
			get
			{
				if(selectedWeapon != null && selectedWeapon.GetWeaponClass() == WeaponClasses.Gun)
				{
					return selectedWeapon.GetPart().FindModuleImplementing<ModuleWeapon>();
				}
				else
				{
					return null;
				}
			}
		}

		//KSP fields and events
		#region kspFields,events,actions
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Scan Interval"),
        	UI_FloatRange(minValue = 1f, maxValue = 60f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float targetScanInterval = 8;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Field of View"),
        	UI_FloatRange(minValue = 10f, maxValue = 360f, stepIncrement = 10f, scene = UI_Scene.All)]
		public float guardAngle = 100f;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Visual Range"),
        	UI_FloatRange(minValue = 100f, maxValue = 8000f, stepIncrement = 100f, scene = UI_Scene.All)]
        public float guardRange = 1500f;
		
		[KSPEvent(guiActive = true, guiName = "Guard Mode: Off", active = true)]
		public void GuiToggleGuardMode()
		{
			guardMode = !guardMode;	
			Fields["guardRange"].guiActive = guardMode;
			Fields["guardAngle"].guiActive = guardMode;
			Fields["targetMissiles"].guiActive = guardMode;
			Fields["targetScanInterval"].guiActive = guardMode;
			
			if(guardMode)
			{
				Events["GuiToggleGuardMode"].guiName = "Guard Mode: ON";
			}
			else
			{
				Events["GuiToggleGuardMode"].guiName = "Guard Mode: Off";
			}
			
			Misc.RefreshAssociatedWindows(part);
		}
		
		[KSPAction("Toggle Guard Mode")]
		public void AGToggleGuardMode(KSPActionParam param)
		{
			GuiToggleGuardMode();	
		}
		
		[KSPField(isPersistant = true)]
		public bool guardMode = false;
		bool wasGuardMode = true;

		
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Target Type: "), 
			UI_Toggle(disabledText = "Vessels", enabledText = "Missiles")]
		public bool targetMissiles = false;
		//bool smartTargetMissiles = false;
		
		[KSPAction("Toggle Target Type")]
		public void AGToggleTargetType(KSPActionParam param)
		{
			ToggleTargetType();
		}
		
		public void ToggleTargetType()
		{
			targetMissiles = !targetMissiles;
			audioSource.PlayOneShot (clickSound);
		}

		[KSPAction("Jettison Weapon")]
		public void AGJettisonWeapon(KSPActionParam param)
		{
			if(currentMissile)
			{
				foreach(var missile in vessel.FindPartModulesImplementing<MissileLauncher>())
				{
					if(missile.GetShortName() == currentMissile.GetShortName())
					{
						missile.Jettison();
					}
				}
			}
			else if(selectedWeapon!=null && selectedWeapon.GetWeaponClass() == WeaponClasses.Rocket)
			{
				foreach(var rocket in vessel.FindPartModulesImplementing<RocketLauncher>())
				{
					rocket.Jettison();
				}
			}
		}
		
		
		
		
		
		[KSPField(guiActiveEditor = true, isPersistant = true, guiActive = true, guiName = "Team: "), 
			UI_Toggle(disabledText = "A", enabledText = "B")]
		public bool team = false;


		
		[KSPAction("Toggle Team")]
		public void AGToggleTeam(KSPActionParam param)
		{
			ToggleTeam();	
		}
		
		public void ToggleTeam()
		{
			audioSource.PlayOneShot(clickSound);
			team = !team;
			foreach(var wpnMgr in vessel.FindPartModulesImplementing<MissileFire>())
			{
				wpnMgr.team = team;	
			}
			if(vessel.GetComponent<TargetInfo>())
			{
				vessel.GetComponent<TargetInfo>().RemoveFromDatabases();
				Destroy(vessel.GetComponent<TargetInfo>());
			}
		}
		
		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Armed: "), 
			UI_Toggle(disabledText = "Off", enabledText = "ARMED")]
		public bool isArmed = false;
		
		
		
		[KSPAction("Arm/Disarm")]
		public void AGToggleArm(KSPActionParam param)
		{
			ToggleArm();
		}
		
		public void ToggleArm()
		{
			isArmed = !isArmed;	
			if(isArmed) audioSource.PlayOneShot(armOnSound);
			else audioSource.PlayOneShot(armOffSound);
			
		}
		
		
		[KSPField(isPersistant = false, guiActive = true, guiName = "Weapon")]
		public string selectedWeaponString = "None";

		IBDWeapon sw = null;
		public IBDWeapon selectedWeapon
		{
			get
			{
				if((sw == null || sw.GetPart().vessel!=vessel) && weaponIndex>0)
				{
					foreach(IBDWeapon weapon in vessel.FindPartModulesImplementing<IBDWeapon>())
					{
						if(weapon.GetShortName() == selectedWeaponString)
						{
							sw = weapon;
							break;
						}
					}
				}
				return sw;
			}
			set
			{
				sw = value;
			}
		}
		
		[KSPAction("Fire")]
		public void AGFire(KSPActionParam param)
		{
			FireMissile();	
		}
		
		[KSPEvent(guiActive = true, guiName = "Fire", active = true)]
		public void GuiFire()
		{
			FireMissile();	
		}
		
		[KSPEvent(guiActive = true, guiName = "Next Weapon", active = true)]
		public void GuiCycle()
		{
			CycleWeapon(true);	
		}
		
		[KSPAction("Next Weapon")]
		public void AGCycle(KSPActionParam param)
		{
			CycleWeapon(true);
		}
		
		[KSPEvent(guiActive = true, guiName = "Previous Weapon", active = true)]
		public void GuiCycleBack()
		{
			CycleWeapon(false);	
		}
		
		[KSPAction("Previous Weapon")]
		public void AGCycleBack(KSPActionParam param)
		{
			CycleWeapon(false);
		}
		
		#endregion


		public bool canRipple = false;

		
		public override void OnStart (PartModule.StartState state)
		{
			UpdateMaxGuardRange();
			
			startTime = Time.time;

			if(HighLogic.LoadedSceneIsFlight)
			{
				part.force_activate();

				if(guardMode)
				{
					Events["GuiToggleGuardMode"].guiName = "Guard Mode: ON";
				}
				else
				{
					Events["GuiToggleGuardMode"].guiName = "Guard Mode: Off";
				}
				
				selectionMessage = new ScreenMessage("", 2, ScreenMessageStyle.LOWER_CENTER);
				
				UpdateList();
				if(weaponArray.Length > 0) selectedWeapon = weaponArray[weaponIndex];
				selectedWeaponString = GetWeaponName(selectedWeapon);
				
				cameraTransform = part.FindModelTransform("BDARPMCameraTransform");
				
				part.force_activate();
				rippleTimer = Time.time;
				targetListTimer = Time.time;
				
				Fields["guardRange"].guiActive = guardMode;
				Fields["guardAngle"].guiActive = guardMode;
				Fields["targetMissiles"].guiActive = guardMode;
				Fields["targetScanInterval"].guiActive = guardMode;

				audioSource = gameObject.AddComponent<AudioSource>();
				audioSource.minDistance = 500;
				audioSource.maxDistance = 1000;
				audioSource.dopplerLevel = 0;

				warningAudioSource = gameObject.AddComponent<AudioSource>();
				warningAudioSource.minDistance = 500;
				warningAudioSource.maxDistance = 1000;
				warningAudioSource.dopplerLevel = 0;

				targetingAudioSource = gameObject.AddComponent<AudioSource>();
				targetingAudioSource.minDistance = 100;
				targetingAudioSource.maxDistance = 1000;
				targetingAudioSource.dopplerLevel = 0;
				targetingAudioSource.loop = true;

				StartCoroutine (MissileWarningResetRoutine());
				
				if(vessel.isActiveVessel)
				{
					BDArmorySettings.Instance.ActiveWeaponManager = this;
				}

				UpdateVolume();
				BDArmorySettings.OnVolumeChange += UpdateVolume;

				StartCoroutine(StartupListUpdater());
			}
		}

		IEnumerator StartupListUpdater()
		{
			while(vessel.packed || !FlightGlobals.ready)
			{
				yield return null;
			}
			UpdateList();
		}

		void UpdateVolume()
		{
			if(audioSource)
			{
				audioSource.volume = BDArmorySettings.BDARMORY_UI_VOLUME;
			}
			if(warningAudioSource)
			{
				warningAudioSource.volume = BDArmorySettings.BDARMORY_UI_VOLUME;
			}
			if(targetingAudioSource)
			{
				targetingAudioSource.volume = BDArmorySettings.BDARMORY_UI_VOLUME;
			}
		}

		void OnDestroy()
		{
			BDArmorySettings.OnVolumeChange -= UpdateVolume;
		}
		
		void Update()
		{	
			
			if(HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
			{
				if(!vessel.packed)
				{
					if(wasGuardMode != guardMode)
					{
						if(!guardMode)
						{
							//disable turret firing and guard mode
							foreach(var weapon in vessel.FindPartModulesImplementing<ModuleWeapon>())
							{
								weapon.legacyTargetVessel = null;
								weapon.autoFire = false;
								weapon.aiControlled = false;
							}

						}

						wasGuardMode = guardMode;
					}

					if(weaponIndex >= weaponArray.Length)
					{
						hasSingleFired = true;
						triggerTimer = 0;
					
						weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
					
						if(BDArmorySettings.GAME_UI_ENABLED && vessel == FlightGlobals.ActiveVessel)
						{
							ScreenMessages.RemoveMessage(selectionMessage);
							selectionText = "Selected Weapon: " + GetWeaponName(weaponArray[weaponIndex]);
							selectionMessage.message = selectionText;
							ScreenMessages.PostScreenMessage(selectionMessage, true);
						}
					}
					if(weaponArray.Length > 0) selectedWeapon = weaponArray[weaponIndex];

					//finding next rocket to shoot (for aimer)
					FindNextRocket();

					//targeting
					if(selectedWeapon != null && (selectedWeapon.GetWeaponClass() == WeaponClasses.Missile || selectedWeapon.GetWeaponClass() == WeaponClasses.Bomb))
					{
						SearchForLaserPoint();
						SearchForHeatTarget();
						SearchForRadarSource();
					}
				}

				UpdateTargetingAudio();
			}

		}
		
		public override void OnFixedUpdate ()
		{
			if(guardMode && vessel.IsControllable)
			{
				GuardMode();
			}
			else
			{
				targetScanTimer = -100;
			}
			
			
			
			if(vessel.isActiveVessel)
			{
				if(guardMode) UpdateMaxGuardRange();
				
				if(!CheckMouseIsOnGui() && isArmed && Input.GetKey(BDArmorySettings.FIRE_KEY))
				{
					triggerTimer += Time.fixedDeltaTime;	
				}
				else
				{
					triggerTimer = 0;	
					hasSingleFired = false;
				}

				
				//firing missiles and rockets===
				if((selectedWeapon != null && 
				    (selectedWeapon.GetWeaponClass() == WeaponClasses.Rocket 
				    || selectedWeapon.GetWeaponClass() == WeaponClasses.Missile 
				    || selectedWeapon.GetWeaponClass() == WeaponClasses.Bomb)))
				{
					canRipple = true;
					if(!MapView.MapIsEnabled && triggerTimer > BDArmorySettings.TRIGGER_HOLD_TIME && !hasSingleFired)
					{
						if(rippleFire)
						{
							if(Time.time-rippleTimer > 60/rippleRPM)
							{
								FireMissile();
								rippleTimer = Time.time;
							}
						}
						else
						{
							FireMissile();
							hasSingleFired = true;
						}
					}
				}
				else
				{
					canRipple = false;
				}
				

				TargetAcquire();

			}
			BombAimer();
		}

		void UpdateTargetingAudio()
		{
			if(BDArmorySettings.GameIsPaused)
			{
				if(targetingAudioSource.isPlaying)
				{
					targetingAudioSource.Stop();
				}
				return;
			}

			if(selectedWeapon!=null && selectedWeapon.GetWeaponClass() == WeaponClasses.Missile && vessel.isActiveVessel)
			{
				MissileLauncher ml = currentMissile;
				if(ml.targetingMode == MissileLauncher.TargetingModes.Heat)
				{
					if(heatTarget.exists)
					{
						targetingAudioSource.clip = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/heatLock");
					}
					else
					{
						targetingAudioSource.clip = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/heatGrowl");
					}

					if(!targetingAudioSource.isPlaying)
					{
						targetingAudioSource.Play();
					}
				}
				else
				{
					if(targetingAudioSource.isPlaying)
					{
						targetingAudioSource.Stop();
					}
				}
			}
			else
			{
				if(targetingAudioSource.isPlaying)
				{
					targetingAudioSource.Stop();
				}
			}

		}



		IEnumerator MissileWarningResetRoutine()
		{
			while(enabled)
			{
				missileIsIncoming = false;
				yield return new WaitForSeconds(1);
			}
		}

		public void UpdateList()
		{
			weaponTypes.Clear();

			foreach(IBDWeapon weapon in vessel.FindPartModulesImplementing<IBDWeapon>())
			{
				string weaponName = weapon.GetShortName();
				bool alreadyAdded = false;
				foreach(var weap in weaponTypes)
				{
					if(weap.GetShortName() == weaponName)
					{
						alreadyAdded = true;
						break;
					}
				}

				if(!alreadyAdded)
				{
					weaponTypes.Add(weapon);
				}
			}
			
			//weaponTypes.Sort();
			weaponTypes = weaponTypes.OrderBy(w => w.GetShortName()).ToList();
			
			List<IBDWeapon> tempList = new List<IBDWeapon>();
			tempList.Add (null);
			tempList.AddRange(weaponTypes);
			
			weaponArray = tempList.ToArray();
			
			if(weaponIndex >= weaponArray.Length)
			{
				hasSingleFired = true;
				triggerTimer = 0;
				
			}

			weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
			selectedWeapon = weaponArray[weaponIndex];
			selectedWeaponString = GetWeaponName(selectedWeapon);

			if(GetWeaponName(selectedWeapon) != GetWeaponName(weaponArray[weaponIndex])&& vessel.isActiveVessel && Time.time-startTime > 1)
			{
				hasSingleFired = true;
				if(BDArmorySettings.GAME_UI_ENABLED && vessel == FlightGlobals.ActiveVessel)
				{
					ScreenMessages.RemoveMessage(selectionMessage);
					selectionText = "Selected Weapon: " + GetWeaponName(weaponArray[weaponIndex]);
					selectionMessage.message = selectionText;
					ScreenMessages.PostScreenMessage(selectionMessage, true);
				}
				
			}
		
			ToggleTurret();
		}

		public void CycleWeapon(bool forward)
		{
			UpdateList();
			if(forward) weaponIndex++;
			else weaponIndex--;
			weaponIndex = (int)Mathf.Repeat(weaponIndex, weaponArray.Length);

			selectedWeapon = weaponArray[weaponIndex];
			selectedWeaponString = GetWeaponName(selectedWeapon);

			hasSingleFired = true;
			triggerTimer = 0;
			
			if(BDArmorySettings.GAME_UI_ENABLED && vessel == FlightGlobals.ActiveVessel)
			{
				ScreenMessages.RemoveMessage(selectionMessage);
				selectionText = "Selected Weapon: " + selectedWeaponString;
				selectionMessage.message = selectionText;
				ScreenMessages.PostScreenMessage(selectionMessage, true);
			}
			
			
			//bomb stuff
			if(selectedWeapon != null && selectedWeapon.GetWeaponClass() == WeaponClasses.Bomb)
			{
				bombPart = selectedWeapon.GetPart();
			}
			else
			{
				bombPart = null;
			}
			
			//gun/turret stuff  
			ToggleTurret();
			
			if(vessel.isActiveVessel && !guardMode)
			{
				audioSource.PlayOneShot(clickSound);
			}



		}
		
		public void CycleWeapon(int index)
		{
			UpdateList();
			if(index >= weaponArray.Length)
			{
				return;
			}
			weaponIndex = index;
			selectedWeapon = weaponArray[index];
			selectedWeaponString = GetWeaponName(selectedWeapon);

			hasSingleFired = true;
			triggerTimer = 0;
			
			//bomb stuff
			if(selectedWeapon != null && selectedWeapon.GetWeaponClass() == WeaponClasses.Bomb)
			{
				bombPart = selectedWeapon.GetPart();
			}
			else
			{
				bombPart = null;
			}
			
			//gun/turret stuff  
			ToggleTurret();

			if(vessel.isActiveVessel && !guardMode)
			{
				audioSource.PlayOneShot(clickSound);
			}

		}
		
		
		public void FireMissile()
		{
			bool hasFired = false;

			if(selectedWeapon == null)
			{
				return;
			}

			if(lastFiredSym && lastFiredSym.partInfo.title != selectedWeapon.GetPart().partInfo.title)
			{
				lastFiredSym = null;
			}


			if(lastFiredSym != null && lastFiredSym.partName == selectedWeapon.GetPart().partName)
			{
				Part nextPart;
				if(FindSym(lastFiredSym)!=null)
				{
					nextPart = FindSym(lastFiredSym);
				}
				else 
				{
					nextPart = null;
				}


				if(selectedWeapon.GetWeaponClass() == WeaponClasses.Missile || selectedWeapon.GetWeaponClass() == WeaponClasses.Bomb)
				{
					foreach(MissileLauncher ml in lastFiredSym.FindModulesImplementing<MissileLauncher>())
					{
						if(ml.dropTime > 0.25f && !CheckBombClearance(ml))
						{
							lastFiredSym = null;
							break;
						}

						if(guardMode && guardTarget!=null && BDArmorySettings.ALLOW_LEGACY_TARGETING)
						{
							ml.FireMissileOnTarget(guardTarget);
						}
						else
						{
							SendTargetDataToMissile(ml);
							ml.FireMissile();
						}

						hasFired = true;

						lastFiredSym = nextPart;
						break;
					}	
				}
				else if(selectedWeapon.GetWeaponClass() == WeaponClasses.Rocket)
				{
					foreach(RocketLauncher rl in lastFiredSym.FindModulesImplementing<RocketLauncher>())
					{
						hasFired = true;
						rl.FireRocket();
						//rippleRPM = rl.rippleRPM;
						if(nextPart!=null)
						{
							foreach(PartResource r in nextPart.Resources.list)
							{
								if(r.amount>0) lastFiredSym = nextPart;
								else lastFiredSym = null;
							}	
						}
						break;
					}
				}

				//lastFiredSym = null;
				
			}



			if(!hasFired && lastFiredSym == null)
			{
				bool firedMissile = false;
				foreach(MissileLauncher ml in vessel.FindPartModulesImplementing<MissileLauncher>())
				{
					if(ml.part.partInfo.title == selectedWeapon.GetPart().partInfo.title)
					{
						if(ml.dropTime > 0 && !CheckBombClearance(ml))
						{
							continue;
						}

						lastFiredSym = FindSym(ml.part);
						
						if(guardMode && guardTarget!=null && BDArmorySettings.ALLOW_LEGACY_TARGETING)
						{
							ml.FireMissileOnTarget(guardTarget);
						}
						else
						{
							SendTargetDataToMissile(ml);
							ml.FireMissile();
						}
						firedMissile = true;
						
						break;
					}
				}

				if(!firedMissile)
				{
					foreach(RocketLauncher rl in vessel.FindPartModulesImplementing<RocketLauncher>())
					{
						bool hasRocket = false;
						foreach(PartResource r in rl.part.Resources.list)
						{
							if(r.amount>0) hasRocket = true;
						}	
						
						if(rl.part.partInfo.title == selectedWeapon.GetPart().partInfo.title && hasRocket)
						{
							lastFiredSym = FindSym(rl.part);
							rl.FireRocket();
							//rippleRPM = rl.rippleRPM;

							break;
						}
					}
				}
			}


			UpdateList();
			if(weaponIndex >= weaponArray.Length)
			{
				triggerTimer = 0;
				hasSingleFired = true;
				weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
				
				if(BDArmorySettings.GAME_UI_ENABLED && vessel == FlightGlobals.ActiveVessel)
				{
					ScreenMessages.RemoveMessage(selectionMessage);
					selectionText = "Selected Weapon: " + GetWeaponName(weaponArray[weaponIndex]);
					selectionMessage.message = selectionText;
					ScreenMessages.PostScreenMessage(selectionMessage, true);
				}

			}

	
		}
		
		//finds a symmetry partner
		public Part FindSym(Part p)
		{
			foreach(Part pSym in p.symmetryCounterparts)
			{
				if(pSym != p)
				{
					return pSym;
				}
			}
			
			return null;
		}

		void SendTargetDataToMissile(MissileLauncher ml)
		{
			if(ml.targetingMode == MissileLauncher.TargetingModes.Laser && laserPointDetected)
			{
				ml.lockedCamera = foundCam;
			}
			else if(ml.targetingMode == MissileLauncher.TargetingModes.GPS)
			{
				ml.targetGPSCoords = designatedGPSCoords;
				ml.targetAcquired = true;
			}
			else if(ml.targetingMode == MissileLauncher.TargetingModes.Heat && heatTarget.exists)
			{
				ml.heatTarget = heatTarget;
			}
			else if(ml.targetingMode == MissileLauncher.TargetingModes.Radar && radar && radar.lockedTarget.exists)
			{
				ml.radarTarget = radar.lockedTarget;
				if(radar.linked && radar.linkedRadar.locked)
				{
					ml.radar = radar.linkedRadar;
				}
				else
				{
					ml.radar = radar;
				}
				radar.lastMissile = ml;

			}
			else if(ml.targetingMode == MissileLauncher.TargetingModes.AntiRad && antiRadTargetAcquired)
			{
				ml.targetAcquired = true;
				ml.targetGPSCoords = VectorUtils.WorldPositionToGeoCoords(antiRadiationTarget, vessel.mainBody);
			}
		}
		
		
		public void TargetAcquire() 
		{
			if(isArmed && BDArmorySettings.ALLOW_LEGACY_TARGETING)
			{
				Vessel acquiredTarget = null;
				float smallestAngle = 8;
				
				if(Time.time-targetListTimer > 1)
				{
					loadedVessels.Clear();
					
					foreach(Vessel v in FlightGlobals.Vessels)
					{
						float viewAngle = Vector3.Angle(-transform.forward, v.transform.position-transform.position);
						if(v.loaded && viewAngle < smallestAngle)
						{
							if(!v.vesselName.Contains("(fired)")) loadedVessels.Add(v);
						}
					}
				}
			
				foreach(Vessel v in loadedVessels)
				{
					float viewAngle = Vector3.Angle(-transform.forward, v.transform.position-transform.position);
					//if(v!= vessel && v.loaded) Debug.Log ("view angle: "+viewAngle);
					if(v!= null && v != vessel && v.loaded && viewAngle < smallestAngle && CanSeeTarget(v))
					{
						acquiredTarget = v;
						smallestAngle = viewAngle;
					}
				}
				
				if(acquiredTarget != null && acquiredTarget != (Vessel)FlightGlobals.fetch.VesselTarget)
				{
					Debug.Log ("found target! : "+acquiredTarget.name);
					FlightGlobals.fetch.SetVesselTarget(acquiredTarget);
				}
			}
		}
		
		
	
		void BombAimer()
		{
			if(selectedWeapon == null)
			{
				showBombAimer = false;
				return;
			}
			if(!bombPart || selectedWeapon.GetPart()!=bombPart)
			{
				if(selectedWeapon.GetWeaponClass() == WeaponClasses.Bomb)
				{
					bombPart = selectedWeapon.GetPart();
				}
				else
				{
					showBombAimer = false;
					return;
				}
			}

			showBombAimer = 
			(
				vessel.isActiveVessel && 
				selectedWeapon !=null && 
				selectedWeapon.GetWeaponClass() == WeaponClasses.Bomb && 
				bombPart!=null && 
				BDArmorySettings.DRAW_AIMERS && 
				vessel.verticalSpeed < 50 && 
				AltitudeTrigger()
			);
			
			if(showBombAimer)
			{
				
				float simDeltaTime = 0.1f;
				float simTime = 0;
				Vector3 dragForce = Vector3.zero;
				Vector3 prevPos = transform.position;
				Vector3 currPos = transform.position;
				Vector3 simVelocity = vessel.rb_velocity;
				MissileLauncher ml = bombPart.GetComponent<MissileLauncher>();
				simVelocity += ml.decoupleSpeed * (ml.decoupleForward ? bombPart.transform.forward : -bombPart.transform.up);
				
				List<Vector3> pointPositions = new List<Vector3>();
				pointPositions.Add(currPos);
				
				
				prevPos = bombPart.transform.position;
				currPos = bombPart.transform.position;
			
				bombAimerPosition = Vector3.zero;

				float atmDensity = (float) FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPos), FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody);

				bool simulating = true;
				while(simulating)
				{
					prevPos = currPos;
					currPos += simVelocity * simDeltaTime;

					simVelocity += FlightGlobals.getGeeForceAtPosition(currPos) * simDeltaTime;
					float simSpeedSquared = simVelocity.sqrMagnitude;
					float drag = bombPart.minimum_drag;
					if(simTime > ml.deployTime)
					{
						drag = 	ml.deployedDrag;
					}
					dragForce = (0.008f * bombPart.mass) * drag * 0.5f * simSpeedSquared * atmDensity * simVelocity.normalized;
					simVelocity -= (dragForce/bombPart.mass)*simDeltaTime;

					Ray ray = new Ray(prevPos, currPos - prevPos);
					RaycastHit hitInfo;
					if(Physics.Raycast(ray, out hitInfo, Vector3.Distance(prevPos, currPos), 1<<15))
					{
						bombAimerPosition = hitInfo.point;
						simulating = false;
					}
					else if(FlightGlobals.getAltitudeAtPos(currPos) < 0)
					{
						bombAimerPosition = currPos - (FlightGlobals.getAltitudeAtPos(currPos)*FlightGlobals.getUpAxis());
						simulating = false;
					}
					
					simTime += simDeltaTime;
					pointPositions.Add(currPos);
				}
				
				/*
				//debug lines
				if(BDArmorySettings.DRAW_DEBUG_LINES && BDArmorySettings.DRAW_AIMERS)
				{
					Vector3[] pointsArray = pointPositions.ToArray();
					LineRenderer lr = GetComponent<LineRenderer>();
					if(!lr)
					{
						lr = gameObject.AddComponent<LineRenderer>();
					}
					lr.enabled = true;
					lr.SetWidth(.1f, .1f);
					lr.SetVertexCount(pointsArray.Length);
					for(int i = 0; i<pointsArray.Length; i++)
					{
						lr.SetPosition(i, pointsArray[i]);	
					}
				}
				else
				{
					if(gameObject.GetComponent<LineRenderer>())
					{
						gameObject.GetComponent<LineRenderer>().enabled = false;	
					}
				}

				*/
				
			}
			
		}
		
		bool AltitudeTrigger()
		{
			float maxAlt = Mathf.Clamp(BDArmorySettings.PHYSICS_RANGE * 0.75f, 2250, 5000);
			double asl = vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass());
			double radarAlt = asl - vessel.terrainAltitude;
			
			return radarAlt < maxAlt || asl < maxAlt;
		}
		
		void OnGUI()
		{
			if(HighLogic.LoadedSceneIsFlight && vessel == FlightGlobals.ActiveVessel && BDArmorySettings.GAME_UI_ENABLED)
			{
				if(showBombAimer && !MapView.MapIsEnabled)
				{
					float size = 128;
					Texture2D texture = BDArmorySettings.Instance.greenCircleTexture;
					if(currentMissile.targetingMode == MissileLauncher.TargetingModes.GPS)
					{
						texture = BDArmorySettings.Instance.largeGreenCircleTexture;
						size = 256;
					}
					BDGUIUtils.DrawTextureOnWorldPos(bombAimerPosition, texture, new Vector2(size,size), 0);
				}



				//MISSILE LOCK HUD
				if(currentMissile)
				{
					if(currentMissile.targetingMode == MissileLauncher.TargetingModes.Laser)
					{
						if(laserPointDetected)
						{
							BDGUIUtils.DrawTextureOnWorldPos(foundCam.groundTargetPosition, BDArmorySettings.Instance.greenCircleTexture, new Vector2(48, 48), 1);
						}

						foreach(var cam in BDATargetManager.ActiveLasers)
						{
							if(cam && cam.vessel != vessel && cam.surfaceDetected && cam.groundStabilized && !cam.gimbalLimitReached)
							{
								BDGUIUtils.DrawTextureOnWorldPos(cam.groundTargetPosition, BDArmorySettings.Instance.greenDiamondTexture, new Vector2(18, 18), 0);
							}
						}
					}
					else if(currentMissile.targetingMode == MissileLauncher.TargetingModes.Heat)
					{
						if(heatTarget.exists)
						{
							BDGUIUtils.DrawTextureOnWorldPos(heatTarget.position, BDArmorySettings.Instance.greenCircleTexture, new Vector2(36, 36), 3);
							float distanceToTarget = Vector3.Distance(heatTarget.position, currentMissile.transform.position);
							BDGUIUtils.DrawTextureOnWorldPos(currentMissile.transform.position + (distanceToTarget * currentMissile.transform.forward), BDArmorySettings.Instance.largeGreenCircleTexture, new Vector2(128, 128), 0);
							Vector3 fireSolution = MissileGuidance.GetAirToAirFireSolution(currentMissile, heatTarget.position, heatTarget.velocity);
							Vector3 fsDirection = (fireSolution - currentMissile.transform.position).normalized;
							BDGUIUtils.DrawTextureOnWorldPos(currentMissile.transform.position + (distanceToTarget * fsDirection), BDArmorySettings.Instance.greenDotTexture, new Vector2(6, 6), 0);
						}
						else
						{
							BDGUIUtils.DrawTextureOnWorldPos(currentMissile.transform.position + (2000 * currentMissile.transform.forward), BDArmorySettings.Instance.greenCircleTexture, new Vector2(36, 36), 3);
							BDGUIUtils.DrawTextureOnWorldPos(currentMissile.transform.position + (2000 * currentMissile.transform.forward), BDArmorySettings.Instance.largeGreenCircleTexture, new Vector2(156, 156), 0);
						}
					}
					else if(currentMissile.targetingMode == MissileLauncher.TargetingModes.Radar)
					{
						if(radar && radar.locked)
						{
							float distanceToTarget = Vector3.Distance(radar.lockedTarget.predictedPosition, currentMissile.transform.position);
							BDGUIUtils.DrawTextureOnWorldPos(currentMissile.transform.position + (distanceToTarget * currentMissile.transform.forward), BDArmorySettings.Instance.dottedLargeGreenCircle, new Vector2(128, 128), 0);
							Vector3 fireSolution = MissileGuidance.GetAirToAirFireSolution(currentMissile, radar.lockedTarget.predictedPosition, radar.lockedTarget.velocity);
							Vector3 fsDirection = (fireSolution - currentMissile.transform.position).normalized;
							BDGUIUtils.DrawTextureOnWorldPos(currentMissile.transform.position + (distanceToTarget * fsDirection), BDArmorySettings.Instance.greenDotTexture, new Vector2(6, 6), 0);
						}
					}
					else if(currentMissile.targetingMode == MissileLauncher.TargetingModes.AntiRad)
					{
						if(rwr && rwr.rwrEnabled)
						{
							for(int i = 0; i < rwr.pingsData.Length; i++)
							{
								if(rwr.pingsData[i].exists)
								{
									BDGUIUtils.DrawTextureOnWorldPos(rwr.pingWorldPositions[i], BDArmorySettings.Instance.greenDiamondTexture, new Vector2(22, 22), 0);
								}
							}
						}

						if(antiRadTargetAcquired)
						{
							BDGUIUtils.DrawTextureOnWorldPos(antiRadiationTarget, BDArmorySettings.Instance.openGreenSquare, new Vector2(22, 22), 0);
						}
					}
				}

				if((currentMissile && currentMissile.targetingMode == MissileLauncher.TargetingModes.GPS) || BDArmorySettings.Instance.showingGPSWindow)
				{
					if(designatedGPSCoords != Vector3d.zero)
					{
						BDGUIUtils.DrawTextureOnWorldPos(VectorUtils.GetWorldSurfacePostion(designatedGPSCoords, vessel.mainBody), BDArmorySettings.Instance.greenSpikedPointCircleTexture, new Vector2(22, 22), 0);
					}
				}
			}
			
			
			
		}
		
		void FindNextRocket()
		{
			if(selectedWeapon!=null && selectedWeapon.GetWeaponClass() == WeaponClasses.Rocket)
			{
				if(lastFiredSym!=null && lastFiredSym.partInfo.title == selectedWeapon.GetPart().partInfo.title)	
				{
					foreach(RocketLauncher rl in lastFiredSym.FindModulesImplementing<RocketLauncher>())
					{
						if(nextRocket!=null) nextRocket.drawAimer = false;
						rl.drawAimer = true;	
						nextRocket = rl;
						return;
					}
				}
				else
				{
					foreach(RocketLauncher rl in vessel.FindPartModulesImplementing<RocketLauncher>())
					{
						bool hasRocket = false;
						foreach(PartResource r in rl.part.Resources.list)
						{
							if(r.amount>0) hasRocket = true;
							else
							{
								rl.drawAimer = false;	
							}
						}	
						
						if(rl.part.partInfo.title == selectedWeapon.GetPart().partInfo.title && hasRocket)
						{
							if(nextRocket!=null) nextRocket.drawAimer = false;
							rl.drawAimer = true;
							nextRocket = rl;
							return;
						}
					}
				}
			}
			else
			{
				foreach(RocketLauncher rl in vessel.FindPartModulesImplementing<RocketLauncher>())
				{
					rl.drawAimer = false;
					nextRocket = null;
				}
			}
		}

		public void ToggleGuardMode()
		{
			guardMode = !guardMode;

			if(guardMode)
			{

			}
			else
			{

			}
		}
		
		void GuardMode()
		{
			//setting turrets to guard mode
			if(selectedWeapon!=null && selectedWeapon.GetWeaponClass() == WeaponClasses.Gun)
			{
				foreach(var weapon in vessel.FindPartModulesImplementing<ModuleWeapon>()) //make this not have to go every frame
				{
					if(weapon.part.partInfo.title == selectedWeapon.GetPart().partInfo.title)	
					{
						weapon.EnableWeapon();
						weapon.aiControlled = true;
						weapon.maxAutoFireAngle = vessel.Landed ? 3 : 10;
					}
				}
			}
			
			if(guardTarget)
			{
				//release target if out of range
				if((guardTarget.transform.position-transform.position).sqrMagnitude > Mathf.Pow(guardRange, 2)) 
				{
					SetTarget(null);
				}
			}
			else if(selectedWeapon!=null && selectedWeapon.GetWeaponClass() == WeaponClasses.Gun)
			{
				foreach(var weapon in vessel.FindPartModulesImplementing<ModuleWeapon>())
				{
					if(weapon.part.partInfo.title == selectedWeapon.GetPart().partInfo.title)	
					{
						weapon.autoFire = false;
						weapon.legacyTargetVessel = null;
					}
				}
			}

			if(missileIsIncoming)
			{
				if(!isFlaring)
				{
					StartCoroutine(FlareRoutine());
				}

				targetScanTimer -= Time.fixedDeltaTime; //advance scan timing (increased urgency)
			}

			//scan and acquire new target
			if(Time.time-targetScanTimer > targetScanInterval)
			{
				SetTarget(null);
				ScanAllTargets();
				SmartFindTarget();
				targetScanTimer = Time.time;

				if(guardTarget == null || selectedWeapon == null)
				{
					return;
				}

				//firing
				if(selectedWeapon != null && selectedWeapon.GetWeaponClass() == WeaponClasses.Missile)
				{
					bool launchAuthorized = true;


					float targetAngle = Vector3.Angle(-transform.forward, guardTarget.transform.position-transform.position);
					if(targetAngle > guardAngle/2 || (pilotAI && !pilotAI.GetLaunchAuthorization(guardTarget, this))) //dont fire yet if target out of guard angle or not AIPilot authorized
					{
						launchAuthorized = false;
					}
					else if((vessel.Landed || guardTarget.Landed) && (currentTarget.position-transform.position).sqrMagnitude < Mathf.Pow(1000, 2))  //fire the missile only if target is further than 1000m
					{
						launchAuthorized = false;
					}
					else if(!vessel.Landed && !guardTarget.Landed && (currentTarget.position-transform.position).sqrMagnitude < Mathf.Pow(400, 2)) //if air2air only fire if futher than 400m
					{
						launchAuthorized = false;
					}

					if(launchAuthorized)
					{
						StartCoroutine(GuardMissileRoutine(currentMissile));
					}
					else
					{
						targetScanTimer -= 0.85f * targetScanInterval;
					}
				}
				else if(selectedWeapon.GetWeaponClass() == WeaponClasses.Gun)
				{
					foreach(var weapon in vessel.FindPartModulesImplementing<ModuleWeapon>())
					{
						if(weapon.part.partInfo.title == selectedWeapon.GetPart().partInfo.title)	
						{
							weapon.legacyTargetVessel = guardTarget;
							weapon.autoFireTimer = Time.time;
							weapon.autoFireLength = targetScanInterval/2;
						}
					}
				}
			}
		}


		bool guardFiringMissile = false;
		IEnumerator GuardMissileRoutine(MissileLauncher ml)
		{
			if(ml && !guardFiringMissile)
			{
				guardFiringMissile = true;

				if(BDArmorySettings.ALLOW_LEGACY_TARGETING)
				{
					Debug.Log("Firing on target: " + guardTarget.GetName()+", (legacy targeting)");
					ml.FireMissileOnTarget(guardTarget);
					UpdateList();
				}
				else if(ml.targetingMode == MissileLauncher.TargetingModes.Radar && radar)
				{
					if(!radar.locked || (radar.lockedTarget.predictedPosition - guardTarget.transform.position).sqrMagnitude > Mathf.Pow(40, 2))
					{
						radar.TryLockTarget(guardTarget.transform.position);
						yield return new WaitForSeconds(Mathf.Clamp(2, 0.2f, targetScanInterval / 2));
					}

					if(guardTarget && radar.locked)
					{
						Debug.Log("Firing on target: " + guardTarget.GetName());
						SendTargetDataToMissile(ml);
						ml.FireMissile();
						UpdateList();
					}
				}
				else if(ml.targetingMode == MissileLauncher.TargetingModes.Heat)
				{
					float attemptStartTime = Time.time;
					while(ml && Time.time-attemptStartTime < targetScanInterval*0.75f && (!heatTarget.exists || (heatTarget.predictedPosition - guardTarget.transform.position).sqrMagnitude > Mathf.Pow(40, 2)))
					{
						yield return null;
					}

					//try uncaged IR lock with radar
					if(guardTarget && !heatTarget.exists && radar && radar.radarEnabled)
					{
						if(!radar.locked || (radar.lockedTarget.predictedPosition - guardTarget.transform.position).sqrMagnitude > Mathf.Pow(40, 2))
						{
							radar.TryLockTarget(guardTarget.transform.position);
							yield return new WaitForSeconds(Mathf.Clamp(1, 0.1f, (targetScanInterval*0.25f) / 2));
						}
					}

					if(guardTarget && ml && heatTarget.exists)
					{
						Debug.Log("Firing on target: " + guardTarget.GetName());
						SendTargetDataToMissile(ml);
						ml.FireMissile();
						UpdateList();
					}
				}

				guardFiringMissile = false;
			}
		}

		bool SmartPickWeapon(TargetInfo target, float turretRange) //change to a target info object
		{
			if(!target)
			{
				return false;
			}

			if(vessel.Landed)
			{
				foreach(var pilot in vessel.FindPartModulesImplementing<BDModulePilotAI>())
				{
					if(pilot)
					{
						return false;
					}
				}
			}

			float distance = Vector3.Distance(transform.position+vessel.srf_velocity, target.position+target.velocity); //take velocity into account (test)
			//distance can be sqr
			if(distance < turretRange || (target.isMissile && distance < turretRange*1.5f))
			{
				if((target.isMissile) && SwitchToLaser()) //need to favor ballistic for ground units
				{
					return true;
				}

				if(!targetMissiles && !vessel.Landed && target.isMissile)
				{
					return false;
				}

				if(SwitchToTurret(distance))
				{
					//dont fire on missiles if airborne unless equipped with laser
					return true;
				}

			}

			if(distance > turretRange || !vessel.Landed)
			{
				//missiles
				if(!target.isLanded)
				{
					if(target.isMissile && !vessel.Landed) //don't fire on missiles if airborne
					{
						return false;
					}

					return SwitchToAirMissile();
				}
				else
				{
					return SwitchToGroundMissile();
				}
			}

			return false;

		}

		public bool CanSeeTarget(Vessel target)
		{
			if(RadarUtils.TerrainCheck(target.transform.position, transform.position))
			{
				return false;
			}
			else
			{
				return true;
			}
		}
		
		void ScanAllTargets()
		{
			//get a target.
			//float angle = 0;

			foreach(Vessel v in FlightGlobals.Vessels)
			{
				if(v.loaded)
				{
					float sqrDistance = (transform.position-v.transform.position).sqrMagnitude;
					if(sqrDistance < guardRange*guardRange && CanSeeTarget(v))
					{
						float angle = Vector3.Angle (-transform.forward, v.transform.position-transform.position);
						if(angle < guardAngle/2)
						{
							foreach(var missile in v.FindPartModulesImplementing<MissileLauncher>())
							{
								if(missile.hasFired && missile.team != team)
								{
									BDATargetManager.ReportVessel(v, this);
									break;
								}
							}

							foreach(var mF in v.FindPartModulesImplementing<MissileFire>())
							{
								if(mF.team != team && mF.vessel.IsControllable && mF.vessel.isCommandable) //added iscommandable check
								{
									BDATargetManager.ReportVessel(v, this);
									break;
								}
							}
						}
					}
				}
			}

		}

		void SmartFindTarget()
		{
			List<TargetInfo> targetsTried = new List<TargetInfo>();

			//if AIRBORNE, try to engage airborne target first
			if(!vessel.Landed && !targetMissiles)
			{
				TargetInfo potentialAirTarget = BDATargetManager.GetAirToAirTarget(this);
				if(potentialAirTarget)
				{
					targetsTried.Add(potentialAirTarget);
					SetTarget(potentialAirTarget);
					if(SmartPickWeapon(potentialAirTarget, 500))
					{
						Debug.Log (vessel.vesselName + " is engaging an airborne target with " + selectedWeapon);
						return;
					}
				}
			}

			TargetInfo potentialTarget = null;
			//=========HIGH PRIORITY MISSILES=============
			//first engage any missiles that are not engaged
			potentialTarget = BDATargetManager.GetUnengagedMissileTarget(this);
			if(potentialTarget)
			{
				targetsTried.Add(potentialTarget);
				SetTarget(potentialTarget);
				if(SmartPickWeapon(potentialTarget, 3000))
				{
					Debug.Log (vessel.vesselName + " is engaging unengaged missile with " + selectedWeapon);
					return;
				}
			}

			//then engage any missiles targeting this vessel
			potentialTarget = BDATargetManager.GetMissileTarget(this);
			if(potentialTarget)
			{
				targetsTried.Add(potentialTarget);
				SetTarget(potentialTarget);
				if(SmartPickWeapon(potentialTarget, 3000))
				{
					Debug.Log (vessel.vesselName + " is engaging incoming missile with " + selectedWeapon);
					return;
				}
			}
			//=========END HIGH PRIORITY MISSILES=============

			//============VESSEL THREATS============
			if(!targetMissiles)
			{
				//then try to engage enemies with least friendlies already engaging them 
				potentialTarget = BDATargetManager.GetLeastEngagedTarget(this);
				if(potentialTarget)
				{
					targetsTried.Add(potentialTarget);
					SetTarget(potentialTarget);
					if(SmartPickWeapon(potentialTarget, 2000))
					{
						Debug.Log (vessel.vesselName + " is engaging the least engaged target with " + selectedWeapon);
						return;
					}
				}
			
				//then engage the closest enemy
				potentialTarget = BDATargetManager.GetClosestTarget(this);
				if(potentialTarget)
				{
					targetsTried.Add(potentialTarget);
					SetTarget(potentialTarget);
					if(SmartPickWeapon(potentialTarget, 2000))
					{
						Debug.Log (vessel.vesselName + " is engaging the closest target with " + selectedWeapon);
						return;
					}
					else
					{
						if(SmartPickWeapon(potentialTarget, 10000))
						{
							Debug.Log (vessel.vesselName + " is engaging the closest target with extended turret range ("+selectedWeapon+")");
							return;
						}
					}

				}
			}
			//============END VESSEL THREATS============


			//============LOW PRIORITY MISSILES=========
			//try to engage least engaged hostile missiles first
			potentialTarget = BDATargetManager.GetMissileTarget(this);
			if(potentialTarget)
			{
				targetsTried.Add(potentialTarget);
				SetTarget(potentialTarget);
				if(SmartPickWeapon(potentialTarget, 2000))
				{
					Debug.Log (vessel.vesselName + " is engaging a missile with " + selectedWeapon);
					return;
				}
			}
			
			//then try to engage closest hostile missile
			potentialTarget = BDATargetManager.GetClosestMissileTarget(this);
			if(potentialTarget)
			{
				targetsTried.Add(potentialTarget);
				SetTarget(potentialTarget);
				if(SmartPickWeapon(potentialTarget, 2000))
				{
					Debug.Log (vessel.vesselName + " is engaging a missile with " + selectedWeapon);
					return;
				}
			}
			//==========END LOW PRIORITY MISSILES=============

			if(targetMissiles)//NO MISSILES BEYOND THIS POINT//
			{
				Debug.Log (vessel.vesselName + " is disengaging - no valid weapons");
				CycleWeapon(0);
				SetTarget(null);
				return;
			}

			//if nothing works, get all remaining targets and try weapons against them
			List<TargetInfo> finalTargets = BDATargetManager.GetAllTargetsExcluding(targetsTried, this);
			foreach(TargetInfo finalTarget in finalTargets)
			{
				SetTarget(finalTarget);
				if(SmartPickWeapon(finalTarget, 10000))
				{
					Debug.Log (vessel.vesselName + " is engaging a final target with " + selectedWeapon);
					return;
				}
			}


			//no valid targets found
			if(potentialTarget == null || selectedWeapon == null)
			{
				Debug.Log (vessel.vesselName + " is disengaging - no valid weapons");
				CycleWeapon(0);
				SetTarget(null);
				return;
			}

			Debug.Log ("Unhandled target case.");
		}

		void SetTarget(TargetInfo target)
		{
			if(target)
			{
				if(currentTarget)
				{
					currentTarget.Disengage(this);
				}
				target.Engage(this);
				currentTarget = target;
				guardTarget = target.Vessel;
			}
			else
			{
				if(currentTarget)
				{
					currentTarget.Disengage(this);
				}
				guardTarget = null;
				currentTarget = null;
			}
		}
		

		/*
		bool SwitchToTurret()
		{
			int turretStatus = CheckTurret(0);
			if(turretStatus != 1)
			{
				CycleWeapon(0); //go to start of array
				//Debug.Log("Starting switch to turret cycle with weapon:"+selectedWeapon);
				while(true)
				{
					CycleWeapon(true);
					//Debug.Log ("Trying "+selectedWeapon);
					if(selectedWeapon == "None")
					{
						//Debug.Log("No valid turret found");
						return false;
					}
					if(CheckTurret(0) == 1)
					{
						//Debug.Log ("Found a valid turret!");
						return true;
					}
				}
				//Debug.Log ("Completed switch to turret cycle");
			}
			else return true;
		}
*/
		bool SwitchToTurret(float distance)
		{
			int turretStatus = CheckTurret(distance);
			if(turretStatus != 1)
			{
				CycleWeapon(0); //go to start of array
				while(true)
				{
					CycleWeapon(true);
					if(selectedWeapon == null)
					{
						return false;
					}
					if(CheckTurret(distance) == 1 && selectedWeapon.GetWeaponClass() != WeaponClasses.DefenseLaser)
					{
						return true;
					}
				}
			}
			else return true;
		}
		
		void SwitchToMissile()
		{
			if(selectedWeapon.GetWeaponClass() != WeaponClasses.Missile)
			{
				CycleWeapon(0); //go to start of array
				while(true)
				{
					CycleWeapon(true);
					if(selectedWeapon == null) return;
					if(selectedWeapon.GetWeaponClass() == WeaponClasses.Missile) return;
				}
			}
			else return;
		}

		bool SwitchToAirMissile()
		{
			CycleWeapon(0); //go to start of array
			while(true)
			{
				CycleWeapon(true);
				if(selectedWeapon == null) return false;
				if(selectedWeapon.GetWeaponClass() == WeaponClasses.Missile)
				{
					foreach(var ml in selectedWeapon.GetPart().FindModulesImplementing<MissileLauncher>())
					{
						if(ml.guidanceMode == MissileLauncher.GuidanceModes.AAMLead || ml.guidanceMode == MissileLauncher.GuidanceModes.AAMPure)
						{
							return true;
						}
						else
						{
							break;
						}
					}
					//return;
				}
			}
		}

		bool SwitchToGroundMissile()
		{
			CycleWeapon(0); //go to start of array
			while(true)
			{
				CycleWeapon(true);
				if(selectedWeapon == null) return false;
				if(selectedWeapon.GetWeaponClass() == WeaponClasses.Missile)
				{
					foreach(var ml in selectedWeapon.GetPart().FindModulesImplementing<MissileLauncher>())
					{
						if(ml.guidanceMode == MissileLauncher.GuidanceModes.AGM 
						   || ml.guidanceMode == MissileLauncher.GuidanceModes.STS
						   || ml.guidanceMode == MissileLauncher.GuidanceModes.Cruise)
						{
							return true;
						}
						else
						{
							break;
						}
					}
					//return;
				}
			}
		}

		bool SwitchToLaser()
		{
			if(selectedWeapon == null || selectedWeapon.GetWeaponClass() != WeaponClasses.DefenseLaser)
			{
				CycleWeapon(0); //go to start of array
				while(true)
				{
					CycleWeapon(true);
					if(selectedWeapon == null) return false;
					if(selectedWeapon.GetWeaponClass() == WeaponClasses.DefenseLaser) return true;
				}
			}
			else return true;
		}

	

		int CheckTurret(float distance)
		{
			if(selectedWeapon == null || !(selectedWeapon.GetWeaponClass() == WeaponClasses.Gun || selectedWeapon.GetWeaponClass() == WeaponClasses.DefenseLaser))
			{
				return 2;
			}
			Debug.Log ("Checking turrets");
			float finalDistance = vessel.Landed ? distance : distance/2; //decrease distance requirement if airborne
			foreach(var weapon in vessel.FindPartModulesImplementing<ModuleWeapon>())
			{
				if(weapon.part.partInfo.title == selectedWeapon.GetPart().partInfo.title)
				{
					float gimbalTolerance = vessel.Landed ? 0 : 15;
					if(((!vessel.Landed && pilotAI) || (TargetInTurretRange(weapon, gimbalTolerance))) && weapon.maxEffectiveDistance >= finalDistance)
					{
						if(CheckAmmo(weapon))
						{
							Debug.Log (selectedWeapon + " is valid!");
							return 1;
						}
						else 
						{
							Debug.Log (selectedWeapon + " has no ammo.");
							return -1;
						}
					}
					else
					{
						Debug.Log (selectedWeapon + " can not reach target ("+distance+" vs "+weapon.maxEffectiveDistance+", yawRange: "+weapon.yawRange+"). Continuing.");
					}
					//else return 0;
				}
			}
			return 2;
		}

		bool TargetInTurretRange(ModuleWeapon weapon, float tolerance)
		{
			if(!guardTarget)
			{
				Debug.Log ("Checking turret range but no guard target");
				return false;
			}
			if(weapon.yawRange == 360)
			{
				Debug.Log ("Checking turret range - turret has full swivel");
				return true;
			}

			Transform turretTransform = weapon.turretBaseTransform;
			Vector3 direction = guardTarget.transform.position-turretTransform.position;
			Vector3 directionYaw = Vector3.ProjectOnPlane(direction, turretTransform.up);
			Vector3 directionPitch = Vector3.ProjectOnPlane(direction, turretTransform.right);

			float angleYaw = Vector3.Angle(turretTransform.forward, directionYaw);
			//float anglePitch = Vector3.Angle(-turret.transform.forward, directionPitch);
			float signedAnglePitch = Misc.SignedAngle(turretTransform.forward, directionPitch, turretTransform.up);
			if(Mathf.Abs(signedAnglePitch) > 90)
			{
				signedAnglePitch -= Mathf.Sign(signedAnglePitch)*180;
			}
			bool withinPitchRange = (signedAnglePitch > weapon.minPitch && signedAnglePitch < weapon.maxPitch);

			if(angleYaw < (weapon.yawRange/2)+tolerance && withinPitchRange)
			{
				Debug.Log ("Checking turret range - target is INSIDE gimbal limits! signedAnglePitch: "+signedAnglePitch+", minPitch: "+weapon.minPitch+", maxPitch: "+weapon.maxPitch);
				return true;
			}
			else
			{
				Debug.Log ("Checking turret range - target is OUTSIDE gimbal limits! signedAnglePitch: "+signedAnglePitch+", minPitch: "+weapon.minPitch+", maxPitch: "+weapon.maxPitch+", angleYaw: "+angleYaw);
				return false;
			}
		}
		
		bool CheckAmmo(ModuleWeapon weapon)
		{
			string ammoName = weapon.ammoName;
			
			foreach(Part p in vessel.parts)
			{
				foreach(var resource in p.Resources.list)	
				{
					if(resource.resourceName == ammoName)
					{
						if(resource.amount > 0)
						{
							return true;	
						}
					}
				}
			}
			
			return false;
		}
		
		void ToggleTurret()
		{
			foreach(var weapon in vessel.FindPartModulesImplementing<ModuleWeapon>())
			{
				if(selectedWeapon == null || weapon.part.partInfo.title != selectedWeapon.GetPart().partInfo.title)
				{
					weapon.DisableWeapon();	
				}
				else
				{
					weapon.EnableWeapon();
				}
			}
		}
		
		public void MissileWarning(float distance, MissileLauncher ml)//take distance parameter
		{
			if(vessel.isActiveVessel && !warningSounding)
			{
				StartCoroutine(WarningSoundRoutine(distance, ml));
			}
			
			missileIsIncoming = true;
			incomingMissileDistance = distance;
		}

		bool warningSounding = false;
		IEnumerator WarningSoundRoutine(float distance, MissileLauncher ml)//give distance parameter
		{
			if(distance < 4000)
			{
				warningSounding = true;
				BDArmorySettings.Instance.missileWarningTime = Time.time;
				BDArmorySettings.Instance.missileWarning = true;
				warningAudioSource.pitch = distance < 800 ? 1.45f : 1f;
				warningAudioSource.PlayOneShot(warningSound);

				float waitTime = distance < 800 ? .25f : 1.5f;

				yield return new WaitForSeconds(waitTime);

				if(ml.vessel && CanSeeTarget(ml.vessel))
				{
					BDATargetManager.ReportVessel(ml.vessel, this);
				}
			}
			warningSounding = false;
		}
		
		public void UpdateMaxGuardRange()
		{
			var rangeEditor = (UI_FloatRange) Fields["guardRange"].uiControlEditor;
			
			if(rangeEditor.maxValue != BDArmorySettings.PHYSICS_RANGE)
			{
				var rangeFlight = (UI_FloatRange) Fields["guardRange"].uiControlFlight;
				if(BDArmorySettings.PHYSICS_RANGE!=0)
				{
					rangeEditor.maxValue = BDArmorySettings.PHYSICS_RANGE;
					rangeFlight.maxValue = BDArmorySettings.PHYSICS_RANGE;
				}
				else
				{
					rangeEditor.maxValue = 2500;
					rangeFlight.maxValue = 2500;
				}
			}
		}
		
		bool CheckMouseIsOnGui()
		{
			return Misc.CheckMouseIsOnGui();	
			
		}


		bool CheckBombClearance(MissileLauncher ml)
		{
			if(!BDArmorySettings.BOMB_CLEARANCE_CHECK) return true;

			if(ml.part.ShieldedFromAirstream)
			{
				return false;
			}

			if(ml.dropTime > 0.3f)
			{
				//debug lines
				LineRenderer lr = null;
				if(BDArmorySettings.DRAW_DEBUG_LINES && BDArmorySettings.DRAW_AIMERS)
				{
					lr = GetComponent<LineRenderer>();
					if(!lr)
					{
						lr = gameObject.AddComponent<LineRenderer>();
					}
					lr.enabled = true;
					lr.SetWidth(.1f, .1f);
				}
				else
				{
					if(gameObject.GetComponent<LineRenderer>())
					{
						gameObject.GetComponent<LineRenderer>().enabled = false;	
					}
				}


				float radius = 0.28f / 2;
				float time = ml.dropTime;
				Vector3 direction = ((ml.decoupleForward ? ml.transform.forward : -ml.transform.up) * ml.decoupleSpeed * time) + ((FlightGlobals.getGeeForceAtPosition(transform.position) - vessel.acceleration) * 0.5f * Mathf.Pow(time, 2));
				Vector3 crossAxis = Vector3.Cross(direction, ml.transform.forward).normalized;

				float rayDistance;
				if(ml.thrust == 0 || ml.cruiseThrust == 0)
				{
					rayDistance = 8;
				}
				else
				{
					//distance till engine starts based on grav accel and vessel accel
					rayDistance = direction.magnitude;
				}

				Ray[] rays = new Ray[] {
					new Ray(ml.transform.position - (radius * crossAxis), direction),
					new Ray(ml.transform.position + (radius * crossAxis), direction),
					new Ray(ml.transform.position, direction)
				};

				if(lr)
				{
					lr.useWorldSpace = false;
					lr.SetVertexCount(4);
					lr.SetPosition(0, transform.InverseTransformPoint(rays[0].origin));
					lr.SetPosition(1, transform.InverseTransformPoint(rays[0].GetPoint(rayDistance)));
					lr.SetPosition(2, transform.InverseTransformPoint(rays[1].GetPoint(rayDistance)));
					lr.SetPosition(3, transform.InverseTransformPoint(rays[1].origin));
				}

				for(int i = 0; i < rays.Length; i++)
				{
					RaycastHit[] hits = Physics.RaycastAll(rays[i], rayDistance, 557057);
					for(int h = 0; h < hits.Length; h++)
					{
						Part p = hits[h].collider.GetComponentInParent<Part>();

						if((p != null && p != ml.part) || p == null) return false;
					}
				}

				return true;
			}
			else
			{
				//forward check for no-drop missiles
				if(Physics.Raycast(new Ray(ml.transform.position, ml.transform.forward), 50, 557057))
				{
					return false;
				}
			}

			return true;
		}

		bool isFlaring = false;
		int flareCounter = 0;
		int flareAmount = 5;
		IEnumerator FlareRoutine()
		{
			isFlaring = true;
			yield return new WaitForSeconds(UnityEngine.Random.Range(.2f, 1f));
			if(incomingMissileDistance < 2500)
			{
				flareAmount = Mathf.RoundToInt((2500-incomingMissileDistance)/400);
				foreach(var flare in vessel.FindPartModulesImplementing<CMDropper>())
				{
					flare.DropCM();
				}
				flareCounter++;
				if(flareCounter < flareAmount)
				{
					yield return new WaitForSeconds(0.15f);
				}
				else
				{	
					flareCounter = 0;
					yield return new WaitForSeconds(UnityEngine.Random.Range(.5f, 1f));
				}
			}
			isFlaring = false;
		}
		
		string GetWeaponName(IBDWeapon weapon)
		{
			if(weapon == null)
			{
				return "None";
			}
			else
			{
				return weapon.GetShortName();
			}
		}

		//ANTIRADIATION
		bool antiRadTargetAcquired = false;
		Vector3 antiRadiationTarget;
		void SearchForRadarSource()
		{
			antiRadTargetAcquired = false;
			if(rwr && rwr.rwrEnabled)
			{
				float closestAngle = 360;
				MissileLauncher missile = currentMissile;
				if(currentMissile.targetingMode != MissileLauncher.TargetingModes.AntiRad) return;
				for(int i = 0; i < rwr.pingsData.Length; i++)
				{
					if(rwr.pingsData[i].exists)
					{
						float angle = Vector3.Angle(rwr.pingWorldPositions[i] - missile.transform.position, missile.transform.forward);
						if(angle < closestAngle && angle < missile.maxOffBoresight)
						{
							closestAngle = angle;
							antiRadiationTarget = rwr.pingWorldPositions[i];
							antiRadTargetAcquired = true;
						}
					}
				}
			}
		}

		//LASER LOCKING
		bool laserPointDetected = false;
		ModuleTargetingCamera foundCam;
		void SearchForLaserPoint()
		{
			MissileLauncher ml = currentMissile;
			if(ml.targetingMode != MissileLauncher.TargetingModes.Laser)
			{
				return;
			}

			foundCam = BDATargetManager.GetLaserTarget(ml);
			if(foundCam)
			{
				laserPointDetected = true;
			}
			else
			{
				laserPointDetected = false;
			}
		}

		//HEAT LOCKING
		TargetSignatureData heatTarget = TargetSignatureData.noTarget;
		void SearchForHeatTarget()
		{
			MissileLauncher ml = currentMissile;
			if(!ml) return;

			if(ml.targetingMode != MissileLauncher.TargetingModes.Heat)
			{
				return;
			}

			float scanRadius = ml.lockedSensorFOV*2;
			float maxOffBoresight = ml.maxOffBoresight*0.85f;

			bool radarSlaved = false;
			if(radar && radar.radarEnabled && radar.locked)
			{
				heatTarget = radar.lockedTarget;
				radarSlaved = true;
			}

			Vector3 direction = 
				heatTarget.exists && Vector3.Angle(heatTarget.position-ml.transform.position, ml.transform.forward) < maxOffBoresight ? 
					heatTarget.position-ml.transform.position 
					: ml.transform.forward;

			float heatThresh = radarSlaved ? ml.heatThreshold*0.75f : ml.heatThreshold;

			heatTarget = BDATargetManager.GetHeatTarget(new Ray(ml.transform.position, direction), scanRadius, ml.heatThreshold, ml.allAspect);
		}
		
		
	}
}

