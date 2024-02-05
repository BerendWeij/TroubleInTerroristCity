using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkInventoryController : NetworkPlayerComponent //TODO: find a way to reuse InventoryController
{
	[SerializeField]
	protected LayerMask m_WallsLayer;

	[SerializeField]
	protected bool m_DropItemsOnDeath = true;

	[Space]

	[SerializeField]
	protected Vector3 m_DropOffset = new(0f, 0f, 0.8f);

	[SerializeField]
	[Range(0.01f, 1f)]
	protected float m_CrouchHeightDropMod = 0.5f;

	[SerializeField]
	protected float m_DropAngularFactor = 150f;

	[SerializeField]
	protected float m_DropSpeed = 8f;

	[Space]

	[SerializeField]
	[Group]
	protected SoundPlayer m_DropSounds;

	protected Inventory m_Inventory;

	public void Start()
	{
		if (IsServer)
		{
			Player.DropItem.AddListener(StartDrop);
		}
		else
		{
			Player.DropItem.AddListener(item => SendToServerRPC(item.Id));
		}
		m_Inventory = GetComponent<Inventory>();
		Player.DropItem.SetTryer(TryDropItem);
		Player.DropItem.AddListener(OnPlayerDropItem);
		Player.Death.AddListener(OnEntityDeath);
	}

	public virtual bool TryDropItem(Item item)
	{
		bool canBeDropped = item != null &&
		                    item.Info.Pickup != null &&
		                    Player.DropItem.LastExecutionTime + 0.5f < Time.time &&
		                    Player.EquipItem.LastExecutionTime + 0.5f < Time.time &&
		                    m_Inventory.RemoveItem(item);
		
		return canBeDropped;
	}

	public void StartDrop(Item item)
	{
		StartCoroutine(C_Drop(item));
	}

	private void OnPlayerDropItem(Item droppedItem)
	{
		Player.EquipItem.Try(null, true);
	}

	public IEnumerator C_Drop(Item item)
	{
		if (item == null)
			yield return null;
		
		float heightDropMultiplier = 1f;

		if (Player.Crouch.Active)
			heightDropMultiplier = m_CrouchHeightDropMod;

		bool nearWall = false;

		Vector3 dropPosition;
		Quaternion dropRotation;

		if (Physics.Raycast(transform.position, transform.InverseTransformDirection(Vector3.forward) * 1.5f, m_DropOffset.z, m_WallsLayer))
		{
			dropPosition = transform.position + transform.TransformVector(new Vector3(0f, m_DropOffset.y * heightDropMultiplier, -0.2f));
			dropRotation = Quaternion.LookRotation(Player.LookDirection.Get());
			nearWall = true;
		}
		else
		{
			dropPosition = transform.position + transform.TransformVector(new Vector3(m_DropOffset.x, m_DropOffset.y * heightDropMultiplier, m_DropOffset.z));
			dropRotation = Random.rotationUniform;
		}

		GameObject droppedItem = Instantiate(item.Info.Pickup, dropPosition, dropRotation);

		droppedItem.transform.parent = null;
		droppedItem.SetActive(true);
		droppedItem.GetComponent<NetworkObject>().Spawn();
		droppedItem.transform.position = dropPosition;
		droppedItem.transform.rotation = dropRotation;

		var rigidbody = droppedItem.GetComponent<Rigidbody>();
		var collider = droppedItem.GetComponent<Collider>();

		if (rigidbody != null)
		{
			Physics.IgnoreCollision(Player.GetComponent<Collider>(), collider);

			rigidbody.isKinematic = false;

			if (rigidbody != null && !nearWall)
			{
				rigidbody.AddTorque(Random.rotation.eulerAngles * m_DropAngularFactor);
				rigidbody.AddForce(Player.LookDirection.Get() * m_DropSpeed, ForceMode.VelocityChange);
			}
		}

		m_DropSounds.Play2D();

		var pickup = droppedItem.GetComponent<ItemPickup>();

		if (pickup != null)
			pickup.SetItem(item);
	}

	private void OnEntityDeath()
	{
		if (m_DropItemsOnDeath)
		{
			for (int i = 0; i < m_Inventory.Containers.Count; i++)
			{
				for (int j = 0; j < m_Inventory.Containers[i].Slots.Length; j++)
				{
					var slot = m_Inventory.Containers[i].Slots[j];

					if (slot.Item)
					{
						Player.DropItem.Try(slot.Item);
						slot.SetItem(null);
					}
				}
			}
		}
	}
    
    [ServerRpc(RequireOwnership = false)]
    private void SendToServerRPC(int itemId)
    {
        ItemInfo itemInfo = ItemDatabase.GetItemById(itemId);

        StartCoroutine(C_Drop(new Item(itemInfo)));
    }
}
