using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class MissileFire : PartModule
	{
		//weapons
		private List<string> weaponTypes = new List<string>();
		private string[] weaponArray;
		
		[KSPField(guiActiveEditor = false, isPersistant = true, guiActive = false)]
		public int weaponIndex = 0;
		
		ScreenMessage selectionMessage;
		ScreenMessage armedMessage;
		string selectionText = "";
		Transform cameraTransform;
		Part lastFiredSym = null;
		
		float startTime;
		
		float rippleTimer;
		float rippleRPM = 650;
		float triggerTimer = 0;
		
		public float triggerHoldTime = 0.3f;
		
		bool hasSingleFired = false;
		//
		
		
		//bomb aimer
		bool bombAimerActive = false;
		Part bombPart = null;
		Vector3 bombAimerPosition = Vector3.zero;
		Texture2D bombAimerTexture = GameDatabase.Instance.GetTexture("BDArmory/Textures/grayCircle", false);
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
		
		//missile warning
		float warningTimer = 0;
		float warningInterval = 2;
		
		
		//guard mode vars
		float targetScanTimer = 0;
		Vessel guardTarget = null;
		
		
		//KSP fields and events
		#region kspFields,events
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Scan Interval"),
        	UI_FloatRange(minValue = 1f, maxValue = 60f, stepIncrement = 1f, scene = UI_Scene.All)]
		float targetScanInterval = 8;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Field of View"),
        	UI_FloatRange(minValue = 10f, maxValue = 360f, stepIncrement = 10f, scene = UI_Scene.All)]
		public float guardAngle = 100f;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Guard Range"),
        	UI_FloatRange(minValue = 100f, maxValue = 8000f, stepIncrement = 100f, scene = UI_Scene.All)]
        public float guardRange = 1500f;
		
		[KSPField(isPersistant = true, guiActive = true, guiName = "Guard Mode")]
		public bool guardMode = false;
		
		[KSPField(isPersistant = true, guiActive = false, guiName = "Target Missiles")]
		public bool targetMissiles = false;
		
		[KSPEvent(guiActive = false, guiName = "Switch Target Type", active = true)]
		public void GuiToggleTargetMissiles()
		{
			targetMissiles = !targetMissiles;	
		}
		
		[KSPEvent(guiActive = true, guiName = "Toggle Guard Mode", active = true)]
		public void GuiToggleGuardMode()
		{
			guardMode = !guardMode;	
			Fields["guardRange"].guiActive = guardMode;
			Fields["guardAngle"].guiActive = guardMode;
			Fields["targetMissiles"].guiActive = guardMode;
			Events["GuiToggleTargetMissiles"].guiActive = guardMode;
			Fields["targetScanInterval"].guiActive = guardMode;
		}
		
		[KSPField(guiActiveEditor = true, isPersistant = true, guiActive = true, guiName = "Team")]
		public string team = "A";
		
		[KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "Toggle Team", active = true)]
		public void GuiToggleTeam()
		{
			if(team == "A") team = "B";
			else team = "A";
		}
		
		
		[KSPField(isPersistant = false, guiActive = true, guiName = "Armed")]
		public bool isArmed = false;
		
		[KSPEvent(guiActive = true, guiName = "Arm/Disarm", active = true)]
		public void ToggleArm()
		{
			isArmed = !isArmed;
			if(isArmed)
			{
				ScreenMessages.RemoveMessage(armedMessage);
				armedMessage = new ScreenMessage("Weapon System ARMED", 25000, ScreenMessageStyle.UPPER_RIGHT);
				ScreenMessages.PostScreenMessage(armedMessage, true);
			}
			else 
			{
				ScreenMessages.RemoveMessage(armedMessage);
				armedMessage = new ScreenMessage("Weapon System Disarmed", 2, ScreenMessageStyle.UPPER_RIGHT);
				ScreenMessages.PostScreenMessage(armedMessage, true);
			}
		}
		
		[KSPAction("Arm/Disarm")]
		public void AGToggleArm(KSPActionParam param)
		{
			ToggleArm();
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
			
			
			startTime = Time.time;
			if(HighLogic.LoadedSceneIsFlight)
			{
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
				Events["GuiToggleTargetMissiles"].guiActive = guardMode;
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
					
					ScreenMessages.RemoveMessage(selectionMessage);
					selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
					selectionMessage.message = selectionText;
					ScreenMessages.PostScreenMessage(selectionMessage, true);
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
				if(isArmed && Input.GetKey(BDArmorySettings.FIRE_KEY))
				{
					triggerTimer += Time.fixedDeltaTime;	
				}
				else
				{
					triggerTimer = 0;	
				}
				
				
				//ripple firing rockets==
				if(isArmed && Input.GetKey(BDArmorySettings.FIRE_KEY) && selectedWeapon.Contains("Rocket") && !MapView.MapIsEnabled && triggerTimer > triggerHoldTime)// && !hasSingleFired)
				{
					if(Time.time - rippleTimer > 60/rippleRPM)
					{
						FireMissile();
						hasSingleFired = true;
						rippleTimer = Time.time;
					}
				}
				//==
				
				
				
				//single firing missiles===
				if(isArmed && Input.GetKey(BDArmorySettings.FIRE_KEY) && (selectedWeapon.Contains("Missile") || selectedWeapon.Contains("Bomb")) && !MapView.MapIsEnabled && triggerTimer > triggerHoldTime && !hasSingleFired)
				{
					FireMissile();
					hasSingleFired = true;
				}
				if(isArmed && !Input.GetKey(BDArmorySettings.FIRE_KEY) && hasSingleFired)
				{
					hasSingleFired = false;	
				}
				//========
				
				TargetAcquire();
				
				if(HighLogic.LoadedSceneIsFlight && vessel.targetObject!=null)
				{
					//TargetCam();
				}
				
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
				ScreenMessages.RemoveMessage(selectionMessage);
				selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
				selectionMessage.message = selectionText;
				ScreenMessages.PostScreenMessage(selectionMessage, true);
				
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
			
			ScreenMessages.RemoveMessage(selectionMessage);
			selectionText = "Selected Weapon: "+selectedWeapon;
			selectionMessage.message = selectionText;
			ScreenMessages.PostScreenMessage(selectionMessage, true);
			
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
					if(guardTarget!=null)
					{
						ml.FireMissileOnTarget(guardTarget);
					}
					else
					{
						ml.FireMissile();
					}
					
					
					/*
					if(BDACameraTools.lastProjectileFired != null)
					{
						BDACameraTools.lastProjectileFired.OnJustAboutToBeDestroyed -= new Callback(BDACameraTools.PostProjectileCamera);
					}
					BDACameraTools.lastProjectileFired = lastFiredSym;
					*/
					lastFiredSym = nextPart;
					
					UpdateList ();
					if(weaponIndex >= weaponArray.Length)
					{
						hasSingleFired = true;
						triggerTimer = 0;
						
						weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
						
						ScreenMessages.RemoveMessage(selectionMessage);
						selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
						selectionMessage.message = selectionText;
						ScreenMessages.PostScreenMessage(selectionMessage, true);
					}
					return;
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
						
						ScreenMessages.RemoveMessage(selectionMessage);
						selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
						selectionMessage.message = selectionText;
						ScreenMessages.PostScreenMessage(selectionMessage, true);
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
						lastFiredSym = FindSym(ml.part);
						
						if(guardTarget!=null)
						{
							ml.FireMissileOnTarget(guardTarget);
						}
						else
						{
							ml.FireMissile();
						}
						
						/*
						if(BDACameraTools.lastProjectileFired != null)
						{
							BDACameraTools.lastProjectileFired.OnJustAboutToBeDestroyed -= new Callback(BDACameraTools.PostProjectileCamera);
						}
						BDACameraTools.lastProjectileFired = ml.part;
						*/
						
						UpdateList ();
						if(weaponIndex >= weaponArray.Length)
						{
							hasSingleFired = true;
							triggerTimer = 0;
							
							weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
							
							ScreenMessages.RemoveMessage(selectionMessage);
							selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
							selectionMessage.message = selectionText;
							ScreenMessages.PostScreenMessage(selectionMessage, true);
						}
						return;
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
							
							ScreenMessages.RemoveMessage(selectionMessage);
							selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
							selectionMessage.message = selectionText;
							ScreenMessages.PostScreenMessage(selectionMessage, true);
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
					
					ScreenMessages.RemoveMessage(selectionMessage);
					selectionText = "Selected Weapon: " + weaponArray[weaponIndex];
					selectionMessage.message = selectionText;
					ScreenMessages.PostScreenMessage(selectionMessage, true);
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
			if(vessel == FlightGlobals.ActiveVessel && bombPart!=null && BDArmorySettings.DRAW_AIMERS && vessel.verticalSpeed < 50 && selectedWeapon.Contains("Bomb") && vessel.altitude < 4500)
			{
				bombAimerActive = true;
				float simDeltaTime = 0.1f;
				Vector3 dragForce = Vector3.zero;
				Vector3 prevPos = transform.position;
				Vector3 currPos = transform.position;
				Vector3 simVelocity = rigidbody.velocity;
				
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
					dragForce = (0.008f * bombPart.rb.mass) * bombPart.maximum_drag * 0.5f * simSpeedSquared * (float) FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPos)) * simVelocity.normalized;
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
			else
			{
				bombAimerActive = false;	
			}
		}
		
		void OnGUI()
		{
			if(vessel == FlightGlobals.ActiveVessel && bombAimerActive && BDArmorySettings.DRAW_AIMERS)
			{
				float size = 50;
				
				Vector3 aimPosition = Camera.main.WorldToViewportPoint(bombAimerPosition);
				Rect drawRect = new Rect(aimPosition.x*Screen.width-(0.5f*size), (1-aimPosition.y)*Screen.height-(0.5f*size), size, size);
				float cameraAngle = Vector3.Angle(Camera.main.transform.forward, bombAimerPosition-Camera.main.transform.position);
				if(cameraAngle<90) GUI.DrawTexture(drawRect, bombAimerTexture);
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
					
					
					
					if(guardTarget!=null)
					{
						targetScanTimer = Time.time;
						
						//check sight line to target
						bool canSeeTarget = false;
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
								//guardTarget = null;
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
										guardTarget = v;
										return;
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
										guardTarget = v;
										return;
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
				ScreenMessages.PostScreenMessage("Incoming Missile", warningInterval * 0.75f, ScreenMessageStyle.UPPER_RIGHT);
			}
		}
	}
}

