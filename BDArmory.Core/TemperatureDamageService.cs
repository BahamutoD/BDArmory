using BDArmory.Core.Interface;

namespace BDArmory.Core
{
    internal class TemperatureDamageService : IDamageService
    {
        public void SetDamageToPart(Part p, double damage)
        {
            p.temperature = damage;
        }

        public void AddDamageToPart(Part p, double damage)
        {
            p.temperature += damage;
        }
    }
}
