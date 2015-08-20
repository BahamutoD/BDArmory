using System;

namespace BahaTurret
{
	public struct GPSTargetInfo
	{
		public Vector3d gpsCoordinates;
		public string name;
		public Vector3d worldPos
		{
			get
			{
				if(!FlightGlobals.currentMainBody) return Vector3d.zero;
				return VectorUtils.GetWorldSurfacePostion(gpsCoordinates, FlightGlobals.currentMainBody);
			}
		}

		public GPSTargetInfo(Vector3d coords, string name)
		{
			gpsCoordinates = coords;
			this.name = name;
		}
	

		public bool EqualsTarget(GPSTargetInfo other)
		{
			return (name == other.name) && (gpsCoordinates == other.gpsCoordinates);
		}
	}
}

