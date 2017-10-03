using BDArmory.Core.Enum;
using BDArmory.Core.Events;
using BDArmory.Core.Module;

namespace BDArmory.Core.Services
{
    internal class ModuleDamageService : DamageService
    {
        public override void SetDamageToPart(Part p, float PartDamage)
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

        public override void AddDamageToPart(Part p, float PartDamage)
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
