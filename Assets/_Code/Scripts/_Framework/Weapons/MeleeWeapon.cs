using System;
using System.Collections;
using HQFPSTemplate.Surfaces;
using UnityEngine;

public class MeleeWeapon : EquipmentItem
{
    #region Anim Hashing

    //Hashed animator strings (Improves performance)
    private readonly int animHash_SwingSpeed = Animator.StringToHash("Swing Speed");
    private readonly int animHash_SwingIndex = Animator.StringToHash("Swing Index");
    private readonly int animHash_Swing = Animator.StringToHash("Swing");

    #endregion

    private MeleeWeaponInfo _meleeWeaponInfo;

    private int m_LastFreeSwing;
    private float m_NextResetSwingSelectionTime;


    public override void Initialize(EquipmentHandler eHandler)
    {
        base.Initialize(eHandler);

        _meleeWeaponInfo = EquipmentInfo as MeleeWeaponInfo;
    }

    public override bool TryUseOnce(Ray[] itemUseRays, int useType)
    {
        if (Time.time < m_NextTimeCanUse)
            return false;

        MeleeWeaponInfo.SwingData swing;

        //Select Swing
        if (Time.time > m_NextResetSwingSelectionTime && _meleeWeaponInfo.MeleeSettings.ResetSwingsIfNotUsed)
            swing = _meleeWeaponInfo.MeleeSettings.Swings[0];
        else
            swing = _meleeWeaponInfo.MeleeSettings.Swings.Select(ref m_LastFreeSwing, _meleeWeaponInfo.MeleeSettings.SwingSelection);

        m_UseThreshold = swing.Cooldown;
        m_NextTimeCanUse = Time.time + m_UseThreshold;

        if (_meleeWeaponInfo.MeleeSettings.ResetSwingsIfNotUsed)
            m_NextResetSwingSelectionTime = Time.time + _meleeWeaponInfo.MeleeSettings.ResetSwingsDelay;

        //EHandler.Animator_SetFloat(animHash_SwingIndex, swing.AnimationIndex);
        //EHandler.Animator_SetTrigger(animHash_Swing);
        //EHandler.Animator_SetFloat(animHash_SwingSpeed, swing.AnimationSpeed);

        //Player.Camera.Physics.AddPositionForce(swing.SwingCamForces.PositionForce);
        //Player.Camera.Physics.AddRotationForce(swing.SwingCamForces.RotationForce);

        EHandler.PlayDelayedSound(swing.SwingAudio);

        StartCoroutine(C_SphereCastDelayed(swing));

        return true;
    }

    public override bool TryUseContinuously(Ray[] itemUseRays, int useType)
    {
        if (!_meleeWeaponInfo.MeleeSettings.CanContinuouslyAttack)
            return false;

        return TryUseOnce(itemUseRays, useType);
    }

    protected virtual IDamageable SphereCast(Ray itemUseRays, MeleeWeaponInfo.SwingData swing)
    {
        IDamageable damageable = null;

        if (Physics.SphereCast(itemUseRays.origin, swing.CastRadius, itemUseRays.direction, out RaycastHit hitInfo,
                _meleeWeaponInfo.MeleeSettings.MaxHitDistance, _meleeWeaponInfo.MeleeSettings.HitMask, QueryTriggerInteraction.Collide))
        {
            SurfaceManager.SpawnEffect(hitInfo, _meleeWeaponInfo.MeleeSettings.ImpactEffect, 1f);

            // Apply an impact impulse
            if (hitInfo.rigidbody != null)
                hitInfo.rigidbody.AddForceAtPosition(itemUseRays.direction * swing.HitImpact, hitInfo.point,
                    ForceMode.Impulse);

            var damageData = new DamageInfo(-swing.HitDamage, _meleeWeaponInfo.MeleeSettings.DamageType, hitInfo.point,
                itemUseRays.direction, swing.HitImpact, Player, hitInfo.transform);

            // Audio
            EHandler.PlayDelayedSound(swing.HitAudio);

            // Camera force
            //Player.Camera.Physics.AddPositionForce(swing.HitCamForces.PositionForce);
            //Player.Camera.Physics.AddRotationForce(swing.HitCamForces.RotationForce);

            // Try to damage the Hit object
            Player.DealDamage.Try(damageData, null);
        }

        return damageable;
    }

    private IEnumerator C_SphereCastDelayed(MeleeWeaponInfo.SwingData swing)
    {
        yield return new WaitForSeconds(swing.CastDelay);

        m_GeneralEvents.OnUse.Invoke();

        // Small hack until a better solution is found
        Ray[] itemUseRay = EHandler.GenerateItemUseRays(Player, EHandler.ItemUseTransform, 1, 1f);

        SphereCast(itemUseRay[0], swing);
    }
}


[CreateAssetMenu(fileName = "Melee Weapon Info", menuName = "HQ FPS Template/Equipment/Melee Weapon")]
public class MeleeWeaponInfo : EquipmentItemInfo
{
    #region Internal

    [Serializable]
    public class MeleeSettingsInfo
    {
        [BHeader("General", true)] public LayerMask HitMask;

        [Range(0f, 3f)] [Tooltip("How far can this weapon hit stuff?")]
        public float MaxHitDistance = 1.5f;

        [Space(3f)] public SurfaceEffects ImpactEffect = SurfaceEffects.Slash;
        public DamageType DamageType = DamageType.Hit;

        [Space(3f)] [BHeader("( Swings )", order = 2)]
        public bool CanContinuouslyAttack;

        public bool ResetSwingsIfNotUsed;

        [ShowIf("ResetSwingsIfNotUsed", true)]
        public float ResetSwingsDelay = 1f;

        public ItemSelection.Method SwingSelection = ItemSelection.Method.RandomExcludeLast;
        public SwingData[] Swings;
    }

    [Serializable]
    public class SwingData
    {
        public string SwingName = "Strong Attack";

        [Space(3)] [Tooltip("Useful for limiting the number of hits you can do in a period of time.")]
        public float Cooldown = 1f;

        [Range(0.01f, 5f)] public float CastDelay = 0.4f;

        [Range(0.01f, 10f)] public float CastRadius = 0.2f;

        [Range(1f, 500f)] public float HitDamage = 15f;

        [Range(1f, 500f)] public float HitImpact = 30f;

        [BHeader("( Animation )", order = 2)] public int AnimationIndex;
        public float AnimationSpeed = 1f;

        [BHeader("( Audio )", order = 2)] public DelayedSound SwingAudio;
        public DelayedSound HitAudio;

        //[BHeader("( Camera )", order = 2)]

        //public RecoilForce SwingCamForces;
        //public RecoilForce HitCamForces;
    }

    #endregion

    [Group("5: ")] public MeleeSettingsInfo MeleeSettings;
}