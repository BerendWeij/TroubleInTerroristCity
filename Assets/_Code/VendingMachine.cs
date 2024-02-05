using System;
using UnityEngine;

public class VendingMachine : InteractiveObject
{
    public Action pickedUpItem;
    
    [SerializeField] private Transform rotator;
    [SerializeField] private new GameObject light;
    
    private ItemInfo _itemInfo;

    private GameObject currentGameobject;
    
    public void SetWeapon(string itemName)
    {
        RemoveCurrentItem();
        light.SetActive(true);
        
        _itemInfo = ItemDatabase.GetItemByName(itemName);

         currentGameobject = Instantiate(_itemInfo.WeaponModel, rotator);
    }
    
    public override void OnInteractionStart(Player player)
    {
        if (currentGameobject == null)
            return;

        int gamemodeCurrency = player.GamemodeCurrency.Get();
        
        if (gamemodeCurrency < 1)
            return;
        
        if (player.Inventory.AddItem(new Item(_itemInfo), ItemContainerFlags.Holster))
        {
            RemoveCurrentItem();

            pickedUpItem();

            player.GamemodeCurrency.Set(gamemodeCurrency - 1);
        }
    }

    public void RemoveCurrentItem()
    {
        if (currentGameobject == null)
            return;
        
        Destroy(currentGameobject);

        light.SetActive(false);
    }
}
