using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class MissileFire : PartModule
	{
		private List<string> weaponTypes = new List<string>();
		private string[] weaponArray;
		private int weaponIndex = 0;
		
		ScreenMessage selectionMessage;
		string selectionText = "";
		Transform cameraTransform;
		MissileLauncher lastFiredSym = null;
		
		
		bool bombAimerActive = true;
		GameObject bombAimer = null;
		
		bool targetAcquireActive = true;
		
		//LineRenderer foof;
		
		
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
				
				//ScreenMessages.PostScreenMessage(selectionMessage, true);
				
				/*
				foof = gameObject.AddComponent<LineRenderer>();
				foof.SetVertexCount(2);
				foof.SetPosition(0, transform.position);
				foof.SetPosition(1, transform.position);
				foof.SetWidth(.2f, 1);
				foof.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
				foof.material.SetColor("_EmissiveColor", Color.red) ;
				*/
			}
			
			
			
		}
		
		public override void OnUpdate ()
		{
			
			
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(weaponIndex >= weaponArray.Length) weaponIndex--;
				if(weaponArray.Length > 0) selectedWeapon = weaponArray[weaponIndex];
				
				
			}
			
			if(selectedWeapon.Contains("Bomb"))
			{
				Events["GuiBombAimer"].guiActive = true;	
			}
			else
			{
				Events["GuiBombAimer"].guiActive = false;
			}
			
		}
		
		public override void OnFixedUpdate ()
		{
			TargetAcquire();
			if(HighLogic.LoadedSceneIsFlight)
			{
				TargetCam();
			}
			if(bombAimerActive && vessel.verticalSpeed < 10 && vessel.altitude > 150 && selectedWeapon.Contains("Bomb"))
			{
				
				float vAccel = (float)FlightGlobals.getGeeForceAtPosition(transform.position).magnitude;
				float riseTime = Mathf.Clamp ((float)vessel.verticalSpeed/vAccel, 0, Mathf.Infinity);
				float riseHeight = 0.5f * (float)vessel.verticalSpeed*(float)vessel.verticalSpeed / vAccel;
				float fallTime = Mathf.Sqrt(2 * (float)(riseHeight+vessel.altitude) / vAccel);
				Vector3 targetPosition = transform.position + rigidbody.velocity*Time.fixedDeltaTime - (vessel.altitude * FlightGlobals.getUpAxis()) + (vessel.srf_velocity * 1.10f*(riseTime + fallTime));
				
				//foof.SetPosition(0, transform.position + rigidbody.velocity*Time.fixedDeltaTime);
				//foof.SetPosition(1, targetPosition);
				
				Ray ray = new Ray(transform.position, targetPosition - transform.position);
				RaycastHit hitInfo;
				if(Physics.Raycast(ray, out hitInfo, 8000, 1<<15))
				{
					if(bombAimer == null)
					{
						
						bombAimer = GameDatabase.Instance.GetModel("BDArmory/Models/bombAimer/model");
						bombAimer.SetActive(true);
						bombAimer.name = "BombAimer";
						bombAimer.transform.rotation = Quaternion.LookRotation(FlightGlobals.getUpAxis()) * Quaternion.Euler(90, 0, 0);
						bombAimer.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
						
						bombAimer = (GameObject) Instantiate(bombAimer);
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
		}
		
		
		public void FireMissile()
		{
			if(lastFiredSym != null && lastFiredSym.part.partInfo.title == selectedWeapon)
			{
				MissileLauncher nextML = FindSymML(lastFiredSym.part);
				lastFiredSym.FireMissile();	
				if(BDACameraTools.lastProjectileFired != null) BDACameraTools.lastProjectileFired.OnJustAboutToBeDestroyed -= new Callback(BDACameraTools.PostProjectileCamera);
				BDACameraTools.lastProjectileFired = lastFiredSym.part;
				lastFiredSym = nextML;
				UpdateList ();
				if(weaponIndex >= weaponArray.Length) weaponIndex = Mathf.Clamp(weaponArray.Length - 1, 0, 999999);
				return;
			}
			else
			{
				foreach(MissileLauncher ml in vessel.FindPartModulesImplementing<MissileLauncher>())
				{
					if(ml.part.partInfo.title == selectedWeapon)
					{
						lastFiredSym = FindSymML(ml.part);
						ml.FireMissile();
						if(BDACameraTools.lastProjectileFired != null) BDACameraTools.lastProjectileFired.OnJustAboutToBeDestroyed -= new Callback(BDACameraTools.PostProjectileCamera);
						BDACameraTools.lastProjectileFired = ml.part;
						UpdateList ();
						if(weaponIndex >= weaponArray.Length) weaponIndex = Mathf.Clamp(weaponArray.Length - 1, 0, 999999);
						return;
					}
				}
			}
			
			lastFiredSym = null;
		}
		
		//finds the a symmetry partner
		public MissileLauncher FindSymML(Part p)
		{
			foreach(Part pSym in p.symmetryCounterparts)
			{
				foreach(MissileLauncher ml in pSym.GetComponentsInChildren<MissileLauncher>())
				{
					return ml;	
				}
			}
			
			return null;
		}
		
		
		public void TargetAcquire() //not working..
		{
			if(targetAcquireActive)// && vessel.targetObject != null)
			{
				Vector3 origin = vessel.transform.position;
				float radius = 50;
				Vector3 direction = vessel.ReferenceTransform.up;
				RaycastHit hitInfo;
				float distance = 2500;
				if(BDArmorySettings.PHYSICS_RANGE > 0) distance = BDArmorySettings.PHYSICS_RANGE;
				int layerMask = 1<<19; //"Disconnected Parts"
				Vessel acquiredTarget = null;
				if(Physics.SphereCast(origin, radius, direction, out hitInfo, distance, layerMask))
				{
					acquiredTarget = Part.FromGO(hitInfo.collider.gameObject).vessel;
					Debug.Log ("found target! : "+Part.FromGO(hitInfo.collider.gameObject).vessel.name);
				}
				if(acquiredTarget != null)
				{
					vessel.orbitTargeter.SetTarget(acquiredTarget.GetOrbitDriver());
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
		
	}
}

