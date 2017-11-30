using System;
using System.Collections.Generic;
using System.Linq;
using BDArmory.Core.Extension;
using BDArmory.Misc;
using BDArmory.UI;
using UnityEngine;
using System.Text;

namespace BDArmory.Control
{
	public abstract class BDGenericAIBase : PartModule, IBDAIControl
	{
		[KSPField(isPersistant = true)]
		public bool pilotEnabled { get; protected set; }

		public MissileFire weaponManager { get; protected set; }
		protected BDAirspeedControl speedController;

		protected Transform vesselTransform => vessel.ReferenceTransform;
		protected StringBuilder debugString = new StringBuilder(); 

		protected Vessel targetVessel;

		protected Vector3d assignedPosition;

		public Vector3d assignedPositionGeo
		{
			get
			{
				return VectorUtils.GetWorldSurfacePostion(assignedPosition, vessel.mainBody);
			}
			protected set
			{
				assignedPosition = VectorUtils.WorldPositionToGeoCoords(value, vessel.mainBody);
			}
		}

		public abstract bool CanEngage();
		public abstract bool IsValidFixedWeaponTarget(Vessel target);

		//wing commander
		public ModuleWingCommander commandLeader { get; protected set; }
		protected PilotCommands command;
		public bool isLeadingFormation { get; set; }
		public string currentStatus { get; protected set; } = "Free";
		protected int commandFollowIndex;

		public PilotCommands currentCommand => command;
		public Vector3d commandGPS => assignedPosition;



		protected abstract void AutoPilot(FlightCtrlState s);

		#region Pilot on/off

		public virtual void ActivatePilot()
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

			assignedPositionGeo = vessel.ReferenceTransform.position;

			RefreshPartWindow();
		}

		public virtual void DeactivatePilot()
		{
			pilotEnabled = false;
			vessel.OnFlyByWire -= AutoPilot;
			RefreshPartWindow();

			if (speedController)
			{
				speedController.Deactivate();
			}
		}

		protected void RemoveAutopilot(Vessel v)
		{
			if (v == vessel)
			{
				v.OnFlyByWire -= AutoPilot;
			}
		}

		protected void RefreshPartWindow()
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

		#region events

		protected virtual void Start()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				part.OnJustAboutToBeDestroyed += DeactivatePilot;
				vessel.OnJustAboutToBeDestroyed += DeactivatePilot;
				MissileFire.OnToggleTeam += OnToggleTeam;

				UpdateWeaponManager();

				if (pilotEnabled)
				{
					ActivatePilot();
				}
			}

			RefreshPartWindow();
		}

		protected virtual void OnDestroy()
		{
			MissileFire.OnToggleTeam -= OnToggleTeam;
		}

		protected virtual void OnGUI()
		{
			if (!pilotEnabled || !vessel.isActiveVessel) return;
			if (BDArmorySettings.DRAW_DEBUG_LABELS)
			{
				GUI.Label(new Rect(200, Screen.height - 200, 400, 400), vessel.name + ":" + debugString.ToString());
			}
		}

		protected virtual void OnToggleTeam(MissileFire mf, BDArmorySettings.BDATeams team)
		{
			if (mf.vessel == vessel || (commandLeader && commandLeader.vessel == mf.vessel))
			{
				ReleaseCommand();
			}
		}

		#endregion

		#region utilities
		protected void UpdateWeaponManager()
		{
			List<MissileFire>.Enumerator mfs = vessel.FindPartModulesImplementing<MissileFire>().GetEnumerator();
			while (mfs.MoveNext())
			{
				if (mfs.Current == null) continue;

				weaponManager = mfs.Current;
				mfs.Current.AI = this;

				break;
			}
			mfs.Dispose();
		}

		protected virtual void GetGuardTarget()
		{
			if (weaponManager == null || weaponManager.vessel == vessel)
				UpdateWeaponManager();
			if (weaponManager != null && weaponManager.vessel == vessel)
			{
				if (weaponManager.guardMode && weaponManager.currentTarget != null)
				{
					targetVessel = weaponManager.currentTarget.Vessel;
				}
				else
				{
					targetVessel = null;
				}
				weaponManager.AI = this;
				return;
			}
		}
		#endregion

		#region WingCommander

		public virtual void ReleaseCommand()
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

			assignedPositionGeo = vesselTransform.position;
		}

		public virtual void CommandFollow(ModuleWingCommander leader, int followerIndex)
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

		public virtual void CommandAG(KSPActionGroup ag)
		{
			if (!pilotEnabled) return;
			vessel.ActionGroups.ToggleGroup(ag);
		}

		public virtual void CommandFlyTo(Vector3 gpsCoords)
		{
			if (!pilotEnabled) return;

			Debug.Log(vessel.vesselName + " was commanded to go to.");
			assignedPosition = gpsCoords;
			command = PilotCommands.FlyTo;
		}

		public virtual void CommandAttack(Vector3 gpsCoords)
		{
			if (!pilotEnabled) return;

			Debug.Log(vessel.vesselName + " was commanded to attack.");
			assignedPosition = gpsCoords;
			command = PilotCommands.Attack;
		}

		public virtual void CommandTakeOff()
		{
			ActivatePilot();
		}

		#endregion
	}
}
