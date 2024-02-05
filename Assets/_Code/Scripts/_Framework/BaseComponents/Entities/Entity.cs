using Unity.Netcode;
using UnityEngine;

public class Entity : NetworkBehaviour
{
    public Inventory Inventory { get { return m_Inventory; } }

    /// <summary></summary>
    public readonly Value<float> Health = new(100f);

    /// <summary> </summary>
    public readonly Attempt<DamageInfo> ChangeHealth = new();

    /// <summary> </summary>
    public readonly Attempt<DamageInfo, IDamageable> DealDamage = new();

    /// <summary> </summary>
    public readonly Value<bool> IsGrounded = new(true);

    /// <summary> </summary>
    public readonly Value<Vector3> Velocity = new(Vector3.zero);

    public Value<Vector3> LookDirection = new();

    /// <summary> </summary>
    //public readonly Message<float> FallImpact = new Message<float>();

    /// <summary></summary>
    public readonly Message Death = new();

    /// <summary></summary>
    public readonly Message Respawn = new();

    public Hitbox[] Hitboxes;

    [SerializeField]
    private Inventory m_Inventory;

    protected virtual void Start()
    {
        Hitboxes = GetComponentsInChildren<Hitbox>();

        foreach (EntityComponent component in GetComponentsInChildren<EntityComponent>(true))
        {
            component.OnEntityStart();
        }
    }
}