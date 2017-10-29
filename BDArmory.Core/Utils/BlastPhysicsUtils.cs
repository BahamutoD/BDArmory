using System;
using BDArmory.Core.Module;

namespace BDArmory.Core.Utils
{
    public static class BlastPhysicsUtils
    {
        public static BlastInfo CalculatePartAcceleration(float partArea, double vesselMass,  float explosiveMass, float range)
        {
    
            double scaledDistance = CalculateScaledDistance(explosiveMass, range);

            double pressure = CalculateIncidentPressure(scaledDistance);

            double force = CalculateForce(pressure, partArea);

            var acceleration = (force / vesselMass);

            return new BlastInfo() {Acceleration = (float) acceleration, Pressure = (float) pressure};
        }

        private static double CalculateScaledDistance(float explosiveCharge, float range)
        {
            return (range / Math.Pow(explosiveCharge, 1f / 3f));
        }

        private static double CalculateIncidentPressure(double scaledDistance)

        {
           var t = Math.Log(scaledDistance) / Math.Log(10);

            //NATO AASTP version
            var u = -0.214362789151 + 1.35034249993 * t;
            var ip = 2.78076916577 - 1.6958988741 * u -
                 0.154159376846 * Math.Pow(u, 2) +
                 0.514060730593 * Math.Pow(u, 3) +
                 0.0988534365274 * Math.Pow(u, 4) -
                 0.293912623038 * Math.Pow(u, 5) -
                 0.0268112345019 * Math.Pow(u, 6) +
                 0.109097496421 * Math.Pow(u, 7) +
                 0.00162846756311 * Math.Pow(u, 8) -
                 0.0214631030242 * Math.Pow(u, 9) +
                 0.0001456723382 * Math.Pow(u, 10) +
                 0.00167847752266 * Math.Pow(u, 11);
            ip = Math.Pow(10, ip);
            return ip;
        }

        /// <summary>
        /// Calculate newtons from the pressure in kPa and the surface on Square meters
        /// </summary>
        /// <param name="pressure">kPa</param>
        /// <param name="surface">m2</param>
        /// <returns></returns>
        private static double CalculateForce(double pressure, float surface)
        {
            return pressure * surface;
        }


        /// <summary>
        /// Method based on Hopkinson-Cranz Scaling Law
        /// Z value of 14.8
        /// </summary>
        /// <param name="tntMass"> tnt equivales mass in kg</param>
        /// <returns>explosive range in meters </returns>
        public static float CalculateBlastRange(double tntMass)
        {
            return (float) (14.8f * Math.Pow(tntMass, 1 / 3f));
        }

        /// <summary>
        /// Method based on Hopkinson-Cranz Scaling Law
        /// Z value of 14.8
        /// </summary>
        /// <param name="range"> expected range in meters</param>
        /// <returns>explosive range in meters </returns>
        public static float CalculateExplosiveMass(float range)
        {
            return (float) Math.Pow((range / 14.8f), 3);
        }

    }

    public struct BlastInfo
    {
        public float Acceleration;
        public float Pressure;
    }
}
