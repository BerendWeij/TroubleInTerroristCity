
public class PlayerSpawnNotifier : NetworkPlayerComponent
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
            return;
            
        PlayerSpawnDispatcher.Instance.OnPlayerSpawn(Player);
    }
}
