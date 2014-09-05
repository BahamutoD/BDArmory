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
		private int weaponIndex = 0;
		
		ScreenMessage selectionMessage;
		ScreenMessage armedMessage;
		string selectionText = "";
		Transform cameraTransform;
		Part lastFiredSym = null;
		
		float rippleTimer;
		float rippleRPM = 650;
		float triggerTimer;
		
		public float triggerHoldTime = 0.5f;
		
		bool hasSingleFired = false;
		//
		
		
		//bomb aimer
		bool bombAimerActive = true;
		GameObject bombAimer = null;
		Part bombPart = null;
		//
		
		
		//targeting
		private List<Vessel> loadedVessels = new List<Vessel>();
		float targetListTimer;
		
		
		//rocket aimer handling
		RocketLauncher nextRocket = null;
		
		
		//guard mode vars
		float targetScanTimer = 0;
		float targetScanInterval = 8;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Field of View"),
        	UI_FloatRange(minValue = 10f, maxValue = 360f, stepIncrement = 10f, scene = UI_Scene.All)]
		public float guardAngle = 100f;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Guard Range"),
        	UI_FloatRange(minValue = 100f, maxValue = 5000f, stepIncrement = 100f, scene = UI_Scene.All)]
        public float guardRange = 1500f;
		
		[KSPField(isPersistant = true, guiActive = true, guiName = "Guard Mode")]
		public bool guardMode = false;
		
		[KSPEvent(guiActive = true, guiName = "Toggle Guard Mode", active = true)]
		public void GuiToggleGuardMode()
		{
			guardMode = !guardMode;	
			Fields["guardRange"].guiActive = guardMode;
			Fields["guardAngle"].guiActive = guardMode;
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
		
		
		[KSPEvent(guiActive = true, guiName = "Toggle Bomb Aimer", active = true)]
		public void GuiBombAimer()
		{
			bombAimerActive = !bombAimerActive;	
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
		
		[KSPEvent(guiActive = true, guiName = "Cycle Weapon", active = true)]
		public void GuiCycle()
		{
			CycleWeapon();	
		}
		
		[KSPAction("Cycle Weapon")]
		public void AGCycle(KSPActionParam param)
		{
			CycleWeapon();
		}
		
		public override void OnStart (PartModule.StartState state)
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				UpdateList();
				if(weaponArray.Length > 0) selectedWeapon = weaponArray[weaponIndex];
				selectionText = "Selected Weapon: "+selectedWeapon;
				selectionMessage = new ScreenMessage(selectionText, 2, ScreenMessageStyle.LOWER_CENTER);
				
				cameraTransform = part.FindModelTransform("BDARPMCameraTransform");
				
				part.force_activate();
				rippleTimer = Time.time;
				targetListTimer = Time.time;
				triggerTimer = Time.time;
				
				Fields["guardRange"].guiActive = guardMode;
				Fields["guardAngle"].guiActive = guardMode;
			}
		}
		
		public override void OnUpdate ()
		{
			
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(weaponIndex >= weaponArray.Length) 
				{
					weaponIndex--;
					hasSingleFired = true;
				}
				if(weaponArray.Length > 0) selectedWeapon = weaponArray[weaponIndex];
				weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
			}
			
			if(selectedWeapon.Contains("Bomb"))
			{
				Events["GuiBombAimer"].guiActive = true;	
			}
			else
			{
				Events["GuiBombAimer"].guiActive = false;
			}
			
			//finding next rocket to shoot (for aimer)
			FindNextRocket();
			
		}
		
		public override void OnFixedUpdate ()
		{
			GuardMode();
			
			if(isArmed && Input.GetKeyDown(BDArmorySettings.FIRE_KEY))
			{
				triggerTimer = Time.time;	
			}
			
			//ripple firing rockets==
			if(isArmed && Input.GetKey(BDArmorySettings.FIRE_KEY) && selectedWeapon.Contains("Rocket") && !MapView.MapIsEnabled && Time.time-triggerTimer > triggerHoldTime && !hasSingleFired)
			{
				if(Time.time - rippleTimer > 60/rippleRPM)
				{
				FireMissile();
				rippleTimer = Time.time;
				}
			}
			//==
			
			
			
			//single firing missiles===
			if(isArmed && Input.GetKey(BDArmorySettings.FIRE_KEY) && (selectedWeapon.Contains("Missile") || selectedWeapon.Contains("Bomb")) && !MapView.MapIsEnabled && Time.time-triggerTimer > triggerHoldTime && !hasSingleFired)
			{
				FireMissile();
				hasSingleFired = true;
			}
			if(isArmed && Input.GetKeyUp(BDArmorySettings.FIRE_KEY) && hasSingleFired)
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
			
			weaponTypes.Sort ();
			weaponArray = new string[weaponTypes.Count];
			int i = 0;
			foreach(string wep in weaponTypes)
			{
				weaponArray[i] = wep;
				i++;
			}
			if(weaponTypes.Count == 0) selectedWeapon = "None";
		}
		
		public void CycleWeapon()
		{
			UpdateList();
			weaponIndex++;
			if(weaponIndex >= weaponArray.Length) weaponIndex = 0; //wrap
			if(weaponArray.Length > 0) selectedWeapon = weaponArray[weaponIndex];
			
			
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
					ml.FireMissile();
					if(BDACameraTools.lastProjectileFired != null)
					{
						BDACameraTools.lastProjectileFired.OnJustAboutToBeDestroyed -= new Callback(BDACameraTools.PostProjectileCamera);
					}
					BDACameraTools.lastProjectileFired = lastFiredSym;
					lastFiredSym = nextPart;
					
					UpdateList ();
					weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
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
					
					weaponIndex = Mathf.Clamp(weaponIndex, 0, weaponArray.Length - 1);
					
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
						ml.FireMissile();
						if(BDACameraTools.lastProjectileFired != null)
						{
							BDACameraTools.lastProjectileFired.OnJustAboutToBeDestroyed -= new Callback(BDACameraTools.PostProjectileCamera);
						}
						BDACameraTools.lastProjectileFired = ml.part;
						UpdateList ();
						if(weaponIndex >= weaponArray.Length) weaponIndex = Mathf.Clamp(weaponArray.Length - 1, 0, 999999);
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
						if(weaponIndex >= weaponArray.Length) weaponIndex = Mathf.Clamp(weaponArray.Length - 1, 0, 999999);
						return;
					}
				}
				UpdateList();
				if(weaponIndex >= weaponArray.Length) weaponIndex = Mathf.Clamp(weaponArray.Length - 1, 0, 999999);
			}
			
			lastFiredSym = null;
		}
		
		//finds the a symmetry partner
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
						float viewAngle = Vector3.Angle(vessel.ReferenceTransform.up, v.transform.position-transform.position);
						if(v.loaded && viewAngle < smallestAngle) loadedVessels.Add(v);
					}
				}
			
				foreach(Vessel v in loadedVessels)
				{
					float viewAngle = Vector3.Angle(vessel.ReferenceTransform.up, v.transform.position-transform.position);
					//if(v!= vessel && v.loaded) Debug.Log ("view angle: "+viewAngle);
					if(v != vessel && v.loaded && viewAngle < smallestAngle)
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
			if(bombAimerActive && vessel.verticalSpeed < 10 && vessel.altitude > 200 && selectedWeapon.Contains("Bomb"))
			{
				float vAccel = (float)FlightGlobals.getGeeForceAtPosition(transform.position).magnitude;
				Vector3 horizVelocity = vessel.srf_velocity - (vessel.verticalSpeed * FlightGlobals.getUpAxis());
				float angle = Vector3.Angle(horizVelocity, vessel.srf_velocity) * Mathf.Deg2Rad * Mathf.Sign((float)vessel.verticalSpeed);
				Debug.Log ("Angle: "+angle*Mathf.Rad2Deg+", horizVelocity: "+horizVelocity.magnitude+", horizontalSrfSpeed: "+vessel.horizontalSrfSpeed);
				float v = (float)vessel.srfSpeed;
				float distance = (v*Mathf.Cos(angle)/vAccel)*(v*Mathf.Sin(angle) + Mathf.Sqrt(v * v * Mathf.Sin(angle) * Mathf.Sin(angle) + 2 * vAccel * vessel.heightFromTerrain));
				
				Vector3 dragForce = Vector3.zero;
				if(bombPart!=null)
				{
					dragForce = 0.008f * bombPart.mass * part.maximum_drag * 0.5f * vessel.srf_velocity.magnitude * vessel.srf_velocity.magnitude * vessel.atmDensity * vessel.srf_velocity.normalized;
				}
				Vector3 targetPosition = (transform.position) - (vessel.heightFromTerrain * FlightGlobals.getUpAxis()) + (horizVelocity.normalized*distance*0.95f) - ((dragForce.magnitude*horizVelocity.normalized) * (distance/(float)vessel.srfSpeed));
				Ray ray = new Ray(transform.position, targetPosition - transform.position);
				RaycastHit hitInfo;
				if(Physics.Raycast(ray, out hitInfo, 8000, 1<<15))
				{
					if(bombAimer == null)
					{
						
						bombAimer = GameDatabase.Instance.GetModel("BDArmory/Models/bombAimer/model");
						
						bombAimer.name = "BombAimer";
						bombAimer.transform.rotation = Quaternion.LookRotation(FlightGlobals.getUpAxis()) * Quaternion.Euler(90, 0, 0);
						//bombAimer.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
						bombAimer = (GameObject) Instantiate(bombAimer);
						bombAimer.SetActive(true);
					}
					if(bombAimer != null)
					{
						bombAimer.transform.position = hitInfo.point + (50-Mathf.Clamp(FlightGlobals.getAltitudeAtPos(hitInfo.point), -Mathf.Infinity, 0))*FlightGlobals.getUpAxis();
						float aimerScale = Mathf.Clamp(hitInfo.distance/5, 5, 1000);
						bombAimer.transform.localScale = new Vector3(aimerScale, 0.001f, aimerScale);
					}
				}
				else
				{
					GameObject.Destroy(bombAimer);
					bombAimer = null;	
				}
			}
			else
			{
				GameObject.Destroy(bombAimer);
				bombAimer = null;
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
			if(guardMode)
			{
				if(Time.time-targetScanTimer > targetScanInterval)
				{
					targetScanTimer = Time.time;
					if(vessel.targetObject!=null)
					{
						Debug.Log ("Firing on target: "+vessel.targetObject.GetName());
						FireMissile();
						vessel.targetObject = null;
					}
					
					//get a target
					float angle = 0;
					foreach(Vessel v in FlightGlobals.Vessels)
					{
						if(v.loaded && Vector3.Distance(transform.position, v.transform.position) < guardRange)
						{
							angle = Vector3.Angle (-transform.forward, v.transform.position-transform.position);
							foreach(var mF in v.FindPartModulesImplementing<MissileFire>())
							{
								if(angle < guardAngle/2 && mF.team != team)
								{
									vessel.targetObject = v;
									return;
								}
							}
						}
					}
					
					
				}
				
			}
		}
		
	}
}

