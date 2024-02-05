
public class Humanoid : Entity
{
    public readonly Value<float> MovementSpeedFactor = new(1f);

    public readonly Value<Item> EquippedItem = new(null);
    public readonly Value<EquipmentItem> ActiveEquipmentItem = new();

    public readonly Value<bool> Interact = new();

    /// <summary>
    /// <para>item - item to equip</para>
    /// <para>bool - do it instantly?</para>
    /// </summary>
    public readonly Attempt<Item, bool> EquipItem = new();
    public readonly Attempt<Item> SwapItem = new();
    public readonly Attempt<Item> DropItem = new();

    /// <summary>
    /// <para>Use held item.</para>
    /// <para>bool - continuosly.</para>
    /// int - use type
    /// </summary>
    public readonly Activity UseItemHeld = new();
    public readonly Attempt<bool, int> UseItem = new();

    public readonly Activity Walk = new();
    public readonly Activity Sprint = new();
    public readonly Activity Crouch = new();
    public readonly Activity<float> Lean = new();
    public readonly Activity Prone = new();
    public readonly Activity Slide = new();

    public readonly Activity Jump = new();

    public readonly Activity Aim = new();
    public readonly Activity PointAim = new();
    public readonly Activity Reload = new();
    public readonly Activity Healing = new();
    
    public readonly Activity Dead = new();

    public readonly Activity Holster = new();
    public readonly Attempt ChangeScope = new();
}