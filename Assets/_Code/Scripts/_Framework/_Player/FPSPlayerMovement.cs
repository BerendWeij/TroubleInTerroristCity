using Kinemation.FPSFramework.Runtime.FPSAnimator;
using UnityEngine;

public class FPSPlayerMovement : FPSPlayerComponent
{
    public float DefaultHeight { get; private set; }
    
    [Header("General")] 
    [SerializeField] private NetworkPlayerAnimController networkPlayerAnimController;
    [SerializeField] private float moveSmoothing = 2f;
    [SerializeField] private LayerMask obstacleCheckMask = ~0;
    [SerializeField] private float gravity;

    [Space] [Header("Dynamic Motions")] 
    [SerializeField] private IKAnimation aimMotionAsset;
    [SerializeField] private IKAnimation leanMotionAsset;
    [SerializeField] private IKAnimation crouchMotionAsset;
    [SerializeField] private IKAnimation unCrouchMotionAsset;
    [SerializeField] private IKAnimation onJumpMotionAsset;
    [SerializeField] private IKAnimation onLandedMotionAsset;

    [Space] [SerializeField] [Group] private CoreMovementModule _coreMovement;

    [SerializeField] [Group] private MovementStateModule _runState;

    [SerializeField] [Group] private LowerHeightStateModule _crouchState;

    [SerializeField] [Group] private LowerHeightStateModule _proneState;

    [SerializeField] [Group] private JumpStateModule _jumpState;

    [SerializeField] [Group] private SlidingStateModule _slidingState;

    private Vector2 _animatorVelocity;
    public Vector2 SmoothMove => _smoothMove;

    private Vector2 _smoothMove;

    private MovementStateModule _currentMovementState;

    private Vector2 _smoothAnimatorMove;

    private CollisionFlags _collisionFlags;

    private float _distMovedSinceLastCycleEnded;
    private float _currentStepLength;

    private Vector3 _slideVelocity;
    private Vector3 _desiredVelocityLocal;
    private bool _previouslyGrounded;
    private float _lastLandTime;
    private float _nextTimeCanChangeHeight;
    private Vector3 _surfaceNormal;

    private float _sprintAnimatorInterp = 8f;
    
    private static readonly int InAir = Animator.StringToHash("InAir");
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int VelocityHash = Animator.StringToHash("Velocity");
    private static readonly int Moving = Animator.StringToHash("Moving");
    private static readonly int Crouching = Animator.StringToHash("Crouching");
    private static readonly int Sliding = Animator.StringToHash("Sliding");
    private static readonly int Sprinting = Animator.StringToHash("Sprinting");
    private static readonly int Proning = Animator.StringToHash("Proning");

    private void Start()
    {
        DefaultHeight = PlayerController.Controller.height;

        Player.Slide.SetStartTryer(TrySlide);

        Player.Slide.AddStartListener(StartSlide);

        Player.Crouch.AddStartListener(Crouch);
        Player.Crouch.AddStopListener(Standup);

        Player.Crouch.SetStartTryer(() => { return Try_ToggleCrouch(_crouchState); });
        Player.Crouch.SetStopTryer(() => { return Try_ToggleCrouch(null); });

        Player.Prone.AddStartListener(Prone);
        Player.Prone.AddStopListener(CancelProne);

        Player.Prone.SetStartTryer(() => { return Try_ToggleProne(_proneState); });
        Player.Prone.SetStopTryer(() => { return Try_ToggleProne(null); });

        Player.Jump.SetStartTryer(Try_Jump);

        Player.Sprint.SetStartTryer(TryStartSprint);
        Player.Sprint.AddStartListener(StartSprint);
        Player.Sprint.AddStopListener(StopSprint);

        Player.Lean.AddStartListener(Lean);
        Player.Lean.AddStopListener(() => Player.CharAnimData.leanDirection = 0);

        Player.DisabledMovement.SetStartTryer(TryDisableMovement);
    }

    private void Update()
    {
        if (Player.DisabledMovement.Active)
            return;

        float deltaTime = Time.deltaTime;

        Vector3 translation;

        if (IsGrounded)
        {
            translation = transform.TransformVector(_desiredVelocityLocal) * deltaTime;

            if (!Player.Jump.Active)
                translation.y = -.05f;
        }
        else
            translation = transform.TransformVector(_desiredVelocityLocal * deltaTime);

        _collisionFlags = PlayerController.Controller.Move(translation);

        if ((_collisionFlags & CollisionFlags.Below) == CollisionFlags.Below && !_previouslyGrounded)
        {
            bool wasJumping = Player.Jump.Active;

            if (Player.Jump.Active)
                Player.Jump.ForceStop();

            //Player.FallImpact.Send(Mathf.Abs(m_DesiredVelocityLocal.y));

            _lastLandTime = Time.time;

            if (wasJumping)
                _desiredVelocityLocal = Vector3.ClampMagnitude(_desiredVelocityLocal, 1f);
        }

        // Check if the top of the controller collided with anything,
        // If it did then add a counter force
        if (((_collisionFlags & CollisionFlags.Above) == CollisionFlags.Above &&
             !PlayerController.Controller.isGrounded) &&
            _desiredVelocityLocal.y > 0)
            _desiredVelocityLocal.y *= -.05f;

        Vector3 targetVelocity = CalcTargetVelocity(Player.MoveInput.Get());

        if (!IsGrounded)
            UpdateAirborneMovement(deltaTime, targetVelocity, ref _desiredVelocityLocal);
        else if (!Player.Jump.Active)
            UpdateGroundedMovement(deltaTime, targetVelocity, ref _desiredVelocityLocal);


        UpdateMovementAnimations();
        Player.IsGrounded.Set(IsGrounded);
        Player.Velocity.Set(Velocity);

        _previouslyGrounded = IsGrounded;
    }

    #region GroundMovement

    private void UpdateGroundedMovement(float deltaTime, Vector3 targetVelocity, ref Vector3 velocity)
    {
        AdjustSpeedOnSteepSurfaces(targetVelocity);
        UpdateVelocity(deltaTime, targetVelocity, ref velocity);
        UpdateWalkActivity(targetVelocity);
        CheckAndStopSprint(targetVelocity);
        HandleSliding(targetVelocity, deltaTime, ref velocity);
        AdvanceStepCycle(deltaTime);
    }

    private void AdjustSpeedOnSteepSurfaces(Vector3 targetVelocity)
    {
        float surfaceAngle = Vector3.Angle(Vector3.up, _surfaceNormal);
        targetVelocity *= _coreMovement.SlopeSpeedMult.Evaluate(surfaceAngle / SlopeLimit);
    }

    private void UpdateVelocity(float deltaTime, Vector3 targetVelocity, ref Vector3 velocity)
    {
        float targetAccel = (targetVelocity.sqrMagnitude > 0f) ? _coreMovement.Acceleration : _coreMovement.Damping;
        velocity = Vector3.Lerp(velocity, targetVelocity, targetAccel * deltaTime);
    }

    private void UpdateWalkActivity(Vector3 targetVelocity)
    {
        bool wantsToMove = targetVelocity.sqrMagnitude > 0.05f && !Player.Sprint.Active && !Player.Crouch.Active;

        if (!Player.Walk.Active && wantsToMove)
            Player.Walk.ForceStart();
        else if (Player.Walk.Active &&
                 (!wantsToMove || Player.Sprint.Active || Player.Crouch.Active || Player.Prone.Active))
            Player.Walk.ForceStop();
    }

    private void CheckAndStopSprint(Vector3 targetVelocity)
    {
        if (Player.Sprint.Active)
        {
            bool wantsToMoveBackwards = Player.MoveInput.Get().y < 0f;
            bool runShouldStop = wantsToMoveBackwards || targetVelocity.sqrMagnitude == 0f || Player.Stamina.Is(0f);

            if (runShouldStop)
                Player.Sprint.ForceStop();
        }
    }

    private void HandleSliding(Vector3 targetVelocity, float deltaTime, ref Vector3 velocity)
    {
        if (_slidingState.Enabled)
        {
            float surfaceAngle = Vector3.Angle(Vector3.up, _surfaceNormal);

            if (surfaceAngle > _slidingState.SlideTreeshold && Player.MoveInput.Get().sqrMagnitude == 0f)
            {
                Vector3 slideDirection = (_surfaceNormal + Vector3.down);
                _slideVelocity += slideDirection * (_slidingState.SlideSpeed * deltaTime);
            }
            else
            {
                _slideVelocity = Vector3.Lerp(_slideVelocity, Vector3.zero, deltaTime * 10f);
            }

            velocity += transform.InverseTransformVector(_slideVelocity);
        }
    }

    private void AdvanceStepCycle(float deltaTime)
    {
        _distMovedSinceLastCycleEnded += _desiredVelocityLocal.magnitude * deltaTime;

        float targetStepLength = _currentMovementState?.StepLength ?? _coreMovement.StepLength;
        _currentStepLength = Mathf.MoveTowards(_currentStepLength, targetStepLength, deltaTime);

        if (_distMovedSinceLastCycleEnded > _currentStepLength)
        {
            _distMovedSinceLastCycleEnded -= _currentStepLength;
            Player.MoveCycleEnded.Send();
        }

        Player.MoveCycle.Set(_distMovedSinceLastCycleEnded / _currentStepLength);
    }

    #endregion

    private void UpdateAirborneMovement(float deltaTime, Vector3 targetVelocity, ref Vector3 velocity)
    {
        if (_previouslyGrounded && !Player.Jump.Active)
            velocity.y = 0f;

        velocity += targetVelocity * (_coreMovement.Acceleration * _coreMovement.AirborneControl * deltaTime);

        velocity.y -= gravity * deltaTime;

        networkPlayerAnimController.SlotLayer.PlayMotion(!IsGrounded ? onJumpMotionAsset : onLandedMotionAsset);
    }

    #region Slide

    private bool TrySlide()
    {
        if (Player.Sprint.Active)
        {
            return true;
        }

        return false;
    }

    private void StartSlide()
    {
        PlayerController.Animator.CrossFade(Sliding, 0.1f);
        Player.Slide.ForceStop();
    }

    #endregion

    #region Sprint

    private bool TryStartSprint()
    {
        if (!_runState.Enabled || Player.Stamina.Get() < 15f)
            return false;

        bool wantsToMoveBack = Player.MoveInput.Get().y < 0f;
        bool canChangeState = Player.IsGrounded.Get() && !wantsToMoveBack && !Player.Crouch.Active &&
                              !Player.Aim.Active && !Player.Prone.Active;

        if (canChangeState)
            _currentMovementState = _runState;

        return canChangeState;
    }

    private void StartSprint()
    {
        networkPlayerAnimController.LookLayer.SetLayerAlpha(0.5f);
        networkPlayerAnimController.AdsLayer.SetLayerAlpha(0f);
        networkPlayerAnimController.LocoLayer.SetReadyWeight(0f);
    }

    private void StopSprint()
    {
        if (Player.Crouch.Active)
        {
            return;
        }

        _currentMovementState = null;
        networkPlayerAnimController.LookLayer.SetLayerAlpha(1f);
        networkPlayerAnimController.AdsLayer.SetLayerAlpha(1f);
    }

    #endregion

    #region Crouch

    private bool Try_ToggleCrouch(LowerHeightStateModule lowerHeightState)
    {
        if (!_crouchState.Enabled)
            return false;

        bool toggledSuccesfully;

        if (!Player.Crouch.Active)
        {
            toggledSuccesfully = Try_ChangeControllerHeight(lowerHeightState);
        }
        else
        {
            toggledSuccesfully = Try_ChangeControllerHeight(null);
        }


        //Stop the prone state if the crouch state is enabled
        if (toggledSuccesfully && Player.Prone.Active)
            Player.Prone.ForceStop();

        return toggledSuccesfully;
    }

    private void Crouch()
    {
        networkPlayerAnimController.LookLayer.SetPelvisWeight(0f);
        PlayerController.Animator.SetBool(Crouching, true);
        networkPlayerAnimController.SlotLayer.PlayMotion(crouchMotionAsset);
    }

    private void Standup()
    {
        networkPlayerAnimController.LookLayer.SetPelvisWeight(1f);
        PlayerController.Animator.SetBool(Crouching, false);
        networkPlayerAnimController.SlotLayer.PlayMotion(unCrouchMotionAsset);
    }

    #endregion

    #region Prone

    private bool Try_ToggleProne(LowerHeightStateModule lowerHeightState)
    {
        if (!_proneState.Enabled)
            return false;

        bool toggledSuccesfully;

        if (!Player.Crouch.Active)
        {
            toggledSuccesfully = Try_ChangeControllerHeight(lowerHeightState);
        }
        else
        {
            toggledSuccesfully = Try_ChangeControllerHeight(null);
        }


        //Stop the prone state if the crouch state is enabled
        if (toggledSuccesfully && Player.Prone.Active)
            Player.Prone.ForceStop();

        return toggledSuccesfully;
    }

    private void Prone()
    {
        networkPlayerAnimController.LookLayer.SetPelvisWeight(1f);
        PlayerController.Animator.SetBool(Crouching, false);
        PlayerController.Animator.SetBool(Proning, true);
        networkPlayerAnimController.SlotLayer.PlayMotion(unCrouchMotionAsset);
    }

    private void CancelProne()
    {
        networkPlayerAnimController.LookLayer.SetPelvisWeight(0f);
        PlayerController.Animator.SetBool(Crouching, true);
        PlayerController.Animator.SetBool(Proning, false);
        networkPlayerAnimController.SlotLayer.PlayMotion(unCrouchMotionAsset);
    }

    #endregion

    #region Lean and Jump

    private void Lean()
    {
        if (Player.Sprint.Active)
            return;

        if (!Player.Holster.Active)
        {
            Player.CharAnimData.leanDirection = (int)Player.Lean.Parameter;
            networkPlayerAnimController.SlotLayer.PlayMotion(leanMotionAsset);
        }
    }

    private bool Try_Jump()
    {
        // If crouched, stop crouching first
        if (Player.Crouch.Active)
        {
            Player.Crouch.TryStop();
            return false;
        }

        if (Player.Prone.Active)
        {
            if (!Player.Prone.TryStop())
                Player.Crouch.TryStart();

            return false;
        }

        bool canJump = _jumpState.Enabled &&
                       IsGrounded &&
                       !Player.Crouch.Active &&
                       Time.time > _lastLandTime + _jumpState.JumpTimer;

        if (!canJump)
            return false;

        float jumpSpeed = Mathf.Sqrt(2 * gravity * _jumpState.JumpHeight);
        _desiredVelocityLocal = new Vector3(_desiredVelocityLocal.x, jumpSpeed, _desiredVelocityLocal.z);

        return true;
    }

    #endregion

    private Vector3 CalcTargetVelocity(Vector2 moveInput)
    {
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        bool wantsToMove = moveInput.sqrMagnitude > 0f;

        // Calculate the direction (relative to the us), in which the player wants to move.
        Vector3 targetDirection =
            (wantsToMove ? new Vector3(moveInput.x, 0f, moveInput.y) : _desiredVelocityLocal.normalized);

        float desiredSpeed = 0f;

        if (wantsToMove)
        {
            // Set the default speed.
            desiredSpeed = _coreMovement.ForwardSpeed;
            // If the player wants to move sideways...
            if (Mathf.Abs(moveInput.x) > 0f)
                desiredSpeed = _coreMovement.SideSpeed;

            // If the player wants to move backwards...
            if (moveInput.y < 0f)
                desiredSpeed = _coreMovement.BackSpeed;

            // If we're currently running...
            if (Player.Sprint.Active)
            {
                // If the player wants to move forward or sideways, apply the run speed multiplier.
                if (desiredSpeed == _coreMovement.ForwardSpeed || desiredSpeed == _coreMovement.SideSpeed)
                    desiredSpeed = _currentMovementState.SpeedMultiplier;
            }
            else
            {
                // If we're crouching/pronning...
                if (_currentMovementState != null)
                    desiredSpeed *= _currentMovementState.SpeedMultiplier;
            }
        }

        return targetDirection * (desiredSpeed * Player.MovementSpeedFactor.Val);
    }

    private void UpdateMovementAnimations()
    {
        float moveX = Player.MoveInput.Get().x;
        float moveY = Player.MoveInput.Get().y;

        Vector2 rawInput = new Vector2(moveX, moveY);
        Vector2 normInput = new Vector2(moveX, moveY);
        normInput.Normalize();

        var animatorVelocity = normInput;

        animatorVelocity *= Player.IsGrounded.Get() ? 1f : 0f;

        _animatorVelocity = Vector2.Lerp(_animatorVelocity, animatorVelocity,
            FPSAnimLib.ExpDecayAlpha(2, Time.deltaTime));

        if (Player.Sprint.Active)
        {
            normInput.x = rawInput.x = 0f;
            normInput.y = rawInput.y = 2f;
        }

        _smoothMove = FPSAnimLib.ExpDecay(_smoothMove, normInput, moveSmoothing, Time.deltaTime);

        moveX = _smoothMove.x;
        moveY = _smoothMove.y;

        Player.CharAnimData.moveInput = normInput;

        bool moving = Mathf.Approximately(0f, normInput.magnitude);

        PlayerController.Animator.SetBool(Moving, !moving);
        PlayerController.Animator.SetFloat(MoveX, _animatorVelocity.x);
        PlayerController.Animator.SetFloat(MoveY, _animatorVelocity.y);
        PlayerController.Animator.SetFloat(VelocityHash, _animatorVelocity.magnitude);

        float a = PlayerController.Animator.GetFloat(Sprinting);
        float b = Player.Sprint.Active ? 1f : 0f;

        a = Mathf.Lerp(a, b, FPSAnimLib.ExpDecayAlpha(_sprintAnimatorInterp, Time.deltaTime));
        PlayerController.Animator.SetFloat(Sprinting, a);
    }

    private bool Try_ChangeControllerHeight(LowerHeightStateModule lowerHeightState)
    {
        bool canChangeHeight =
            (Time.time > _nextTimeCanChangeHeight || _nextTimeCanChangeHeight == 0f) &&
            Player.IsGrounded.Get() &&
            !Player.Sprint.Active;


        if (canChangeHeight)
        {
            float height = (lowerHeightState == null) ? DefaultHeight : lowerHeightState.ControllerHeight;

            //If the "lowerHeightState" height is bigger than the current one check if there's anything over the Player's head
            if (height > PlayerController.Controller.height)
            {
                if (DoCollisionCheck(true, Mathf.Abs(height - PlayerController.Controller.height)))
                    return false;
            }

            if (lowerHeightState != null)
                _nextTimeCanChangeHeight = Time.time + lowerHeightState.TransitionDuration;

            SetHeight(height);

            _currentMovementState = lowerHeightState;
        }

        return canChangeHeight;
    }

    private bool DoCollisionCheck(bool checkAbove, float maxDistance)
    {
        Vector3 rayOrigin = transform.position +
                            (checkAbove ? Vector3.up * PlayerController.Controller.height : Vector3.zero);
        Vector3 rayDirection = checkAbove ? Vector3.up : Vector3.down;

        return Physics.Raycast(rayOrigin, rayDirection, maxDistance, obstacleCheckMask,
            QueryTriggerInteraction.Ignore);
    }

    private void SetHeight(float height)
    {
        PlayerController.Controller.height = height;
        PlayerController.Controller.center = Vector3.up * height * 0.5f;
    }

    private bool TryDisableMovement()
    {
        return true;
    }
    
    private bool IsGrounded
    {
        get => PlayerController.Controller.isGrounded;
    }

    private Vector3 Velocity
    {
        get => PlayerController.Controller.velocity;
    }

    private float SlopeLimit
    {
        get => PlayerController.Controller.slopeLimit;
    }
}