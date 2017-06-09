using BDArmory.Core.Interface;

namespace BDArmory.Core.Extension
{
    public static class PartExtensions
    {
        public static  void AddDamage(this Part p, double damage)
        {
            Dependencies.Get<IDamageService>().AddDamageToPart(p, damage);
        }

        public static void SetDamage(this Part p, double damage)
        {
            Dependencies.Get<IDamageService>().SetDamageToPart(p, damage);
        }
    }
}