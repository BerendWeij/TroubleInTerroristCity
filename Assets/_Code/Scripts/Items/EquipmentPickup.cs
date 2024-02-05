using UnityEngine;
using UnityEngine.Events;

public class EquipmentPickup : ItemPickup
{
	public UnityEvent PickedUpEquipment;
	
	protected override void TryPickUp(Humanoid humanoid, float interactProgress)
	{
		if (m_ItemInstance != null)
		{
			// Check if this Item is swappable
			if (interactProgress > 0.45f && humanoid.SwapItem.Try(m_ItemInstance))
            {
				//PickedUpEquipment.Invoke();
				//gameObject.SetActive(false);
				print("swap to " + m_ItemInstance.Name);
			}
			else
			{
				bool addedItem;

				addedItem = humanoid.Inventory.AddItem(m_ItemInstance, m_TargetContainers); 
					

				
				// Item added to inventory
				if (addedItem)
				{
					if (m_ItemInstance.Info.StackSize > 1)
						print("pickup " + m_ItemInstance.Name);
					//UI_MessageDisplayer.Instance.PushMessage(string.Format("Picked up <color={0}>{1}</color> x {2}", ColorUtils.ColorToHex(m_ItemCountColor), m_ItemInstance.Name, m_ItemInstance.CurrentStackSize), m_BaseMessageColor);
					else
						print("pickup " + m_ItemInstance.Name);
					//UI_MessageDisplayer.Instance.PushMessage(string.Format("Picked up <color={0}>{1}</color>", ColorUtils.ColorToHex(m_ItemCountColor), m_ItemInstance.Name), m_BaseMessageColor);
					
					PickedUpEquipment.Invoke();
				}
				// Item not added to inventory
				else
				{
					print("inventory full");
					//UI_MessageDisplayer.Instance.PushMessage(string.Format("<color={0}>Inventory Full</color>", ColorUtils.ColorToHex(m_InventoryFullColor)), m_BaseMessageColor);
				}
			}
		}
		else
		{
			Debug.LogError("Item Instance is null, can't pick up anything.");
		}
	}
}