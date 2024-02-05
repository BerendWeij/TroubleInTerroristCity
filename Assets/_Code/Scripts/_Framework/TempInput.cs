using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class TempInput : PlayerComponent
{
    private bool _holster;
    private bool _pause;

    public void SetLookInput(CallbackContext context)
    {
        Player.LookInput.Set(context.ReadValue<Vector2>());
    }

    public void SetMovementInput(CallbackContext context)
    {
        Player.MoveInput.Set(context.ReadValue<Vector2>());
    }

    public void SetAim(CallbackContext context)
    {
        if (context.started)
        {
            Player.Aim.TryStart();
        }

        if (context.canceled)
        {
            Player.Aim.ForceStop();
        }
    }

    public void SetUseItem(CallbackContext context)
    {
        if (context.started)
        {
            Player.UseItem.Try(false, 0);
            Player.UseItemHeld.TryStart(true);
        }
        if (context.canceled)
        {
            Player.UseItemHeld.ForceStop();
        }
    }
    public void DropItem(CallbackContext context)
    {
        if (context.started)
        {
            Player.DropItem.Try(Player.EquippedItem.Get());
        }
    }


    public void SetJump(CallbackContext context)
    {
        if (context.started)
        {
            Player.Jump.TryStart();
        }
    }

    public void SetCrouch(CallbackContext context)
    {
        if (context.started)
        {
            Player.Crouch.TryStart();
            Player.Slide.TryStart();
        }
        if (context.canceled)
        {
            Player.Crouch.TryStop();
        }
    }
    
    public void SetProne(CallbackContext context)
    {
        if (context.started)
        {
            Player.Prone.TryStart();
        }
        if (context.canceled)
        {
            Player.Prone.TryStop();
        }
    }

    public void SetSprint(CallbackContext context)
    {
        if (context.started)
        {
            Player.Sprint.TryStart();
        }
        if (context.canceled)
        {
            Player.Sprint.ForceStop();
        }
    }

    public void SetLean(CallbackContext context)
    {
        if (context.started)
        {
            Player.Lean.TryStart(context.ReadValue<float>());
        }
        if (context.canceled)
        {
            Player.Lean.ForceStop();
        }
    }

    public void SetScroll(CallbackContext context)
    {
        if (context.performed)
        {
            Player.ScrollValue.Set((int)Mathf.Clamp(context.ReadValue<float>(), -1, 1));
        }
        if (context.canceled)
        {
            Player.ScrollValue.SetAndDontUpdate(0);
        }
    }

    public void SetChangeScope(CallbackContext context)
    {
        if (context.started)
        {
            Player.ChangeScope.Try();
        }
    }

    public void SetPointAiming(CallbackContext context)
    {
        if (context.started)
        {
            Player.PointAim.TryStart();
        }

        if(context.canceled)
        {
            Player.PointAim.ForceStop();
        }
    }
    public void SetHolster(CallbackContext context)
    {
        if (context.started)
        {
            _holster = !_holster;
            if(_holster)
            {
                Player.Holster.TryStart();
            }
            else
            {
                Player.Holster.ForceStop();
            }
        }
    }

    public void SetReload(CallbackContext context)
    {
        if (context.started)
        {
            Player.Reload.TryStart();
        }
    }

    public void SetInteract(CallbackContext context)
    {
        Player.Interact.Set(context.phase.IsInProgress());
    }
}
