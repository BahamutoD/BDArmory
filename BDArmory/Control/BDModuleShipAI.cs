using System;
using System.Collections.Generic;
using System.Linq;
using BDArmory.Core.Extension;
using BDArmory.Misc;
using BDArmory.Parts;
using BDArmory.UI;
using UnityEngine;
using System.Text;

namespace BDArmory.Control
{
	public class BDModuleShipAI : PartModule, IBDAIControl
	{
		#region Declarations
		[KSPField(isPersistant = true)]
		public bool pilotEnabled { get; private set; }

		public MissileFire weaponManager { get; private set; }

		BDAirspeedControl speedController;
		Transform vesselTransform;

		Vector3d assignedPosition;

		public bool CanEngage() => vessel.Splashed;
		public bool IsValidDirectFireTarget(Vessel target) => (target?.Splashed ?? false) && !BroadsideAttack; //valid if splashed and using bow fire

		//settings
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max speed"),
			UI_FloatRange(minValue = 5f, maxValue = 400f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float MaxSpeed = 30;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max drift"),
			UI_FloatRange(minValue = 1f, maxValue = 180f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float MaxDrift = 30;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Moving pitch"),
			UI_FloatRange(minValue = -45f, maxValue = 45f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float TargetPitch = 0;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Bank angle"),
			UI_FloatRange(minValue = -45f, maxValue = 45f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float BankAngle = 0;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steering"),
			UI_Toggle(enabledText = "Powered", disabledText = "Passive")]
		public bool PoweredSteering = true;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Attack vector"),
			UI_Toggle(enabledText = "Broadside", disabledText = "Bow")]
		public bool BroadsideAttack = true;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Engagement range"),
			UI_FloatRange(minValue = 100f, maxValue = 10000f, stepIncrement = 100f, scene = UI_Scene.All)]
		public float EngagementRange = 2000;


		//wing commander
		public ModuleWingCommander commandLeader { get; private set; }
		PilotCommands command;
		public PilotCommands currentCommand => command;
		public bool isLeadingFormation { get; set; }
		public string currentStatus { get; private set; } = "Free";
		int commandFollowIndex;

		Vector3d commandGeoPos;
		public Vector3d commandGPS => commandGeoPos;
		public Vector3d commandPosition
		{
			get
			{
				return VectorUtils.GetWorldSurfacePostion(commandGeoPos, vessel.mainBody);
			}
			set
			{
				commandGeoPos = VectorUtils.WorldPositionToGeoCoords(value, vessel.mainBody);
			}
		}

		#endregion

		#region Unity events
		void Start()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				part.OnJustAboutToBeDestroyed += DeactivatePilot;
				vessel.OnJustAboutToBeDestroyed += DeactivatePilot;
				MissileFire.OnToggleTeam += OnToggleTeam;
				vesselTransform = vessel.ReferenceTransform;

				List<MissileFire>.Enumerator wms = vessel.FindPartModulesImplementing<MissileFire>().GetEnumerator();
				while (wms.MoveNext())
				{
					weaponManager = wms.Current;
					break;
				}
				wms.Dispose();

				if (pilotEnabled)
				{
					ActivatePilot();
				}
			}

			RefreshPartWindow();
		}

		void OnDestroy()
		{
			MissileFire.OnToggleTeam -= OnToggleTeam;
		}
		#endregion

		#region Pilot on/off

		public void ActivatePilot()
		{
			pilotEnabled = true;
			vessel.OnFlyByWire -= AutoPilot;
			vessel.OnFlyByWire += AutoPilot;

			if (!speedController)
			{
				speedController = gameObject.AddComponent<BDAirspeedControl>();
				speedController.vessel = vessel;
			}

			speedController.Activate();

			GameEvents.onVesselDestroy.Remove(RemoveAutopilot);
			GameEvents.onVesselDestroy.Add(RemoveAutopilot);

			assignedPosition = VectorUtils.WorldPositionToGeoCoords(vessel.ReferenceTransform.position, vessel.mainBody);

			RefreshPartWindow();
		}

		public void DeactivatePilot()
		{
			pilotEnabled = false;
			vessel.OnFlyByWire -= AutoPilot;

			RefreshPartWindow();
		}

		void RemoveAutopilot(Vessel v)
		{
			if (v == vessel)
			{
				v.OnFlyByWire -= AutoPilot;
			}
		}

		void RefreshPartWindow()
		{
			Events["TogglePilot"].guiName = pilotEnabled ? "Deactivate Pilot" : "Activate Pilot";
		}

		[KSPEvent(guiActive = true, guiName = "Toggle Pilot", active = true)]
		public void TogglePilot()
		{
			if (pilotEnabled)
			{
				DeactivatePilot();
			}
			else
			{
				ActivatePilot();
			}
		}

		[KSPAction("Activate Pilot")]
		public void AGActivatePilot(KSPActionParam param) => ActivatePilot();

		[KSPAction("Deactivate Pilot")]
		public void AGDeactivatePilot(KSPActionParam param) => DeactivatePilot();

		[KSPAction("Toggle Pilot")]
		public void AGTogglePilot(KSPActionParam param) => TogglePilot();
		#endregion

		#region Actual AI Pilot

		void AutoPilot(FlightCtrlState s)
		{
			if (!vessel || !vessel.transform || vessel.packed || !vessel.mainBody)
			{
				return;
			}
			vesselTransform = vessel.ReferenceTransform;

			AdjustThrottle(MaxSpeed);
		}

		void AdjustThrottle(float targetSpeed) => speedController.targetSpeed = targetSpeed;

		#endregion

		#region WingCommander

		public void ReleaseCommand()
		{
			if (!vessel || command == PilotCommands.Free) return;
			if (command == PilotCommands.Follow && commandLeader)
			{
				List<IBDAIControl>.Enumerator pilots = commandLeader.vessel.FindPartModulesImplementing<IBDAIControl>().GetEnumerator();
				while (pilots.MoveNext())
				{
					if (pilots.Current == null) continue;
					pilots.Current.isLeadingFormation = false;
				}
				pilots.Dispose();
				commandLeader = null;
			}
			Debug.Log(vessel.vesselName + " was released from command.");
			command = PilotCommands.Free;

			assignedPosition = VectorUtils.WorldPositionToGeoCoords(vesselTransform.position, vessel.mainBody);
		}

		public void CommandFollow(ModuleWingCommander leader, int followerIndex)
		{
			if (!pilotEnabled) return;
			if (leader == vessel || followerIndex < 0) return;

			Debug.Log(vessel.vesselName + " was commanded to follow.");
			command = PilotCommands.Follow;
			commandLeader = leader;
			commandFollowIndex = followerIndex;
			List<IBDAIControl>.Enumerator pilots = commandLeader.vessel.FindPartModulesImplementing<IBDAIControl>().GetEnumerator();
			while (pilots.MoveNext())
			{
				if (pilots.Current == null) continue;
				pilots.Current.isLeadingFormation = true;
			}
			pilots.Dispose();
		}

		public void CommandAG(KSPActionGroup ag)
		{
			if (!pilotEnabled) return;
			vessel.ActionGroups.ToggleGroup(ag);
		}

		public void CommandFlyTo(Vector3 gpsCoords)
		{
			if (!pilotEnabled) return;

			Debug.Log(vessel.vesselName + " was commanded to fly to.");
			assignedPosition = gpsCoords;
			commandGeoPos = gpsCoords;
			command = PilotCommands.FlyTo;
		}

		public void CommandAttack(Vector3 gpsCoords)
		{
			if (!pilotEnabled) return;

			Debug.Log(vessel.vesselName + " was commanded to attack.");
			assignedPosition = gpsCoords;
			commandGeoPos = gpsCoords;
			command = PilotCommands.Attack;
		}

		public void CommandTakeOff()
		{
			ActivatePilot();
		}

		void OnToggleTeam(MissileFire mf, BDArmorySettings.BDATeams team)
		{
			if (mf.vessel == vessel || (commandLeader && commandLeader.vessel == mf.vessel))
			{
				ReleaseCommand();
			}
		}
		#endregion
	}
}
