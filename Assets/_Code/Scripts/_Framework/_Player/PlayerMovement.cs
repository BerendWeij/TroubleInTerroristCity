using Kinemation.FPSFramework.Runtime.FPSAnimator;
using System;
using UnityEngine;

public class PlayerMovement : PlayerComponent
{
    #region Internal

    [Serializable]
    public class MovementStateModule
    {
        public bool Enabled = true;

        [ShowIf("Enabled", true)] [Range(0f, 10f)]
        public float SpeedMultiplier = 4.5f;

        [ShowIf("Enabled", true)] [Range(0f, 3f)]
        public float StepLength = 1.9f;
    }

    [Serializable]
    public class CoreMovementModule
    {
        [Range(0f, 20f)] public float Acceleration = 5f;

        [Range(0f, 20f)] public float Damping = 8f;

        [Range(0f, 1f)] public float AirborneControl = 0.15f;

        [Range(0f, 3f)] public float StepLength = 1.2f;

        [Range(0f, 10f)] public float ForwardSpeed = 2.5f;

        [Range(0f, 10f)] public float BackSpeed = 2.5f;

        [Range(0f, 10f)] public float SideSpeed = 2.5f;

        public AnimationCurve SlopeSpeedMult = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));

        public float AntiBumpFactor = 1f;

        [Range(0f, 1f)] public float HeadBounceFactor = 0.65f;
    }

    [Serializable]
    public class JumpStateModule
    {
        public bool Enabled = true;

        [ShowIf("Enabled", true)] [Range(0f, 3f)]
        public float JumpHeight = 1f;

        [ShowIf("Enabled", true)] [Range(0f, 1.5f)]
        public float JumpTimer = 0.3f;
    }

    [Serializable]
    public class LowerHeightStateModule : MovementStateModule
    {
        [ShowIf("Enabled", true)] [Range(0f, 2f)]
        public float ControllerHeight = 1f;

        [ShowIf("Enabled", true)] [Range(0f, 1f)]
        public float TransitionDuration = 0.3f;
    }

    [Serializable]
    public class SlidingStateModule
    {
        public bool Enabled = false;

        [ShowIf("Enabled", true)] [Range(20f, 90f)]
        public float SlideTreeshold = 32f;

        [ShowIf("Enabled", true)] [Range(0f, 50f)]
        public float SlideSpeed = 15f;
    }

    #endregion

    public bool IsGrounded
    {
        get => controller.isGrounded;
    }

    public Vector3 Velocity
    {
        get => controller.velocity;
    }

    public Vector3 SurfaceNormal { get; private set; }

    public float SlopeLimit
    {
        get => controller.slopeLimit;
    }

    public float DefaultHeight { get; private set; }

    private static readonly int InAir = Animator.StringToHash("InAir");
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int VelocityHash = Animator.StringToHash("Velocity");
    private static readonly int Moving = Animator.StringToHash("Moving");
    private static readonly int Crouching = Animator.StringToHash("Crouching");
    private static readonly int Sliding = Animator.StringToHash("Sliding");
    private static readonly int Sprinting = Animator.StringToHash("Sprinting");
    private static readonly int Proning = Animator.StringToHash("Proning");
    private static readonly int TurnRight = Animator.StringToHash("TurnRight");
    private static readonly int TurnLeft = Animator.StringToHash("TurnLeft");

    [Header("General")] [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;
    [SerializeField] private NetworkPlayerAnimController networkPlayerAnimController;
    [SerializeField] private float moveSmoothing = 2f;
    [SerializeField] private LayerMask m_ObstacleCheckMask = ~0;
    [SerializeField] private float gravity;

    [Space] [Header("Camera")] [SerializeField]
    private Transform mainCamera;

    [SerializeField] private Transform cameraHolder;

    [SerializeField] private Transform firstPersonCamera;

    //[SerializeField] private float sensitivity;
    [SerializeField] private Vector2 freeLookAngle;

    [Space] [Header("Dynamic Motions")] [SerializeField]
    private IKAnimation aimMotionAsset;

    [SerializeField] private IKAnimation leanMotionAsset;
    [SerializeField] private IKAnimation crouchMotionAsset;
    [SerializeField] private IKAnimation unCrouchMotionAsset;
    [SerializeField] private IKAnimation onJumpMotionAsset;
    [SerializeField] private IKAnimation onLandedMotionAsset;

    [Space] [Header("Turning")] [SerializeField]
    private float turnInPlaceAngle;

    [SerializeField] private AnimationCurve turnCurve = new AnimationCurve(new Keyframe(0f, 0f));
    [SerializeField] private float turnSpeed = 1f;

    [Space] [SerializeField] [Group] private CoreMovementModule m_CoreMovement;

    [SerializeField] [Group] private MovementStateModule m_RunState;

    [SerializeField] [Group] private LowerHeightStateModule m_CrouchState;

    [SerializeField] [Group] private LowerHeightStateModule m_ProneState;

    [SerializeField] [Group] private JumpStateModule m_JumpState;

    [SerializeField] [Group] private SlidingStateModule m_SlidingState;

    private MovementStateModule m_CurrentMovementState;


    private CollisionFlags m_CollisionFlags;

    private Quaternion moveRotation;
    private float turnProgress = 1f;
    private bool isTurning;

    private Vector2 _freeLookInput;
    private Vector2 _smoothAnimatorMove;
    private Vector2 _smoothMove;

    private Vector2 look;
    private bool _freeLook;

    private float m_DistMovedSinceLastCycleEnded;
    private float m_CurrentStepLength;

    private Vector3 m_SlideVelocity;
    private Vector3 m_DesiredVelocityLocal;
    private bool m_PreviouslyGrounded;
    private float m_LastLandTime;
    private float m_NextTimeCanChangeHeight;

    private bool skippedFirstFrame = false;

    private float _sprintAnimatorInterp = 8f;

    private void Start()
    {
        DefaultHeight = controller.height;

        moveRotation = transform.rotation;

        Player.Slide.SetStartTryer(TrySlide);

        Player.Slide.AddStartListener(StartSlide);

        Player.Crouch.AddStartListener(Crouch);
        Player.Crouch.AddStopListener(Standup);

        Player.Crouch.SetStartTryer(() => { return Try_ToggleCrouch(m_CrouchState); });
        Player.Crouch.SetStopTryer(() => { return Try_ToggleCrouch(null); });

        Player.Prone.AddStartListener(Prone);
        Player.Prone.AddStopListener(CancelProne);

        Player.Prone.SetStartTryer(() => { return Try_ToggleProne(m_ProneState); });
        Player.Prone.SetStopTryer(() => { return Try_ToggleProne(null); });

        Player.Jump.SetStartTryer(Try_Jump);

        Player.Sprint.SetStartTryer(TryStartSprint);
        Player.Sprint.AddStartListener(StartSprint);
        Player.Sprint.AddStopListener(StopSprint);

        Player.Lean.AddStartListener(Lean);
        Player.Lean.AddStopListener(() => Player.CharAnimData.leanDirection = 0);

        Player.DisabledMovement.SetStartTryer(TryDisableMovement);

        networkPlayerAnimController.FpsAnimator.onPostUpdate.AddListener(UpdateCameraRotation);
    }

    private void Update()
    {
        if (Player.DisabledMovement.Active)
            return;

        float deltaTime = Time.deltaTime;

        Vector3 translation;

        if (IsGrounded)
        {
            translation = transform.TransformVector(m_DesiredVelocityLocal) * deltaTime;

            if (!Player.Jump.Active)
                translation.y = -.05f;
        }
        else
            translation = transform.TransformVector(m_DesiredVelocityLocal * deltaTime);

        m_CollisionFlags = controller.Move(translation);

        if ((m_CollisionFlags & CollisionFlags.Below) == CollisionFlags.Below && !m_PreviouslyGrounded)
        {
            bool wasJumping = Player.Jump.Active;

            if (Player.Jump.Active)
                Player.Jump.ForceStop();

            //Player.FallImpact.Send(Mathf.Abs(m_DesiredVelocityLocal.y));

            m_LastLandTime = Time.time;

            if (wasJumping)
                m_DesiredVelocityLocal = Vector3.ClampMagnitude(m_DesiredVelocityLocal, 1f);
        }

        // Check if the top of the controller collided with anything,
        // If it did then add a counter force
        if (((m_CollisionFlags & CollisionFlags.Above) == CollisionFlags.Above && !controller.isGrounded) &&
            m_DesiredVelocityLocal.y > 0)
            m_DesiredVelocityLocal.y *= -.05f;

        Vector3 targetVelocity = CalcTargetVelocity(Player.MoveInput.Get());

        if (!IsGrounded)
            UpdateAirborneMovement(deltaTime, targetVelocity, ref m_DesiredVelocityLocal);
        else if (!Player.Jump.Active)
            UpdateGroundedMovement(deltaTime, targetVelocity, ref m_DesiredVelocityLocal);

        if (!Player.Pause.Active)
        {
            UpdateLookInput();
            UpdateRecoil();
        }

        UpdateMovementAnimations();
        Player.IsGrounded.Set(IsGrounded);
        Player.Velocity.Set(Velocity);

        m_PreviouslyGrounded = IsGrounded;
    }

    public Vector2 _cameraRecoilOffset;
    public Vector2 _controllerRecoil;
    public float _recoilStep;
    private bool _isFiring;

    private void UpdateRecoil()
    {
        if (Mathf.Approximately(_controllerRecoil.magnitude, 0f)
            && Mathf.Approximately(_cameraRecoilOffset.magnitude, 0f))
        {
            return;
        }

        float smoothing = 8f;
        float restoreSpeed = 8f;
        float cameraWeight = 0f;

        RecoilPattern recoilPattern = Player.ActiveEquipmentItem.Get().recoilPattern;
        if (recoilPattern != null)
        {
            smoothing = recoilPattern.smoothing;
            restoreSpeed = recoilPattern.cameraRestoreSpeed;
            cameraWeight = recoilPattern.cameraWeight;
        }

        _controllerRecoil = Vector2.Lerp(_controllerRecoil, Vector2.zero,
            FPSAnimLib.ExpDecayAlpha(smoothing, Time.deltaTime));
        
        look += _controllerRecoil * Time.deltaTime;

        Vector2 clamp = Vector2.Lerp(Vector2.zero, new Vector2(90f, 90f), cameraWeight);
        _cameraRecoilOffset -= _controllerRecoil * Time.deltaTime;
        _cameraRecoilOffset = Vector2.ClampMagnitude(_cameraRecoilOffset, clamp.magnitude);

        if (_isFiring) return;

        _cameraRecoilOffset = Vector2.Lerp(_cameraRecoilOffset, Vector2.zero,
            FPSAnimLib.ExpDecayAlpha(restoreSpeed, Time.deltaTime));
    }

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
        animator.CrossFade(Sliding, 0.1f);
        Player.Slide.ForceStop();
    }

    private bool TryDisableMovement()
    {
        return true;
    }

    #region Sprint

    private bool TryStartSprint()
    {
        if (!m_RunState.Enabled || Player.Stamina.Get() < 15f)
            return false;

        bool wantsToMoveBack = Player.MoveInput.Get().y < 0f;
        bool canChangeState = Player.IsGrounded.Get() && !wantsToMoveBack && !Player.Crouch.Active &&
                              !Player.Aim.Active && !Player.Prone.Active;

        if (canChangeState)
            m_CurrentMovementState = m_RunState;

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

        m_CurrentMovementState = null;
        networkPlayerAnimController.LookLayer.SetLayerAlpha(1f);
        networkPlayerAnimController.AdsLayer.SetLayerAlpha(1f);
    }

    #endregion

    #region Crouch

    private bool Try_ToggleCrouch(LowerHeightStateModule lowerHeightState)
    {
        if (!m_CrouchState.Enabled)
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
        animator.SetBool(Crouching, true);
        networkPlayerAnimController.SlotLayer.PlayMotion(crouchMotionAsset);
    }

    private void Standup()
    {
        networkPlayerAnimController.LookLayer.SetPelvisWeight(1f);
        animator.SetBool(Crouching, false);
        networkPlayerAnimController.SlotLayer.PlayMotion(unCrouchMotionAsset);
    }

    #endregion

    private bool Try_ToggleProne(LowerHeightStateModule lowerHeightState)
    {
        if (!m_ProneState.Enabled)
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
        animator.SetBool(Crouching, false);
        animator.SetBool(Proning, true);
        networkPlayerAnimController.SlotLayer.PlayMotion(unCrouchMotionAsset);
    }

    private void CancelProne()
    {
        networkPlayerAnimController.LookLayer.SetPelvisWeight(0f);
        animator.SetBool(Crouching, true);
        animator.SetBool(Proning, false);
        networkPlayerAnimController.SlotLayer.PlayMotion(unCrouchMotionAsset);
    }

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

        bool canJump = m_JumpState.Enabled &&
                       IsGrounded &&
                       !Player.Crouch.Active &&
                       Time.time > m_LastLandTime + m_JumpState.JumpTimer;

        if (!canJump)
            return false;

        float jumpSpeed = Mathf.Sqrt(2 * gravity * m_JumpState.JumpHeight);
        m_DesiredVelocityLocal = new Vector3(m_DesiredVelocityLocal.x, jumpSpeed, m_DesiredVelocityLocal.z);

        return true;
    }

    // ReSharper disable Unity.PerformanceAnalysis
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
        float surfaceAngle = Vector3.Angle(Vector3.up, SurfaceNormal);
        targetVelocity *= m_CoreMovement.SlopeSpeedMult.Evaluate(surfaceAngle / SlopeLimit);
    }

    private void UpdateVelocity(float deltaTime, Vector3 targetVelocity, ref Vector3 velocity)
    {
        float targetAccel = (targetVelocity.sqrMagnitude > 0f) ? m_CoreMovement.Acceleration : m_CoreMovement.Damping;
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
        if (m_SlidingState.Enabled)
        {
            float surfaceAngle = Vector3.Angle(Vector3.up, SurfaceNormal);

            if (surfaceAngle > m_SlidingState.SlideTreeshold && Player.MoveInput.Get().sqrMagnitude == 0f)
            {
                Vector3 slideDirection = (SurfaceNormal + Vector3.down);
                m_SlideVelocity += slideDirection * (m_SlidingState.SlideSpeed * deltaTime);
            }
            else
            {
                m_SlideVelocity = Vector3.Lerp(m_SlideVelocity, Vector3.zero, deltaTime * 10f);
            }

            velocity += transform.InverseTransformVector(m_SlideVelocity);
        }
    }

    private void AdvanceStepCycle(float deltaTime)
    {
        m_DistMovedSinceLastCycleEnded += m_DesiredVelocityLocal.magnitude * deltaTime;

        float targetStepLength = (m_CurrentMovementState != null)
            ? m_CurrentMovementState.StepLength
            : m_CoreMovement.StepLength;
        m_CurrentStepLength = Mathf.MoveTowards(m_CurrentStepLength, targetStepLength, deltaTime);

        if (m_DistMovedSinceLastCycleEnded > m_CurrentStepLength)
        {
            m_DistMovedSinceLastCycleEnded -= m_CurrentStepLength;
            Player.MoveCycleEnded.Send();
        }

        Player.MoveCycle.Set(m_DistMovedSinceLastCycleEnded / m_CurrentStepLength);
    }

    private void UpdateAirborneMovement(float deltaTime, Vector3 targetVelocity, ref Vector3 velocity)
    {
        AdjustVelocityForJump(deltaTime, ref velocity);
        ApplyAirborneControl(targetVelocity, deltaTime, ref velocity);
        ApplyGravity(deltaTime, ref velocity);
        PlayMotionBasedOnGroundedState();
    }

    private void AdjustVelocityForJump(float deltaTime, ref Vector3 velocity)
    {
        if (m_PreviouslyGrounded && !Player.Jump.Active)
            velocity.y = 0f;
    }

    private void ApplyAirborneControl(Vector3 targetVelocity, float deltaTime, ref Vector3 velocity)
    {
        velocity += targetVelocity * (m_CoreMovement.Acceleration * m_CoreMovement.AirborneControl * deltaTime);
    }

    private void ApplyGravity(float deltaTime, ref Vector3 velocity)
    {
        velocity.y -= gravity * deltaTime;
    }

    private void PlayMotionBasedOnGroundedState()
    {
        networkPlayerAnimController.SlotLayer.PlayMotion(!IsGrounded ? onJumpMotionAsset : onLandedMotionAsset);
    }

    private void UpdateLookInput()
    {
        //_freeLook = Input.GetKey(KeyCode.X);

        float deltaMouseX = Player.LookInput.Get().x * SettingMenu.Instance.GameSettings.Sensitivity;
        float deltaMouseY = -Player.LookInput.Get().y * SettingMenu.Instance.GameSettings.Sensitivity;

        if (_freeLook)
        {
            // No input for both controller and animation component. We only want to rotate the camera

            _freeLookInput.x += deltaMouseX;
            _freeLookInput.y += deltaMouseY;

            _freeLookInput.x = Mathf.Clamp(_freeLookInput.x, -freeLookAngle.x, freeLookAngle.x);
            _freeLookInput.y = Mathf.Clamp(_freeLookInput.y, -freeLookAngle.y, freeLookAngle.y);

            return;
        }

        _freeLookInput = Vector2.Lerp(_freeLookInput, Vector2.zero,
            FPSAnimLib.ExpDecayAlpha(15f, Time.deltaTime));

        look.x += deltaMouseX;
        look.y += deltaMouseY;

        float proneWeight = animator.GetFloat("ProneWeight");
        Vector2 pitchClamp = Vector2.Lerp(new Vector2(-90f, 90f), new Vector2(-30, 0f), proneWeight);

        look.y = Mathf.Clamp(look.y, pitchClamp.x, pitchClamp.y);
        moveRotation *= Quaternion.Euler(0f, deltaMouseX, 0f);
        TurnInPlace();

        //_jumpState = Mathf.Lerp(_jumpState, movementComponent.IsInAir() ? 1f : 0f,
        // FPSAnimLib.ExpDecayAlpha(10f, Time.deltaTime));

        float moveWeight = Mathf.Clamp01(Mathf.Abs(_smoothMove.magnitude));
        transform.rotation = Quaternion.Slerp(transform.rotation, moveRotation, moveWeight);
        //transform.rotation = Quaternion.Slerp(transform.rotation, moveRotation, _jumpState);
        look.x *= 1f - moveWeight;
        //look.x *= 1f - _jumpState;

        Player.CharAnimData.SetAimInput(look);
        Player.CharAnimData.AddDeltaInput(new Vector2(deltaMouseX, Player.CharAnimData.deltaAimInput.y));
    }

    private void UpdateCameraRotation()
    {
        Vector2 finalInput = new Vector2(look.x, look.y);
        (Quaternion, Vector3) cameraTransform =
            (transform.rotation * Quaternion.Euler(finalInput.y, finalInput.x, 0f),
                firstPersonCamera.position);

        cameraHolder.rotation = cameraTransform.Item1;
        cameraHolder.position = cameraTransform.Item2;

        mainCamera.rotation = cameraHolder.rotation * Quaternion.Euler(_freeLookInput.y, _freeLookInput.x, 0f);
    }

    private void TurnInPlace()
    {
        float turnInput = look.x;
        look.x = Mathf.Clamp(look.x, -90f, 90f);
        turnInput -= look.x;

        float sign = Mathf.Sign(look.x);
        if (Mathf.Abs(look.x) > turnInPlaceAngle)
        {
            if (!isTurning)
            {
                turnProgress = 0f;

                animator.ResetTrigger(TurnRight);
                animator.ResetTrigger(TurnLeft);

                animator.SetTrigger(sign > 0f ? TurnRight : TurnLeft);
            }

            isTurning = true;
        }

        transform.rotation *= Quaternion.Euler(0f, turnInput, 0f);

        float lastProgress = turnCurve.Evaluate(turnProgress);
        turnProgress += Time.deltaTime * turnSpeed;
        turnProgress = Mathf.Min(turnProgress, 1f);

        float deltaProgress = turnCurve.Evaluate(turnProgress) - lastProgress;

        look.x -= sign * turnInPlaceAngle * deltaProgress;

        transform.rotation *= Quaternion.Slerp(Quaternion.identity,
            Quaternion.Euler(0f, sign * turnInPlaceAngle, 0f), deltaProgress);

        if (Mathf.Approximately(turnProgress, 1f) && isTurning)
        {
            isTurning = false;
        }
    }

    private Vector3 CalcTargetVelocity(Vector2 moveInput)
    {
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        bool wantsToMove = moveInput.sqrMagnitude > 0f;

        // Calculate the direction (relative to the us), in which the player wants to move.
        Vector3 targetDirection =
            (wantsToMove ? new Vector3(moveInput.x, 0f, moveInput.y) : m_DesiredVelocityLocal.normalized);

        float desiredSpeed = 0f;

        if (wantsToMove)
        {
            // Set the default speed.
            desiredSpeed = m_CoreMovement.ForwardSpeed;
            // If the player wants to move sideways...
            if (Mathf.Abs(moveInput.x) > 0f)
                desiredSpeed = m_CoreMovement.SideSpeed;

            // If the player wants to move backwards...
            if (moveInput.y < 0f)
                desiredSpeed = m_CoreMovement.BackSpeed;

            // If we're currently running...
            if (Player.Sprint.Active)
            {
                // If the player wants to move forward or sideways, apply the run speed multiplier.
                if (desiredSpeed == m_CoreMovement.ForwardSpeed || desiredSpeed == m_CoreMovement.SideSpeed)
                    desiredSpeed = m_CurrentMovementState.SpeedMultiplier;
            }
            else
            {
                // If we're crouching/pronning...
                if (m_CurrentMovementState != null)
                    desiredSpeed *= m_CurrentMovementState.SpeedMultiplier;
            }
        }

        return targetDirection * (desiredSpeed * Player.MovementSpeedFactor.Val);
    }

    public Vector2 AnimatorVelocity { get; private set; }

    private void UpdateMovementAnimations()
    {
        float moveX = Player.MoveInput.Get().x;
        float moveY = Player.MoveInput.Get().y;

        Vector2 rawInput = new Vector2(moveX, moveY);
        Vector2 normInput = new Vector2(moveX, moveY);
        normInput.Normalize();

        var animatorVelocity = normInput;

        animatorVelocity *= Player.IsGrounded.Get() ? 1f : 0f;

        AnimatorVelocity = Vector2.Lerp(AnimatorVelocity, animatorVelocity,
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

        animator.SetBool(Moving, !moving);
        animator.SetFloat(MoveX, AnimatorVelocity.x);
        animator.SetFloat(MoveY, AnimatorVelocity.y);
        animator.SetFloat(VelocityHash, AnimatorVelocity.magnitude);

        float a = animator.GetFloat(Sprinting);
        float b = Player.Sprint.Active ? 1f : 0f;

        a = Mathf.Lerp(a, b, FPSAnimLib.ExpDecayAlpha(_sprintAnimatorInterp, Time.deltaTime));
        animator.SetFloat(Sprinting, a);
    }

    private bool Try_ChangeControllerHeight(LowerHeightStateModule lowerHeightState)
    {
        bool canChangeHeight =
            (Time.time > m_NextTimeCanChangeHeight || m_NextTimeCanChangeHeight == 0f) &&
            Player.IsGrounded.Get() &&
            !Player.Sprint.Active;


        if (canChangeHeight)
        {
            float height = (lowerHeightState == null) ? DefaultHeight : lowerHeightState.ControllerHeight;

            //If the "lowerHeightState" height is bigger than the current one check if there's anything over the Player's head
            if (height > controller.height)
            {
                if (DoCollisionCheck(true, Mathf.Abs(height - controller.height)))
                    return false;
            }

            if (lowerHeightState != null)
                m_NextTimeCanChangeHeight = Time.time + lowerHeightState.TransitionDuration;

            SetHeight(height);

            m_CurrentMovementState = lowerHeightState;
        }

        return canChangeHeight;
    }

    private bool DoCollisionCheck(bool checkAbove, float maxDistance)
    {
        Vector3 rayOrigin = transform.position + (checkAbove ? Vector3.up * controller.height : Vector3.zero);
        Vector3 rayDirection = checkAbove ? Vector3.up : Vector3.down;

        return Physics.Raycast(rayOrigin, rayDirection, maxDistance, m_ObstacleCheckMask,
            QueryTriggerInteraction.Ignore);
    }

    private void SetHeight(float height)
    {
        controller.height = height;
        controller.center = Vector3.up * height * 0.5f;
    }
}