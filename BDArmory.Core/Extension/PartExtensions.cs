using BDArmory.Core.Services;
using UnityEngine;

namespace BDArmory.Core.Extension
{
    public static class PartExtensions
    {
        public static  void AddDamage(this Part p, double damage)
        {
            Dependencies.Get<DamageService>().AddDamageToPart(p, damage);  
        }

        public static void SetDamage(this Part p, double damage)
        {
            Dependencies.Get<DamageService>().SetDamageToPart(p, damage);
        }

    
        public static bool HasArmor(this Part p)
        {
            return p.GetArmorMass() > 0;
        }

        public static void ReduceArmor(this Part p, double massToReduce)
        {
            if (p.HasArmor())
            {
                p.RequestResource("Armor", massToReduce);
                p.SetDamage(p.maxTemp * (1f - p.GetArmorPercentage()));
            }
        }

        /// <summary>
        /// This method returns the amount of Armor resource
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double GetArmorMass(this Part p)
        {
            if (p == null) return 0;

            using (var resourceEnumerator = p.Resources.GetEnumerator())
            {
                while (resourceEnumerator.MoveNext())
                {
                    if(resourceEnumerator.Current == null) continue;
                    
                    PartResource currentr = resourceEnumerator.Current;
                    if (currentr.resourceName == "Armor")
                    {
                        return currentr.amount;
                    }
                }
            }
            return 0;            
        }

        public static float GetArmorPercentage(this Part p)
        {
            if (p == null) return 0;

            using (var resourceEnumerator = p.Resources.GetEnumerator())
            {
                while (resourceEnumerator.MoveNext())
                {
                    if (resourceEnumerator.Current == null) continue;

                    PartResource currentr = resourceEnumerator.Current;
                    if (currentr.resourceName == "Armor")
                    {
                        return (float) (currentr.amount / currentr.maxAmount);
                    }
                }
            }
            return 0;
        }

        /// <summary>
        ///     Gets the dry mass of the part.
        /// </summary>
        public static double GetDryMass(this Part part)
        {
            return (part.physicalSignificance == Part.PhysicalSignificance.FULL) ? part.mass : 0d;
        }
    }
}