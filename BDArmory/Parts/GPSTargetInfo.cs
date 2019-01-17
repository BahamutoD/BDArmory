using System;
using BDArmory.Misc;

namespace BDArmory.Parts
{
    [Serializable]
    public struct GPSTargetInfo
    {
        public Vector3d gpsCoordinates;

        public string name;

        [NonSerialized]
        public Vessel gpsVessel;

        public Vector3d worldPos
        {
            get
            {
                if (!FlightGlobals.currentMainBody)
                    return Vector3d.zero;

                return VectorUtils.GetWorldSurfacePostion(gpsCoordinates, FlightGlobals.currentMainBody);
            }
        }

        public GPSTargetInfo(Vector3d coords, string name, Vessel vessel = null)
        {
            gpsVessel = vessel;
            gpsCoordinates = coords;
            this.name = name;
        }


        public bool EqualsTarget(GPSTargetInfo other)
        {
            return name == other.name && gpsCoordinates == other.gpsCoordinates && gpsVessel == other.gpsVessel;
        }
    }
}