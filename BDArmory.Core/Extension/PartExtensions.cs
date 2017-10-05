using System.Collections.Generic;
using BDArmory.Core.Services;
using UniLinq;
using UnityEngine;

namespace BDArmory.Core.Extension
{
    public static class PartExtensions
    {
        public static  void AddDamage(this Part p, float damage)
        {
            Dependencies.Get<DamageService>().AddDamageToPart(p, damage);  
        }

        public static void Destroy(this Part p)
        {
            Dependencies.Get<DamageService>().SetDamageToPart(p, float.MaxValue);
        }

        public static bool HasArmor(this Part p)
        {
            return p.GetArmorMass() > 0d;
        }

        public static float Damage(this Part p)
        {
            return Dependencies.Get<DamageService>().GetPartDamage(p);
        }

        public static float MaxDamage(this Part p)
        {
            return Dependencies.Get<DamageService>().GetMaxPartDamage(p);
        }

        public static void ReduceArmor(this Part p, double massToReduce)
        {
            if (!p.HasArmor()) return;

            //p.RequestResource("Armor", massToReduce);

            Dependencies.Get<DamageService>().ReduceArmorToPart(p, (float) massToReduce );

            var maxPartDamage = Dependencies.Get<DamageService>().GetMaxPartDamage(p);

            //Dependencies.Get<DamageService>().SetDamageToPart(p, maxPartDamage * (1f - p.GetArmorPercentage()));
        }

        /// <summary>
        /// This method returns the amount of Armor resource
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double GetArmorMass(this Part p)
        {

            if (p == null) return 0d;

            //using (var resourceEnumerator = p.Resources.GetEnumerator())
            //{
            //    while (resourceEnumerator.MoveNext())
            //    {
            //        if(resourceEnumerator.Current == null) continue;

            //        PartResource currentr = resourceEnumerator.Current;
            //        if (currentr.resourceName == "Armor")
            //        {
            //            return currentr.amount;
            //        }
            //    }
            //}

            //return 0d;            
            return Dependencies.Get<DamageService>().GetPartArmor(p);
        }

        public static float GetArmorPercentage(this Part p)
        {
            if (p == null) return 0;

            //using (var resourceEnumerator = p.Resources.GetEnumerator())
            //{
            //    while (resourceEnumerator.MoveNext())
            //    {
            //        if (resourceEnumerator.Current == null) continue;

            //        PartResource currentr = resourceEnumerator.Current;
            //        if (currentr.resourceName == "Armor")
            //        {
            //            return (float) (currentr.amount / currentr.maxAmount);
            //        }
            //    }
            //}
            //return 0;
            return Dependencies.Get<DamageService>().GetPartArmor(p) / Dependencies.Get<DamageService>().GetMaxArmor(p);
        }
        //Thanks FlowerChild
        //refreshes part action window
        public static void RefreshAssociatedWindows(this Part part)
        {
            IEnumerator<UIPartActionWindow> window = Object.FindObjectsOfType(typeof(UIPartActionWindow)).Cast<UIPartActionWindow>().GetEnumerator();
            while (window.MoveNext())
            {
                if (window.Current == null) continue;
                if (window.Current.part == part)
                {
                    window.Current.displayDirty = true;
                }
            }
            window.Dispose();
        }
    }
}