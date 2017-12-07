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
	public static class AIUtils
	{
		/// <summary>
		/// Predict a future position of a vessel given its current position, velocity and acceleration
		/// </summary>
		/// <param name="v">vessel to be extrapolated</param>
		/// <param name="time">after this time</param>
		/// <returns>Vector3 extrapolated position</returns>
		public static Vector3 PredictPosition(Vessel v, float time)
		{
			Vector3 pos = v.CoM;
			pos += v.Velocity() * time;
			pos += 0.5f * v.acceleration * time * time;
			return pos;
		}

		/// <summary>
		/// Get the altitude of terrain below/above a point.
		/// </summary>
		/// <param name="position">World position, not geo position (use VectorUtils.GetWorldSurfacePostion to convert lat,long,alt to world position)</param>
		/// <param name="body">usually vessel.MainBody</param>
		/// <returns>terrain height</returns>
		public static float GetTerrainAltitude(Vector3 position, CelestialBody body)
		{
			return (float)body.TerrainAltitude(body.GetLatitude(position), body.GetLongitude(position), true);
		}

		/// <summary>
		/// Get the local position of your place in a formation
		/// </summary>
		/// <param name="index">index of formation position</param>
		/// <returns>vector of location relative to your commandLeader</returns>
		public static Vector3d GetLocalFormationPosition(this IBDAIControl ai, int index)
		{
			float indexF = (float)index;
			indexF++;

			double rightSign = indexF % 2 == 0 ? -1 : 1;
			double positionFactor = Math.Ceiling(indexF / 2);
			double spread = ai.commandLeader.spread;
			double lag = ai.commandLeader.lag;

			double right = rightSign * positionFactor * spread;
			double back = positionFactor * lag * -1;

			return new Vector3d(right, back, 0);
		}

		/// <summary>
		/// Sets SAS to rotate to the specified direction at the specified roll
		/// </summary>
		/// <param name="vessel">Reference to the vessel for which SAS should be set</param>
		/// <param name="forwardDirection">Vector3 going in the direction to face</param>
		/// <param name="roll">angle in degrees relative to the horizon plane</param>
		public static void SetSASDirection(this Vessel vessel, Vector3 forwardDirection, float roll)
		{
			var upCP = Vector3.Cross(forwardDirection, Vector3.Cross(forwardDirection, VectorUtils.GetUpDirection(vessel.CoM)));
			if (upCP != Vector3.zero)
				vessel.setSASDirection(Quaternion.AngleAxis(roll, -forwardDirection) * Quaternion.LookRotation(upCP, forwardDirection));
			else
				vessel.setSASDirection(Quaternion.LookRotation(-vessel.ReferenceTransform.forward, forwardDirection));
		}

		/// <summary>
		/// Set the SAS target direction to specified pitch, yaw and roll
		/// </summary>
		/// <param name="vessel">Reference to the vessel for which SAS should be set</param>
		/// <param name="pitch">angle in degrees relative to the horizon</param>
		/// <param name="yaw">angle in degrees relative to north</param>
		/// <param name="roll">angle in degrees relative to the horizon plane</param>
		public static void SetSASDirection(this Vessel vessel, float pitch, float yaw, float roll)
		{
			var upDir = VectorUtils.GetUpDirection(vessel.CoM);
			var north = VectorUtils.GetNorthVector(vessel.CoM, vessel.mainBody);
			var direction = Quaternion.AngleAxis(yaw, upDir) * Quaternion.AngleAxis(pitch, Vector3.Cross(north, upDir)) * Quaternion.LookRotation(-upDir, north);
			vessel.setSASDirection(Quaternion.AngleAxis(roll, direction * Vector3.up) * direction);
		}

		// this one sets rotation relative to the orientation of the solar system => usefulness is limited => kept private
		private static void setSASDirection(this Vessel vessel, Quaternion rotation)
		{
			if (!vessel.Autopilot.Enabled)
				vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);

			vessel.Autopilot.SAS.LockRotation(rotation);
		}

		/// <summary>
		/// A orientation controller for some of your control needs. I don't think it's a PID controller, but then again, I don't know what a PID controller is.
		/// Calculates control input effect derivatives and uses those to apply force and counterforce to achieve a desire orientation.
		/// It does make some assumptions concerning response linearity, but those shouldn't be horrible ones.
		/// </summary>
		public class MomentumController
		{
			/// <summary>
			/// How fast maximum calculated derivatives decay. Increase this if orientation wobbles too much, however it reduces immediacy of response. (valid values are between 0 and 1)
			/// </summary>
			public float DecayFactor { get; set; } = 0.997f;
			public float UpdatePErrorThreshold { get; set; } = 4f;

			private float previousOrientation = 0.001f;
			private float previousFirstDerivative = 0.001f;
			private float previousSecondDerivative = 0.001f;
			private float previousError = 20f;

			public float MaxSecondDerivative { get; private set; } = 1f;
			public float MaxThirdDerivative { get; private set; } = 1f;

			/// <summary>
			/// Calculate control input. The controller assumes that it is the only thing providing control input.
			/// This is not a PID controller, and will probably not deal well with significant forces affecting rotation. Not sure. Try it out.
			/// The controller also assumes the update function is called every update. This assumption can be relaxed, but would require a small calculation overhead, and use cases are very niche.
			/// </summary>
			/// <param name="currentOrientation">The current orientation in degrees from -180 to 180. You can use VectorUtils.SignedAngle to get that.</param>
			/// <param name="error">Error of orientation, i.e. how much it should be changed from the currentOrientation, in degreesfrom -180 to 180. Error should be desired orientation less current orientation</param>
			/// <returns>The control input on a scale from -1 to 1. Positive input should turn in the direction of positive error.</returns>
			public float Update(float currentOrientation, float error, bool updateMax = true)
			{
				float controlInput = 0;
				float d1 = (currentOrientation - previousOrientation); //first derivative per update
				if (Mathf.Abs(d1) > 180) d1 -= 360 * Mathf.Sign(d1); // angles
				d1 = d1 / Time.deltaTime; // normalize to seconds? that probably is seconds
				float d2 = d1 - previousFirstDerivative; //second derivative
				float d3 = d2 - previousSecondDerivative; //third derivative

				// calculate for how many frames we'd have to apply our current change in momentum to halt our momentum exactly when facing the target direction
				// if we have more frames left, continue yawing in the same direction, otherwise apply counterforce in the opposite direction

				if (error * d1 < 0)
				{
					float timeToZero = 0; // not including timeSmall
					float timeSmall = d2 / MaxThirdDerivative * Mathf.Sign(d1);
					if (Mathf.Abs(d1) < (MaxSecondDerivative * MaxSecondDerivative / MaxThirdDerivative) - timeSmall * Mathf.Abs(d2) / 2)
					{
						timeToZero = 2 * Mathf.Sqrt((Mathf.Abs(d1) + timeSmall * Mathf.Abs(d2)) / MaxThirdDerivative);

					}
					else
					{
						timeToZero = (MaxSecondDerivative / MaxThirdDerivative * 2) + (Mathf.Abs(d1) + timeSmall * Mathf.Abs(d2) 
							- (MaxSecondDerivative * MaxSecondDerivative / MaxThirdDerivative)) / MaxSecondDerivative;
					}
					float angleChangeTillStop = Mathf.Abs(d1) * timeToZero * 2 + timeSmall * d2 * d2 / 3; // if I'm not missing anything, the first term should be divided by two, not multiplied
																										  // but since this works better, I'm probably missing something

					if (float.IsNaN(angleChangeTillStop))
						controlInput = -Mathf.Sign(d1); // if timeSmall is more than area, cancel momentum
					else if (angleChangeTillStop >= Mathf.Abs(error))
						controlInput = -Mathf.Sign(error);
					else
						controlInput = Mathf.Sign(error);
				}
				else
				{
					controlInput = Mathf.Sign(error); // if we're turning in the opposite side of the want we one, stop that
				}

				// update derivatives
				if (updateMax && Mathf.Abs(previousError) > UpdatePErrorThreshold)
				{
					if (Mathf.Abs(d2) > MaxSecondDerivative) MaxSecondDerivative = Mathf.Abs(d2);
					else MaxSecondDerivative *= DecayFactor;
					if (Mathf.Abs(d3) > MaxThirdDerivative) MaxThirdDerivative = Mathf.Abs(d3);
					else MaxThirdDerivative *= DecayFactor;
					Debug.Log("MaxThirdDerivative " + MaxThirdDerivative + " MaxSecondDerivative " + MaxSecondDerivative);
				}

				previousOrientation = currentOrientation;
				previousFirstDerivative = d1;
				previousSecondDerivative = d2;
				previousError = error;

				return controlInput;
			}
		}
	}
}
