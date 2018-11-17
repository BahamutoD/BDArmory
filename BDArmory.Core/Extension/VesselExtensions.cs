using UnityEngine;

namespace BDArmory.Core.Extension
{
    public static class VesselExtensions
    {
        public static bool InOrbit(this Vessel v)
        {
            try
            {
                return !v.LandedOrSplashed &&
                       (v.situation == Vessel.Situations.ORBITING ||
                        v.situation == Vessel.Situations.SUB_ORBITAL ||
                        v.situation == Vessel.Situations.ESCAPING);
            }
            catch
            {
                return false;
            }
        }

        public static bool InVacuum(this Vessel v)
        {
            return v.atmDensity <= 0.001f;
        }

        public static Vector3d Velocity(this Vessel v)
        {
            try
            {
                if (!v.InOrbit())
                {
                    return v.srf_velocity;
                }
                else
                {
                    return v.obt_velocity;
                }
            }
            catch
            {
                //return v.srf_velocity;
                return new Vector3d(0, 0, 0);
            }
        }

    }
}