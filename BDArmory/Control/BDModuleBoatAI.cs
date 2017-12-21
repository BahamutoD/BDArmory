using BDArmory.Misc;
using BDArmory.UI;
using BDArmory.Core;
using UnityEngine;

namespace BDArmory.Control
{
	public class BDModuleBoatAI : BDGenericAIBase, IBDAIControl
	{
		#region Declarations
		
		Vessel extendingTarget = null;

		Vector3 targetDirection;
		float targetVelocity; // the velocity the ship should target, not the velocity of its target

		int collisionDetectionTicker = 0;
		Vector3? dodgeVector;
		float weaveAdjustment = 0;
		float weaveDirection = 1;
		const float weaveLimit = 10;
		const float weaveFactor = 3.5f;

		Vector3 upDir;

		AIUtils.TraversabilityMatrix pathingMatrix;

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

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steer Factor"),
			UI_FloatRange(minValue = 0.1f, maxValue = 20f, stepIncrement = .1f, scene = UI_Scene.All)]
		public float steerMult = 6;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steer Damping"),
			UI_FloatRange(minValue = 1f, maxValue = 8f, stepIncrement = 0.1f, scene = UI_Scene.All)]
		public float steerDamping = 3;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steering"),
			UI_Toggle(enabledText = "Powered", disabledText = "Passive")]
		public bool PoweredSteering = true;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Attack vector"),
			UI_Toggle(enabledText = "Broadside", disabledText = "Bow")]
		public bool BroadsideAttack = true;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Min engagement range"),
			UI_FloatRange(minValue = 200f, maxValue = 6000f, stepIncrement = 200f, scene = UI_Scene.All)]
		public float MinEngagementRange = 1000;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max engagement range"),
			UI_FloatRange(minValue = 1000f, maxValue = 8000f, stepIncrement = 500f, scene = UI_Scene.All)]
		public float MaxEngagementRange = 4000;

		const float AttackAngleAtMaxRange = 30f;
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
<b>Steer Factor</b> - higher will make the AI apply more control input for the same desired rotation
<b>Bank angle</b> - higher will make the AI apply more control input when it wants to stop rotation
<b>Steering</b> - can the ship turn with engines off
<b>Attack vector</b> - does the ship attack from the front or the sides
<b>Min engagement range</b> - AI will try to move away from oponents if closer than this range
<b>Max engagement range</b> - AI will prioritize getting closer over attacking when beyond this range
";
		}
		#endregion

		#region events

		public override void ActivatePilot()
		{
			base.ActivatePilot();

			pathingMatrix = new AIUtils.TraversabilityMatrix(vessel.CoM, vessel.mainBody, AIUtils.VehicleMovementType.Water, 5);
		}

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

			pathingMatrix.DrawDebug();
		}

		#endregion

		#region Actual AI Pilot

		protected override void AutoPilot(FlightCtrlState s)
		{
			if (!vessel.Autopilot.Enabled)
				vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);

			targetVelocity = 0;
			targetDirection = vesselTransform.up;
			upDir = VectorUtils.GetUpDirection(vesselTransform.position);

			// check if we should be panicking
			if (!PanicModes())
			{
				// pilot logic figures out what we're supposed to be doing, and sets the base state
				PilotLogic();
				// situational awareness modifies the base as best as it can (evasive mainly)
				Tactical();
			}

			AttitudeControl(s); // move according to our targets
			AdjustThrottle(targetVelocity); // set throttle according to our targets and movement
		}

		void PilotLogic()
		{
			// check for collisions, but not every frame
			if (collisionDetectionTicker == 0)
			{
				collisionDetectionTicker = 20;

				pathingMatrix.Recenter(vessel.CoM);

				float predictMult = Mathf.Clamp(10 / MaxDrift, 1, 10);
				dodgeVector = PredictRunningAshore(10f * predictMult, 2f);

				foreach (Vessel vs in BDATargetManager.LoadedVessels)
				{
					if (vs == null || vs == vessel) continue;
					if (!vs.Splashed || vs.FindPartModuleImplementing<IBDAIControl>()?.commandLeader?.vessel == vessel) continue;
					dodgeVector = PredictCollisionWithVessel(vs, 5f * predictMult, 0.5f);
					if (dodgeVector != null) break;
				}
			}
			else
				collisionDetectionTicker--;
			// avoid collisions if any are found
			if (dodgeVector != null)
			{
				targetVelocity = PoweredSteering ? MaxSpeed : CruiseSpeed;
				targetDirection = (Vector3)dodgeVector;
				SetStatus($"Avoiding Collision");
				return;
			}

			// check for enemy targets and engage
			// not checking for guard mode, because if guard mode is off now you can select a target manually and if it is of opposing team, the AI will try to engage while you can man the turrets
			if (weaponManager && targetVessel != null && !BDArmorySettings.PEACE_MODE)
			{
				Vector3 vecToTarget = targetVessel.CoM - vessel.CoM;
				float distance = vecToTarget.magnitude;
				// lead the target a bit, where 950f is a ballpark estimate of the average bullet velocity (gau 983, vulcan 950, .50 860)
				vecToTarget = targetVessel.PredictPosition(distance / 950f) - vessel.CoM;  

				if (BroadsideAttack)
				{
					Vector3 sideVector = Vector3.Cross(vecToTarget, upDir); //find a vector perpendicular to direction to target
					sideVector *= Mathf.Sign(Vector3.Dot(vesselTransform.up, sideVector)); // pick a side for attack
					float sidestep = distance >= MaxEngagementRange ? Mathf.Clamp01((MaxEngagementRange - distance) / (CruiseSpeed * Mathf.Clamp(90 / MaxDrift, 0, 10)) + 1) * AttackAngleAtMaxRange / 90 : // direct to target to attackAngle degrees if over maxrange
						(distance <= MinEngagementRange ? 1.5f - distance / (MinEngagementRange * 2) : // 90 to 135 degrees if closer than minrange
						(MaxEngagementRange - distance) / (MaxEngagementRange - MinEngagementRange) * (1 - AttackAngleAtMaxRange / 90)+ AttackAngleAtMaxRange / 90); // attackAngle to 90 degrees from maxrange to minrange 
					targetDirection = Vector3.LerpUnclamped(vecToTarget.normalized, sideVector.normalized, sidestep); // interpolate between the side vector and target direction vector based on sidestep
					targetVelocity = MaxSpeed;
					DebugLine($"Broadside attack angle {sidestep}");
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

		void Tactical()
		{
			// enable RCS if we're in combat
			vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, weaponManager && targetVessel && !BDArmorySettings.PEACE_MODE
				&& (weaponManager.selectedWeapon != null || (vessel.CoM - targetVessel.CoM).sqrMagnitude < MaxEngagementRange * MaxEngagementRange) || weaponManager.underFire || weaponManager.missileIsIncoming);

			// if weaponManager thinks we're under fire, do the evasive dance
			if (weaponManager.underFire || weaponManager.missileIsIncoming)
			{
				targetVelocity = MaxSpeed;
				if (weaponManager.underFire || weaponManager.incomingMissileDistance < 2500)
				{
					if (Mathf.Abs(weaveAdjustment) + Time.deltaTime * weaveFactor > weaveLimit) weaveDirection *= -1;
					weaveAdjustment += weaveFactor * weaveDirection * Time.deltaTime;
				}
				else
				{
					weaveAdjustment = 0;
				}
			}
			else
			{
				weaveAdjustment = 0;
			}
			//DebugLine($"underFire {weaponManager.underFire} weaveAdjustment {weaveAdjustment}");
		}

		bool PanicModes()
		{
			if (!vessel.LandedOrSplashed)
			{
				targetVelocity = 0;
				targetDirection = Vector3.ProjectOnPlane(vessel.srf_velocity, upDir);
				SetStatus("Airtime!");
				return true;
			}
			else if (vessel.Landed)
			{
				targetVelocity = 0;
				SetStatus("Stranded");
				return true;
			}
			return false;
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
				DebugLine($"Target velocity: {targetVelocity}");

			speedController.targetSpeed = targetSpeed;
		}

		void AttitudeControl(FlightCtrlState s)
		{
			Vector3 yawTarget = Vector3.ProjectOnPlane(targetDirection, upDir);
			
			// limit "aoa" if we're moving
			if (vessel.horizontalSrfSpeed * 10 > CruiseSpeed)
				yawTarget = Vector3.RotateTowards(vessel.srf_velocity, yawTarget, MaxDrift * Mathf.Deg2Rad, 0);

			float yawError = VectorUtils.SignedAngle(vesselTransform.up, yawTarget, vesselTransform.right) + weaveAdjustment;
			float pitchAngle = TargetPitch * Mathf.Clamp01((float)vessel.horizontalSrfSpeed / CruiseSpeed);
			float drift = VectorUtils.SignedAngle(vesselTransform.up, Vector3.ProjectOnPlane(vessel.GetSrfVelocity(), upDir), vesselTransform.right);

			float pitch = 90 - Vector3.Angle(vesselTransform.up, upDir);
			float pitchError = pitchAngle - pitch;

			float bank = VectorUtils.SignedAngle(-vesselTransform.forward, upDir, -vesselTransform.right);
			float rollError = drift / MaxDrift * BankAngle - bank;

			Vector3 localAngVel = vessel.angularVelocity;
			s.roll = steerMult * 0.0015f * rollError - .10f * steerDamping * -localAngVel.y;
			s.pitch = (0.015f * steerMult * pitchError) - (steerDamping * -localAngVel.x);
			s.yaw = (0.005f * steerMult * yawError) - (steerDamping * 0.2f * -localAngVel.z);
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
				Vector3 tPos = v.PredictPosition(time);
				Vector3 myPos = vessel.PredictPosition(time);
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
				Vector3 testVector = vessel.PredictPosition(time) - vessel.CoM;
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

		Vector3 GetFormationPosition()
		{
			return commandLeader.vessel.CoM + Quaternion.LookRotation(commandLeader.vessel.up, upDir) * this.GetLocalFormationPosition(commandFollowIndex);
		}
		#endregion
	}
}
