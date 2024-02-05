using System.Collections;
using UnityEngine;

public class EquipmentController : PlayerComponent
{
    public EquipmentHandler activeEHandler;

    [SerializeField]
    private FPSPlayerCamera playerCameraController;

    [SerializeField]
    private bool m_AimWhileReloading;

    [SerializeField]
    private bool m_AutoReloadOnEmpty = true;
    
    private bool LastFrameActiveReload;

    private float m_NextTimeCanAutoReload;
    private float m_NextTimeToEquip;
    private bool m_WaitingToEquip = true;
    
    private float _recoilStep;

    private int _index;

    private void Awake()
    {
        Player.EquipItem.SetTryer(TryChangeItem);
        Player.SwapItem.SetTryer(TrySwapItems);
        Player.ScrollValue.AddChangeListener(ChangeWeapon);

        Player.DestroyEquippedItem.SetTryer(TryDestroyHeldItem);

        Player.UseItemHeld.SetStartTryer(TryStartUse);
        Player.UseItemHeld.AddStopListener(StopUseItemHeld);
        Player.Sprint.AddStopListener(() => activeEHandler.RecoilAnimation.Stop());

        Player.Aim.SetStartTryer(TryStartAim);
        Player.Aim.AddStartListener(StartAim);
        Player.Aim.AddStopListener(OnAimStop);

        Player.UseItem.SetTryer(TryUse);
        Player.Reload.SetStartTryer(TryStartReload);
        Player.Reload.AddStartListener(StartReload);

        Player.Lean.SetStartTryer(TryStartLean);
        Player.ChangeScope.SetTryer(TrySwitchScope);

        Player.PointAim.SetStartTryer(TryStartPointAiming);
        Player.PointAim.AddStopListener(OnPointAimingStop);

        Player.Holster.SetStartTryer(TryStartHolster);
        Player.Holster.AddStopListener(OnHolsterStop);
    }
    
    private void Update()
    {
        //temp solution 
        if (activeEHandler.EquipmentItem != null)
        {
            bool endedReloading = activeEHandler.EquipmentItem.IsDoneReloading();

            if (endedReloading)
                Player.Reload.ForceStop();
        }

        if (Player.Reload.Active || (LastFrameActiveReload != Player.Reload.Active))
        {

        }
        //Equip the new item after the previous one has been unequipped
        if (m_WaitingToEquip && Time.time > m_NextTimeToEquip)
        {
            Equip(Player.EquippedItem.Get());
            m_WaitingToEquip = false;
        }

        LastFrameActiveReload = Player.Reload.Active;

        StartCoroutine(UseLate());
    }

    private bool TryStartUse()
    {
        if (activeEHandler.EquipmentItem.recoilPattern != null)
        {
            _recoilStep = activeEHandler.EquipmentItem.recoilPattern.step;
        }
        
        return true;
    }

    private void StopUseItemHeld()
    {
        activeEHandler.RecoilAnimation.Stop();
        _recoilStep = 0;
    }

    private IEnumerator UseLate()
    {
        yield return new WaitForEndOfFrame();

        if (Player.UseItemHeld.Active)
        {
            Player.UseItem.Try(true, 0);
        }
    }

    public void Equip(Item item)
    {
        if (Player.Aim.Active)
            Player.Aim.ForceStop();

        if (Player.Reload.Active)
            Player.Reload.ForceStop();

        activeEHandler.EquipItem(item);
        
        Player.ActiveEquipmentItem.Set(activeEHandler.EquipmentItem);
        //m_FPCamera.fieldOfView = activeEHandler.EquipmentItem.EModel.TargetFOV;
    }

    private void ChangeWeapon(int index)
    {
        if (m_WaitingToEquip)
            return;
        
        int maxWeapons = 3;
        _index = (_index + index + maxWeapons) % maxWeapons;

        string[] containerNames = { "Pistol", "Primary", "Special" };
        string containerName = containerNames[_index];

        Item item = Player.Inventory.GetContainerWithName(containerName).Slots[0].Item;

        if (item != null)
        {
            Player.EquipItem.Try(item, false);
        }
        else
        {
            Player.EquipItem.Try(null, false);
        }
    }
    
    private bool TryChangeItem(Item item, bool instantly)
    {
        if (Player.EquippedItem.Get() == item && item != null)
            return false;

        ChangeItem(item, instantly);
        activeEHandler.StartWeaponChange();

        return true;
    }

    private void ChangeItem(Item item, bool instantly)
    {
        // Register the equipment item for equipping
        m_WaitingToEquip = true;

        // Register the current equipped item for disabling
        if (activeEHandler.EquipmentItem != null)
        {
            if (activeEHandler.UsingItem.Active)
            {
                activeEHandler.UsingItem.ForceStop();
                activeEHandler.EquipmentItem.OnUseEnd();
            }

            if (Player.Aim.Active)
                Player.Aim.ForceStop();

            if (Player.Reload.Active)
                Player.Reload.ForceStop();

            activeEHandler.UnequipItem();

            if (!instantly)
                m_NextTimeToEquip = Time.time + activeEHandler.EquipmentItem.EquipmentInfo.Unequipping.Duration;
        }

        Player.EquippedItem.Set(item);
    }

    private bool TrySwapItems(Item item)
    {
        Item currentItem = Player.EquippedItem.Get();

        if (currentItem != null && ContainsEquipmentItem(item)) // Check if the passed item is swappable
        {
            ItemSlot itemSlot = Player.Inventory.GetItemSlot(currentItem);

            if (Player.DropItem.Try(currentItem))
            {
                Player.DestroyEquippedItem.Try();
                Player.EquipItem.Try(item, false);

                itemSlot.SetItem(item);

                return true;
            }
        }

        return false;
    }

    private bool ContainsEquipmentItem(Item item)
    {
        if (activeEHandler.ContainsEquipmentItem(item.Id))
            return true;

        return false;
    }

    private bool TryDestroyHeldItem()
    {
        if (Player.EquippedItem.Get() == null)
            return false;
        Player.Inventory.RemoveItem(Player.EquippedItem.Get());
        //Player.EquippedItem.Get().gameObject.SetActive(false);
        Player.EquipItem.Try(null, false);
        return true;
    }
    
    private bool TryStartReload()
    {
        bool reloadStarted = activeEHandler.TryStartReload();

        if (reloadStarted)
        {
            if (Player.Aim.Active && !m_AimWhileReloading)
                Player.Aim.ForceStop();
        }

        return reloadStarted;
    }

    private void StartReload() => activeEHandler.StartReload();

    private bool TryUse(bool continuously, int useIndex)
    {
        EquipmentItem eItem = activeEHandler.EquipmentItem;

        if (eItem == null)
            return false;
        //float staminaTakePerUse = eItem.EInfo.General.StaminaTakePerUse;
        bool eItemCanBeUsed = eItem.CanBeUsed();
        // Interrupt the reload if possible
        if (!continuously && Player.Reload.Active /*&& eItem.EInfo.General.CanStopReloading*/ && eItemCanBeUsed)
            Player.Reload.ForceStop();

        if (CanUseItem(eItem))
        {
            bool usedSuccessfully = activeEHandler.TryUse(continuously, useIndex);

            if (usedSuccessfully)
            {
                //if (staminaTakePerUse > 0f)
                //    Player.Stamina.Set(Mathf.Max(Player.Stamina.Get() - staminaTakePerUse, 0f));
                if (activeEHandler.EquipmentItem.recoilPattern != null)
                {
                    float aimRatio = Player.Aim.Active ? activeEHandler.EquipmentItem.recoilPattern.aimRatio : 1f;
                    float hRecoil = Random.Range(activeEHandler.EquipmentItem.recoilPattern.horizontalVariation.x,
                        activeEHandler.EquipmentItem.recoilPattern.horizontalVariation.y);
                
                    playerCameraController.AddRecoil(new Vector2(hRecoil, _recoilStep) * aimRatio);
                }
                _recoilStep += activeEHandler.EquipmentItem.recoilPattern.acceleration;

                m_NextTimeCanAutoReload = Time.time + 0.35f;
            }

            if (!eItemCanBeUsed)
            {
                activeEHandler.RecoilAnimation.Stop();
            }
            
            //Try reload the item if the item can't be used (e.g. out of ammo) and 'Reload on empty' is active
            //if (!eItemCanBeUsed && m_AutoReloadOnEmpty && !continuously && m_NextTimeCanAutoReload < Time.time)
            //Player.Reload.TryStart();
            return usedSuccessfully;
        }

        return false;
    }

    private bool CanUseItem(EquipmentItem eItem)
    {
        if (eItem != null)
        {
            //float staminaTakePerUse = eItem.EInfo.General.StaminaTakePerUse;

            bool airborneCondition = Player.IsGrounded.Get();// || eItem.EInfo.General.UseWhileAirborne;
            bool runningCondition = !Player.Sprint.Active;// || eItem.EInfo.General.UseWhileRunning;
            //bool staminaCondition = staminaTakePerUse == 0f || Player.Stamina.Get() > staminaTakePerUse;

            return airborneCondition /*&& staminaCondition*/ && runningCondition && !Player.Reload.Active;
        }

        return false;
    }

    private bool TryStartAim()
    {
        if (Player.Sprint.Active ||
            Player.Reload.Active ||
            (!m_AimWhileReloading && Player.Aim.Active))
            return false;
        
        return activeEHandler.TryStartAim();
    }

    private void StartAim() => activeEHandler.StartAim();

    private void OnAimStop() => activeEHandler.OnAimStop();

    private bool TryStartLean(float lean)
    {
        return true;
    }

    private bool TrySwitchScope()
    {
        if (!Player.Aim.Active)
            return false;

        return true;
    }

    private bool TryStartPointAiming()
    {
        if (!Player.Aim.Active)
            return false;

        return activeEHandler.TryStartPointAiming();
    }

    private void OnPointAimingStop() => activeEHandler.OnPointAimingStop();

    private bool TryStartHolster()
    {
        if (Player.Aim.Active && !Player.Holster.Active)
            return false;

        return activeEHandler.TryStartHolster();
    }

    private void OnHolsterStop() => activeEHandler.OnHolsterStop();
}
