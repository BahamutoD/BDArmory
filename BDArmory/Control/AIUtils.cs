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

			// how much the gird can get distorted before it is rebuilt instead of expanded
			const float MaxDistortion = 0.02f;

			Dictionary<Coords, Cell> grid;
			Dictionary<Coords, float> cornerAlts;

			float rebuildDistance;
			CelestialBody body;
			float maxSlopeAngle;
			Vector3 origin;
			VehicleMovementType movementType;

			/// <summary>
			/// Create a new traversability matrix.
			/// </summary>
			/// <param name="start">Origin point, world position</param>
			/// <param name="end">Destination point, world position</param>
			/// <param name="body">Body on which the grid is created</param>
			/// <param name="vehicleType">Movement type of the vehicle (surface/land)</param>
			/// <param name="maxSlopeAngle">The highest slope angle (in degrees) the vessel can traverse in a straight line</param>
			public List<Vector3> Pathfind(Vector3 start, Vector3 end, CelestialBody body, VehicleMovementType vehicleType, float maxSlopeAngle)
			{
				checkGrid(start, body, vehicleType, maxSlopeAngle);

				Coords startCoords = getGridCoord(start);
				Coords endCoords = getGridCoord(end);

				Dictionary<Cell, float> candidates = new Dictionary<Cell, float> //fScore and openSet
				{ [getCellAt(startCoords)] = gridDistance(startCoords, endCoords) };

				Dictionary<Cell, float> nodes = new Dictionary<Cell, float> //gScore
				{ [getCellAt(startCoords)] = 0 };

				Dictionary<Cell, Cell> backtrace = new Dictionary<Cell, Cell>(); //cameFrom
				HashSet<Cell> visited = new HashSet<Cell>();

				Cell current = null;

				while (candidates.Count > 0)
				{
					// find the best candidate
					using (var e = candidates.GetEnumerator())
					{
						e.MoveNext();
						var min = e.Current;
						while (e.MoveNext())
							if (e.Current.Value < min.Value)
								min = e.Current;
						current = min.Key;
					}
					// stop if we found our destination
					if (current.Coords == endCoords) break;

					candidates.Remove(current);
					visited.Add(current);
					float currentNodeScore = nodes[current];

					foreach (var adj in adjacent)
					{
						Cell neighbour = getCellAt(current.Coords + adj.Key);
						if (!neighbour.Traversable || visited.Contains(neighbour)) continue;
						float value;
						if (candidates.TryGetValue(neighbour, out value) && currentNodeScore + adj.Value >= value)
							continue;
						nodes[neighbour] = currentNodeScore + adj.Value;
						backtrace[neighbour] = current;
						candidates[neighbour] = currentNodeScore + adj.Value + gridDistance(neighbour.Coords, endCoords);
					}
				}

				var path = new List<Vector3>();
				while(current.Coords != startCoords)
				{
					path.Add(current.WorldPos);
					current = backtrace[current];
				}
				path.Reverse();

				return path;
			}

			private void checkGrid(Vector3 origin, CelestialBody body, VehicleMovementType vehicleType, float maxSlopeAngle)
			{
				if (grid == null || VectorUtils.GeoDistance(this.origin, origin, body) > rebuildDistance ||
					this.body != body || movementType != vehicleType || this.maxSlopeAngle != maxSlopeAngle)
				{
					this.body = body;
					this.maxSlopeAngle = maxSlopeAngle;
					rebuildDistance = Mathf.Clamp(Mathf.Asin(MaxDistortion) * (float)body.Radius, GridSize * 4, GridSize * 256);
					movementType = vehicleType;
					this.origin = VectorUtils.WorldPositionToGeoCoords(origin, body);
					grid = new Dictionary<Coords, Cell>();
					cornerAlts = new Dictionary<Coords, float>();
				}
				includeDebris();
			}

			private Cell getCellAt(int x, int y) => getCellAt(new Coords(x, y));
			private Cell getCellAt(Coords coords)
			{
				Cell cell;
				if (!grid.TryGetValue(coords, out cell))
				{
					cell = new Cell(coords, gridToGeo(coords), CheckTraversability(coords), body);
					grid[coords] = cell;
				}
				return cell;
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
					Coords coords = getGridCoord(vs.CoM);
					if (grid.TryGetValue(coords, out cell))
						cell.Traversable = false;
					else
						grid[coords] = new Cell(coords, gridToGeo(coords), false, body);
				}
			}

			private bool directPath(Coords origin, Coords target)
			{
				int x = origin.X;
				int y = origin.Y;
				int dX = Math.Sign(target.X - x);
				int dY = Math.Sign(target.Y - y);
				float ratio = dX / dY;


				return true;
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
			private Coords getGridCoord(float[] gridLocation)
				=> new Coords(Mathf.RoundToInt(gridLocation[0]), Mathf.RoundToInt(gridLocation[1]));
			private Coords getGridCoord(Vector3 worldPosition)
				=> getGridCoord(getGridLocation(VectorUtils.WorldPositionToGeoCoords(worldPosition, body)));

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

			private class Cell
			{
				public Cell(Coords coords, Vector3 geoPos, bool traversible, CelestialBody body)
				{
					Coords = coords;
					GeoPos = geoPos;
					GeoPos.z = (float)body.TerrainAltitude(GeoPos.x, GeoPos.y);
					Traversable = traversible;
					WorldPos = VectorUtils.GetWorldSurfacePostion(GeoPos, body);
				}

				public readonly Coords Coords;
				public readonly Vector3 GeoPos;
				public readonly Vector3 WorldPos;
				public bool Traversable;

				public int X => Coords.X;
				public int Y => Coords.Y;

				public override string ToString() => $"[{X}, {Y}, {Traversable}]";
				public override int GetHashCode() => Coords.GetHashCode();
				public bool Equals(Cell other) => X == other?.X && Y == other.Y && Traversable == other.Traversable;
				public override bool Equals(object obj) => Equals(obj as Cell);
				public static bool operator ==(Cell left, Cell right) => object.Equals(left, right);
				public static bool operator !=(Cell left, Cell right) => !object.Equals(left, right);
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
				public static Coords operator +(Coords left, Coords right) => new Coords(left.X + right.X, left.Y + right.Y);
				public override int GetHashCode() => X.GetHashCode() * 1009 + Y.GetHashCode();
				public override string ToString() => $"[{X}, {Y}]";
			}

			private Dictionary<Coords, float> adjacent = new Dictionary<Coords, float>
			{[new Coords(0, 1)] = GridSize, [new Coords(1, 0)] = GridSize, [new Coords(0, -1)] = GridSize, [new Coords(-1, 0)] = GridSize,
				[new Coords(1, 1)] = GridDiagonal, [new Coords(1, -1)] = GridDiagonal, [new Coords(-1, -1)] = GridDiagonal, [new Coords(-1, 1)] = GridDiagonal};

			private float getCornerAlt(int x, int y) => getCornerAlt(new Coords(x, y));
			private float getCornerAlt(Coords coords)
			{
				float alt;
				if (!cornerAlts.TryGetValue(coords, out alt))
				{
					var geo = gridToGeo(coords.X - 0.5f, coords.Y - 0.5f);
					alt = (float)body.TerrainAltitude(geo.x, geo.y, true);
					cornerAlts[coords] = alt;
				}
				return alt;
			}

			private bool CheckTraversability(Coords coords)
			{
				float[] cornerAlts = new float[4]
				{
					getCornerAlt(coords.X, coords.Y),
					getCornerAlt(coords.X+1, coords.Y),
					getCornerAlt(coords.X+1, coords.Y+1),
					getCornerAlt(coords.X, coords.Y+1),
				};

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
					// set max to zero for slope check
					if (cornerAlts[i] < 0) cornerAlts[i] = 0;
				}

				// check if angles are not too steep (if it's a land vehicle)
				if ((movementType & VehicleMovementType.Land) == VehicleMovementType.Land
					&& (checkSlope(cornerAlts[0], cornerAlts[1], GridSize)
					|| checkSlope(cornerAlts[1], cornerAlts[2], GridSize)
					|| checkSlope(cornerAlts[2], cornerAlts[3], GridSize)
					|| checkSlope(cornerAlts[3], cornerAlts[0], GridSize)
					|| checkSlope(cornerAlts[0], cornerAlts[2], GridDiagonal)
					|| checkSlope(cornerAlts[1], cornerAlts[3], GridDiagonal)))
					return false;

				return true;
			}
			bool checkSlope(float alt1, float alt2, float length) => Mathf.Abs(Mathf.Sin((alt1 - alt2) / length)) > maxSlopeAngle;

			public void DrawDebug(Vector3 currentWorldPos, List<Vector3> waypoints = null)
			{
				Vector3 upVec = VectorUtils.GetUpDirection(currentWorldPos) * 5;
				foreach (var kvp in grid)
				{
					BDGUIUtils.DrawLineBetweenWorldPositions(kvp.Value.WorldPos, kvp.Value.WorldPos + upVec, 3,
						kvp.Value.Traversable ? Color.green : Color.red);
				}
				if (waypoints != null)
				{
					var previous = currentWorldPos;
					foreach (var wp in waypoints)
					{
						BDGUIUtils.DrawLineBetweenWorldPositions(previous + upVec, wp + upVec, 2, Color.blue);
						previous = wp;
					}
				}
			}
		}
	}
}
