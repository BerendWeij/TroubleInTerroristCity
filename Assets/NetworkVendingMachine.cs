using Unity.Netcode;

public class NetworkVendingMachine : NetworkBehaviour
{
    private VendingMachine _vendingMachine;
    
    private void Start()
    {
        _vendingMachine = GetComponent<VendingMachine>();
        _vendingMachine.pickedUpItem += PickedUpWeaponServerRPC;
    }

    public void SetWeapon(string itemName)
    {
        SendWeaponClientRPC(itemName);
    }
    
    [ClientRpc]
    private void SendWeaponClientRPC(string itemName)
    {
        _vendingMachine.SetWeapon(itemName);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void PickedUpWeaponServerRPC()
    {
        PickedUpWeaponClientRPC();
    }
    
    [ClientRpc]
    private void PickedUpWeaponClientRPC()
    {
        _vendingMachine.RemoveCurrentItem();
    }
}
