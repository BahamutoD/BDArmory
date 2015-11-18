using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class WMTurretGroup : MonoBehaviour
	{
		public enum TargetTypes{Air, Ground, Missiles, All}

		public List<ModuleWeapon> weapons;

		public bool guardMode = false;
		public TargetTypes targetType = TargetTypes.All;


		public void StartFiringOnTarget(Vessel targetVessel, float burstLength)
		{
			foreach(var weapon in weapons)
			{
				weapon.legacyTargetVessel = targetVessel;
				weapon.autoFireTimer = Time.time;
				weapon.autoFireLength = burstLength;
			}
		}

		public void ForceStopFiring()
		{
			foreach(var weapon in weapons)
			{
				weapon.autoFire = false;
				weapon.legacyTargetVessel = null;
			}
		}
	}
}

