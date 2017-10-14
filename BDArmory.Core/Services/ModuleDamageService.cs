using System;
using BDArmory.Core.Enum;
using BDArmory.Core.Events;
using BDArmory.Core.Module;

namespace BDArmory.Core.Services
{
    internal class ModuleDamageService : DamageService
    {
        public override void ReduceArmor_svc(Part p, float armorMass)
        {
            var damageModule = p.Modules.GetModule<DamageTracker>();

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
            var damageModule = p.Modules.GetModule<DamageTracker>();

            damageModule.SetDamage(PartDamage);

            PublishEvent(new DamageEventArgs()
            {
                VesselId = p.vessel.GetInstanceID(),
                PartId = p.GetInstanceID(),
                Damage = PartDamage,
                Operation = DamageOperation.Set
            });
        }

        public override void SetArmorThickness_svc(Part p, float thickness)
        {
            var damageModule = p.Modules.GetModule<DamageTracker>();

            damageModule.SetThickness(thickness);

            PublishEvent(new DamageEventArgs()
            {
                VesselId = p.vessel.GetInstanceID(),
                PartId = p.GetInstanceID(),
                Armor = thickness,
                Operation = DamageOperation.Set
            });
        }            

        public override void AddDamageToPart_svc(Part p, float PartDamage)
        {
            var damageModule = p.Modules.GetModule<DamageTracker>();

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
            return p.Modules.GetModule<DamageTracker>().Damage;
        }

        public override float GetPartArmor_svc(Part p)
        {
            return p.Modules.GetModule<DamageTracker>().Armor;
        }

        public override float GetMaxPartDamage_svc(Part p)
        {
            return p.Modules.GetModule<DamageTracker>().GetMaxPartDamage();
        }

        public override float GetMaxArmor_svc(Part p)
        {
            return p.Modules.GetModule<DamageTracker>().GetMaxArmor();
        }

        public override void DestroyPart(Part p)
        {
            p.Modules.GetModule<DamageTracker>().DestroyPart();
        }
    }
}
