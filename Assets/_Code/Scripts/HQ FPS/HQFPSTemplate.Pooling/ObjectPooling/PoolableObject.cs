using UnityEngine;
using UnityEngine.Events;

namespace HQFPSTemplate.Pooling
{
    public class PoolableObject : MonoBehaviour
    {
        public string PoolId { get => m_PoolId; }

        public UnityEvent OnReleasedEvent = new();
        public UnityEvent OnUseEvent = new();

        private bool m_Initialized;
        private string m_PoolId;


        public void Init(string poolId)
        {
            if(m_Initialized)
            {
                Debug.LogError("You are attempting to initialize a poolable object, but it's already initialized!!");
                return;
            }

            m_PoolId = poolId;
            m_Initialized = true;
        }

        public void OnUse()
        {
            OnUseEvent.Invoke();
        }

        public void OnReleased()
        {
            OnReleasedEvent.Invoke();
        }
    }
}