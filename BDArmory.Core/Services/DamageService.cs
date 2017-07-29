using BDArmory.Core.Events;

namespace BDArmory.Core.Services
{
    public abstract class DamageService : NotificableService<DamageEventArgs>
    {
        public abstract void SetDamageToPart(Part p, double damage);

        public abstract void AddDamageToPart(Part p, double damage);
    }
}