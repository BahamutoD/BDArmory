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
	}
}
