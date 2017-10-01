using BDArmory.Core.Events;

namespace BDArmory.Core.Services
{
    public abstract class DamageService : NotificableService<DamageEventArgs>
    {
        public abstract void SetDamageToPart(Part p, float damage);

        public abstract void AddDamageToPart(Part p, float damage);

        public abstract float GetPartDamage(Part p);

        public abstract float GetMaxPartDamage(Part p);

        public abstract void DestroyPart(Part p);
    }
}