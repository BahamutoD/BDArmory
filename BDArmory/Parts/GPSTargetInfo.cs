using Newtonsoft.Json;
using BDArmory.Misc;

namespace BDArmory.Parts
{
    [JsonObject(MemberSerialization.OptIn)]
    public struct GPSTargetInfo
    {
        [JsonProperty]
        public Vector3d gpsCoordinates;

        [JsonProperty]
        public string name;

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