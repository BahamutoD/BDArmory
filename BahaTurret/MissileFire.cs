using System;
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
		float rippleRPM = 650;
		float triggerTimer = 0;
		
		public float triggerHoldTime = 0.3f;
		
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
		AudioClip clickSound = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/click");
		AudioClip warningSound = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/warning");
		AudioClip armOnSound = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/armOn");
		AudioClip armOffSound = GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/armOff");

		//missile warning
		float warningTimer = 0;
		float warningInterval = 2;
		
		
		//guard mode vars
		float targetScanTimer = 0;
		Vessel guardTarget = null;
		
		
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
			
			RefreshAssociatedWindows();
		}
		
		[KSPAction("Toggle Guard Mode")]
		public void AGToggleGuardMode(KSPActionParam param)
		{
			GuiToggleGuardMode();	
		}
		
		[KSPField(isPersistant = true)]
		public bool guardMode = false;
		
		
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Target Type: "), 
			UI_Toggle(disabledText = "Vessels", enabledText = "Missiles")]
		public bool targetMissiles = false;
		
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
		
		[KSPField(isPersistant = false, guiActive = true, guiName = "Armed: "), 
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
			
		}
		
		public override void OnUpdate ()
		{	
			
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(weaponIndex >= weaponArray.Length)
				{
					hasSingleFired = true;
					triggerTimer = 0;
					
					weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
					
					if(BDArmorySettings.GAME_UI_ENABLED)
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
			GuardMode();
			
			
			
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
				
				
				
				
				
				
				//single firing missiles===
				if((selectedWeapon.Contains("Missile") || selectedWeapon.Contains("Bomb")) && !MapView.MapIsEnabled && triggerTimer > triggerHoldTime && !hasSingleFired)
				{
					FireMissile();
					hasSingleFired = true;
				}
				
				//========
				
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
				
				TargetAcquire();
				
				BombAimer();
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
				if(BDArmorySettings.GAME_UI_ENABLED)
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
			
			if(BDArmorySettings.GAME_UI_ENABLED)
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
			
			audioSource.PlayOneShot(clickSound);
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
			
			audioSource.PlayOneShot(clickSound);
		}
		
		
		public void FireMissile()
		{
			if(lastFiredSym != null && lastFiredSym.partInfo.title == selectedWeapon)
			{
				Part nextPart;
				if(FindSym(lastFiredSym)!=null) nextPart = FindSym(lastFiredSym);
				else nextPart = null;
				
				foreach(MissileLauncher ml in lastFiredSym.FindModulesImplementing<MissileLauncher>())
				{
					
					if(!lastFiredSym.partInfo.title.Contains("Bomb") || CheckBombClearance(ml))
					{
						if(guardTarget!=null)
						{
							ml.FireMissileOnTarget(guardTarget);
						}
						else
						{
							ml.FireMissile();
						}
						
						
						lastFiredSym = nextPart;
						
						UpdateList ();
						if(weaponIndex >= weaponArray.Length)
						{
							hasSingleFired = true;
							triggerTimer = 0;
							
							weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
							
							if(BDArmorySettings.GAME_UI_ENABLED)
							{
								ScreenMessages.RemoveMessage(selectionMessage);
								selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
								selectionMessage.message = selectionText;
								ScreenMessages.PostScreenMessage(selectionMessage, true);
							}
						}
						return;
					}
				}	
				
				foreach(RocketLauncher rl in lastFiredSym.FindModulesImplementing<RocketLauncher>())
				{
					rl.FireRocket();
					rippleRPM = rl.rippleRPM;
					if(nextPart!=null)
					{
						foreach(PartResource r in nextPart.Resources.list)
						{
							if(r.amount>0) lastFiredSym = nextPart;
							else lastFiredSym = null;
						}	
					}
					
					UpdateList ();
					
					if(weaponIndex >= weaponArray.Length)
					{
						hasSingleFired = true;
						triggerTimer = 0;
						
						weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
						
						if(BDArmorySettings.GAME_UI_ENABLED)
						{
							ScreenMessages.RemoveMessage(selectionMessage);
							selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
							selectionMessage.message = selectionText;
							ScreenMessages.PostScreenMessage(selectionMessage, true);
						}
					}
					
					
					return;
				}
				
			}
			else
			{
				foreach(MissileLauncher ml in vessel.FindPartModulesImplementing<MissileLauncher>())
				{
					if(ml.part.partInfo.title == selectedWeapon)
					{
						if(!ml.part.partInfo.title.Contains("Bomb") || CheckBombClearance(ml))
						{
							lastFiredSym = FindSym(ml.part);
							
							if(guardTarget!=null)
							{
								ml.FireMissileOnTarget(guardTarget);
							}
							else
							{
								ml.FireMissile();
							}
							
							
							
							UpdateList ();
							if(weaponIndex >= weaponArray.Length)
							{
								hasSingleFired = true;
								triggerTimer = 0;
								
								weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
								
								if(BDArmorySettings.GAME_UI_ENABLED)
								{
									ScreenMessages.RemoveMessage(selectionMessage);
									selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
									selectionMessage.message = selectionText;
									ScreenMessages.PostScreenMessage(selectionMessage, true);
								}
							}
							return;
						}
					}
				}
				
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
						rippleRPM = rl.rippleRPM;
						
						UpdateList();
						if(weaponIndex >= weaponArray.Length)
						{
							hasSingleFired = true;
							triggerTimer = 0;
							
							weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
							
							if(BDArmorySettings.GAME_UI_ENABLED)
							{
								ScreenMessages.RemoveMessage(selectionMessage);
								selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
								selectionMessage.message = selectionText;
								ScreenMessages.PostScreenMessage(selectionMessage, true);
							}
						}
						return;
					}
				}
				UpdateList();
				if(weaponIndex >= weaponArray.Length)
				{
					hasSingleFired = true;
					triggerTimer = 0;
					
					weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
					
					if(BDArmorySettings.GAME_UI_ENABLED)
					{
						ScreenMessages.RemoveMessage(selectionMessage);
						selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
						selectionMessage.message = selectionText;
						ScreenMessages.PostScreenMessage(selectionMessage, true);
					}
				}
			}
			
			lastFiredSym = null;
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
		
		
		#region targetCam
		void TargetCam()
		{
			if(vessel.targetObject!=null)
			{
				cameraTransform.LookAt(vessel.targetObject.GetTransform(), FlightGlobals.upAxis);	
			}
			else
			{
				cameraTransform.localRotation = Quaternion.identity;
				cameraTransform.LookAt(cameraTransform.position - 5*part.transform.forward, FlightGlobals.upAxis);	
			}
		}
		#endregion
		
		void BombAimer()
		{
			if(bombPart!=null && bombPart.partInfo.title != selectedWeapon)
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
				
				float simDeltaTime = 0.1f;
				float simTime = 0;
				Vector3 dragForce = Vector3.zero;
				Vector3 prevPos = transform.position;
				Vector3 currPos = transform.position;
				Vector3 simVelocity = rigidbody.velocity;
				simVelocity += bombPart.GetComponent<MissileLauncher>().decoupleSpeed * -bombPart.transform.up;
				
				List<Vector3> pointPositions = new List<Vector3>();
				pointPositions.Add(currPos);
				
				
				prevPos = bombPart.transform.position;
				currPos = bombPart.transform.position;
			
				bombAimerPosition = Vector3.zero;
				
				bool simulating = true;
				while(simulating)
				{
					prevPos = currPos;
					
					simVelocity += FlightGlobals.getGeeForceAtPosition(currPos) * simDeltaTime;
					float simSpeedSquared = (float) simVelocity.sqrMagnitude;
					float drag = bombPart.maximum_drag;
					if(simTime > bombPart.GetComponent<MissileLauncher>().deployTime)
					{
						drag = 	bombPart.GetComponent<MissileLauncher>().deployedDrag;
					}
					dragForce = (0.008f * bombPart.rb.mass) * drag * 0.5f * simSpeedSquared * (float) FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPos)) * simVelocity.normalized;
					simVelocity -= (dragForce/bombPart.mass)*simDeltaTime;
					
					currPos += simVelocity * simDeltaTime;
					
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
				
				
				//debug lines
				if(BDArmorySettings.DRAW_DEBUG_LINES && BDArmorySettings.DRAW_AIMERS)
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
		
		bool AltitudeTrigger()
		{
			double asl = vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass());
			double radarAlt = asl - vessel.terrainAltitude;
			
			return radarAlt < 4999 || asl < 4999;
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
		
		void GuardMode()
		{
			if(guardMode && vessel.IsControllable)
			{
				//setting turrets to guard mode
				foreach(var turret in vessel.FindPartModulesImplementing<BahaTurret>())
				{
					if(turret.part.partInfo.title == selectedWeapon)	
					{
						turret.turretEnabled = true;
						turret.guardMode = true;
					}
				}
				
				if(guardTarget != null && Vector3.Distance(guardTarget.transform.position, transform.position) > guardRange) guardTarget = null;
				if(guardTarget == null)
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
				
				if(Time.time-targetScanTimer > targetScanInterval)
				{
					bool canSeeTarget = false;
					
					
					
					if(guardTarget!=null)
					{
						
						//check sight line to target
						Vector3 direction = (transform.position-guardTarget.transform.position).normalized;
						float distance = Vector3.Distance(transform.position, guardTarget.transform.position);
						RaycastHit sightHit;
						if(Physics.Raycast(guardTarget.transform.position+(direction*50), direction, out sightHit, distance, 557057))
						{
							Vessel hitVessel = null;
							try
							{
								hitVessel = Part.FromGO(sightHit.rigidbody.gameObject).vessel;	
							}
							catch(NullReferenceException){}
							if(hitVessel!=null && hitVessel == vessel)
							{
								canSeeTarget = true;	
							}
						}
						
						//fire if visible
						if(canSeeTarget)
						{
							targetScanTimer = Time.time;
							
							//pick a weapon
							SwitchToGuardWeapon();
							
							if(selectedWeapon.Contains("Missile"))
							{
								Debug.Log ("Firing on target: "+guardTarget.GetName());
								FireMissile();
								guardTarget = null;
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
					
					
					//get a target.
					Vessel previousTarget = guardTarget;
					guardTarget = null;
					float angle = 0;
					if(targetMissiles)
					{
						foreach(Vessel v in FlightGlobals.Vessels)
						{
							float distance = Vector3.Distance(transform.position, v.transform.position);
							if(v.loaded && distance < guardRange)
							{
								angle = Vector3.Angle (-transform.forward, v.transform.position-transform.position);
								foreach(var missile in v.FindPartModulesImplementing<MissileLauncher>())
								{
									if(angle < guardAngle/2 && missile.hasFired && missile.team != team)
									{
										if(!(v == previousTarget && !canSeeTarget))
										{
											guardTarget = v;
											return;
										}
									}
								}
							}
						}
					}
					else
					{
						foreach(Vessel v in FlightGlobals.Vessels)
						{
							if(v.loaded && Vector3.Distance(transform.position, v.transform.position) < guardRange)
							{
								angle = Vector3.Angle (-transform.forward, v.transform.position-transform.position);
								foreach(var mF in v.FindPartModulesImplementing<MissileFire>())
								{
									if(angle < guardAngle/2 && mF.team != team && mF.vessel.IsControllable)
									{
										if(!(v == previousTarget && !canSeeTarget))
										{
											guardTarget = v;
											return;
										}
									}
								}
							}
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
						turret.guardMode = false;
					}
				}	
			}
		}
		
		void SwitchToGuardWeapon()
		{
			if(!CheckWeaponForGuard())
			{
				string startingWeapon = selectedWeapon;
				while(true)
				{
					CycleWeapon(true);
					if(startingWeapon == selectedWeapon || CheckWeaponForGuard()) return;
				}
			}
			else return;
		}
		
		
		bool CheckWeaponForGuard()
		{
			int turretStatus = CheckTurret();
			
			return (selectedWeapon != "None" && !selectedWeapon.Contains("Bomb") && !selectedWeapon.Contains("Rocket") && turretStatus > 0);	
		}
		
		//0: weapon is a turret and fixed, 1: weapon is a turret and not fixed, 2: weapon is not a turret -1: turret is good but out of ammo
		int CheckTurret()
		{
			foreach(var turret in vessel.FindPartModulesImplementing<BahaTurret>())
			{
				if(turret.part.partInfo.title == selectedWeapon)
				{
					
					if (turret.yawRange > 25 || turret.yawRange == -1)
					{
						if(CheckAmmo(turret)) return 1;
						else return -1;
					}
					else return 0;
				}
			}
			return 2;
		}
		
		bool CheckAmmo(BahaTurret turret)
		{
			string ammoName = turret.ammoName;
			double amount = 0;
			
			foreach(Part p in vessel.parts)
			{
				foreach(var resource in p.Resources.list)	
				{
					if(resource.resourceName == ammoName)
					{
						amount += resource.amount;	
					}
				}
			}
			Debug.Log ("Checked for turret ammo.  Amount: "+amount);
			
			return (amount > 0);
		}
		
		void ToggleTurret()
		{
			foreach(var turret in vessel.FindPartModulesImplementing<BahaTurret>())
			{
				if(turret.part.partInfo.title != selectedWeapon)
				{
					turret.turretEnabled = false;	
				}
				else
				{
					turret.turretEnabled = true;	
				}
			}
		}
		
		public void MissileWarning()
		{
			if(Time.time-warningTimer > warningInterval && vessel.isActiveVessel)
			{
				warningTimer = Time.time;
				audioSource.PlayOneShot(warningSound);
				BDArmorySettings.Instance.missileWarning = true;
				BDArmorySettings.Instance.missileWarningTime = Time.time;
			}
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
			
			float radius = 0.385f/2;
			//Vector3 direction = -FlightGlobals.getUpAxis();
			Vector3 direction = FlightGlobals.getGeeForceAtPosition(transform.position) - vessel.acceleration;
			Ray ray1 = new Ray(ml.transform.position - (radius * ml.transform.right), direction);
			Ray ray2 = new Ray(ml.transform.position + (radius * ml.transform.right), direction);
			RaycastHit[] hits1 = Physics.RaycastAll(ray1, 20, 557057);
			foreach(RaycastHit hit in hits1)
			{
				Part p;
				try
				{
					p = Part.FromGO(hit.transform.gameObject);
				}
				catch(NullReferenceException)
				{
					p = null;
				}
				if((p!=null && p != ml.part) || p == null) return false;
			}
			
			RaycastHit[] hits2 = Physics.RaycastAll(ray2, 20, 557057);
			foreach(RaycastHit hit in hits2)
			{
				Part p;
				try
				{
					p = Part.FromGO(hit.transform.gameObject);
				}
				catch(NullReferenceException)
				{
					p = null;
				}
				if((p!=null && p != ml.part) || p == null) return false;
			}
			
			return true;
			
		}
		
		//Thanks FlowerChild
		//refreshes part action window
		void RefreshAssociatedWindows()
        {
			foreach ( UIPartActionWindow window in FindObjectsOfType( typeof( UIPartActionWindow ) ) ) 
            {
				if ( window.part == part )
                {
                    window.displayDirty = true;
                }
            }
        }
		
		
	}
}

