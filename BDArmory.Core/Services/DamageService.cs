using BDArmory.Core.Events;
using BDArmory.Core.Services;

namespace BDArmory.Core.Services
{
    public abstract class DamageService : NotificableService<DamageEventArgs>
    {
        public abstract void SetDamageToPart(Part p, double damage);

        public abstract void AddDamageToPart(Part p, double damage);
    }
}