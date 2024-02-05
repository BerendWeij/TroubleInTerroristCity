using UnityEngine;

[RequireComponent(typeof(Entity))]
public class EntityDamageDealer : EntityComponent
{
    private void Start()
    {
        Entity.DealDamage.SetTryer(DealDamage);
    }

    protected virtual bool DealDamage(DamageInfo damageInfo, IDamageable damageable = null)
    {
        if (damageable != null)
        {
            DealDamage(damageable, damageInfo);

            return true;
        }

        if (damageInfo.HitObject.TryGetComponent(out IDamageable dmgObject))
        {
            DealDamage(dmgObject, damageInfo);

            return true;
        }

        return false;
    }

    protected virtual void DealDamage(IDamageable damageable, DamageInfo damageInfo)
    {
        damageable.TakeDamage(damageInfo);
    }
}