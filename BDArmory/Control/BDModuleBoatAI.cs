using System;
using System.Collections.Generic;
using BDArmory.Misc;
using BDArmory.UI;
using UnityEngine;

namespace BDArmory.Control
{
	public class BDModuleBoatAI : BDGenericAIBase, IBDAIControl
	{
		#region Declarations
		
		Vessel extendingTarget = null;

		Vector3d targetDirection;
		float targetVelocity; // the velocity the ship should target, not the velocity of its target

		AIUtils.MomentumController yawController = new AIUtils.MomentumController();

		float[] yawDerivatives = new float[7] { 0.001f, 0.001f, 0.001f, 0.001f, 0.001f, 0.001f, 0.001f };
		float[] pitchDerivatives = new float[2];
		float[] rollDerivatives = new float[4] { 0.05f, 0, 0, 0 };

		int collisionDetectionTicker = 0;
		Vector3? dodgeVector;

		Vector3 upDir;

		//settings
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cruise speed"),
			UI_FloatRange(minValue = 5f, maxValue = 200f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float CruiseSpeed = 40;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max speed"),
			UI_FloatRange(minValue = 5f, maxValue = 300f, stepIncrement = 1f, scene = UI_Scene.All)]
		public float MaxSpeed = 60;

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

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Turning"),
			UI_Toggle(enabledText = "Careful", disabledText = "Reckless")]
		public bool DriveCarefully = true;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Attack vector"),
			UI_Toggle(enabledText = "Broadside", disabledText = "Bow")]
		public bool BroadsideAttack = true;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Min engagement range"),
			UI_FloatRange(minValue = 200f, maxValue = 6000f, stepIncrement = 200f, scene = UI_Scene.All)]
		public float MinEngagementRange = 1000;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max engagement range"),
			UI_FloatRange(minValue = 1000f, maxValue = 8000f, stepIncrement = 500f, scene = UI_Scene.All)]
		public float MaxEngagementRange = 4000;
		#endregion

		#region RMB info in editor
		public override string GetInfo()
		{
			return @"
<b>Available settings</b>:
<b>Cruise speed</b> - the default speed at which it is safe to maneuver
<b>Max speed</b> - the maximum combat speed
<b>Max drift</b> - maximum allowed angle between facing and velocity vector
<b>Moving pitch</b> - the pitch level to maintain when moving at cruise speed
<b>Bank angle</b> - the limit on roll when turning, positive rolls into turns
<b>Steering</b> - can the ship turn with engines off
<b>Turning</b> - ""Careful"" will restrict yaw input when above cruise speed and instead slow down first
<b>Attack vector</b> - does the ship attack from the front or the sides
<b>Min engagement range</b> - AI will try to move away from oponents if closer than this range
<b>Max engagement range</b> - AI will prioritize getting closer over attacking when beyond this range
";
		}
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
			if (!vessel.Autopilot.Enabled)
				vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);

			// if we're not in water, cut throttle and panic
			if (!vessel.Splashed) return;


			targetVelocity = 0;
			targetDirection = vesselTransform.up;
			upDir = VectorUtils.GetUpDirection(vesselTransform.position);

			vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, weaponManager && targetVessel && !BDArmorySettings.PEACE_MODE
				&& (weaponManager.selectedWeapon != null || (vessel.CoM - targetVessel.CoM).sqrMagnitude < MaxEngagementRange * MaxEngagementRange));

			PilotLogic();

			AttitudeControl(s);
			AdjustThrottle(targetVelocity);
		}

		void PilotLogic()
		{
			// check for collisions, but not every frame
			if (collisionDetectionTicker == 0)
			{
				collisionDetectionTicker = 20;
				float predictMult = Mathf.Clamp(10 / MaxDrift, 1, 10);
				dodgeVector = PredictRunningAshore(10f * predictMult, 2f);
				List<Vessel>.Enumerator vs = BDATargetManager.LoadedVessels.GetEnumerator();
				while (dodgeVector == null && vs.MoveNext())
				{
					if (vs.Current == null || vs.Current == vessel) continue;
					if (!vs.Current.Splashed || vs.Current.FindPartModuleImplementing<IBDAIControl>()?.commandLeader?.vessel == vessel) continue;
					dodgeVector = PredictCollisionWithVessel(vs.Current, 5f * predictMult, 0.5f);
				}
				vs.Dispose();
				if (dodgeVector != null) DebugLine(dodgeVector.ToString());
			}
			else
				collisionDetectionTicker--;
			// avoid collisions if any are found
			if (dodgeVector != null)
			{
				targetVelocity = DriveCarefully ? CruiseSpeed : MaxSpeed;
				targetDirection = (Vector3)dodgeVector;
				SetStatus($"Avoiding Collision");
				return;
			}

			// Possible Improvement: check for incoming fire and try to dodge
			// though ships are probably too slow for that, generally, so for now just try to keep moving

			// check for enemy targets and engage
			// not checking for guard mode, because if guard mode is off now you can select a target manually and if it is of opposing team, the AI will try to engage while you can man the turrets
			if (weaponManager && targetVessel != null && !BDArmorySettings.PEACE_MODE)
			{
				Vector3 vecToTarget = targetVessel.CoM - vessel.CoM;
				float distance = vecToTarget.magnitude;
				// lead the target a bit, where 950f is a ballpark estimate of the average bullet velocity (gau 983, vulcan 950, .50 860)
				vecToTarget = AIUtils.PredictPosition(targetVessel, distance / 950f) - vessel.CoM;  

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

			const float targetRadius = 400f;
			targetDirection = Vector3.ProjectOnPlane(assignedPositionGeo - vesselTransform.position, upDir);
			if (targetDirection.sqrMagnitude > targetRadius * targetRadius)
			{
				targetVelocity = Mathf.Clamp(((float)targetDirection.magnitude - targetRadius / 2) / 5f, 0, command == PilotCommands.Attack ? MaxSpeed : CruiseSpeed);
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
			Vector3 yawTarget = Vector3.ProjectOnPlane(targetDirection, upDir);
			
			// limit "aoa" if we're moving
			if (vessel.horizontalSrfSpeed * 10 > CruiseSpeed)
				yawTarget = Vector3.RotateTowards(vessel.srf_velocity, yawTarget, MaxDrift * Mathf.Deg2Rad
					* (DriveCarefully ? Mathf.Clamp01((MaxSpeed - (float)vessel.srfSpeed) / (MaxSpeed - CruiseSpeed)) : 1), 0);

			float yawAngle = VectorUtils.SignedAngle(vesselTransform.up, yawTarget, vesselTransform.right);
			float pitchAngle = TargetPitch * Mathf.Clamp01((float)vessel.horizontalSrfSpeed / CruiseSpeed);
			float drift = VectorUtils.SignedAngle(vesselTransform.up, Vector3.ProjectOnPlane(vessel.GetSrfVelocity(), upDir), vesselTransform.right);

			var north = VectorUtils.GetNorthVector(vesselTransform.position, vessel.mainBody);
			float orientation = VectorUtils.SignedAngle(north, vesselTransform.up, Vector3.Cross(north, upDir));
			
			s.yaw = yawController.Update(orientation, yawAngle, vessel.horizontalSrfSpeed * 10 > CruiseSpeed);

			PitchControl(s, pitchAngle);
			RollControl(s, yawAngle, drift);

			if (DriveCarefully && Mathf.Abs(yawAngle) + Mathf.Abs(drift) > 5)
				targetVelocity = Mathf.Clamp(targetVelocity, 0, CruiseSpeed);
		}

		void SetYaw(FlightCtrlState s, float angle)
		{
			var north = VectorUtils.GetNorthVector(vesselTransform.position, vessel.mainBody);
			float orientation = VectorUtils.SignedAngle(north, vesselTransform.up, Vector3.Cross(north, upDir));
			float d1 = (orientation - yawDerivatives[2]); //first derivative per update
			if (Mathf.Abs(d1) > 180) d1 -= 360 * Mathf.Sign(d1); // angles
			d1 = d1 / Time.deltaTime; // normalize to seconds? that probably is seconds
			float d2 = d1 - yawDerivatives[3]; //second derivative
			float d3 = d2 - yawDerivatives[4]; //third derivative

			// calculate for how many frames we'd have to apply our current change in momentum to halt our momentum exactly when facing the target direction
			// if we have more frames left, continue yawing in the same direction, otherwise apply counterforce in the opposite direction

			if (angle * d1 < 0)
			{
				float timeToZero = 0; // not including timeSmall
				float timeSmall = d2 / yawDerivatives[1] * Mathf.Sign(d1);
				if (Mathf.Abs(d1) < yawDerivatives[6] - timeSmall * Mathf.Abs(d2) / 2)
				{
					timeToZero = 2 * Mathf.Sqrt((Mathf.Abs(d1) + timeSmall * Mathf.Abs(d2)) / yawDerivatives[1]);
					//DebugLine("Triangle yaw.");
					
				}
				else
				{
					timeToZero = yawDerivatives[5] + (Mathf.Abs(d1) + timeSmall * Mathf.Abs(d2) - yawDerivatives[6]) / yawDerivatives[0];
					//DebugLine("Trapezoid yaw.");
				}
				float angleChangeTillStop = Mathf.Abs(d1) * timeToZero * 2 + timeSmall * d2 * d2 / 3; // if I'm not missing anything, the first term should be divided by two, not multiplied
				                                                                                      // but since this works better, I'm probably missing something

				//DebugLine("timeToZero " + timeToZero + " angleToStop " + angleChangeTillStop + " timeSmall " + timeSmall);

				if (float.IsNaN(angleChangeTillStop))
					s.yaw = -Mathf.Sign(d1); // if timeSmall is more than area, cancel momentum
				else if (angleChangeTillStop >= Mathf.Abs(angle))
					s.yaw = -Mathf.Sign(angle);
				else
					s.yaw = Mathf.Sign(angle);
			}
			else
			{
				s.yaw = Mathf.Sign(angle); // if we're turning in the opposite side of the want we one, stop that
				//DebugLine("straightforward, turn the oppposite way");
			}

			//DebugLine("d2 " + d2 + " yawDer1 " + yawDerivatives[1] + " yawDer6 " + yawDerivatives[6] + " d1 " + d1 + " or " + orientation + " yawDer2 " + yawDerivatives[2] + " yawDer3 " + yawDerivatives[3]);

			// update derivatives
			const float decayFactor = 0.997f;
			if (vessel.horizontalSrfSpeed * 10 > CruiseSpeed) // so we don't get ridiculous 180 degree derivatives when accelerating from backwards drift
			{
				if (Mathf.Abs(d2) > yawDerivatives[0]) yawDerivatives[0] = Mathf.Abs(d2);
				else yawDerivatives[0] *= decayFactor;
				if (Mathf.Abs(d3) > yawDerivatives[1]) yawDerivatives[1] = Mathf.Abs(d3);
				else yawDerivatives[1] *= decayFactor;
				yawDerivatives[5] = yawDerivatives[0] / yawDerivatives[1] * 2; //time to full change
				yawDerivatives[6] = yawDerivatives[0] * yawDerivatives[5] / 2; //triangle 2 (x2)
			}

			yawDerivatives[2] = orientation;
			yawDerivatives[3] = d1;
			yawDerivatives[4] = d2;

			DebugLine("YawAngle " + angle + " maxD2 " + yawDerivatives[0] + " maxD3 " + yawDerivatives[1]);
		}

		void PitchControl(FlightCtrlState s, float angle)
		{
			float pitch = 90 - Vector3.Angle(vesselTransform.up, upDir);
			float error = angle - pitch;
			float change = pitch - pitchDerivatives[1];
			float targetChange = Mathf.Clamp(error / 512, -0.01f, 0.01f);

			float pitchOrder = Mathf.Clamp(pitchDerivatives[0] + Mathf.Clamp(targetChange - change, -0.1f, 0.1f) * 0.02f, -1, 1); // very basic - change pitch input slowly until we're at the right pitch

			if (float.IsNaN(pitchOrder) || vessel.horizontalSrfSpeed < CruiseSpeed / 10) pitchOrder = 0;

			pitchDerivatives[0] = pitchOrder;
			pitchDerivatives[1] = pitch;

			s.pitch = pitchOrder;
			//DebugLine(pitch+"PitchAngle " + angle.ToString() + " factor3 " + Mathf.Clamp(targetChange - change, -0.1f, 0.1f) + " retainedOrder " + pitchDerivatives[0] + "change" + change);
		}

		void RollControl(FlightCtrlState s, float yawAngle, float drift)
		{
			// we're gonna do this really simply (same way a person would do it):
			// if we're turning and over the bank angle, panic and apply max input to roll 
			// otherwise let SAS keep it stable
			// since this is a ship, center of drag should pretty much always be below the center of mass, so the physics of turning will work to roll ship outwards, so only considering it one way

			const float errorMargin = 4f; // this is how much deviation is still considered stable

			if (vessel.horizontalSrfSpeed * 10 < CruiseSpeed) return;

			float currentBank = VectorUtils.SignedAngle(-vesselTransform.forward, upDir, -vesselTransform.right);
			float rollMomentum = currentBank - rollDerivatives[2];
			//DebugLine("calculated drift " + drift + " bank " + currentBank + " bank limit " + BankAngle * Mathf.Clamp01(Mathf.Abs(drift) / MaxDrift));

			if (Mathf.Abs(yawAngle) < errorMargin && Mathf.Abs(drift) < errorMargin)
			{
				//stable state
				if (rollDerivatives[1] * currentBank > 0) //if overcorrected 
				{
					rollDerivatives[0] *= 0.5f;
					debugString.Append("Overcorrecting, reducing factor: ");
				}
				else if (rollDerivatives[1] * currentBank < 0 && rollMomentum * currentBank > 0 && Mathf.Abs(rollMomentum) > Mathf.Abs(rollDerivatives[3])) //if undercorrected
				{
					rollDerivatives[0] /= 0.99f;
					debugString.Append("Undercorrecting, increasing factor: ");
				}
					
				if (Mathf.Abs(currentBank) > errorMargin)
				{
					s.roll = -currentBank * rollDerivatives[0];
					rollDerivatives[1] = Mathf.Sign(-currentBank);
					DebugLine("Reverting roll: " + -currentBank * rollDerivatives[0] + " factor " + rollDerivatives[0]);
					return;
				}
			}
			else
			{
				rollDerivatives[1] = 0;
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

			rollDerivatives[2] = BankAngle;
			rollDerivatives[3] = rollMomentum;

			DebugLine("SAS roll control");
		}

		#endregion

		#region Autopilot helper functions

		public override bool CanEngage()
		{
			if (!vessel.Splashed) DebugLine(vessel.vesselName + " cannot engage: boat not in water");
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
				if (Vector3.SqrMagnitude(tPos - myPos) < 2500f)
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
				Vector3 testVector = AIUtils.PredictPosition(vessel, time) - vessel.CoM;
				Vector3 sideVector = Vector3.Cross(testVector, upDir).normalized * (float)vessel.srfSpeed;
				// unrolled loop, because I am lazy
				if (AIUtils.GetTerrainAltitude(vessel.CoM + Vector3.RotateTowards(testVector * 2, sideVector, 0.03f, 0), vessel.mainBody) > -minDepth)
					return -sideVector;
				if (AIUtils.GetTerrainAltitude(vessel.CoM + Vector3.RotateTowards(testVector * 2, -sideVector, 0.03f, 0), vessel.mainBody) > -minDepth)
					return sideVector;
				if (AIUtils.GetTerrainAltitude(vessel.CoM + sideVector + Vector3.RotateTowards(testVector, sideVector, 0.15f, 0), vessel.mainBody) > -minDepth)
					return -sideVector;
				if (AIUtils.GetTerrainAltitude(vessel.CoM - sideVector + Vector3.RotateTowards(testVector, -sideVector, 0.15f, 0), vessel.mainBody) > -minDepth)
					return sideVector;

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
