using BDArmory.Core.Enum;
using BDArmory.Core.Events;
using BDArmory.Core.Module;

namespace BDArmory.Core.Services
{
    internal class ModuleDamageService : DamageService
    {
        public override void SetDamageToPart(Part p, float damage)
        {
            var damageModule = p.Modules.GetModule<DamageTracker>();

            damageModule.SetDamage(damage);

            PublishEvent(new DamageEventArgs()
            {
                VesselId = p.vessel.GetInstanceID(),
                PartId = p.GetInstanceID(),
                Damage = damage,
                Operation = DamageOperation.Set
            });
        }

        public override void AddDamageToPart(Part p, float damage)
        {
            var damageModule = p.Modules.GetModule<DamageTracker>();

            damageModule.AddDamage(damage);

            PublishEvent(new DamageEventArgs()
            {
                VesselId = p.vessel.GetInstanceID(),
                PartId = p.GetInstanceID(),
                Damage = damage,
                Operation = DamageOperation.Add
            });
        }

        public override float GetPartDamage(Part p)
        {
            return p.Modules.GetModule<DamageTracker>().Damage;
        }

        public override float GetMaxPartDamage(Part p)
        {
            return p.Modules.GetModule<DamageTracker>().GetMaxPartDamage();
        }

        public override void DestroyPart(Part p)
        {
            p.Modules.GetModule<DamageTracker>().DestroyPart();
        }
    }
}
