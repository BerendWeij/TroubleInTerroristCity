using Unity.Netcode;

public class NetworkPickup : NetworkBehaviour
{
    private EquipmentPickup _equipmentPickup;
    private NetworkObject _networkObject;
    private void Start()
    {
        _equipmentPickup = GetComponent<EquipmentPickup>();
        _networkObject = GetComponent<NetworkObject>();
        
        _equipmentPickup.PickedUpEquipment.AddListener(DespawnServerRpc);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void DespawnServerRpc()
    {
        _networkObject.Despawn(false);
        gameObject.SetActive(false);
    }
}
