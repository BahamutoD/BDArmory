using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class MissileFire : PartModule
	{
		//weapons
		private List<string> weaponTypes = new List<string>();
		public string[] weaponArray;
		
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
		AudioSource warningAudioSource;
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

		//current weapon ref
		public MissileLauncher currentMissile
		{
			get
			{
				if(selectedWeapon.Contains("Missile"))
				{
					foreach(var ml in vessel.FindPartModulesImplementing<MissileLauncher>())
					{
						if(ml!=null)
						{
							return ml;
						}
						else
						{
							return null;
						}
					}
					return null;
				}
				else
				{
					return null;
				}
			}
		}
		//BahaTurret currentTurret = null;
		
		//KSP fields and events
		#region kspFields,events,actions
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Scan Interval"),
        	UI_FloatRange(minValue = 1f, maxValue = 60f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float targetScanInterval = 8;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Field of View"),
        	UI_FloatRange(minValue = 10f, maxValue = 360f, stepIncrement = 10f, scene = UI_Scene.All)]
		public float guardAngle = 100f;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Guard Range"),
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
		public string selectedWeapon = "";
		
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
				
				cameraTransform = part.FindModelTransform("BDARPMCameraTransform");
				
				part.force_activate();
				rippleTimer = Time.time;
				targetListTimer = Time.time;
				
				Fields["guardRange"].guiActive = guardMode;
				Fields["guardAngle"].guiActive = guardMode;
				Fields["targetMissiles"].guiActive = guardMode;
				Fields["targetScanInterval"].guiActive = guardMode;
			}
			
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.minDistance = 500;
			audioSource.maxDistance = 1000;
			audioSource.dopplerLevel = 0;
			audioSource.volume = Mathf.Sqrt(GameSettings.UI_VOLUME);

			warningAudioSource = gameObject.AddComponent<AudioSource>();
			warningAudioSource.minDistance = 500;
			warningAudioSource.maxDistance = 1000;
			warningAudioSource.dopplerLevel = 0;
			warningAudioSource.volume = Mathf.Sqrt(GameSettings.UI_VOLUME);

			StartCoroutine (MissileWarningResetRoutine());
			
		}
		
		public override void OnUpdate ()
		{	
			
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(wasGuardMode != guardMode)
				{
					if(!guardMode)
					{

						//disable turret firing and guard mode
						foreach(var turret in vessel.FindPartModulesImplementing<BahaTurret>())
						{
							if(turret.part.partInfo.title == selectedWeapon)	
							{
								turret.autoFireTarget = null;
								turret.autoFire = false;
								turret.guardMode = false;
							}
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
						selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
						selectionMessage.message = selectionText;
						ScreenMessages.PostScreenMessage(selectionMessage, true);
					}
				}
				if(weaponArray.Length > 0) selectedWeapon = weaponArray[weaponIndex];
			}
			
			
			//finding next rocket to shoot (for aimer)
			FindNextRocket();
			
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
				if((selectedWeapon.Contains("Rocket") || selectedWeapon.Contains("Missile") || selectedWeapon.Contains("Bomb")))
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
				
				//========
				/*
				//ripple firing rockets==
				else if(selectedWeapon.Contains("Rocket") && !MapView.MapIsEnabled && triggerTimer > triggerHoldTime && !hasSingleFired)
				{
					if(Time.time - rippleTimer > 60/rippleRPM)
					{
						FireMissile();
						rippleTimer = Time.time;
					}
				}
				//==
				*/


				TargetAcquire();
				
				BombAimer();
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
			
			
			foreach(MissileLauncher ml in vessel.FindPartModulesImplementing<MissileLauncher>())
			{
				string weaponName = ml.part.partInfo.title;
				if(!weaponTypes.Contains(weaponName))
				{
					weaponTypes.Add(weaponName);	
				}
			}
			
			foreach(RocketLauncher rl in vessel.FindPartModulesImplementing<RocketLauncher>())
			{
				bool hasRocket = false;
				foreach(PartResource r in rl.part.Resources.list)
				{
					if(r.amount>0) hasRocket = true;
				}	
				string weaponName = rl.part.partInfo.title;
				if(!weaponTypes.Contains(weaponName) && hasRocket)
				{
					weaponTypes.Add(weaponName);	
				}
			}
			
			foreach(BahaTurret turret in vessel.FindPartModulesImplementing<BahaTurret>())
			{
				string weaponName = turret.part.partInfo.title;
				if(!weaponTypes.Contains(weaponName))
				{
					weaponTypes.Add(weaponName);	
				}
			}
			
			weaponTypes.Sort();
			
			List<string> tempList = new List<string>();
			tempList.Add ("None");
			tempList.AddRange(weaponTypes);
			
			weaponArray = tempList.ToArray();
			
			if(weaponIndex >= weaponArray.Length)
			{
				hasSingleFired = true;
				triggerTimer = 0;
				
				weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
			}
			
			if(selectedWeapon != weaponArray[weaponIndex] && vessel.isActiveVessel && Time.time-startTime > 1)
			{
				hasSingleFired = true;
				if(BDArmorySettings.GAME_UI_ENABLED && vessel == FlightGlobals.ActiveVessel)
				{
					ScreenMessages.RemoveMessage(selectionMessage);
					selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
					selectionMessage.message = selectionText;
					ScreenMessages.PostScreenMessage(selectionMessage, true);
				}
				
				selectedWeapon = weaponArray[weaponIndex];
			}
						
			if(weaponTypes.Count == 0) selectedWeapon = "None";
			
			ToggleTurret();
		}
		
		public void CycleWeapon(bool forward)
		{
			UpdateList();
			if(forward) weaponIndex++;
			else weaponIndex--;
			if(weaponIndex >= weaponArray.Length) weaponIndex = 0; //wrap
			if(weaponIndex < 0) weaponIndex = weaponArray.Length-1;
			if(weaponArray.Length > 0) selectedWeapon = weaponArray[weaponIndex];
			
			hasSingleFired = true;
			triggerTimer = 0;
			
			if(BDArmorySettings.GAME_UI_ENABLED && vessel == FlightGlobals.ActiveVessel)
			{
				ScreenMessages.RemoveMessage(selectionMessage);
				selectionText = "Selected Weapon: "+selectedWeapon;
				selectionMessage.message = selectionText;
				ScreenMessages.PostScreenMessage(selectionMessage, true);
			}
			
			
			//bomb stuff
			if(selectedWeapon.Contains("Bomb"))
			{
				foreach(Part p in vessel.Parts)
				{
					if(p.partInfo.title == selectedWeapon)
					{
						bombPart = p;
					}
				}
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
			weaponIndex = index;
			selectedWeapon = weaponArray[index];
			
			hasSingleFired = true;
			triggerTimer = 0;
			
			//bomb stuff
			if(selectedWeapon.Contains("Bomb"))
			{
				foreach(Part p in vessel.Parts)
				{
					if(p.partInfo.title == selectedWeapon)
					{
						bombPart = p;
					}
				}
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

			if(lastFiredSym != null && lastFiredSym.partInfo.title == selectedWeapon)
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

				bool firedMissile = false;

				foreach(MissileLauncher ml in lastFiredSym.FindModulesImplementing<MissileLauncher>())
				{
					if(ml.dropTime > 0.25f && !CheckBombClearance(ml))
					{
						lastFiredSym = null;
						break;
					}

					if(guardMode && guardTarget!=null)
					{
						ml.FireMissileOnTarget(guardTarget);
					}
					else
					{
						ml.FireMissile();
					}

					hasFired = true;
					firedMissile = true;
					
					lastFiredSym = nextPart;
					
					break;

				}	

				if(!firedMissile)
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
					if(ml.part.partInfo.title == selectedWeapon)
					{
						if(ml.dropTime > 0 && !CheckBombClearance(ml))
						{
							continue;
						}

						lastFiredSym = FindSym(ml.part);
						
						if(guardMode && guardTarget!=null)
						{
							ml.FireMissileOnTarget(guardTarget);
						}
						else
						{
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
						
						if(rl.part.partInfo.title == selectedWeapon && hasRocket)
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
					selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
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
				return pSym;
			}
			
			return null;
		}
		
		
		public void TargetAcquire() 
		{
			if(isArmed)
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
					if(v!= null && v != vessel && v.loaded && viewAngle < smallestAngle)
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
			
			if(bombPart == null)
			{
				foreach(Part p in vessel.Parts)
				{
					if(p.partInfo.title == selectedWeapon)
					{
						bombPart = p;
					}
				}
			}
			else if(bombPart.partInfo.title != selectedWeapon)
			{
				bombPart = null;
				foreach(Part p in vessel.Parts)
				{
					if(p.partInfo.title == selectedWeapon)
					{
						bombPart = p;
					}
				}	
			}
			
			showBombAimer = 
			(
				vessel == FlightGlobals.ActiveVessel && 
				selectedWeapon.Contains("Bomb") && 
				bombPart!=null && 
				BDArmorySettings.DRAW_AIMERS && 
				vessel.verticalSpeed < 50 && 
				AltitudeTrigger()
			);
			
			if(showBombAimer)
			{
				
				float simDeltaTime = 0.3f;
				float simTime = 0;
				Vector3 dragForce = Vector3.zero;
				Vector3 prevPos = transform.position;
				Vector3 currPos = transform.position;
				Vector3 simVelocity = vessel.rigidbody.velocity;
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
					dragForce = (0.008f * bombPart.rb.mass) * drag * 0.5f * simSpeedSquared * atmDensity * simVelocity.normalized;
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
			if(vessel == FlightGlobals.ActiveVessel && BDArmorySettings.GAME_UI_ENABLED)
			{
				if(showBombAimer && !MapView.MapIsEnabled)
				{
					float size = 50;
					
					Vector3 aimPosition = FlightCamera.fetch.mainCamera.WorldToViewportPoint(bombAimerPosition);
					Rect drawRect = new Rect(aimPosition.x*Screen.width-(0.5f*size), (1-aimPosition.y)*Screen.height-(0.5f*size), size, size);
					float cameraAngle = Vector3.Angle(FlightCamera.fetch.GetCameraTransform().forward, bombAimerPosition-FlightCamera.fetch.mainCamera.transform.position);
					if(cameraAngle<90) GUI.DrawTexture(drawRect, bombAimerTexture);
				}
			}
			
			
			
		}
		
		void FindNextRocket()
		{
			if(selectedWeapon.Contains("Rocket"))
			{
				if(lastFiredSym!=null && lastFiredSym.partInfo.title == selectedWeapon)	
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
						
						if(rl.part.partInfo.title == selectedWeapon && hasRocket)
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
			foreach(var turret in vessel.FindPartModulesImplementing<BahaTurret>()) //make this not have to go every frame
			{
				if(turret.part.partInfo.title == selectedWeapon)	
				{
					turret.turretEnabled = true;
					turret.guardMode = true;
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
			else
			{
				foreach(var turret in vessel.FindPartModulesImplementing<BahaTurret>())
				{
					if(turret.part.partInfo.title == selectedWeapon)	
					{
						turret.autoFire = false;
						turret.autoFireTarget = null;
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

				if(guardTarget!=null)
				{
					//firing
					if(selectedWeapon.Contains("Missile"))
					{
						bool launchAuthorized = true;


						float targetAngle = Vector3.Angle(-transform.forward, guardTarget.transform.position-transform.position);
						if(targetAngle > guardAngle/2 || (pilotAI && !pilotAI.GetLaunchAuthorizion(guardTarget, this))) //dont fire yet if target out of guard angle or not AIPilot authorized
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
							Debug.Log ("Firing on target: "+guardTarget.GetName());
							FireMissile();
						}
						else
						{
							targetScanTimer -= 0.85f * targetScanInterval;
						}
					}
					else
					{
						foreach(var turret in vessel.FindPartModulesImplementing<BahaTurret>())
						{
							if(turret.part.partInfo.title == selectedWeapon)	
							{
								turret.autoFireTarget = guardTarget;
								turret.autoFireTimer = Time.time;
								turret.autoFireLength = targetScanInterval/2;
							}
						}
					}
				}
				else
				{
					foreach(var turret in vessel.FindPartModulesImplementing<BahaTurret>())
					{
						if(turret.part.partInfo.title == selectedWeapon)	
						{
							turret.autoFireTarget = null;
							turret.autoFire = false;
						}
					}	
				}
			}
		}

		bool SmartPickWeapon(TargetInfo target, float turretRange) //change to a target info object
		{
			if(!target)
			{
				return false;
			}
			float distance = Vector3.Distance(transform.position+vessel.srf_velocity, target.position+target.velocity); //take velocity into account (test)
			if((vessel.Landed && (distance < turretRange || (target.isMissile && distance < turretRange*1.65f))) || (!vessel.Landed && distance < 400))
			{
				if(SwitchToLaser())
				{
					return true;
				}
				else if(SwitchToTurret(distance))
				{
					if(target.isMissile && !vessel.Landed) //dont fire on missiles if airborne unless equipped with laser
					{
						return false;
					}

					return true;
				}
				else
				{
					return false;
				}
			}
			else //missiles
			{
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
		}

		public bool CanSeeTarget(Vessel target)
		{
			if(!target)
			{
				return false;
			}

			//check sight line to target
			Vector3 direction = (transform.position-target.transform.position).normalized;
			float distance = guardRange;
			//float distanceToTarget = Vector3.Distance(guardTarget.transform.position, transform.position);
			RaycastHit sightHit;
			if(Physics.Raycast(target.transform.position+(direction*20), direction, out sightHit, distance, 557057))
			{
				Vessel hitVessel = null;
				try
				{
					hitVessel = Part.FromGO(sightHit.rigidbody.gameObject).vessel;	
				}
				catch(NullReferenceException){}
				if(hitVessel!=null && hitVessel == vessel)
				{
					return true;	
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
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
					if(sqrDistance < guardRange*guardRange)
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
					if(SmartPickWeapon(potentialAirTarget, 800))
					{
						Debug.Log (vessel.vesselName + " is engaging an airborne target with " + selectedWeapon);
						return;
					}
				}
			}

			//=========MISSILES=============
			//try to engage least engaged hostile missiles first
			TargetInfo potentialTarget = BDATargetManager.GetMissileTarget(this);
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
				else if(potentialTarget.missileModule && potentialTarget.missileModule.targetMf && potentialTarget.missileModule.targetMf == this)
				{
					if(SwitchToTurret(0))
					{
						Debug.Log (vessel.vesselName + " is engaging incoming missile with " + selectedWeapon);
						return;
					}
				}
			}
			if(targetMissiles && potentialTarget == null) //break if target missiles only.
			{
				SetTarget(null);
				return;
			}
			//=========END MISSILES=============

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
			if(potentialTarget == null || selectedWeapon == "None")
			{
				Debug.Log (vessel.vesselName + " is disengaging - no valid weapons");
				CycleWeapon(0);
				SetTarget(null);
			}
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
		
		void SwitchToGuardWeapon()
		{
			Debug.Log ("Switching to any valid weapon");
			if(!CheckWeaponForGuard())
			{
				CycleWeapon(0);
				while(true)
				{
					CycleWeapon(true);
					if(selectedWeapon == "None") return;
					if(CheckWeaponForGuard()) return;
				}
			}
			else return;
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
					if(selectedWeapon == "None")
					{
						return false;
					}
					if(CheckTurret(distance) == 1)
					{
						return true;
					}
				}
			}
			else return true;
		}
		
		void SwitchToMissile()
		{
			if(!selectedWeapon.Contains("Missile"))
			{
				CycleWeapon(0); //go to start of array
				while(true)
				{
					CycleWeapon(true);
					if(selectedWeapon == "None") return;
					if(selectedWeapon.Contains("Missile")) return;
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
				if(selectedWeapon == "None") return false;
				if(selectedWeapon.Contains("Missile"))
				{
					foreach(var ml in vessel.FindPartModulesImplementing<MissileLauncher>())
					{
						if(ml.part.partInfo.title == selectedWeapon)
						{
							if(ml.homingType == "AAM")
							{
								return true;
							}
							else
							{
								break;
							}
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
				if(selectedWeapon == "None") return false;
				if(selectedWeapon.Contains("Missile"))
				{
					foreach(var ml in vessel.FindPartModulesImplementing<MissileLauncher>())
					{
						if(ml.part.partInfo.title == selectedWeapon)
						{
							if(ml.homingType == "AGM" || ml.homingType == "Cruise")
							{
								return true;
							}
							else
							{
								break;
							}
						}
					}
					//return;
				}
			}
		}

		bool SwitchToLaser()
		{
			if(!selectedWeapon.Contains("Laser"))
			{
				CycleWeapon(0); //go to start of array
				while(true)
				{
					CycleWeapon(true);
					if(selectedWeapon == "None") return false;
					if(selectedWeapon.Contains("Laser")) return true;
				}
			}
			else return true;
		}

		bool CheckWeaponForGuard()
		{
			int turretStatus = CheckTurret(0);
			
			return (selectedWeapon != "None" && !selectedWeapon.Contains("Bomb") && !selectedWeapon.Contains("Rocket") && turretStatus > 0);	
		}

		int CheckTurret(float distance)
		{
			if(selectedWeapon == "None" || selectedWeapon.Contains("Missile"))
			{
				return 2;
			}
			Debug.Log ("Checking turrets");
			foreach(var turret in vessel.FindPartModulesImplementing<BahaTurret>())
			{
				if(turret.part.partInfo.title == selectedWeapon)
				{
					if((TargetInTurretRange(turret, 15)) && turret.maxEffectiveDistance >= distance)
					{
						if(CheckAmmo(turret))
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
						Debug.Log (selectedWeapon + " can not reach target ("+distance+" vs "+turret.maxEffectiveDistance+", yawRange: "+turret.yawRange+"). Continuing.");
					}
					//else return 0;
				}
			}
			return 2;
		}

		bool TargetInTurretRange(BahaTurret turret, float tolerance)
		{
			if(!guardTarget)
			{
				Debug.Log ("Checking turret range but no guard target");
				return false;
			}
			if(turret.yawRange == 360)
			{
				Debug.Log ("Checking turret range - turret has full swivel");
				return true;
			}

			Vector3 direction = guardTarget.transform.position-turret.transform.position;
			Vector3 directionYaw = Vector3.ProjectOnPlane(direction, turret.transform.up);
			Vector3 directionPitch = Vector3.ProjectOnPlane(direction, turret.transform.right);

			float angleYaw = Vector3.Angle(-turret.transform.forward, directionYaw);
			float anglePitch = Vector3.Angle(-turret.transform.forward, directionPitch);
			if(angleYaw < (turret.yawRange/2)+tolerance && anglePitch < (turret.maxPitch-turret.minPitch)+tolerance)
			{
				Debug.Log ("Checking turret range - target is within gimbal limits");
				return true;
			}
			else
			{
				Debug.Log ("Checking turret range - target is outside gimbal limits!");
				return false;
			}
		}
		
		bool CheckAmmo(BahaTurret turret)
		{
			string ammoName = turret.ammoName;
			
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
			foreach(var turret in vessel.FindPartModulesImplementing<BahaTurret>())
			{
				if(turret.part.partInfo.title != selectedWeapon)
				{
					if(turret.turretEnabled)
					{
						turret.toggle();
					}	
				}
				else
				{
					if(!turret.turretEnabled)
					{
						turret.toggle();	
					}
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


			float radius = 0.28f/2;
			float time = ml.dropTime;
			Vector3 direction = ((ml.decoupleForward ? ml.transform.forward : -ml.transform.up) * ml.decoupleSpeed * time) + ((FlightGlobals.getGeeForceAtPosition(transform.position)-vessel.acceleration) * 0.5f * Mathf.Pow(time, 2));
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

			Ray[] rays = new Ray[]
			{
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
					Part p;
					try
					{
						p = Part.FromGO(hits[h].transform.gameObject);
					}
					catch(NullReferenceException)
					{
						p = null;
					}
					if((p!=null && p != ml.part) || p == null) return false;
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
		
		
		
		
	}
}

