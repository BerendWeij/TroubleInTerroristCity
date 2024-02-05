using System.Collections.Generic;
using Unity.Netcode;

public class ActivitySyncer : NetworkPlayerComponent
{
    private Dictionary<string, Activity> _activities;
    private void Start()
    {
        _activities = Player.GetAllActivities();
        foreach (var activity in _activities)
        {
            if(!IsOwner)
                continue;
            
            if (IsHost)
            {
                activity.Value.AddStartListener(() => SyncActivityClientRpc(activity.Key, true));
                activity.Value.AddStopListener(() => SyncActivityClientRpc(activity.Key, false));
            }
            else if(IsClient)
            {
                activity.Value.AddStartListener(() => SyncActivityServerRpc(activity.Key, true));
                activity.Value.AddStopListener(() => SyncActivityServerRpc(activity.Key, false));
            }
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SyncActivityServerRpc(string activity, bool active)
    {
        SyncActivityClientRpc(activity, active);
    }
    
    [ClientRpc]
    private void SyncActivityClientRpc(string activityName, bool active)
    {
        if (IsOwner)
            return;

        if (!_activities.TryGetValue(activityName, out Activity activity))
            return;
        
        if (active)
        {
            activity.ForceStart();
        }
        else
        {
            activity.ForceStop();
        }
    }
}
