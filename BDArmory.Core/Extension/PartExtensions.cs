using BDArmory.Core.Services;

namespace BDArmory.Core.Extension
{
    public static class PartExtensions
    {
        public static  void AddDamage(this Part p, double damage)
        {
            Dependencies.Get<DamageService>().AddDamageToPart(p, damage);
        }

        public static void SetDamage(this Part p, double damage)
        {
            Dependencies.Get<DamageService>().SetDamageToPart(p, damage);
        }
    }
}