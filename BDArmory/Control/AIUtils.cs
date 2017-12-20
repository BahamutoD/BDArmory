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
		/// A grid approximation for a spherical body for AI pathing purposes.
		/// </summary>
		public class TraversabilityMatrix
		{
			// edge of each grid cell
			public const float GridSize = 200;

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
				rebuildDistance = Mathf.Max(Mathf.Asin(MaxDistortion) * (float)body.Radius, GridSize * 4);
				movementType = vehicleType;

				createGrid(start);
			}

			private void createGrid(Vector3 origin)
			{
				this.origin = origin;
				lastCenter = origin;
				grid = new Dictionary<Coords, Cell>();

				BuildGrid(-DefaultSize, -DefaultSize, DefaultSize, DefaultSize);
			}

			// create cells in grid
			private void BuildGrid(int minX, int minY, int maxX, int maxY)
			{
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
						if (grid.ContainsKey(new Coords(x, y))) continue;
						grid[new Coords( x, y )] = new Cell(x, y, gridToGeo(x, y),
							CheckTraversibility(new float[4] {
								altDict[new Coords( x, y )], altDict[new Coords( x+1, y )],
								altDict[new Coords( x, y+1 )], altDict[new Coords( x+1, y+1 )]
							}, movementType, maxSlopeAngle),
							body);
					}
				}
			}

			// calculate location on grid
			private float[] getGridLocation(Vector3 geoPoint)
			{
				var distance = VectorUtils.GeoDistance(origin, geoPoint, body);
				var bearing = VectorUtils.GeoForwardAzimuth(origin, geoPoint) * Mathf.Deg2Rad;
				var x = distance * Mathf.Sin(bearing);
				var y = distance * Mathf.Cos(bearing);
				return new float[2] { x, y };
			}

			// round grid coordinates to get cell
			private Coords getGridCell(float[] gridLocation) => new Coords(Mathf.RoundToInt(gridLocation[0]), Mathf.RoundToInt(gridLocation[1]));
			
			/// <summary>
			/// Should be called when the vessel moves. Will either extend grid to ensure coverage of surrounding are,
			/// or rebuild the grid with the new center if the covered distance is large enough to cause distortion.
			/// </summary>
			/// <param name="point">new center in world coordinate format</param>
			public void RecenterGrid(Vector3 point)
			{
				Debug.Log("recenter at at " + point.ToString() + ", distance2 " + (point - lastCenter).sqrMagnitude);
				if ((point - lastCenter).sqrMagnitude < GridSize * GridSize) return;
				if (VectorUtils.GeoDistance(origin, point, body) > rebuildDistance)
					createGrid(point);
				else
				{
					var recenter = getGridCell(getGridLocation(point));
					BuildGrid(recenter.X - DefaultSize, recenter.Y - DefaultSize, recenter.X + DefaultSize, recenter.Y + DefaultSize);
					lastCenter = point;
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

			// positive y towards north, positive x towards east
			Vector3 gridToGeo(float x, float y)
			{
				if (x == 0 && y == 0) return origin;
				return VectorUtils.GeoCoordinateOffset(origin, body, Mathf.Atan2(y, x) * Mathf.Rad2Deg, Mathf.Sqrt(x * x + y * y) * GridSize);
			}
			float altAtGeo(Vector3 geo) => (float)body.TerrainAltitude(geo.x, geo.y, true);

			private class Cell
			{
				public Cell(int x, int y, Vector3 geoPos, bool traversible, CelestialBody body)
				{
					Coords = new Coords(x, y);
					GeoPos = geoPos;
					GeoPos.z = (float)body.TerrainAltitude(Lat, Lon);
					Traversible = traversible;
				}

				public Coords Coords;
				public Vector3 GeoPos;
				public bool Traversible;

				public int X => Coords.X;
				public int Y => Coords.Y;
				public float Lat => GeoPos.x;
				public float Lon => GeoPos.y;
				public float Alt => GeoPos.z;
			}

			// because int[] does not produce proper hashes
			private class Coords
			{
				public int X;
				public int Y;

				public Coords(int x, int y)
				{
					X = x;
					Y = y;
				}

				public bool Equals(Coords other) => (X == other?.X && Y == other?.Y);
				public override bool Equals(object obj) => Equals(obj as Coords);
				public static bool operator ==(Coords left, Coords right) => object.Equals(left, right);
				public static bool operator !=(Coords left, Coords right) => !object.Equals(left, right);
				public override int GetHashCode() => X.GetHashCode() * 1009 + Y.GetHashCode();
			}

			private bool CheckTraversibility(float[] cornerAlts, VehicleMovementType movementType, float maxAngle)
			{
				const float minDepth = 10f;

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
							if (cornerAlts[i] > -minDepth) return false;
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

			public void DrawMatrix()
			{
				foreach (var kvp in grid)
				{
					Vector3 worldPos = VectorUtils.GetWorldSurfacePostion(kvp.Value.GeoPos, body);
					Vector3 upVec = VectorUtils.GetUpDirection(worldPos);
					BDGUIUtils.DrawLineBetweenWorldPositions(worldPos, worldPos + upVec * 5, 3, kvp.Value.Traversible ? Color.green : Color.red);
				}
			}
		}

		public static List<Vector3> Pathfind(Vector3 start, Vector3 end, CelestialBody body, VehicleMovementType vehicleType = VehicleMovementType.Land)
		{
			Vector3 startGeo = VectorUtils.WorldPositionToGeoCoords(start, body);
			Vector3 endGeo = VectorUtils.WorldPositionToGeoCoords(end, body);

			GeoGrid grid = new GeoGrid(startGeo, endGeo, body, vehicleType);

			// pathfind over the grid

			// eliminate unnecessary waypoints


			throw new NotImplementedException();
		}

		private class GeoGrid
		{
			const float gridSize = 200;
			const int outerGridPoints = (int)(10000 / gridSize);

			public GeoGrid(Vector3 startGeo, Vector3 endGeo, CelestialBody body, VehicleMovementType vehicleType)
			{
				float geoDistance = VectorUtils.GeoDistance(startGeo, endGeo, body);
				innerGridPoints = (int)Mathf.Ceil(geoDistance / gridSize);

				gridWidth = outerGridPoints * 2 + 1;
				gridLength = gridWidth + innerGridPoints;
				grid = new GridPoint[gridLength][];

				float gridStartBearing = VectorUtils.GeoForwardAzimuth(startGeo, endGeo);

				for (int i = 0; i < gridLength; i++)
				{
					grid[i] = new GridPoint[gridWidth];
					grid[i][outerGridPoints] = new GridPoint(i, outerGridPoints,
						VectorUtils.GeoCoordinateOffset(startGeo, body, gridStartBearing, gridSize * (i - outerGridPoints)),
						vehicleType, body);
					float localBearing = VectorUtils.GeoForwardAzimuth(startGeo, grid[i][outerGridPoints].GeoPos);
				}
			}

			private GridPoint[][] grid;
			private int gridLength;
			private int gridWidth;
			private int innerGridPoints;

			public GridPoint StartPoint => grid[outerGridPoints][outerGridPoints];
			public GridPoint EndPoint => grid[outerGridPoints + innerGridPoints][outerGridPoints];

			public class GridPoint
			{
				public GridPoint(int x, int y, Vector3 geoPos, VehicleMovementType movementType, CelestialBody body)
				{
					Coords = new int[2] { x, y };
					GeoPos = geoPos;
					GeoPos.z = (float)body.TerrainAltitude(Lat, Lon);
					switch (movementType)
					{
						case VehicleMovementType.Amphibious:
							Traversible = true;
							break;
						case VehicleMovementType.Land:
							Traversible = body.TerrainAltitude(Lat, Lon, true) > 0;
							break;
						case VehicleMovementType.Water:
							Traversible = body.TerrainAltitude(Lat, Lon, true) < -5;
							break;
						case VehicleMovementType.Stationary:
						default:
							Traversible = false;
							break;
					}
					Body = body;
				}

				public int[] Coords;
				public Vector3 GeoPos;
				public bool Traversible;
				public CelestialBody Body;

				public int X => Coords[0];
				public int Y => Coords[1];
				public float Lat => GeoPos.x;
				public float Lon => GeoPos.y;
				public float Alt => GeoPos.z;

				private Dictionary<int[], float> distanceTo = new Dictionary<int[], float>();

				public float DistanceTo(GridPoint gridPoint)
				{
					if (!distanceTo.ContainsKey(gridPoint.Coords))
						distanceTo[gridPoint.Coords] = VectorUtils.GeoDistance(GeoPos, gridPoint.GeoPos, Body);
					return distanceTo[gridPoint.Coords];
				}
			}
		}
	}
}
