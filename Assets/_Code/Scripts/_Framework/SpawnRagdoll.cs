using Unity.Netcode;
using UnityEngine;

public class SpawnRagdoll : NetworkPlayerComponent
{
    [SerializeField] private GameObject prefab;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        Player.Death.AddListener(SendToServerRPC);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendToServerRPC()
    {
        GameObject ragdoll = Instantiate(prefab, transform.position, transform.rotation);
        ragdoll.GetComponent<NetworkObject>().Spawn();
    }
}
