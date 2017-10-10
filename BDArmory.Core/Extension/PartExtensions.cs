using System.Collections.Generic;
using System;
using BDArmory.Core.Services;
using UniLinq;
using UnityEngine;

namespace BDArmory.Core.Extension
{
    public static class PartExtensions
    {
        public static void AddDamage(this Part p, float damage)
        {
            //////////////////////////////////////////////////////////
            // Basic Add Damage for compatibility
            //////////////////////////////////////////////////////////
            damage = (float)Math.Round((double)damage, 2);
            Dependencies.Get<DamageService>().AddDamageToPart(p, damage);
            Debug.Log("[BDArmory]: Final Damage Applied : " + damage);

        }
        public static void AddDamage_Explosive(this Part p,
                                               float heat,
                                               float EXP_MOD,
                                               float DMG_MULT,
                                               float distanceFactor,
                                               float caliber,
                                               bool isMissile)
        {
            double armorMass_ = p.GetArmorMass();
            double armorPCT_ = p.GetArmorPercentage();
            float armorReduction = 0;

            //////////////////////////////////////////////////////////
            // Explosive Damage
            //////////////////////////////////////////////////////////
            float damage = (DMG_MULT / 100) * EXP_MOD * heat * (distanceFactor / (float)armorMass_);

            //////////////////////////////////////////////////////////
            // Armor Reduction
            //////////////////////////////////////////////////////////
            if (p.HasArmor())
            {
                if (!isMissile)
                {
                    if (caliber < 50) damage = damage * heat / 100; //penalty for low-mid caliber HE rounds hitting armor panels
                    armorReduction = damage / 16;
                }
                else
                {
                    armorReduction = damage / 8;
                }
                
            }

            if (armorReduction != 0) p.ReduceArmor(armorReduction);

            //////////////////////////////////////////////////////////
            // Do The Damage
            //////////////////////////////////////////////////////////
            damage = (float)Math.Round((double)damage, 2);
            Dependencies.Get<DamageService>().AddDamageToPart(p, (float)damage);
            Debug.Log("[BDArmory]: ====== Explosion ray hit part! Damage : " + damage);
        }
        public static void AddDamage_Ballistic(this Part p,
                                               float mass,
                                               float caliber,
                                               float multiplier,
                                               float penetrationfactor,
                                               float DMG_MULT,
                                               float impactVelocity)
        {
            double armorMass_ = p.GetArmorMass();
            double armorPCT_ = p.GetArmorPercentage();

            //////////////////////////////////////////////////////////
            // Basic Kinetic Formula
            //////////////////////////////////////////////////////////
            double damage = ((0.5f * (mass * Math.Pow(impactVelocity, 2))) *
                             DMG_MULT
                             * 0.0055f);

            //Now, we know exactly how well the bullet was stopped by the armor. 
            //This value will be below 1 when it is stopped by the armor.
            //That means that we should not apply all the damage to the part that stopped by the bullet
            //Also we are not considering hear the angle of penetration , because we already did on the armor penetration calculations.
            //As armor is decreased level of damage should increase 

            damage = (damage * multiplier); // (float)Math.Max(1f, armorPCT_);

            //penalty for low caliber rounds,not if armor is very low
            if (caliber <= 30f && armorMass_ >= 100d) damage *= 0.0625f; 

            //////////////////////////////////////////////////////////
            // Do The Damage
            //////////////////////////////////////////////////////////
            damage = (float)Math.Round((double)damage, 2);
            Dependencies.Get<DamageService>().AddDamageToPart(p, (float)damage);
            Debug.Log("[BDArmory]: mass: " + mass + " caliber: " + caliber + " multiplier: " + multiplier + " penetrationfactor: " + penetrationfactor);
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
            Debug.Log("[BDArmory]: Final Armor Removed : " + massToReduce);
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