using BDArmory.Core.Events;

namespace BDArmory.Core.Services
{
    public abstract class DamageService : NotificableService<DamageEventArgs>
    {
        public abstract void ReduceArmor_svc(Part p, float armorMass);

        public abstract void SetDamageToPart_svc(Part p, float damage);

        public abstract void AddDamageToPart_svc(Part p, float damage);

        public abstract void AddDamageToKerbal_svc(KerbalEVA kerbal, float damage);

        public abstract float GetPartDamage_svc(Part p);

        public abstract float GetPartArmor_svc(Part p);

        public abstract float GetMaxPartDamage_svc(Part p);

        public abstract float GetMaxArmor_svc(Part p);

        public abstract void DestroyPart_svc(Part p);

        public abstract string GetExplodeMode_svc(Part p);

        public abstract bool HasFireFX_svc(Part p);

        public abstract float GetFireFXTimeOut(Part p);
    }
}
