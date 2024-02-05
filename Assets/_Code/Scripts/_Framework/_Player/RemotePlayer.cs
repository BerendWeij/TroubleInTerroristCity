using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class RemotePlayer : NetworkBehaviour
{
    public UnityEvent Remote;
    
    [SerializeField] private Camera _camera;
    [SerializeField] private AudioListener _audioListener;
    [SerializeField] private List<MonoBehaviour> toDelete = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if(!IsLocalPlayer)
        {
            foreach (MonoBehaviour component in toDelete)
            {
                Destroy(component);
            }
            _camera.enabled = false;
            _audioListener.enabled = false;
            Remote?.Invoke();;
        }
    }
    
}
