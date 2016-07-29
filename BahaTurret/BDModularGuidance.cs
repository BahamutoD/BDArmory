using System;
using UnityEngine;

namespace BahaTurret
{
	public class BDModularGuidance : PartModule
	{
		//public GameObject target = null;

		public bool hasFired = false;
		
		public bool guidanceActive = false;

		Vessel targetVessel;
		Vessel parentVessel;

		Transform vesselTransform;
		Transform velocityTransform;


		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "SteerFactor"),
		 UI_FloatRange(minValue = 0.1f, maxValue = 20f, stepIncrement = .1f, scene = UI_Scene.All)]
		public float steerMult = 10;
		
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "SteerLimiter"),
		 UI_FloatRange(minValue = .1f, maxValue = 1f, stepIncrement = .05f, scene = UI_Scene.All)]
		public float maxSteer = 1;
		
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "SteerDamping"),
		 UI_FloatRange(minValue = 0f, maxValue = 20f, stepIncrement = .05f, scene = UI_Scene.All)]
		public float steerDamping = 5;
		
		[KSPField(isPersistant = true)]
		public int guidanceMode = 1;
		[KSPField(guiActive = true, guiName = "Guidance Type ", guiActiveEditor = true)]
		public string guidanceLabel = "AAM";
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "CruiseAltitude"),
		 UI_FloatRange(minValue = 50f, maxValue = 1500f, stepIncrement = 50f, scene = UI_Scene.All)]
		public float cruiseAltitude = 500;


		public float timeToImpact;

		[KSPAction("Start Guidance")]
		public void AGStartGuidance(KSPActionParam param)
		{
			StartGuidance();
		}

		[KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Start Guidance", active = true)]
		public void StartGuidance()
		{
			if(hasFired)
			{
				return;
			}

			if(vessel.targetObject!=null && vessel.targetObject.GetVessel()!=null)
			{
				targetVessel = vessel.targetObject.GetVessel();
			}
			else if(parentVessel!=null && parentVessel.targetObject!=null && parentVessel.targetObject.GetVessel()!=null)
			{
				targetVessel = parentVessel.targetObject.GetVessel();
			}
			else
			{
				return;
			}

			if(!hasFired && targetVessel!=null)
			{
				hasFired = true;
				guidanceActive = true;
				vessel.OnFlyByWire += GuidanceSteer;
				vessel.SetReferenceTransform(part);
				GameObject velocityObject = new GameObject("velObject");
				velocityObject.transform.position = transform.position;
				velocityObject.transform.parent = transform;
				velocityTransform = velocityObject.transform;

				Events["StartGuidance"].guiActive = false;
				Misc.RefreshAssociatedWindows(part);

				vessel.OnJustAboutToBeDestroyed += RemoveGuidance;
				part.OnJustAboutToBeDestroyed += RemoveGuidance;
			}
		}




		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "GuidanceMode", active = true)]
		public void SwitchGuidanceMode()
		{
			guidanceMode++;
			if(guidanceMode > 3)
			{
				guidanceMode = 1;
			}

			RefreshGuidanceMode();
		}

		void RefreshGuidanceMode()
		{
			switch(guidanceMode)
			{
			case 1:
				guidanceLabel = "AAM";
				break;
			case 2:
				guidanceLabel = "AGM/STS";
				break;
			case 3:
				guidanceLabel = "Cruise";
				break;
			}
			
			Fields["cruiseAltitude"].guiActive = (guidanceMode == 3);
			Fields["cruiseAltitude"].guiActiveEditor = (guidanceMode == 3);
			
			
			Misc.RefreshAssociatedWindows(part);
		}

		
		public override void OnStart (PartModule.StartState state)
		{
			part.force_activate();
			vesselTransform = part.FindModelTransform("vesselTransform");
			if(vesselTransform!=null)
			{
				part.SetReferenceTransform(vesselTransform);
			}
			parentVessel = vessel;

			RefreshGuidanceMode();

		}

		void RemoveGuidance()
		{
			vessel.OnFlyByWire -= GuidanceSteer;
		}

		public void GuidanceSteer(FlightCtrlState s)
		{
			if(guidanceActive && targetVessel!=null && vessel!=null && vesselTransform!=null && velocityTransform!=null)
			{
				velocityTransform.rotation = Quaternion.LookRotation(vessel.srf_velocity, -vesselTransform.forward);
				Vector3 targetPosition = targetVessel.CoM;
				Vector3 localAngVel = vessel.angularVelocity;

				if(guidanceMode == 1)
				{
					targetPosition = MissileGuidance.GetAirToAirTarget(targetPosition, vessel.srf_velocity, vessel.acceleration, vessel, out timeToImpact);
				}
				else if(guidanceMode == 2)
				{
					targetPosition = MissileGuidance.GetAirToGroundTarget(targetPosition, vessel, 1.85f);
				}
				else
				{
					targetPosition = MissileGuidance.GetCruiseTarget(targetPosition, vessel, cruiseAltitude);
				}

				Vector3 targetDirection = velocityTransform.InverseTransformPoint(targetPosition).normalized;
				targetDirection = Vector3.RotateTowards(Vector3.forward, targetDirection, 15*Mathf.Deg2Rad, 0);


		
				float steerYaw = (steerMult * targetDirection.x) - (steerDamping * -localAngVel.z);
				float steerPitch = (steerMult * targetDirection.y) - (steerDamping * -localAngVel.x);

				s.yaw = Mathf.Clamp(steerYaw, -maxSteer, maxSteer);
				s.pitch = Mathf.Clamp(steerPitch, -maxSteer, maxSteer);

				s.mainThrottle = 1;
			}
		}
	}
}

