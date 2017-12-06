using System;
using BDArmory.Core.Enum;
using BDArmory.Core.Extension;
using BDArmory.Core.Module;
using UnityEngine;

namespace BDArmory.Core.Utils
{
    public static class BlastPhysicsUtils
    {
        // This values represent percentage of the blast radius where we consider that the damage happens.

        public static BlastInfo CalculatePartBlastEffects(Part part, float distanceToHit, double vesselMass,  float explosiveMass, float range)
        {
            float clampedMinDistanceToHit = ClampRange(explosiveMass, distanceToHit);
            float clampedMaxDistanceToHit = ClampRange(explosiveMass, distanceToHit + part.GetAverageBoundSize());

            double minScaledDistance = CalculateScaledDistance(explosiveMass, clampedMinDistanceToHit);
            double maxScaledDistance = CalculateScaledDistance(explosiveMass, clampedMaxDistanceToHit);

            double maxPressurePerMs = CalculateIncidentImpulse(minScaledDistance, explosiveMass);
            double minPressurePerMs = CalculateIncidentImpulse(maxScaledDistance, explosiveMass);


            double totalDamage = (maxPressurePerMs + minPressurePerMs);// * 2 / 2 ;

            float effectivePartArea = part.GetArea() * 0.33f;

            float positivePhase = Mathf.Clamp(distanceToHit, 0.5f, 5f);

            double force = CalculateForce(maxPressurePerMs, effectivePartArea, positivePhase);

            float acceleration = (float) (force / vesselMass);

            // Calculation of damage

            float finalDamage = (float) totalDamage;
         
            return new BlastInfo() { TotalPressure = maxPressurePerMs, EffectivePartArea = effectivePartArea, PositivePhaseDuration = positivePhase,  VelocityChange = acceleration , Damage = finalDamage };
        }

 
        private static double CalculateScaledDistance(float explosiveCharge, float distanceToHit)
        {
            return (distanceToHit / Math.Pow(explosiveCharge, 1f / 3f));
        }


        private static float ClampRange (float explosiveCharge , float distanceToHit)
        {
            float cubeRootOfChargeWeight = (float) Math.Pow(explosiveCharge, 1f / 3f);

            if (distanceToHit < 0.0674f * cubeRootOfChargeWeight)
            {
                return 0.0674f * cubeRootOfChargeWeight;
            }
                return distanceToHit;    
        }

        private static double CalculateIncidentImpulse(double scaledDistance, float explosiveCharge)
        {
            double t = Math.Log(scaledDistance) / Math.Log(10);
            double  cubeRootOfChargeWeight = Math.Pow(explosiveCharge, 0.3333333);
            double  ii = 0;
            if (scaledDistance <= 0.955)
            { //NATO version
                double U = 2.06761908721 + 3.0760329666 * t;
                ii = 2.52455620925 - 0.502992763686 * U +
                     0.171335645235 * Math.Pow(U, 2) +
                     0.0450176963051 * Math.Pow(U, 3) -
                     0.0118964626402 * Math.Pow(U, 4);
            }
            else if (scaledDistance > 0.955)
            { //version from ???
               var  U = -1.94708846747 + 2.40697745406 * t;
                ii = 1.67281645863 - 0.384519026965 * U -
                     0.0260816706301 * Math.Pow(U, 2) +
                     0.00595798753822 * Math.Pow(U, 3) +
                     0.014544526107 * Math.Pow(U, 4) -
                     0.00663289334734 * Math.Pow(U, 5) -
                     0.00284189327204 * Math.Pow(U, 6) +
                     0.0013644816227 * Math.Pow(U, 7);
            }
           
            ii = Math.Pow(10, ii);
            ii = ii * cubeRootOfChargeWeight;
            return ii;
        }

        /// <summary>
        /// Calculate newtons from the pressure in kPa and the surface on Square meters
        /// </summary>
        /// <param name="pressure">kPa</param>
        /// <param name="surface">m2</param>
        /// <returns></returns>
        private static double CalculateForce(double pressure, float surface, double timeInMs)
        {
            return pressure * 1000f * surface * (timeInMs / 1000f);
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
        public float VelocityChange { get; set; }
        public float EffectivePartArea { get; set; }
        public float Damage { get; set; }
        public double TotalPressure { get; set; }
        public double PositivePhaseDuration { get; set; }
    }
}
