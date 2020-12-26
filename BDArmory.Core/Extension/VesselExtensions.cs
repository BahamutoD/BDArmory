using System;
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

        public static double GetFutureAltitude(this Vessel vessel, float predictionTime = 10)
        {
            Vector3 futurePosition = vessel.CoM + vessel.Velocity() * predictionTime
                                                + 0.5f * vessel.acceleration_immediate * Mathf.Pow(predictionTime, 2);

            return GetRadarAltitudeAtPos(futurePosition);
        }

        public static Vector3 GetFuturePosition (this Vessel vessel, float predictionTime = 10)
        {
            return vessel.CoM + vessel.Velocity() * predictionTime + 0.5f * vessel.acceleration_immediate * Math.Pow(predictionTime, 2);
        }

        public static float GetRadarAltitudeAtPos(Vector3 position)
        {
            double latitudeAtPos = FlightGlobals.currentMainBody.GetLatitude(position);
            double longitudeAtPos = FlightGlobals.currentMainBody.GetLongitude(position);

            float radarAlt = Mathf.Clamp(
                (float)(FlightGlobals.currentMainBody.GetAltitude(position) -
                        FlightGlobals.currentMainBody.TerrainAltitude(latitudeAtPos, longitudeAtPos)), 0,
                (float)FlightGlobals.currentMainBody.GetAltitude(position));
            return radarAlt;
        }
    }
}
