using BDArmory.Core.Events;

namespace BDArmory.Core.Services
{
    public abstract class DamageService : NotificableService<DamageEventArgs>
    {
        public abstract void ReduceArmorToPart(Part p, float armorMass);

        public abstract void SetDamageToPart(Part p, float damage);

        public abstract void AddDamageToPart(Part p, float damage);

        public abstract float GetPartDamage(Part p);

        public abstract float GetPartArmor(Part p);

        public abstract float GetMaxPartDamage(Part p);

        public abstract float GetMaxArmor(Part p);

        public abstract void DestroyPart(Part p);
    }
}