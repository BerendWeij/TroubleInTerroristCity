using Unity.Netcode;
using UnityEngine.Events;

public class RoleNetworkHandler : NetworkBehaviour
{
    public UnityEvent ReceiveTraitorRole;
    public UnityEvent ReceiveInnocentRole;
    
    private TroubleInTerroristGamemode _gamemode;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _gamemode = GetComponent<TroubleInTerroristGamemode>();
    }

    public void SendRoles()
    {
        if (!IsHost)
            return;
        
        ulong[] innocents = _gamemode.GetInnocents.ToArray();
        ulong[] traitors = _gamemode.GetTraitors.ToArray();
        
        ClientRpcParams clientRpcParamsInnocent = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = innocents
            }
        };
        
        ClientRpcParams clientRpcParamsTraitor = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = traitors
            }
        };

        GetRoleInnocentClientRPC(clientRpcParamsInnocent);
        GetRoleTraitorClientRPC(clientRpcParamsTraitor);
    }
    
    [ClientRpc]
    private void GetRoleInnocentClientRPC(ClientRpcParams clientRpcParams = default)
    {
        ReceiveInnocentRole.Invoke();
    }
    
    [ClientRpc]
    private void GetRoleTraitorClientRPC(ClientRpcParams clientRpcParams = default)
    {
        ReceiveTraitorRole.Invoke();

        PlayerData playerData = PlayerManager.Instance.GetPlayerData();

        playerData.playerObject.GamemodeCurrency.Set(5);
    }
}
