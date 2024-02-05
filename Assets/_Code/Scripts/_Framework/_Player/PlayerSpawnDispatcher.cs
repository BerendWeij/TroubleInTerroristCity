using UnityEngine.Events;

public class PlayerSpawnDispatcher : NetworkSingleton<PlayerSpawnDispatcher>
{
    public UnityEvent<Player> playerSpawn;
    public void OnPlayerSpawn(Player player)
    {
        playerSpawn.Invoke(player);
    }
}
