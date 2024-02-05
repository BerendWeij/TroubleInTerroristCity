using UnityEngine;

public class AnimEventReceiver : PlayerComponent
{
    [SerializeField] private NetworkPlayerAnimController controller;

    private void Start()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<NetworkPlayerAnimController>();
        }
    }
        
    public void SetActionActive(int isActive)
    {
        controller.SetActionActive(isActive);
    }
        
    public void ChangeWeapon()
    {
        //equipmentController.Equip(Player.EquippedItem.Get());
        //equipmentController.m_WaitingToEquip = false;
        //controller.EquipWeapon();
        //controller.EquipWeapon();
    }
}