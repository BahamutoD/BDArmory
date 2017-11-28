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
		StringBuilder debugString = new StringBuilder();

		BDAirspeedControl speedController;
		Transform vesselTransform;

		Vector3d assignedPosition;
		Vessel targetVessel;

		Vector3d targetDirection;
		float targetVelocity;

		//max second derivative, max third derivative, previous momentum, previous momentum derivative, previous command -- pitch, yaw, roll
		Vector3[] derivatives = new Vector3[5];

		public bool CanEngage() => vessel.Splashed;
		public bool IsValidDirectFireTarget(Vessel target) => (target?.Splashed ?? false) && !BroadsideAttack; //valid if splashed and using bow fire

		//settings
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cruise speed"),
			UI_FloatRange(minValue = 5f, maxValue = 200f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float CruiseSpeed = 50;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max speed"),
			UI_FloatRange(minValue = 5f, maxValue = 300f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float MaxSpeed = 100;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max drift"),
			UI_FloatRange(minValue = 1f, maxValue = 180f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float MaxDrift = 180;

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

		void OnGUI()
		{
			if (!pilotEnabled || !vessel.isActiveVessel) return;
			if (BDArmorySettings.DRAW_DEBUG_LABELS)
			{
				GUI.Label(new Rect(200, Screen.height - 200, 400, 400), vessel.name + ":" + debugString.ToString());
			}

			if (!BDArmorySettings.DRAW_DEBUG_LINES) return;
			if (command == PilotCommands.Follow)
			{
				BDGUIUtils.DrawLineBetweenWorldPositions(vesselTransform.position, commandPosition, 2, Color.red);
			}

			BDGUIUtils.DrawLineBetweenWorldPositions(vesselTransform.position, vesselTransform.position + targetDirection * 10f, 2, Color.blue);
			BDGUIUtils.DrawLineBetweenWorldPositions(vesselTransform.position + (0.05f * vesselTransform.right), vesselTransform.position + (0.05f * vesselTransform.right), 2, Color.green);
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

			if (speedController)
			{
				speedController.Deactivate();
			}
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
			debugString.Length = 0;
			debugString.Append(Environment.NewLine);

			targetVelocity = 0;
			targetDirection = vesselTransform.up;
			vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);

			GetGuardTarget();

			// if we're not in water, cut throttle and panic
			if (!vessel.Splashed) return;

			PilotLogic();

			targetVelocity = Mathf.Clamp(targetVelocity, 0, MaxSpeed);

			TargetYaw(s, targetDirection);
			AdjustThrottle(targetVelocity);
		}

		void PilotLogic()
		{
			// check for collisions
			{
				float predictMult = Mathf.Clamp(10 / MaxDrift, 1, 10);
				Vector3? dodgeVector = PredictRunningAshore(10f * predictMult, 2f);
				List<Vessel>.Enumerator vs = BDATargetManager.LoadedVessels.GetEnumerator();
				while (dodgeVector == null && vs.MoveNext())
				{
					if (vs.Current == null || vs.Current == vessel) continue;
					if (!vs.Current.Splashed || (command == PilotCommands.Follow && vs.Current == commandLeader)) continue;
					dodgeVector = PredictCollisionWithVessel(vs.Current, 2.5f * predictMult, 0.5f);
				}
				vs.Dispose();

				if (dodgeVector != null)
				{
					targetVelocity = MaxSpeed;
					targetDirection = (Vector3)dodgeVector;
					currentStatus = "AvoidCollision";
					debugString.Append($"Avoiding Collision");
					debugString.Append(Environment.NewLine);
					return;
				}
			}

			// TODO: check for incoming fire and try to dodge

			Vector3 upDir = VectorUtils.GetUpDirection(vesselTransform.position);

			// if guard mode on, check for enemy targets and engage
			if (weaponManager && weaponManager.guardMode && 
				(BDATargetManager.TargetDatabase[BDATargetManager.BoolToTeam(weaponManager.team)].Count == 0 || BDArmorySettings.PEACE_MODE))
			{

			}

			// follow
			
			if (command == PilotCommands.Follow)
			{
				Vector3 targetPosition = GetFormationPosition();
				Vector3 targetDistance = targetPosition - vesselTransform.position;
				if (Vector3.Dot(targetDistance, vesselTransform.up) < 0 
					&& Vector3.ProjectOnPlane(targetDistance, upDir).sqrMagnitude < 250f*250f 
					&& Vector3.Angle(vesselTransform.up, commandLeader.vessel.Velocity()) < 0.8f)
				{
					targetVelocity = (float)(commandLeader.vessel.horizontalSrfSpeed - (vesselTransform.position - targetPosition).magnitude / 15);
					targetDirection = Vector3.RotateTowards(Vector3.ProjectOnPlane(commandLeader.vessel.srf_vel_direction, upDir), targetDistance, 0.2f, 0);
				}
				else
				{
					targetVelocity = (float)(commandLeader.vessel.horizontalSrfSpeed + (vesselTransform.position - targetPosition).magnitude / 15);
					targetDirection = Vector3.ProjectOnPlane(targetDistance, upDir);
				}
				return;
			}

			// goto

			targetDirection = Vector3.ProjectOnPlane(VectorUtils.GetWorldSurfacePostion(assignedPosition, vessel.mainBody) - vesselTransform.position, upDir);
			if (targetDirection.sqrMagnitude > 500f * 500f)
			{
				targetVelocity = command == PilotCommands.Attack ? MaxSpeed : CruiseSpeed;
				return;
			}
			
			targetDirection = vesselTransform.up;
		}

		void AdjustThrottle(float targetSpeed) => speedController.targetSpeed = targetSpeed;

		void TargetYaw(FlightCtrlState s, Vector3 target)
		{
			Vector3 yawTarget = Vector3.ProjectOnPlane(target, vesselTransform.forward);
			yawTarget = Vector3.RotateTowards(vessel.srf_velocity, yawTarget, MaxDrift, 0); // limit "aoa"
			float angle = VectorUtils.SignedAngle(vesselTransform.up, yawTarget, vesselTransform.right);
			float yawMomentum = vesselTransform.localRotation.Yaw();
			float d2 = Math.Abs(Math.Abs(yawMomentum) - derivatives[2].y);
			float d3 = d2 - derivatives[3].y;

			float yawOrder = Mathf.Clamp((angle * 2 / (derivatives[0].y * (derivatives[0].y + 1)) - yawMomentum) / derivatives[0].y, -1, 1);

			// update derivatives
			if (derivatives[4].y > 0.2f)
			{
				derivatives[1].y = derivatives[1].y * 0.9f + d3 / derivatives[4].y * 0.1f;
				derivatives[0].y = derivatives[0].y * 0.9f + derivatives[3].y / derivatives[4].y * 0.1f;
				debugString.Append("Derivatives: " + derivatives[3].y + " " + derivatives[4].y);
				debugString.Append(Environment.NewLine);
			}
			derivatives[3].y = d2;
			derivatives[2].y = Math.Abs(yawMomentum);
			derivatives[4].y = Math.Abs(yawOrder);

			// set yaw
			s.yaw = yawOrder;

			debugString.Append("YawAngle " + angle.ToString()+" momentum "+yawMomentum.ToString()+" derivative "+derivatives[0].y.ToString()+" order "+yawOrder.ToString());
			debugString.Append(Environment.NewLine);
		}

		#endregion

		#region Autopilot helper functions

		void GetGuardTarget()
		{
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
			else
			{
				List<MissileFire>.Enumerator mfs = vessel.FindPartModulesImplementing<MissileFire>().GetEnumerator();
				while (mfs.MoveNext())
				{
					if (mfs.Current == null) continue;
					targetVessel = mfs.Current.currentTarget != null
						? mfs.Current.currentTarget.Vessel
						: null;

					weaponManager = mfs.Current;
					mfs.Current.AI = this;

					return;
				}
				mfs.Dispose();
			}
		}

		/// <returns>null if no collision, dodge vector if one detected</returns>
		Vector3? PredictCollisionWithVessel(Vessel v, float maxTime, float interval)
		{
			//evasive will handle avoiding missiles
			if (v == weaponManager.incomingMissileVessel) return null;

			float time = Mathf.Min(0.5f, maxTime);
			while (time < maxTime)
			{
				Vector3 tPos = PredictPosition(v, time);
				Vector3 myPos = PredictPosition(vessel, time);
				if (Vector3.SqrMagnitude(tPos - myPos) < 2500f) //changed this variable though
				{
					return Vector3.Dot(tPos - myPos, vesselTransform.right) > 0 ? -vesselTransform.right : vesselTransform.right;
				}

				time = Mathf.MoveTowards(time, maxTime, interval);
			}

			return null;
		}

		/// <returns>null if no collision, dodge vector if one detected</returns>
		Vector3? PredictRunningAshore(float maxTime, float interval)
		{
			float time = Mathf.Min(0.5f, maxTime);
			while (time < maxTime)
			{
				Vector3 myPos = PredictPosition(vessel, time);
				if (GetAltitude(vessel.CoM + Vector3.RotateTowards(myPos - vessel.CoM, vesselTransform.right, 0.02f, 0)) > -5f)
				{
					return -vesselTransform.right;
				}
				if (GetAltitude(vessel.CoM + Vector3.RotateTowards(myPos - vessel.CoM, -vesselTransform.right, 0.02f, 0)) > -5f)
				{
					return vesselTransform.right;
				}

				time = Mathf.MoveTowards(time, maxTime, interval);
			}
			return null;
		}

		Vector3 PredictPosition(Vessel v, float time)
		{
			Vector3 pos = v.CoM;
			pos += v.Velocity() * time;
			pos += 0.5f * v.acceleration * time * time;
			return pos;
		}

		float GetAltitude(Vector3 position) => (float)vessel.mainBody.TerrainAltitude(vessel.mainBody.GetLatitude(position), vessel.mainBody.GetLongitude(position), true);

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

		Vector3d GetFormationPosition()
		{
			return commandLeader.vessel.ReferenceTransform.TransformPoint(GetLocalFormationPosition(commandFollowIndex));
		}

		Vector3d GetLocalFormationPosition(int index)
		{
			float indexF = (float)index;
			indexF++;

			double rightSign = indexF % 2 == 0 ? -1 : 1;
			double positionFactor = Math.Ceiling(indexF / 2);
			double spread = commandLeader.spread;
			double lag = commandLeader.lag;

			double right = rightSign * positionFactor * spread;
			double back = positionFactor * lag * -1;

			return new Vector3d(right, back, 0);
		}
		#endregion
	}
}
