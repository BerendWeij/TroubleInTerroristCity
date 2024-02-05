using UnityEngine;

public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo);
}

public enum DamageType
{
    Generic,
    Cut,
    Hit,
    Stab,
    Bullet,
    Explosion,
    Fire
}

/// <summary>
/// 
/// </summary>
public struct DamageInfo
{
    /// <summary>
    /// Damage amount
    /// </summary>
    public float Delta { get; set; }

    /// <summary> </summary>
    public Entity Source { get; set; }

    public DamageType DamageType { get; set; }

    public Transform HitObject { get; set; }

    /// <summary> </summary>
    public Vector3 HitPoint { get; set; }

    /// <summary> </summary>
    public Vector3 HitDirection { get; set; }

    /// <summary> </summary>
    public float HitImpulse { get; set; }

    /// <summary> </summary>
    public Vector3 HitNormal { get; set; }


    public DamageInfo(float delta, Entity source = null, Transform hitObject = null)
    {
        Delta = delta;
        DamageType = DamageType.Generic;
        HitPoint = Vector3.zero;
        HitDirection = Vector3.zero;
        HitImpulse = 0f;
        HitNormal = Vector3.zero;
        Source = source;
        HitObject = hitObject;
    }

    public DamageInfo(float delta, DamageType damageType, Entity source = null, Transform hitObject = null)
    {
        Delta = delta;
        DamageType = damageType;
        HitPoint = Vector3.zero;
        HitDirection = Vector3.zero;
        HitImpulse = 0f;
        HitNormal = Vector3.zero;
        Source = source;
        HitObject = hitObject;
    }

    public DamageInfo(float delta, Vector3 hitPoint, Vector3 hitDirection, float hitImpulse, Entity source = null, Transform hitObject = null)
    {
        Delta = delta;
        DamageType = DamageType.Generic;
        HitPoint = hitPoint;
        HitDirection = hitDirection;
        HitImpulse = hitImpulse;
        HitNormal = Vector3.zero;
        Source = source;
        HitObject = hitObject;
    }

    public DamageInfo(float delta, DamageType damageType, Vector3 hitPoint, Vector3 hitDirection, float hitImpulse, Entity source = null, Transform hitObject = null)
    {
        Delta = delta;
        DamageType = damageType;
        HitPoint = hitPoint;
        HitDirection = hitDirection;
        HitImpulse = hitImpulse;
        HitNormal = Vector3.zero;
        Source = source;
        HitObject = hitObject;
    }

    public DamageInfo(float delta, Vector3 hitPoint, Vector3 hitDirection = default, float hitImpulse = 0f, Vector3 hitNormal = default, Entity source = null, Transform hitObject = null)
    {
        Delta = delta;
        DamageType = DamageType.Generic;
        HitPoint = hitPoint;
        HitDirection = hitDirection;
        HitImpulse = hitImpulse;
        HitNormal = hitNormal;
        Source = source;
        HitObject = hitObject;
    }

    public DamageInfo(float delta, DamageType damageType, Vector3 hitPoint = default, Vector3 hitDirection = default, float hitImpulse = default, Vector3 hitNormal = default, Entity source = null, Transform hitObject = null)
    {
        Delta = delta;
        DamageType = damageType;
        HitPoint = hitPoint;
        HitDirection = hitDirection;
        HitImpulse = hitImpulse;
        HitNormal = hitNormal;
        Source = source;
        HitObject = hitObject;
    }
}