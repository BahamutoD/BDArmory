using System;
using System.Collections.Generic;
using BDArmory.Core.Extension;
using BDArmory.Misc;
using BDArmory.UI;
using UnityEngine;

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
		public static Vector3 PredictPosition(this Vessel v, float time)
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
		public static float GetTerrainAltitude(Vector3 position, CelestialBody body, bool underwater = true)
		{
			return (float)body.TerrainAltitude(body.GetLatitude(position), body.GetLongitude(position), underwater);
		}

		/// <summary>
		/// Get the local position of your place in a formation
		/// </summary>
		/// <param name="index">index of formation position</param>
		/// <returns>vector of location relative to your commandLeader</returns>
		public static Vector3 GetLocalFormationPosition(this IBDAIControl ai, int index)
		{
			if (ai.commandLeader == null) return Vector3.zero;

			float indexF = (float)index;
			indexF++;

			float rightSign = indexF % 2 == 0 ? -1 : 1;
			float positionFactor = Mathf.Ceil(indexF / 2);
			float spread = ai.commandLeader.spread;
			float lag = ai.commandLeader.lag;

			float right = rightSign * positionFactor * spread;
			float back = positionFactor * lag * -1;

			return new Vector3(right, back, 0);
		}

		[Flags]
		public enum VehicleMovementType
		{
			Stationary = 0,
			Land = 1,
			Water = 2,
			Amphibious = Land | Water,
		}

		/// <summary>
		/// Minimum depth for water ships to consider the terrain safe.
		/// </summary>
		public const float MinDepth = -10f;

		/// <summary>
		/// A grid approximation for a spherical body for AI pathing purposes.
		/// </summary>
		public class TraversabilityMatrix
		{
			// edge of each grid cell
			public const float GridSize = 200;
			private const float GridDiagonal = GridSize * 1.414213562373f;

			// default grid size in each direction
			const int DefaultSize = (int)(4000 / GridSize);

			// how much the gird can get distorted before it is rebuilt instead of expanded
			const float MaxDistortion = 0.02f;

			Dictionary<Coords, Cell> grid;

			float rebuildDistance;
			CelestialBody body;
			float maxSlopeAngle;
			Vector3 origin;
			VehicleMovementType movementType;

			Vector3 lastCenter;
			Coords lastCenterCoords;

			/// <summary>
			/// Create a new traversability matrix.
			/// </summary>
			/// <param name="start">Origin point, world position</param>
			/// <param name="body">Body on which the grid is created</param>
			/// <param name="vehicleType">Movement type of the vehicle (surface/land)</param>
			/// <param name="maxSlopeAngle">The highest slope angle (in degrees) the vessel can traverse in a straight line</param>
			public TraversabilityMatrix(Vector3 start, CelestialBody body, VehicleMovementType vehicleType, float maxSlopeAngle)
			{
				this.body = body;
				this.maxSlopeAngle = maxSlopeAngle;
				rebuildDistance = Mathf.Clamp(Mathf.Asin(MaxDistortion) * (float)body.Radius, GridSize * 4, GridSize * DefaultSize * 10);
				movementType = vehicleType;

				createGrid(start);
			}

			private void createGrid(Vector3 origin)
			{
				this.origin = VectorUtils.WorldPositionToGeoCoords(origin, body);
				lastCenter = origin;
				lastCenterCoords = new Coords(0, 0);

				grid = new Dictionary<Coords, Cell>();

				BuildGrid(-DefaultSize, -DefaultSize, DefaultSize, DefaultSize);
			}

			// create cells in grid
			private void BuildGrid(int minX, int minY, int maxX, int maxY)
			{
				if (minX > maxX || minY > maxY) return;

				var altDict = new Dictionary<Coords, float>();
				altDict[new Coords(minX, minY)] = altAtGeo(gridToGeo(minX - 0.5f, minY - 0.5f));
				for (int x = minX; x <= maxX; x++)
					altDict[new Coords(x + 1, minY)] = altAtGeo(gridToGeo(x + 0.5f, minY - 0.5f));

				for (int y = minY; y <= maxY; y++)
				{
					altDict[new Coords(minX, y + 1)] = altAtGeo(gridToGeo(minX - 0.5f, y + 0.5f));
					for (int x = minX; x <= maxX; x++)
					{
						altDict[new Coords(x + 1, y + 1)] = altAtGeo(gridToGeo(x + 0.5f, y + 0.5f));
						var coords = new Coords(x, y);
						if (grid.ContainsKey(coords)) continue;
						grid[coords] = new Cell(coords, gridToGeo(x, y),
							CheckTraversability(new float[4] {
								altDict[new Coords( x, y )], altDict[new Coords( x+1, y )],
								altDict[new Coords( x, y+1 )], altDict[new Coords( x+1, y+1 )]
							}, movementType, maxSlopeAngle),
							body);
					}
				}
			}

			/// <summary>
			/// Check all debris on the ground, and mark those squares impassable.
			/// </summary>
			private void includeDebris()
			{
				foreach (Vessel vs in BDATargetManager.LoadedVessels)
				{
					if ((vs == null || vs.vesselType != VesselType.Debris || !vs.LandedOrSplashed
						|| vs.mainBody.GetAltitude(vs.CoM) < MinDepth)) continue;

					Cell cell;
					Coords coords = getGridCell(vs.CoM);
					if (grid.TryGetValue(coords, out cell))
						cell.Traversible = false;
					else
						grid[coords] = new Cell(coords, gridToGeo(coords), false, body);
				}
			}

			// calculate location on grid
			private float[] getGridLocation(Vector3 geoPoint)
			{
				var distance = VectorUtils.GeoDistance(origin, geoPoint, body) / GridSize;
				var bearing = VectorUtils.GeoForwardAzimuth(origin, geoPoint) * Mathf.Deg2Rad;
				var x = distance * Mathf.Cos(bearing);
				var y = distance * Mathf.Sin(bearing);
				return new float[2] { x, y };
			}

			// round grid coordinates to get cell
			private Coords getGridCell(float[] gridLocation) 
				=> new Coords(Mathf.RoundToInt(gridLocation[0]), Mathf.RoundToInt(gridLocation[1]));
			private Coords getGridCell(Vector3 worldPosition) 
				=> getGridCell(getGridLocation(VectorUtils.WorldPositionToGeoCoords(worldPosition, body)));

			/// <summary>
			/// Should be called when the vessel moves. Will either extend grid to ensure coverage of surrounding are,
			/// or rebuild the grid with the new center if the covered distance is large enough to cause distortion.
			/// </summary>
			/// <param name="point">new center (world position)</param>
			public void Recenter(Vector3 point)
			{
				if ((point - lastCenter).sqrMagnitude < GridSize * GridSize) return;

				var geoPoint = VectorUtils.WorldPositionToGeoCoords(point, body);
				if (VectorUtils.GeoDistance(origin, geoPoint, body) > rebuildDistance)
					createGrid(point);
				else
				{
					Coords recenter = getGridCell(getGridLocation(geoPoint));

					if (recenter.X > lastCenterCoords.X)
						BuildGrid(lastCenterCoords.X + DefaultSize + 1, recenter.Y - DefaultSize, recenter.X + DefaultSize, recenter.Y + DefaultSize);
					else if (recenter.X < lastCenterCoords.X)
						BuildGrid(recenter.X - DefaultSize, recenter.Y - DefaultSize, lastCenterCoords.X - DefaultSize - 1, recenter.Y + DefaultSize);
					if (recenter.Y > lastCenterCoords.Y)
						BuildGrid(recenter.X - DefaultSize, lastCenterCoords.Y + DefaultSize + 1, recenter.X + DefaultSize, recenter.Y + DefaultSize);
					else if (recenter.Y < lastCenterCoords.Y)
						BuildGrid(recenter.X - DefaultSize, recenter.Y - DefaultSize, recenter.X + DefaultSize, lastCenterCoords.Y - DefaultSize - 1);
					lastCenter = point;
					lastCenterCoords = recenter;
				}
			}

			public float GetSafeDistance(Vector3 start, float bearing)
			{
				throw new NotImplementedException();
			}

			public List<Vector3> Pathfind(Vector3 start, Vector3 end)
			{
				throw new NotImplementedException();
			}

			private float gridDistance(Coords point, Coords other)
			{
				float dX = Mathf.Abs(point.X - other.X);
				float dY = Mathf.Abs(point.Y - other.Y);
				return GridDiagonal * Mathf.Min(dX, dY) + GridSize * Mathf.Abs(dX - dY);
			}

			// positive y towards north, positive x towards east
			Vector3 gridToGeo(float x, float y)
			{
				if (x == 0 && y == 0) return origin;
				return VectorUtils.GeoCoordinateOffset(origin, body, Mathf.Atan2(y, x) * Mathf.Rad2Deg, Mathf.Sqrt(x * x + y * y) * GridSize);
			}
			Vector3 gridToGeo(Coords coords) => gridToGeo(coords.X, coords.Y);

			float altAtGeo(Vector3 geo) => (float)body.TerrainAltitude(geo.x, geo.y, true);

			private class Cell
			{
				public Cell(Coords coords, Vector3 geoPos, bool traversible, CelestialBody body)
				{
					Coords = coords;
					GeoPos = geoPos;
					GeoPos.z = (float)body.TerrainAltitude(Lat, Lon);
					Traversible = traversible;
					WorldPos = VectorUtils.GetWorldSurfacePostion(GeoPos, body);
				}

				public readonly Coords Coords;
				public readonly Vector3 GeoPos;
				public readonly Vector3 WorldPos;
				public bool Traversible;

				public int X => Coords.X;
				public int Y => Coords.Y;
				public float Lat => GeoPos.x;
				public float Lon => GeoPos.y;
				public float Alt => GeoPos.z;
			}

			// because int[] does not produce proper hashes
			private struct Coords
			{
				public readonly int X;
				public readonly int Y;

				public Coords(int x, int y)
				{
					X = x;
					Y = y;
				}

				public bool Equals(Coords other)
				{
					if (other == null) return false;
					return (X == other.X && Y == other.Y);
				}
				public override bool Equals(object obj)
				{
					if (!(obj is Coords)) return false;
					return Equals((Coords)obj);
				}
				public static bool operator ==(Coords left, Coords right) => object.Equals(left, right);
				public static bool operator !=(Coords left, Coords right) => !object.Equals(left, right);
				public override int GetHashCode() => X.GetHashCode() * 1009 + Y.GetHashCode();
				public override string ToString() => $"[{X}, {Y}]";
			}

			private bool CheckTraversability(float[] cornerAlts, VehicleMovementType movementType, float maxAngle)
			{
				for (int i = 0; i < 4; i++)
				{
					// check if we have the correct surface on all corners (land/water)
					switch (movementType)
					{
						case VehicleMovementType.Amphibious:
							break;
						case VehicleMovementType.Land:
							if (cornerAlts[i] < 0) return false;
							break;
						case VehicleMovementType.Water:
							if (cornerAlts[i] > MinDepth) return false;
							break;
						case VehicleMovementType.Stationary:
						default:
							return false;
					}

					// check if angles are not too steep (if it's a land vehicle)
					if ((movementType & VehicleMovementType.Land) == VehicleMovementType.Land)
					{
						for (int j = i + 1; i < 4; i++)
						{
							// technically wrong, since on diagonal we should divide by the longer distance, but mostly if it's that bad, it's not traversible
							if (Mathf.Abs(Mathf.Sin((Mathf.Max(cornerAlts[i], 0) - Mathf.Max(cornerAlts[j], 0)) / GridSize)) > maxAngle)
								return false;
						}
					}
				}
				return true;
			}

			public void DrawDebug()
			{
				Vector3 upVec = VectorUtils.GetUpDirection(lastCenter);
				foreach (var kvp in grid)
				{
					BDGUIUtils.DrawLineBetweenWorldPositions(kvp.Value.WorldPos, kvp.Value.WorldPos + upVec * 5, 3, kvp.Value.Traversible ? Color.green : Color.red);
				}
			}
		}
	}
}
