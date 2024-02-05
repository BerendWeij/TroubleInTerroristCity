using System;
using System.Collections.Generic;
using Kinemation.FPSFramework.Runtime.Recoil;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EquipmentHandler : PlayerComponent
{
    [Serializable]
    public struct UseRaySpread
    {
        [Range(0.01f, 10f)]
        public float JumpSpreadMod,
                     RunSpreadMod,
                     CrouchSpreadMod,
                     ProneSpreadMod,
                     WalkSpreadMod,
                     AimSpreadMod;
    }

    public int ContinuouslyUsedTimes { get => _continuouslyUsedTimes; }
    public Message OnChangeItem = new();
    public Activity UsingItem = new();
    
    public Transform ItemUseTransform => _itemUseTransform;
    public NetworkPlayerAnimController NetworkPlayerAnimController => networkPlayerAnimController;
    public EquipmentItem EquipmentItem => _attachedEquipmentItem;
    public RecoilAnimation RecoilAnimation => recoilAnimation;
    

    [SerializeField]
    protected Transform _itemUseTransform;

    [SerializeField]
    [Group("Inverse of Accuracy - ")]
    protected UseRaySpread _useRaySpread;

    protected EquipmentItem _attachedEquipmentItem;
    protected Item _attachedItem;
    protected Unarmed _unarmed;

    [SerializeField] private Animator animator;
    [FormerlySerializedAs("playerAnimController")] [SerializeField] private NetworkPlayerAnimController networkPlayerAnimController;

    [SerializeField] private RecoilAnimation recoilAnimation;
    private static readonly int OverlayType = Animator.StringToHash("OverlayType");
    private static readonly int Equip = Animator.StringToHash("Equip");
    private static readonly int UnEquip = Animator.StringToHash("Unequip");

    protected AudioSource m_AudioSource;
    protected AudioSource m_PersistentAudioSource;

    protected int _continuouslyUsedTimes;
    protected float _nextTimeCanUseItem = -1f;

    protected List<QueuedSound> m_QueuedSounds = new();

    public Dictionary<int, EquipmentItem> _equipmentItems = new();

    protected void Start()
    {
        _unarmed = GetComponentInChildren<Unarmed>(true);

        EquipmentItem[] equipmentItems = GetComponentsInChildren<EquipmentItem>(true);
        ItemInfo itemInfo;
        foreach (var eItem in equipmentItems)
        {
            itemInfo = ItemDatabase.GetItemByName(eItem.CorrespondingItemName);
            
            if (eItem != _unarmed)
            {
                int id = itemInfo.Id;

                //Initialize the equipment items
                if (!_equipmentItems.ContainsKey(id))
                    _equipmentItems.Add(id, eItem);
                else
                    Debug.LogWarning($"There are multiple equipment items that correspond to the same item under '{gameObject.name}'");
            }
                
            eItem.Initialize(this);

            // Notify the item components (e.g. animation, physics etc.) present on the Equipment Item object
            var itemComponents = eItem.gameObject.GetComponents<IEquipmentComponent>();

            if (itemComponents.Length > 0)
            {
                foreach (var component in itemComponents)
                    component.Initialize(eItem);
            }

            eItem.gameObject.SetActive(false);
        }
        
        // Equipment Items AudioSource (For Overall first person items audio)
        m_AudioSource = AudioUtils.CreateAudioSource("Audio Source", transform, Vector3.zero);
        m_AudioSource.bypassEffects = m_AudioSource.bypassListenerEffects = m_AudioSource.bypassReverbZones = false;
        m_AudioSource.maxDistance = 500f;

        // Persistent AudioSource (e.g. used for the fire tail sounds)
        m_PersistentAudioSource = AudioUtils.CreateAudioSource("Persistent Audio Source", transform, Vector3.zero, true, 1f, 2.5f);
        m_PersistentAudioSource.bypassEffects = m_PersistentAudioSource.bypassListenerEffects = m_PersistentAudioSource.bypassReverbZones = false;
        m_PersistentAudioSource.maxDistance = 500f;
    }

    public bool ContainsEquipmentItem(int itemId) => _equipmentItems.ContainsKey(itemId);

    public virtual void EquipItem(Item item)
    {
        ClearDelayedSounds();
        _attachedItem = item;

        // Disable previous equipment item
        if (_attachedEquipmentItem != null)
            _attachedEquipmentItem.gameObject.SetActive(false);

        int itemId = item != null ? item.Id : 0;

        // Enable next equipment item
        _attachedEquipmentItem = GetEquipmentItem(itemId);
        _attachedEquipmentItem.gameObject.SetActive(true);

        //this should be moved
        animator.SetFloat(OverlayType, (float)_attachedEquipmentItem.EquipmentInfo.General.overlayType);
        networkPlayerAnimController.StopAnimation(0.1f);

        InitWeapon(_attachedEquipmentItem);
        animator.Play(Equip);
        //animator.Play(gun.poseName);
        //Player.EquipmentController.SetActiveEquipment(gun);
        animator.SetFloat(OverlayType, (float) _attachedEquipmentItem.EquipmentInfo.General.overlayType);
        
        _attachedEquipmentItem.gameObject.SetActive(true);
        // Notify the item components (e.g. animation, physics etc.) present on the Equipment Item object
        IEquipmentComponent[] itemComponents = _attachedEquipmentItem.GetComponents<IEquipmentComponent>();

        if (itemComponents.Length > 0)
        {
            foreach (var component in itemComponents)
                component.OnSelected();
        }

        //SetCharacterMovementSpeed(Player.Aim.Active ? _attachedEquipmentItem.EInfo.Aiming.AimMovementSpeedMod : 1f);
        _nextTimeCanUseItem = Time.time + _attachedEquipmentItem.EquipmentInfo.Equipping.Duration;

        OnChangeItem.Send();
        
        _attachedEquipmentItem.Equip(item);
    }

    public void StartWeaponChange()
    {
        animator.CrossFade(UnEquip, 0.1f);
    }


    public EquipmentItem GetEquipmentItem(int itemId)
    {
        if (itemId == 0)
            return _unarmed;

        if (_equipmentItems.TryGetValue(itemId, out EquipmentItem equipmentItem))
            return equipmentItem;
        return null;
    }

    protected virtual void Update()
    {
        for (int i = 0; i < m_QueuedSounds.Count; i++)
        {
            if (Time.time >= m_QueuedSounds[i].PlayTime)
            {
                m_QueuedSounds[i].DelayedSound.Sound.Play(ItemSelection.Method.RandomExcludeLast, m_AudioSource);
                m_QueuedSounds.RemoveAt(i);
            }
        }

        if (_attachedEquipmentItem != null)
        {
            //Stop the UsingItem activity after a few miliseconds from being used (e.g. this will not stop the activity if an item being used continuously)
            if (Player.UseItem.LastExecutionTime + Mathf.Clamp(_attachedEquipmentItem.GetTimeBetweenUses() * 2f, 0f, 0.3f) < Time.time && UsingItem.Active)
            {
                UsingItem.ForceStop();
                _attachedEquipmentItem.OnUseEnd();
                _continuouslyUsedTimes = 0;
            }
        }
    }

    protected void InitWeapon(EquipmentItem weapon)
    {
        recoilAnimation.Init(weapon.generalInfo.weaponAnimAsset.recoilData, weapon.FireRate, weapon.FireMode);
        networkPlayerAnimController.FpsAnimator.OnGunEquipped(weapon.generalInfo.weaponAnimAsset, weapon.generalInfo.weaponTransformData);
        networkPlayerAnimController.FpsAnimator.ikRigData.weaponTransform = weapon.generalInfo.weaponAnimAsset.weaponBone;
        networkPlayerAnimController.LookLayer.SetAimOffsetTable(weapon.generalInfo.weaponAnimAsset.aimOffsetTable);

        networkPlayerAnimController.FpsAnimator.OnPrePoseSampled();
        networkPlayerAnimController.PlayPose(weapon.generalInfo.weaponAnimAsset.overlayPose);
        networkPlayerAnimController.FpsAnimator.OnPoseSampled();
    }

    public virtual void UnequipItem()
    {
        if (_attachedEquipmentItem == null)
            return;

        animator.CrossFade(UnEquip, 0.1f);
        _attachedItem = null;
        _nextTimeCanUseItem = Time.time + _attachedEquipmentItem.EquipmentInfo.Unequipping.Duration;

        EquipmentItem.Unequip();
    }

    public virtual bool TryStartReload() => _attachedEquipmentItem.TryStartReload();
    public virtual void StartReload() => _attachedEquipmentItem.StartReload();

    public virtual bool TryStartAim()
    {
        if (_nextTimeCanUseItem > Time.time ||
            (!_attachedEquipmentItem.EquipmentInfo.Aiming.AimWhileAirborne && !Player.IsGrounded.Get()) || // Can this item be aimed while airborne?
            !_attachedEquipmentItem.EquipmentInfo.Aiming.Enabled || !_attachedEquipmentItem.CanAim()) // Can this item be aimed?
            return false;

        return true;
    }

    public virtual void StartAim()
    {
        //SetCharacterMovementSpeed(m_AttachedEquipmentItem.EInfo.Aiming.AimMovementSpeedMod);
        networkPlayerAnimController.AdsLayer.SetAds(true);
        networkPlayerAnimController.SwayLayer.SetFreeAimEnable(false);
        recoilAnimation.isAiming = true;

        _attachedEquipmentItem.OnAimStart();
    }

    public virtual void OnAimStop()
    {
        //SetCharacterMovementSpeed(1f);
        networkPlayerAnimController.AdsLayer.SetAds(false);
        networkPlayerAnimController.AdsLayer.SetPointAim(false);
        networkPlayerAnimController.SwayLayer.SetFreeAimEnable(true);
        recoilAnimation.isAiming = false;

        if (_attachedEquipmentItem != null)
            _attachedEquipmentItem.OnAimStop();
    }

    public virtual bool TryStartPointAiming()
    {
        networkPlayerAnimController.AdsLayer.SetPointAim(false);

        return true;
    }

    public virtual void OnPointAimingStop()
    {
        networkPlayerAnimController.AdsLayer.SetPointAim(true);
    }

    public virtual bool TryStartHolster()
    {
        networkPlayerAnimController.LocoLayer.SetReadyWeight(1f);
        networkPlayerAnimController.LookLayer.SetLayerAlpha(.5f);

        return true;
    }

    public virtual void OnHolsterStop()
    {
        networkPlayerAnimController.LocoLayer.SetReadyWeight(0f);
        networkPlayerAnimController.LookLayer.SetLayerAlpha(1f);
    }

    public virtual bool TryUse(bool continuously, int useType)
    {
        bool usedSuccessfully = false;

        if (_nextTimeCanUseItem < Time.time)
        {
            // Use Rays (E.g Weapons with more projectiles per shot will need more rays - Shotguns)
            Ray[] itemUseRays = GenerateItemUseRays(Player, _itemUseTransform, _attachedEquipmentItem.GetUseRaysAmount(), _attachedEquipmentItem.GetUseRaySpreadMod());

            if (continuously)
                usedSuccessfully = _attachedEquipmentItem.TryUseContinuously(itemUseRays, useType);
            else
                usedSuccessfully = _attachedEquipmentItem.TryUseOnce(itemUseRays, useType);
            
            if (usedSuccessfully)
            {
                if (!UsingItem.Active)
                {
                    UsingItem.ForceStart();
                    EquipmentItem.OnUseStart();
                }

                //Increment the 'm_ContinuouslyUsedTimes' variable, which shows how many times the weapon has been used consecutively
                if (UsingItem.Active)
                    _continuouslyUsedTimes++;
                else
                    _continuouslyUsedTimes = 1;
            }
        }

        return usedSuccessfully;
    }

    public Ray[] GenerateItemUseRays(Humanoid humanoid, Transform anchor, int raysAmount, float equipmentSpreadMod)
    {
        var itemUseRays = new Ray[raysAmount];

        float spreadMod = 1f;

        if (humanoid != null)
        {
            if (humanoid.Jump.Active)
                spreadMod *= _useRaySpread.JumpSpreadMod;
            else if (humanoid.Sprint.Active)
                spreadMod *= _useRaySpread.RunSpreadMod;
            else if (humanoid.Crouch.Active)
                spreadMod *= _useRaySpread.CrouchSpreadMod;
            else if (humanoid.Prone.Active)
                spreadMod *= _useRaySpread.ProneSpreadMod;
            else if (humanoid.Walk.Active)
                spreadMod *= _useRaySpread.WalkSpreadMod;

            if (humanoid.Aim.Active)
                spreadMod *= _useRaySpread.AimSpreadMod;
        }

        float raySpread = equipmentSpreadMod * spreadMod;

        for (int i = 0; i < itemUseRays.Length; i++)
        {
            Vector3 raySpreadVector = anchor.TransformVector(new Vector3(Random.Range(-raySpread, raySpread), Random.Range(-raySpread, raySpread), 0f));
            Vector3 rayDirection = Quaternion.Euler(raySpreadVector) * anchor.forward;

            itemUseRays[i] = new Ray(anchor.position, rayDirection);
        }

        return itemUseRays;
    }

    #region Audio

    public void PlayPersistentAudio(SoundPlayer soundPlayer, float volume, ItemSelection.Method selectionMethod = ItemSelection.Method.RandomExcludeLast)
    {
        soundPlayer.Play(selectionMethod, m_PersistentAudioSource, volume);
    }

    public void PlayPersistentAudio(AudioClip clip, float volume)
    {
        m_PersistentAudioSource.PlayOneShot(clip, volume);
    }

    public void ClearDelayedSounds() { m_QueuedSounds.Clear(); }

    public void PlayDelayedSound(DelayedSound delayedSound)
    {
        m_QueuedSounds.Add(new QueuedSound(delayedSound, Time.time + delayedSound.Delay));
    }

    public void PlayDelayedSounds(DelayedSound[] clipsData)
    {
        for (int i = 0; i < clipsData.Length; i++)
            PlayDelayedSound(clipsData[i]);
    }

    public void PlaySound(SoundPlayer soundPlayer, float volume, ItemSelection.Method selectionMethod = ItemSelection.Method.RandomExcludeLast)
    {
        soundPlayer.Play(selectionMethod, m_AudioSource, volume);
    }

    #endregion
}
