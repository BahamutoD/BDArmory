using BDArmory.Core.Events;

namespace BDArmory.Core.Services
{
    public abstract class DamageService : NotificableService<DamageEventArgs>
    {
        public abstract void ReduceArmor_svc(Part p, float armorMass);

        public abstract void SetDamageToPart_svc(Part p, float damage);

        public abstract void AddDamageToPart_svc(Part p, float damage);

        public abstract void SetArmorThickness_svc(Part p, float thickness);

        public abstract float GetPartDamage_svc(Part p);

        public abstract float GetPartArmor_svc(Part p);

        public abstract float GetMaxPartDamage_svc(Part p);

        public abstract float GetMaxArmor_svc(Part p);

        public abstract void DestroyPart(Part p);
    }
}