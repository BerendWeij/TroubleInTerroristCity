using Unity.Netcode;
using UnityEngine.Events;

public class TimerNetworkHandler : NetworkBehaviour
{
    public UnityEvent<float> OnTimeChange;

    private TroubleInTerroristGamemode _gamemode;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _gamemode = GetComponent<TroubleInTerroristGamemode>();
    }

    public void SendTimes()
    {
        //SetTimerClientRPC(_gamemode.GetCurrentTimeLeft);
        //print(_gamemode.GetCurrentTimeLeft);
    }
    
    [ClientRpc]
    private void SetTimerClientRPC(float time)
    {
        if (IsHost)
            return;
        
        print(time);
        OnTimeChange.Invoke(time);
    }
}
