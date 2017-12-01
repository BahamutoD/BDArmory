using System;
using System.Collections.Generic;
using BDArmory.Misc;
using BDArmory.UI;
using UnityEngine;

namespace BDArmory.Control
{
	public class BDModuleShipAI : BDGenericAIBase, IBDAIControl
	{
		#region Declarations
		
		Vessel extendingTarget = null;

		Vector3d targetDirection;
		float targetVelocity;

		float[] yawDerivatives = new float[7];

		//settings
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cruise speed"),
			UI_FloatRange(minValue = 5f, maxValue = 200f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float CruiseSpeed = 50;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max speed"),
			UI_FloatRange(minValue = 5f, maxValue = 300f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float MaxSpeed = 100;

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

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steering"),
			UI_Toggle(enabledText = "Careful", disabledText = "Reckless")]
		public bool DriveCarefully = true;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Attack vector"),
			UI_Toggle(enabledText = "Broadside", disabledText = "Bow")]
		public bool BroadsideAttack = true;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Min engagement range"),
			UI_FloatRange(minValue = 200f, maxValue = 6000f, stepIncrement = 200f, scene = UI_Scene.All)]
		public float MinEngagementRange = 2000;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max engagement range"),
			UI_FloatRange(minValue = 1000f, maxValue = 8000f, stepIncrement = 500f, scene = UI_Scene.All)]
		public float MaxEngagementRange = 4000;

		#endregion

		#region Unity events

		protected override void OnGUI()
		{
			base.OnGUI();

			if (!pilotEnabled || !vessel.isActiveVessel) return;

			if (!BDArmorySettings.DRAW_DEBUG_LINES) return;
			if (command == PilotCommands.Follow)
			{
				BDGUIUtils.DrawLineBetweenWorldPositions(vesselTransform.position, assignedPositionGeo, 2, Color.red);
			}

			BDGUIUtils.DrawLineBetweenWorldPositions(vesselTransform.position, vesselTransform.position + targetDirection * 10f, 2, Color.blue);
			BDGUIUtils.DrawLineBetweenWorldPositions(vesselTransform.position + (0.05f * vesselTransform.right), vesselTransform.position + (0.05f * vesselTransform.right), 2, Color.green);
		}
		#endregion

		#region Actual AI Pilot

		protected override void AutoPilot(FlightCtrlState s)
		{
			targetVelocity = 0;
			targetDirection = vesselTransform.up;

			vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, weaponManager && targetVessel && !BDArmorySettings.PEACE_MODE 
				&& (weaponManager.selectedWeapon != null || (vessel.CoM - targetVessel.CoM).sqrMagnitude < MaxEngagementRange * MaxEngagementRange));

			// if we're not in water, cut throttle and panic
			if (!vessel.Splashed) return;

			PilotLogic();

			AttitudeControl(s);
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
					dodgeVector = PredictCollisionWithVessel(vs.Current, 5f * predictMult, 1f);
				}
				vs.Dispose();

				if (dodgeVector != null)
				{
					targetVelocity = MaxSpeed;
					targetDirection = (Vector3)dodgeVector;
					SetStatus($"Avoiding Collision");
					return;
				}
			}

			// PointForImprovement: check for incoming fire and try to dodge
			// though ships are probably too slow for that, generally, so for now just try to keep moving

			Vector3 upDir = VectorUtils.GetUpDirection(vesselTransform.position);

			// check for enemy targets and engage
			// not checking for guard mode, because if guard mode is off now you can select a target manually and if it is of opposing team, the AI will try to engage while you can man the turrets
			if (weaponManager && targetVessel != null && !BDArmorySettings.PEACE_MODE)
			{
				Vector3 vecToTarget = targetVessel.CoM - vessel.CoM;
				float distance = vecToTarget.magnitude;
				if (BroadsideAttack)
				{
					Vector3 sideVector = Vector3.Cross(vecToTarget, upDir); //find a vector perpendicular to direction to target
					sideVector *= Mathf.Sign(Vector3.Dot(vesselTransform.up, sideVector)); // pick a side for attack
					float sidestep = distance >= MaxEngagementRange ? Mathf.Clamp01((MaxEngagementRange - distance) / (CruiseSpeed * Mathf.Clamp(90 / MaxDrift, 0, 10)) + 1) / 2 : // direct to target to 45 degrees if over maxrange
						(distance <= MinEngagementRange ? 1.5f - distance / (MinEngagementRange * 2) : // 90 to 135 degrees if closer than minrange
						(MaxEngagementRange - distance) / ((MaxEngagementRange - MinEngagementRange) * 2) + 0.5f); // 45 to 90 degrees from maxrange to minrange 
					targetDirection = Vector3.LerpUnclamped(vecToTarget.normalized, sideVector.normalized, sidestep); // interpolate between the side vector and target direction vector based on sidestep
					targetVelocity = MaxSpeed;
					DebugLine("Broadside attack angle " + sidestep);
				}
				else // just point at target and go
				{
					if ((targetVessel.horizontalSrfSpeed < 10 || Vector3.Dot(Vector3.ProjectOnPlane(targetVessel.srf_vel_direction, upDir), vessel.up) < 0) //if target is stationary or we're facing in opposite directions
						&& (distance < MinEngagementRange || (distance < (MinEngagementRange*3 + MaxEngagementRange) / 4 //and too close together
						&& extendingTarget != null && targetVessel != null && extendingTarget == targetVessel))) 
					{
						extendingTarget = targetVessel;
						targetDirection = -vecToTarget; //extend
						targetVelocity = MaxSpeed;
						SetStatus($"Extending");
						return;
					}
					else
					{
						extendingTarget = null;
						targetDirection = Vector3.ProjectOnPlane(vecToTarget, upDir);
						if (Vector3.Dot(targetDirection, vesselTransform.up) < 0) targetVelocity = PoweredSteering ? MaxSpeed : 0; // if facing away from target
						else if (distance >= MaxEngagementRange || distance <= MinEngagementRange) targetVelocity = MaxSpeed;
						else targetVelocity = CruiseSpeed + (MaxSpeed - CruiseSpeed) * (distance - MinEngagementRange) / (MaxEngagementRange - MinEngagementRange); //slow down if inside engagement range to extend shooting opportunities
						targetVelocity = Mathf.Clamp(targetVelocity, PoweredSteering ? CruiseSpeed / 5 : 0, MaxSpeed); // maintain a bit of speed if using powered steering
					}
				}
				SetStatus($"Engaging target");
				return;
			}
			
			// follow
			if (command == PilotCommands.Follow)
			{
				Vector3 targetPosition = GetFormationPosition();
				Vector3 targetDistance = targetPosition - vesselTransform.position;
				if (Vector3.Dot(targetDistance, vesselTransform.up) < 0
					&& Vector3.ProjectOnPlane(targetDistance, upDir).sqrMagnitude < 250f * 250f
					&& Vector3.Angle(vesselTransform.up, commandLeader.vessel.srf_velocity) < 0.8f)
				{
					targetVelocity = (float)(commandLeader.vessel.horizontalSrfSpeed - (vesselTransform.position - targetPosition).magnitude / 15);
					targetDirection = Vector3.RotateTowards(Vector3.ProjectOnPlane(commandLeader.vessel.srf_vel_direction, upDir), targetDistance, 0.2f, 0);
				}
				else
				{
					targetVelocity = (float)(commandLeader.vessel.horizontalSrfSpeed + (vesselTransform.position - targetPosition).magnitude / 15);
					targetDirection = Vector3.ProjectOnPlane(targetDistance, upDir);
				}
				if (Vector3.Dot(targetDirection, vesselTransform.up) < 0 && !PoweredSteering) targetVelocity = 0;
				SetStatus($"Following");
				return;
			}

			// goto

			targetDirection = Vector3.ProjectOnPlane(assignedPositionGeo - vesselTransform.position, upDir);
			if (targetDirection.sqrMagnitude > 400f * 400f)
			{
				targetVelocity = command == PilotCommands.Attack ? MaxSpeed : CruiseSpeed;
				if (Vector3.Dot(targetDirection, vesselTransform.up) < 0 && !PoweredSteering) targetVelocity = 0;
				SetStatus($"Moving");
				return;
			}

			SetStatus($"Not doing anything in particular");
			targetDirection = vesselTransform.up;
		}

		void AdjustThrottle(float targetSpeed)
		{
			targetVelocity = Mathf.Clamp(targetVelocity, 0, MaxSpeed);

			if (float.IsNaN(targetSpeed)) //because yeah, I might have left division by zero in there somewhere
			{
				targetSpeed = CruiseSpeed;
				DebugLine("Target velocity NaN, set to CruiseSpeed.");
			}
			else
				DebugLine("Target velocity: " + targetVelocity);

			speedController.targetSpeed = targetSpeed;
		}

		void AttitudeControl(FlightCtrlState s)
		{
			var upDir = VectorUtils.GetUpDirection(vessel.CoM);
			Vector3 yawTarget = Vector3.ProjectOnPlane(targetDirection, upDir);
			
			// limit "aoa" if we're moving
			if (vessel.horizontalSrfSpeed * 10 > CruiseSpeed)
				yawTarget = Vector3.RotateTowards(vessel.srf_velocity, yawTarget, MaxDrift * Mathf.Deg2Rad
					* (DriveCarefully ? Mathf.Clamp01((MaxSpeed - (float)vessel.srfSpeed) / (MaxSpeed - CruiseSpeed)) : 1), 0);

			float yawAngle = VectorUtils.SignedAngle(vesselTransform.up, yawTarget, vesselTransform.right);
			float pitchAngle = TargetPitch * Mathf.Clamp01((float)vessel.horizontalSrfSpeed / CruiseSpeed);
			float drift = VectorUtils.SignedAngle(vesselTransform.up, Vector3.ProjectOnPlane(vessel.GetSrfVelocity(), upDir), vesselTransform.right);

			SetYaw(s, yawAngle);
			PitchControl(s, pitchAngle);
			RollControl(s, yawAngle, drift);

			if (DriveCarefully && Mathf.Abs(yawAngle) + Mathf.Abs(drift) > 5)
				targetVelocity = Mathf.Clamp(targetVelocity, 0, CruiseSpeed);
		}

		void SetYaw(FlightCtrlState s, float angle)
		{
			var north = VectorUtils.GetNorthVector(vesselTransform.position, vessel.mainBody);
			float orientation = VectorUtils.SignedAngle(north, vesselTransform.up, Vector3.Cross(north, VectorUtils.GetUpDirection(vesselTransform.position)));
			float d1 = orientation - yawDerivatives[2]; //first derivative
			float d2 = d1 - yawDerivatives[3]; //second derivative
			float d3 = d2 - yawDerivatives[4]; //third derivative

			// calculate for how many frames we'd have to apply our current change in momentum to halt our momentum exactly when facing the target direction
			// if we have more frames left, continue yawing in the same direction, otherwise apply counterforce in the opposite direction

			float timeToZero = 0; // not including timeSmall
			float timeSmall = d2 / yawDerivatives[1] * Mathf.Sign(d1);
			if (Mathf.Abs(d1) < yawDerivatives[6] - timeSmall * Mathf.Abs(d2) / 2)
				timeToZero = 2 * Mathf.Sqrt((Mathf.Abs(d1) + timeSmall * Mathf.Abs(d2)) / yawDerivatives[1]);
			else
				timeToZero = yawDerivatives[5] + (Mathf.Abs(d1) + timeSmall * Mathf.Abs(d2) - yawDerivatives[6]) / yawDerivatives[0];
			float angleChangeTillStop = d1 * timeToZero / 2 + timeSmall * d2 * d2 / 3;

			if (float.IsNaN(timeToZero) || angleChangeTillStop > Mathf.Abs(angle))
				s.yaw = -Mathf.Sign(angle);
			else
				s.yaw = Mathf.Sign(angle);


			// update derivatives
			const float safetyFactor = 0.9f; // a small margin to acount for favorable drag conditions and stuff
			if (Mathf.Abs(d2) * safetyFactor > yawDerivatives[0] || Mathf.Abs(d3) * safetyFactor > yawDerivatives[1])
			{
				if (Mathf.Abs(d2) * safetyFactor > yawDerivatives[0]) yawDerivatives[0] = Mathf.Abs(d2) * safetyFactor;
				if (Mathf.Abs(d3) * safetyFactor > yawDerivatives[1]) yawDerivatives[1] = Mathf.Abs(d3) * safetyFactor;
				yawDerivatives[5] = yawDerivatives[0] / yawDerivatives[1] * 2; //time to full change
				yawDerivatives[6] = yawDerivatives[0] * yawDerivatives[5] / 2; //triangle 2 (x2)
			}
				
			yawDerivatives[2] = orientation;
			yawDerivatives[3] = d1;
			yawDerivatives[4] = d2;

			DebugLine("YawAngle " + angle + " maxD2 " + yawDerivatives[0] + " maxD3 " + yawDerivatives[1]);
		}

		void RollControl(FlightCtrlState s, float yawAngle, float drift)
		{
			// we're gonna do this really simply (same way a person would do it):
			// if we're turning and over the bank angle, panic and apply max input to roll 
			// otherwise let SAS keep it stable
			// since this is a ship, center of drag should pretty much always be below the center of mass, so the physics of turning will work to roll ship outwards, so only considering it one way

			const float errorMargin = 2f; // this is how much deviation is still considered stable

			if (vessel.horizontalSrfSpeed * 10 < CruiseSpeed) return;

			var upDir = VectorUtils.GetUpDirection(vesselTransform.position);
			float currentBank = VectorUtils.SignedAngle(-vesselTransform.forward, upDir, -vesselTransform.right);
			//DebugLine("calculated drift " + drift + " bank " + currentBank + " bank limit " + BankAngle * Mathf.Clamp01(Mathf.Abs(drift) / MaxDrift));

			if (Mathf.Abs(yawAngle) < errorMargin && Mathf.Abs(drift) < errorMargin)
			{
				//stable state
				if (Mathf.Abs(currentBank) > errorMargin)
				{
					s.roll = -currentBank / Mathf.Abs(BankAngle);
					DebugLine("Reverting roll: " + -Math.Sign(currentBank));
					return;
				}
			}
			else
			{

				if (BankAngle > 0)
				{
					if (currentBank * drift > 0 || Mathf.Abs(currentBank) < BankAngle * Mathf.Clamp01(Mathf.Abs(drift) / MaxDrift))
					{
						s.roll = -Math.Sign(drift);
						DebugLine("Increasing roll: " + -Math.Sign(drift));
						return;
					}
				}
				else
				{
					if (currentBank * drift > 0 && -Mathf.Abs(currentBank) < BankAngle)
					{
						s.roll = -Math.Sign(drift);
						DebugLine("Reducing roll: " + -Math.Sign(drift));
						return;
					}
				}
			}
			DebugLine("SAS roll control");
		}

		#endregion

		#region Autopilot helper functions

		public override bool CanEngage()
		{
			if (!vessel.Splashed) DebugLine(vessel.vesselName + " cannot engage: ship not in water");
			return vessel.Splashed;
		}

		public override bool IsValidFixedWeaponTarget(Vessel target) => (target?.Splashed ?? false) && !BroadsideAttack; //valid if splashed and using bow fire

		/// <returns>null if no collision, dodge vector if one detected</returns>
		Vector3? PredictCollisionWithVessel(Vessel v, float maxTime, float interval)
		{
			//evasive will handle avoiding missiles
			if (v == weaponManager.incomingMissileVessel) return null;

			float time = Mathf.Min(0.5f, maxTime);
			while (time < maxTime)
			{
				Vector3 tPos = AIUtils.PredictPosition(v, time);
				Vector3 myPos = AIUtils.PredictPosition(vessel, time);
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
				const float minDepth = 10f;
				Vector3 myPos = AIUtils.PredictPosition(vessel, time);
				if (AIUtils.GetTerrainAltitude(vessel.CoM + Vector3.RotateTowards(myPos - vessel.CoM, vesselTransform.right, 0.05f, 0), vessel.mainBody) > -minDepth)
				{
					return -vesselTransform.right;
				}
				if (AIUtils.GetTerrainAltitude(vessel.CoM + Vector3.RotateTowards(myPos - vessel.CoM, -vesselTransform.right, 0.05f, 0), vessel.mainBody) > -minDepth)
				{
					return vesselTransform.right;
				}

				time = Mathf.MoveTowards(time, maxTime, interval);
			}
			return null;
		}

		#endregion

		#region WingCommander

		Vector3d GetFormationPosition()
		{
			return commandLeader.vessel.ReferenceTransform.TransformPoint(this.GetLocalFormationPosition(commandFollowIndex));
		}
		#endregion
	}
}
