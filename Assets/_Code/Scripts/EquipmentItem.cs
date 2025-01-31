using Kinemation.FPSFramework.Runtime.FPSAnimator;
using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class DelayedSound : ICloneable
{
    public float Delay;
    public SoundPlayer Sound;

    public object Clone() => MemberwiseClone();
}

public class QueuedSound
{
    public DelayedSound DelayedSound { get; private set; }
    public float PlayTime { get; private set; }

    public QueuedSound(DelayedSound clip, float playTime)
    {
        DelayedSound = clip;
        PlayTime = playTime;
    }
}

public class EquipmentItem : PlayerComponent
{
    #region Internal

    [Serializable]
    public class GeneralEvents
    {
        [Serializable]
        public class SimpleBoolEvent : UnityEvent<bool>
        { }

        [BHeader("Equipped / Unequipped", true)]
        public SimpleBoolEvent OnEquipped = new SimpleBoolEvent();

        [BHeader("Reload Start / Reload Stop", true)]
        public SimpleBoolEvent OnReload = new SimpleBoolEvent();

        [BHeader("Aim Start / Aim Stop", true)]
        public SimpleBoolEvent OnAim = new SimpleBoolEvent();

        [Space]

        public UnityEvent OnUse;
        public UnityEvent OnChangeUseMode;
    }

    #endregion
    public int id = 0;
    public OverlayType overlayType;
    public EquipmentHandler EHandler { get; private set; }

    public EquipmentItemInfo EquipmentInfo = null;

    [SerializeField, Group]
    public GeneralEvents m_GeneralEvents = null;

    protected Animator animator;

    // Using
    protected float m_UseThreshold = 0.1f;
    protected float m_NextTimeCanUse;

    // Aiming
    protected float m_NextTimeCanAim;

    public virtual void OnAimStart()
    {
        //EHandler.Animator_SetInteger(animHash_IdleIndex, 0);
        m_NextTimeCanAim = Time.time + EquipmentInfo.Aiming.AimThreshold;

        m_GeneralEvents.OnAim.Invoke(true);
    }

    public virtual void OnAimStop()
    {
        //EHandler.Animator_SetInteger(animHash_IdleIndex, 1);

        m_GeneralEvents.OnAim.Invoke(false);
    }

    public virtual void Equip(Item item)
    {
        //EAnimation.AssignArmAnimations(EHandler.FPArmsHandler.Animator);
        //EHandler.Animator_SetTrigger(animHash_Equip);
        //EHandler.Animator_SetFloat(animHash_UnequipSpeed, m_GeneralInfo.EquipmentInfo.Unequipping.AnimationSpeed);
        //EHandler.Animator_SetFloat(animHash_EquipSpeed, m_GeneralInfo.EquipmentInfo.Equipping.AnimationSpeed);

        //EHandler.PlayDelayedSounds(m_GeneralInfo.EquipmentInfo.Equipping.Audio);

        //Player.Camera.Physics.PlayDelayedCameraForces(m_GeneralInfo.EquipmentInfo.Equipping.CameraForces);
        //Player.Camera.Physics.AimHeadbobMod = m_GeneralInfo.EquipmentInfo.Aiming.AimCamHeadbobMod;

        //m_GeneralInfo.EquipmentModel.UpdateSkinIDProperty(item);
        //m_GeneralInfo.EquipmentModel.UpdateMaterialsFov();

        m_GeneralEvents.OnEquipped.Invoke(true);
    }

    public virtual void Unequip()
    {
        //if (m_GeneralInfo.EquipmentInfo.Unequipping.Audio != null)
        //	EHandler.PlayPersistentAudio(m_GeneralInfo.EquipmentInfo.Unequipping.Audio[0].Sound, 1f, ItemSelection.Method.RandomExcludeLast);

        //Player.Camera.Physics.PlayDelayedCameraForces(m_GeneralInfo.EquipmentInfo.Unequipping.CameraForces);

        //EHandler.Animator_SetTrigger(animHash_Unequip);

        m_GeneralEvents.OnEquipped.Invoke(false);
    }

    public virtual void Initialize(EquipmentHandler eHandler)
    {
        EHandler = eHandler;
    }


    // Using Methods
    public virtual bool TryUseOnce(Ray[] itemUseRays, int useType = 0) { return false; }
    public virtual bool TryUseContinuously(Ray[] itemUseRays, int useType = 0) { return false; }
    public virtual void OnUseStart() {; }
    public virtual void OnUseEnd() {; }
    public virtual bool TryChangeUseMode() { return false; }

    // Get Using Info
    public virtual float GetUseRaySpreadMod() { return 1f; }
    public virtual float GetTimeBetweenUses() { return m_UseThreshold; }
    public virtual bool CanBeUsed() { return true; } // E.g. Gun: has enough bullets in the magazine
    public virtual int GetUseRaysAmount() { return 1; }

    // Reloading Methods
    public virtual bool TryStartReload() { return false; }
    public virtual bool IsDoneReloading() { return false; }
    public virtual void OnReloadStop() {; }

    // Aiming Methods
    public virtual bool CanAim() => m_NextTimeCanAim < Time.time;
}
