using UnityEngine;

public class Unarmed : MeleeWeapon
{
    #region Anim Hashing

    //Hashed animator strings (Improves performance)
    private readonly int animHash_Hide = Animator.StringToHash("Hide");
    private readonly int animHash_Airborne = Animator.StringToHash("Airborne");
    private readonly int animHash_RunSpeed = Animator.StringToHash("Run Speed");
    private readonly int animHash_ArmsAreVisible = Animator.StringToHash("Arms Are Visible");
    private readonly int animHash_Jumping = Animator.StringToHash("Jumping");
    private readonly int animHash_Falling = Animator.StringToHash("Falling");
    private readonly int animHash_Running = Animator.StringToHash("Running");

    #endregion

    private UnarmedInfo _unarmedInfo;

    private float m_NextTimeToHideArms = 1f;
    private bool m_ArmsAreVisible;


    public override void Initialize(EquipmentHandler eHandler)
    {
        base.Initialize(eHandler);

        _unarmedInfo = EquipmentInfo as UnarmedInfo;
    }

    public override void Equip(Item item)
    {
        //EAnimation.AssignArmAnimations(EHandler.FPArmsHandler.Animator);

       // if (_unarmedInfo.UnarmedSettings.AlwaysShowArms || Player.Sprint.Active)
       //     ChangeArmsVisibility(true);
        m_NextTimeCanUse = Time.time + _unarmedInfo.MeleeSettings.Swings[0].Cooldown;

        Player.Sprint.AddStartListener(OnStartRunning);
        Player.Sprint.AddStopListener(OnStopRunning);
        Player.Jump.AddStartListener(OnStartJumping);
        Player.IsGrounded.AddChangeListener(OnStartFalling);

        if (_unarmedInfo.UnarmedSettings.AlwaysShowArms)
        {
            //EHandler.Animator_SetBool(animHash_ArmsAreVisible, true);
            m_ArmsAreVisible = true;
        }

        m_GeneralEvents.OnEquipped.Invoke(true);

        //EHandler.Animator_SetFloat(animHash_RunSpeed, m_U.UnarmedSettings.RunAnimSpeed);
    }

    public override void Unequip()
    {
        //if (m_ArmsAreVisible)
        //    EHandler.Animator_SetTrigger(animHash_Hide);

        Player.Sprint.RemoveStartListener(OnStartRunning);
        Player.Sprint.RemoveStopListener(OnStopRunning);
        Player.Jump.RemoveStartListener(OnStartJumping);
        Player.IsGrounded.RemoveChangeListener(OnStartFalling);

        //EHandler.Animator_SetBool(animHash_Airborne, false);

        ChangeArmsVisibility(false);

        m_GeneralEvents.OnEquipped.Invoke(false);
    }

    public override bool TryUseOnce(Ray[] itemUseRays, int useType)
    {
        if (Player.IsGrounded.Val)
        {
            m_NextTimeToHideArms = Time.time + _unarmedInfo.UnarmedSettings.ArmsShowDuration;

            //If the arms are not on screen play the show animation
            if (!m_ArmsAreVisible)
            {
                EHandler.PlayDelayedSound(_unarmedInfo.UnarmedSettings.ShowArmsAudio);
                ChangeArmsVisibility(true);
            }
            else
                return base.TryUseOnce(itemUseRays, useType);
        }

        return false;
    }

    protected virtual void OnStartFalling(bool isGrounded)
    {
        if (isGrounded)
        {
            //EHandler.Animator_SetBool(animHash_Airborne, false);
            //EHandler.Animator_SetBool(animHash_Jumping, false);

            m_NextTimeCanUse = Time.time + _unarmedInfo.MeleeSettings.Swings[0].Cooldown;
        }
        //EHandler.Animator_SetTrigger(animHash_Falling);
        //EHandler.Animator_SetBool(animHash_Airborne, true);
    }

    protected virtual void OnStartRunning()
    {
        //EHandler.Animator_SetBool(animHash_Running, true);
    }

    protected virtual void OnStopRunning()
    {
        //EHandler.Animator_SetBool(animHash_Running, false);

        m_NextTimeCanUse = Time.time + _unarmedInfo.MeleeSettings.Swings[0].Cooldown;

        ChangeArmsVisibility(false);
    }

    protected virtual void OnStartJumping()
    {
        //EHandler.Animator_SetBool(animHash_Airborne, true);
        //EHandler.Animator_SetBool(animHash_Jumping, true);
    }

    protected virtual void Update()
    {
        //if (!_unarmedInfo.UnarmedSettings.AlwaysShowArms && m_NextTimeToHideArms < Time.time && m_ArmsAreVisible)
        //{
        //    ChangeArmsVisibility(false);
        //    //EHandler.Animator_SetTrigger(animHash_Hide);
        //}
    }

    private void ChangeArmsVisibility(bool show)
    {
        m_ArmsAreVisible = show;
        //EHandler.Animator_SetBool(animHash_ArmsAreVisible, show);
    }
/*
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (EHandler != null)
        {
            EHandler.Animator_SetFloat(animHash_RunSpeed, m_U.UnarmedSettings.RunAnimSpeed);
        }
    }
#endif*/
}