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
                Armor = armorMass,
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

        public override void AddDamageToKerbal_svc(KerbalEVA kerbal, float damage)
        {
            var damageModule = kerbal.part.Modules.GetModule<HitpointTracker>();

            damageModule.AddDamageToKerbal(kerbal, damage);

            PublishEvent(new DamageEventArgs()
            {
                VesselId = kerbal.part.vessel.GetInstanceID(),
                PartId = kerbal.part.GetInstanceID(),
                Damage = damage,
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

        public override void DestroyPart_svc(Part p)
        {
            p.Modules.GetModule<HitpointTracker>().DestroyPart();
        }

        public override string GetExplodeMode_svc(Part p)
        {
            return p.Modules.GetModule<HitpointTracker>().ExplodeMode;
        }

        public override bool HasFireFX_svc(Part p)
        {
            if (p == null) return false;
            if (p.Modules.GetModule<HitpointTracker>() == null) return false;

            return p.Modules.GetModule<HitpointTracker>().GetFireFX();
        }

        public override float GetFireFXTimeOut(Part p)
        {
            return p.Modules.GetModule<HitpointTracker>().FireFXLifeTimeInSeconds;
        }
    }
}
