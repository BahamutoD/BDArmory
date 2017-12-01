using System;
using System.Collections.Generic;
using BDArmory.Misc;
using BDArmory.UI;
using UnityEngine;
using System.Text;

namespace BDArmory.Control
{
	/// <summary>
	/// A base class for implementing AI.
	/// Note: You do not have to use it, it is just for convenience, all the game cares about is that you implement the IBDAIControl interface.
	/// </summary>
	public abstract class BDGenericAIBase : PartModule, IBDAIControl
	{
		#region declarations
		[KSPField(isPersistant = true)]
		public bool pilotEnabled { get; protected set; }

		public MissileFire weaponManager { get; protected set; }

		/// <summary>
		/// The default is BDAirspeedControl. If you want to use something else, just override ActivatePilot  (and, potentially, DeactivatePilot), and make it use something else.
		/// </summary>
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

		//wing commander
		public ModuleWingCommander commandLeader { get; protected set; }
		protected PilotCommands command;
		public bool isLeadingFormation { get; set; }
		public string currentStatus { get; protected set; } = "Free";
		protected int commandFollowIndex;

		public PilotCommands currentCommand => command;
		public Vector3d commandGPS => assignedPosition;

		#endregion

		public abstract bool CanEngage();
		public abstract bool IsValidFixedWeaponTarget(Vessel target);

		/// <summary>
		/// This will be called every update and should run the autopilot logic.
		/// 
		/// For simple use cases:
		///		1. Engage your target (get in position to engage, shooting is done by guard mode)
		///		2. If no target, check command, and follow it
		///		Do this by setting s.pitch, s.yaw and s.roll.
		///		
		/// For advanced use cases you probably know what you're doing :P
		/// </summary>
		/// <param name="s">current flight control state</param>
		protected abstract void AutoPilot(FlightCtrlState s);

		// A small wrapper to make sure the autopilot does not do anything when it shouldn't
		private void autoPilot(FlightCtrlState s)
		{
			if (!vessel || !vessel.transform || vessel.packed || !vessel.mainBody)
				return;

			debugString.Length = 0;

			// generally other AI and guard mode expects this target to be engaged
			GetGuardTarget(); // get the guard target from weapon manager
			GetNonGuardTarget(); // if guard mode is off, get the UI target
			GetGuardNonTarget(); // pick a target if guard mode is on, but no target is selected, 
								 // though really targeting should be managed by the weaponManager, what if we pick an airplane while having only abrams cannons? :P
								 // (this is another reason why target selection is hardcoded into the base class, so changing this later is less of a mess :) )

			AutoPilot(s);
		}

		#region Pilot on/off

		public virtual void ActivatePilot()
		{
			pilotEnabled = true;
			vessel.OnFlyByWire -= autoPilot;
			vessel.OnFlyByWire += autoPilot;

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
			vessel.OnFlyByWire -= autoPilot;
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
				v.OnFlyByWire -= autoPilot;
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

		protected void GetGuardTarget()
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

		/// <summary>
		/// If guard mode is set but not target is selected, pick something
		/// </summary>
		protected virtual void GetGuardNonTarget()
		{
			if (weaponManager && weaponManager.guardMode && !targetVessel)
			{
				TargetInfo potentialTarget = BDATargetManager.GetLeastEngagedTarget(weaponManager);
				if (potentialTarget && potentialTarget.Vessel)
				{
					targetVessel = potentialTarget.Vessel;
				}
			}
		}

		/// <summary>
		/// If guard mode off, and UI target is of the opposing team, set it as target
		/// </summary>
		protected void GetNonGuardTarget()
		{
			if (weaponManager != null && !weaponManager.guardMode)
			{
				if (vessel.targetObject?.GetVessel()?.FindPartModuleImplementing<MissileFire>()?.team == !weaponManager.team)
					targetVessel = (Vessel)vessel.targetObject;
			}
		}

		/// <summary>
		/// Write some text to the debug field (the one on lower left when debug labels are on), followed by a newline.
		/// </summary>
		/// <param name="text">text to write</param>
		protected void DebugLine(string text)
		{
			debugString.Append(text);
			debugString.Append(Environment.NewLine);
		}

		protected void SetStatus(string text)
		{
			currentStatus = text;
			DebugLine(text);
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
