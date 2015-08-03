using System;
using UnityEngine;
namespace BahaTurret
{
	public class ModuleECMJammer : PartModule
	{
		[KSPField]
		public float jammerStrength = 700;

		[KSPField]
		public float resourceDrain = 5;

		[KSPField(isPersistant = true, guiActive = true, guiName = "Enabled")]
		public bool jammerEnabled = false;

		VesselECMJInfo vesselJammer;

		[KSPEvent(guiActiveEditor = false, guiActive = true, guiName = "Toggle")]
		public void Toggle()
		{
			if(jammerEnabled)
			{
				DisableJammer();
			}
			else
			{
				EnableJammer();
			}
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);
			if(HighLogic.LoadedSceneIsFlight)
			{
				part.force_activate();
				foreach(var wm in vessel.FindPartModulesImplementing<MissileFire>())
				{
					wm.jammers.Add(this);
				}
			}
		}


		public void EnableJammer()
		{
			foreach(var jammer in vessel.FindPartModulesImplementing<ModuleECMJammer>())
			{
				jammer.DisableJammer();
			}

			EnsureVesselJammer();

			vesselJammer.jammerStrength = jammerStrength;
			vesselJammer.jammerEnabled = true;
			jammerEnabled = true;
		}

		public void DisableJammer()
		{
			EnsureVesselJammer();
		
			vesselJammer.jammerEnabled = false;
			jammerEnabled = false;
		}

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();

			if(jammerEnabled)
			{
				EnsureVesselJammer();

				DrainElectricity();
			}
		}

		void EnsureVesselJammer()
		{
			if(!vesselJammer)
			{
				vesselJammer = vessel.gameObject.GetComponent<VesselECMJInfo>();
				if(!vesselJammer)
				{
					vesselJammer = vessel.gameObject.AddComponent<VesselECMJInfo>();
				}
			}
		}


		void DrainElectricity()
		{
			double drainAmount = resourceDrain * TimeWarp.fixedDeltaTime;
			double chargeAvailable = part.RequestResource("ElectricCharge",drainAmount, ResourceFlowMode.ALL_VESSEL);
			if(chargeAvailable < drainAmount)
			{
				DisableJammer();
			}
		}
	}
}

