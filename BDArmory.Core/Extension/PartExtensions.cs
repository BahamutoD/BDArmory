using BDArmory.Core.Services;

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

        /// <summary>
        /// This method returns the amount of Armor resource
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double GetArmorMass(this Part p)
        {
            using (var resourceEnumerator = p.Resources.GetEnumerator())
            {
                while (resourceEnumerator.MoveNext())
                {
                    PartResource currentr = resourceEnumerator.Current;
                    if (currentr.resourceName == "Armor")
                    {
                        return currentr.amount;
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