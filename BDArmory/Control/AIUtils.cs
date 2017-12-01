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

		public static void SetSASDirection(this Vessel vessel, Vector3 forwardDirection, float roll)
		{
			var upDir = VectorUtils.GetUpDirection(vessel.CoM);
			var rightDir = Vector3.Cross(forwardDirection, upDir);
			vessel.SetSASDirection(forwardDirection, Vector3.RotateTowards(upDir, rightDir != Vector3.zero ? rightDir : VectorUtils.GetNorthVector(vessel.CoM, vessel.mainBody), roll, 0));
		}

		public static void SetSASDirection(this Vessel vessel, Vector3 forwardDirection, Vector3 upDirection)
			=> vessel.SetSASDirection(Quaternion.LookRotation(forwardDirection, upDirection));

		public static void SetSASDirection(this Vessel vessel, float pitch, float yaw, float roll)
			=> vessel.SetSASDirection(Quaternion.Euler(pitch, yaw, roll));

		public static void SetSASDirection(this Vessel vessel, Quaternion rotation)
		{
			vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
			vessel.Autopilot.SAS.LockRotation(rotation);
		}
	}
}
