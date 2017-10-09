using System.Collections.Generic;
using System;
using BDArmory.Core.Services;
using UniLinq;
using UnityEngine;

namespace BDArmory.Core.Extension
{
    public static class PartExtensions
    {
        public static  void AddDamage(this Part p, float damage, float caliber = 35)
        {
            //Dependencies.Get<DamageService>().AddDamageToPart(p, damage);

            //var maxPartDamage = Dependencies.Get<DamageService>().GetMaxPartDamage(p);
            //Dependencies.Get<DamageService>().AddDamageToPart(p, (maxPartDamage * (1f - p.GetArmorPercentage())) * 0.5f);

            //double armorPct_ = p.GetArmorPercentage();
            //double damage_d = (Mathf.Clamp((float)Math.Log10(armorPct_),10,100) + 5) * damage;
            //float damage_f = (float) damage_d;

            //if (caliber <= 30 && armorPct_ >= 0.10) damage_f *= 0.0625f; //penalty for low caliber rounds,not if armor is very low

            //Moving to ApplyDamage / PooledBullet
            damage = (float) Math.Round((double)damage, 2);
            Dependencies.Get<DamageService>().AddDamageToPart(p, damage);
            Debug.Log("[BDArmory]: Final Damage Applied : " + damage);
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
            massToReduce = Math.Max(0.10,Math.Round(massToReduce, 2));
            Dependencies.Get<DamageService>().ReduceArmorToPart(p, (float) massToReduce );            
           
        }

        /// <summary>
        /// This method returns the amount of Armor resource
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double GetArmorMass(this Part p)
        {
            if (p == null) return 0d;        
            return Dependencies.Get<DamageService>().GetPartArmor(p);
        }

        public static float GetArmorPercentage(this Part p)
        {
            if (p == null) return 0;
            float armor_ = Dependencies.Get<DamageService>().GetPartArmor(p);
            float maxArmor_ = Dependencies.Get<DamageService>().GetMaxArmor(p);
            return armor_ / maxArmor_;
        }
        //Thanks FlowerChild
        //refreshes part action window
        public static void RefreshAssociatedWindows(this Part part)
        {
            IEnumerator<UIPartActionWindow> window = UnityEngine.Object.FindObjectsOfType(typeof(UIPartActionWindow)).Cast<UIPartActionWindow>().GetEnumerator();
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