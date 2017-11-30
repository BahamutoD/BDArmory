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
	public class BDModuleShipAI : BDGenericAIBase, IBDAIControl
	{
		#region Declarations
		
		Vessel extendingTarget = null;

		Vector3d targetDirection;
		float targetVelocity;

		//max second derivative, max third derivative, previous orientation, previous momentum, previous momentum derivative, previous command -- pitch, yaw, roll
		Vector3[] derivatives = new Vector3[6];

		public override bool CanEngage() => vessel.Splashed;
		public override bool IsValidFixedWeaponTarget(Vessel target) => (target?.Splashed ?? false) && !BroadsideAttack; //valid if splashed and using bow fire

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

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steer Limiter"),
			UI_FloatRange(minValue = .1f, maxValue = 1f, stepIncrement = .05f, scene = UI_Scene.All)]
		public float maxSteer = 1;

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
			if (!vessel || !vessel.transform || vessel.packed || !vessel.mainBody)
			{
				return;
			}
			debugString.Length = 0;
			debugString.Append(Environment.NewLine);

			targetVelocity = 0;
			targetDirection = vesselTransform.up;
			vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);

			GetGuardTarget(); // get the guard target from weapon manager
			GetNonGuardTarget(); // if guard mode is off, get the UI target
			GetGuardNonTarget(); // pick a target if guard mode is on, but no target is selected, 
								 // though really targeting should be managed by the weaponManager, what if we pick an airplane while having only abrams cannons? :P

			vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, weaponManager && targetVessel && !BDArmorySettings.PEACE_MODE 
				&& (weaponManager.selectedWeapon != null || (vessel.CoM - targetVessel.CoM).sqrMagnitude < MaxEngagementRange * MaxEngagementRange));

			// if we're not in water, cut throttle and panic
			if (!vessel.Splashed) return;

			PilotLogic();

			targetVelocity = Mathf.Clamp(targetVelocity, 0, MaxSpeed);
			debugString.Append("target velocity: " + targetVelocity);

			SetYaw(s, targetDirection);
			SetPitch(s);
			SetRoll(s);

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
					currentStatus = "AvoidCollision";
					debugString.Append($"Avoiding Collision");
					debugString.Append(Environment.NewLine);
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
					debugString.Append("Broadside attack angle " + sidestep);
					debugString.Append(Environment.NewLine);
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
						currentStatus = "Extending";
						debugString.Append($"Extending");
						debugString.Append(Environment.NewLine);
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
				currentStatus = "Engaging target";
				debugString.Append($"Engaging target");
				debugString.Append(Environment.NewLine);
				return;
			}

			// follow
			if (command == PilotCommands.Follow)
			{
				Vector3 targetPosition = GetFormationPosition();
				Vector3 targetDistance = targetPosition - vesselTransform.position;
				if (Vector3.Dot(targetDistance, vesselTransform.up) < 0
					&& Vector3.ProjectOnPlane(targetDistance, upDir).sqrMagnitude < 250f * 250f
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
				if (Vector3.Dot(targetDirection, vesselTransform.up) < 0 && !PoweredSteering) targetVelocity = 0;
				currentStatus = "Following";
				debugString.Append($"Following");
				debugString.Append(Environment.NewLine);
				return;
			}

			// goto

			targetDirection = Vector3.ProjectOnPlane(assignedPositionGeo - vesselTransform.position, upDir);
			if (targetDirection.sqrMagnitude > 500f * 500f)
			{
				targetVelocity = command == PilotCommands.Attack ? MaxSpeed : CruiseSpeed;
				if (Vector3.Dot(targetDirection, vesselTransform.up) < 0 && !PoweredSteering) targetVelocity = 0;
				currentStatus = "Moving";
				debugString.Append($"Moving");
				debugString.Append(Environment.NewLine);
				return;
			}

			currentStatus = "Free";
			debugString.Append($"Not doing anything in particular");
			debugString.Append(Environment.NewLine);
			targetDirection = vesselTransform.up;
		}

		void AdjustThrottle(float targetSpeed)
		{
			if (float.IsNaN(targetSpeed)) //because yeah, I might have left division by zero in there somewhere
			{
				targetSpeed = CruiseSpeed;
				debugString.Append("Narrowly avoided setting speed to NaN");
				debugString.Append(Environment.NewLine);
			}
			speedController.targetSpeed = targetSpeed;
		} 
			

		void SetYaw(FlightCtrlState s, Vector3 target)
		{
			Vector3 yawTarget = Vector3.ProjectOnPlane(target, vesselTransform.forward);
			if (vessel.horizontalSrfSpeed * 10 > CruiseSpeed)
				yawTarget = Vector3.RotateTowards(vessel.srf_velocity, yawTarget, MaxDrift*Mathf.Deg2Rad, 0); // limit "aoa"
			float angle = VectorUtils.SignedAngle(vesselTransform.up, yawTarget, vesselTransform.right);
			var north = VectorUtils.GetNorthVector(vesselTransform.position, vessel.mainBody);
			float orientation = VectorUtils.SignedAngle(north, vesselTransform.up, Vector3.Cross(north, VectorUtils.GetUpDirection(vesselTransform.position)));
			float yawMomentum = orientation - derivatives[2].y;
			float d2 = Math.Abs(Math.Abs(yawMomentum) - derivatives[3].y);
			//float d3 = d2 - derivatives[4].y;

			// calculate for how many frames we'd have to apply our current change in momentum to halt our momentum exactly when facing the target direction
			// if we have more frames left, continue yawing in the same direction, otherwise apply counterforce in the opposite direction
			// this pretty much guarantees that we're applying non-max yaw for single frames only
			// and completely disregards how fast we can change our yaw (that's the commented out parts)
			// but works rather well for boats which use gimballed engines and gyroscopes which change force instantly
			float yawOrder = Mathf.Clamp((Mathf.Sqrt(Math.Abs(angle / derivatives[0].y))-1)*Math.Sign(angle) + (yawMomentum / derivatives[0].y), -1, 1);
			if (float.IsNaN(yawOrder)) yawOrder = 1; // division by zero :(

			// update derivatives
			if (derivatives[5].y > 0.2f)
			{
				//derivatives[1].y = derivatives[1].y * 0.95f + d3 / derivatives[5].y * 0.05f;
				derivatives[0].y = derivatives[0].y * 0.95f + d2 / derivatives[5].y * 0.05f;
			}
			derivatives[2].y = orientation;
			//derivatives[4].y = d2;
			derivatives[3].y = Math.Abs(yawMomentum);
			derivatives[5].y = Math.Abs(yawOrder);

			// set yaw
			s.yaw = yawOrder * maxSteer;

			debugString.Append("YawAngle " + angle.ToString() + " momentum " + yawMomentum.ToString() + " derivative " + derivatives[0].y.ToString() + " order " + yawOrder.ToString());
			debugString.Append(Environment.NewLine);
		}

		void SetPitch(FlightCtrlState s)
		{
			float angle = TargetPitch * Mathf.Clamp((float)vessel.horizontalSrfSpeed / CruiseSpeed, 0, 1);
			float pitch = 90 - Vector3.Angle(vesselTransform.up, VectorUtils.GetUpDirection(vesselTransform.position));
			float error = angle - pitch;
			float change = pitch - derivatives[2].x;
			float targetChange = Mathf.Clamp(error / 512, -0.01f, 0.01f);

			float pitchOrder = Mathf.Clamp(derivatives[1].x + Mathf.Clamp(targetChange - change, -0.1f, 0.1f) * 0.1f, -1, 1); // very basic - change pitch input slowly until we're at the right pitch

			if (float.IsNaN(pitchOrder) || vessel.horizontalSrfSpeed < CruiseSpeed / 10) pitchOrder = 0;

			derivatives[1].x = pitchOrder;
			derivatives[2].x = pitch;

			s.pitch = pitchOrder * maxSteer;
			//debugString.Append(pitch+"PitchAngle " + angle.ToString() + " factor3 " + Mathf.Clamp(targetChange - change, -0.1f, 0.1f) + " retainedOrder " + derivatives[1].x + "change" + change);
			//debugString.Append(Environment.NewLine);
		}

		void SetRoll(FlightCtrlState s)
		{
			float angleRatio = Mathf.Clamp(VectorUtils.SignedAngle(vessel.GetSrfVelocity(), vesselTransform.up, vesselTransform.right) / (MaxDrift), -1, 1);
			float angle = BankAngle * angleRatio;
			float roll = VectorUtils.SignedAngle(VectorUtils.GetUpDirection(vesselTransform.position), -vesselTransform.forward, vesselTransform.right);
			float error = angle - roll;
			float change = roll - derivatives[2].z;

			if (vessel.horizontalSrfSpeed > CruiseSpeed / 5f)
			{
				// stable state factor
				if (change * error > 0.25f)
				{
					if (error * angle >= 0)
						derivatives[0].z *= 0.99f;
					else
						derivatives[0].z += 0.003f;
				}
				// change factor
				if (Mathf.Abs(error) > 2)
				{
					const int oF = 20; // let the compiler figure them out
					const int uF = 100;
					if (oF * change + oF * (oF - 1) / 2 * (change - derivatives[3].z) > error) 
						derivatives[1].z *= 0.95f;
					if (uF * change + uF * (uF - 1) / 2 * (change - derivatives[3].z) > error)
						derivatives[1].z += 0.01f;
				}
			}

			float rollOrder = Mathf.Clamp(derivatives[0].z * angleRatio + derivatives[1].z * error * Mathf.Abs(error), -1, 1);
			if (float.IsNaN(rollOrder)) rollOrder = 0;

			derivatives[2].z = roll;
			derivatives[3].z = change;

			s.roll = rollOrder * maxSteer;
			//debugString.Append("BankAngle " + angle.ToString() + " roll " + roll + " factor1 " + derivatives[0].z + " factor2 " + derivatives[1].z);
			//debugString.Append(Environment.NewLine);
		}

		#endregion

		#region Autopilot helper functions

		/// <summary>
		/// If guard mode off, and UI target is of the opposing team, set it as target
		/// </summary>
		void GetNonGuardTarget()
		{
			if (weaponManager != null && !weaponManager.guardMode)
			{
				if (vessel.targetObject != null && vessel.targetObject is Vessel 
					&& BDATargetManager.TargetDatabase[BDATargetManager.BoolToTeam(!weaponManager.team)].FirstOrDefault(x => x.weaponManager.vessel == (Vessel)vessel.targetObject))
					targetVessel = (Vessel)vessel.targetObject;
			}
		}

		void GetGuardNonTarget()
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
				if (AIUtils.GetAltitude(vessel.CoM + Vector3.RotateTowards(myPos - vessel.CoM, vesselTransform.right, 0.05f, 0), vessel.mainBody) > -minDepth)
				{
					return -vesselTransform.right;
				}
				if (AIUtils.GetAltitude(vessel.CoM + Vector3.RotateTowards(myPos - vessel.CoM, -vesselTransform.right, 0.05f, 0), vessel.mainBody) > -minDepth)
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
