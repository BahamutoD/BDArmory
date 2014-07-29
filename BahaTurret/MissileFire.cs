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
		
		MissileLauncher lastFiredSym = null;
		
		
		
		
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
				//ScreenMessages.PostScreenMessage(selectionMessage, true);
			}
			
			
			
		}
		
		public override void OnUpdate ()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(weaponIndex >= weaponArray.Length) weaponIndex--;
				if(weaponArray.Length > 0) selectedWeapon = weaponArray[weaponIndex];
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
		
	}
}

