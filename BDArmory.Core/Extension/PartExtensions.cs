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

        public static bool hasArmor(this Part p)
        {
            for (int i = 0; i < p.Resources.Count; i++) //"armor" resource containing parts, armor set to how much resource inside
            {
                PartResource currentr = p.Resources[i];
                if (currentr.resourceName == "Armor" && currentr.amount != 0)
                {
                    return true;                
                }
             }

            return false;
        }

        public static double armorMass(this Part p)
        {
            if (hasArmor(p))
            {
                for (int i = 0; i < p.Resources.Count; i++) //"armor" resource containing parts, armor set to how much resource inside
                {
                    PartResource currentr = p.Resources[i];
                    if (currentr.resourceName == "Armor")
                    {
                        if (currentr.amount != 0)
                            return currentr.amount;
                        else
                            return 0.1;
                    }
                }
            }
                      
            return 0.1;            
        }
    }
}