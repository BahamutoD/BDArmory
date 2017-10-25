using System;
using BDArmory.Core.Enum;
using BDArmory.Core.Events;
using BDArmory.Core.Module;
using UnityEngine;

namespace BDArmory.Core.Services
{
    internal class ModuleDamageService : DamageService
    {
        public override void ReduceArmor_svc(Part p, float armorMass)
        {
            var damageModule = p.Modules.GetModule<HitpointTracker>();

            damageModule.ReduceArmor(armorMass);

            PublishEvent(new DamageEventArgs()
            {
                VesselId = p.vessel.GetInstanceID(),
                PartId = p.GetInstanceID(),
                Armor= armorMass,
                Operation = DamageOperation.Set
            });
        }
        
        public override void SetDamageToPart_svc(Part p, float PartDamage)
        {
            var damageModule = p.Modules.GetModule<HitpointTracker>();

            damageModule.SetDamage(PartDamage);

            PublishEvent(new DamageEventArgs()
            {
                VesselId = p.vessel.GetInstanceID(),
                PartId = p.GetInstanceID(),
                Damage = PartDamage,
                Operation = DamageOperation.Set
            });
        }              

        public override void AddDamageToPart_svc(Part p, float PartDamage)
        {
            var damageModule = p.Modules.GetModule<HitpointTracker>();

            damageModule.AddDamage(PartDamage);

            PublishEvent(new DamageEventArgs()
            {
                VesselId = p.vessel.GetInstanceID(),
                PartId = p.GetInstanceID(),
                Damage = PartDamage,
                Operation = DamageOperation.Add
            });
        }

        public override float GetPartDamage_svc(Part p)
        {
            return p.Modules.GetModule<HitpointTracker>().Hitpoints;
        }

        public override float GetPartArmor_svc(Part p)
        {
            float armor_ = Mathf.Max(1, p.Modules.GetModule<HitpointTracker>().Armor);
            return armor_;
        }

        public override float GetMaxPartDamage_svc(Part p)
        {
            return p.Modules.GetModule<HitpointTracker>().GetMaxHitpoints();
        }

        public override float GetMaxArmor_svc(Part p)
        {
            return p.Modules.GetModule<HitpointTracker>().GetMaxArmor();
        }
 
        public override void DestroyPart(Part p)
        {
            p.Modules.GetModule<HitpointTracker>().DestroyPart();
        }
    }
}
