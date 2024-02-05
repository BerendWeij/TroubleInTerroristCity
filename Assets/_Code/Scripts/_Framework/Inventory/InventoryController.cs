using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class InventoryController: HumanoidComponent
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


	public bool dropItem = true;

	public override void OnEntityStart()
	{
		m_Inventory = GetComponent<Inventory>();
		Humanoid.DropItem.SetTryer(TryDropItem);
		Humanoid.DropItem.AddListener(StartDrop);
		Humanoid.DropItem.AddListener(OnPlayerDropItem);
		Entity.Death.AddListener(OnEntityDeath);
	}

	public virtual bool TryDropItem(Item item)
	{
		bool canBeDropped = item != null &&
		                    item.Info.Pickup != null &&
		                    Humanoid.DropItem.LastExecutionTime + 0.5f < Time.time &&
		                    Humanoid.EquipItem.LastExecutionTime + 0.5f < Time.time &&
		                    m_Inventory.RemoveItem(item);
		
		return canBeDropped;
	}

	public void StartDrop(Item item)
	{
		StartCoroutine(C_Drop(item));
		print("test");
	}

	private void OnPlayerDropItem(Item droppedItem)
	{
		Humanoid.EquipItem.Try(Humanoid.Inventory.GetContainerWithName("Pistol").Slots[0].Item, true);
	}

	public IEnumerator C_Drop(Item item)
	{
		if (item == null)
			yield return null;

		if (!dropItem)
			yield return null;
		
		float heightDropMultiplier = 1f;

		if (Humanoid.Crouch.Active)
			heightDropMultiplier = m_CrouchHeightDropMod;

		bool nearWall = false;

		Vector3 dropPosition;
		Quaternion dropRotation;

		if (Physics.Raycast(transform.position, transform.InverseTransformDirection(Vector3.forward) * 1.5f, m_DropOffset.z, m_WallsLayer))
		{
			dropPosition = transform.position + transform.TransformVector(new Vector3(0f, m_DropOffset.y * heightDropMultiplier, -0.2f));
			dropRotation = Quaternion.LookRotation(Entity.LookDirection.Get());
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
		droppedItem.transform.position = dropPosition;
		droppedItem.transform.rotation = dropRotation;

		var rigidbody = droppedItem.GetComponent<Rigidbody>();
		var collider = droppedItem.GetComponent<Collider>();

		if (rigidbody != null)
		{
			Physics.IgnoreCollision(Entity.GetComponent<Collider>(), collider);

			rigidbody.isKinematic = false;

			if (rigidbody != null && !nearWall)
			{
				rigidbody.AddTorque(Random.rotation.eulerAngles * m_DropAngularFactor);
				rigidbody.AddForce(Entity.LookDirection.Get() * m_DropSpeed, ForceMode.VelocityChange);
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
						TryDropItem(slot.Item);
						slot.SetItem(null);
					}
				}
			}
		}
	}
}
