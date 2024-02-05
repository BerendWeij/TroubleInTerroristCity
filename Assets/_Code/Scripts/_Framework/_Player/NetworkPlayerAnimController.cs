using Kinemation.FPSFramework.Runtime.Camera;
using Kinemation.FPSFramework.Runtime.Core.Components;
using Kinemation.FPSFramework.Runtime.Core.Playables;
using Kinemation.FPSFramework.Runtime.Core.Types;
using Kinemation.FPSFramework.Runtime.Layers;
using Kinemation.FPSFramework.Runtime.Recoil;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class NetworkPlayerAnimController : NetworkPlayerComponent
{
    public FPSCameraShake shake;
    private FPSCamera fpsCamera;
    public CoreAnimComponent FpsAnimator => fpsAnimator;

    private RecoilAnimation recoilAnimation;
    private CoreAnimComponent fpsAnimator;

    [HideInInspector] public LookLayer LookLayer;
    [HideInInspector] public AdsLayer AdsLayer;
    [HideInInspector] public SwayLayer SwayLayer;
    [HideInInspector] public LocomotionLayer LocoLayer;
    [HideInInspector] public SlotLayer SlotLayer;

    //public CharAnimStates _charAnimStates = new CharAnimStates();

    protected NetworkVariable<CharAnimData> charAnimData = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    private bool _hasActiveAction;

    // Used primarily for function calls from Animation Events
    // Runs once at the beginning of the next update
    protected UnityEvent queuedAnimEvents;
    private void Start()
    {
        if (fpsCamera != null)
            fpsAnimator.onPostUpdate.AddListener(fpsCamera.UpdateCamera);
    }

    private void Awake()
    {
        recoilAnimation = GetComponentInChildren<RecoilAnimation>();
        fpsAnimator = GetComponentInChildren<CoreAnimComponent>();
        
        InitAnimController();
    }
    
    private void Update()
    {
        UpdateAnimController();
    }

    private void LateUpdate()
    {
        if (IsOwner)
        {
            charAnimData.Value = Player.CharAnimData;
        }
        else
        {
            Player.CharAnimData = charAnimData.Value;
            Player.MoveInput.Set(Player.CharAnimData.moveInput);
        }

        fpsAnimator.SetCharData(Player.CharAnimData);
    }

    public void SetActionActive(int isActive)
    {
        _hasActiveAction = isActive != 0;
    }

    protected void InitAnimController()
    {
        LookLayer = GetComponentInChildren<LookLayer>();
        AdsLayer = GetComponentInChildren<AdsLayer>();
        LocoLayer = GetComponentInChildren<LocomotionLayer>();
        SwayLayer = GetComponentInChildren<SwayLayer>();
        SlotLayer = GetComponentInChildren<SlotLayer>();
        fpsAnimator = GetComponentInChildren<CoreAnimComponent>();
        fpsAnimator.animGraph.InitPlayableGraph();
        fpsAnimator.InitializeLayers();
        Player.CharAnimData = new CharAnimData();

        fpsCamera = GetComponentInChildren<FPSCamera>();
    }

    // Call this during Update after all the gameplay logic
    protected void UpdateAnimController()
    {
        if (queuedAnimEvents != null)
        {
            queuedAnimEvents.Invoke();
            queuedAnimEvents = null;
        }

        Player.CharAnimData.recoilAnim = new LocRot(recoilAnimation.OutLoc, Quaternion.Euler(recoilAnimation.OutRot));
        fpsAnimator.SetCharData(Player.CharAnimData);
        
        fpsAnimator.animGraph.UpdateGraph();
        fpsAnimator.UpdateCoreComponent();
    }
    public CoreAnimGraph GetAnimGraph()
    {
        return fpsAnimator.animGraph;
    }

    public void PlayCameraShake(FPSCameraShake shake)
    {
        if (fpsCamera != null)
        {
            fpsCamera.PlayShake(shake.shakeInfo);
        }
    }
        
    public void PlayController(RuntimeAnimatorController controller, AnimSequence motion)
    {
        if (motion == null) return;
        fpsAnimator.animGraph.PlayController(controller, motion.clip, motion.blendTime.blendInTime);
    }
        
    // Call this to play a static pose on the character upper body
    public void PlayPose(AnimSequence motion)
    {
        if (motion == null) return;
        fpsAnimator.animGraph.PlayPose(motion);
    }

    // Call this to play an animation on the character upper body
    public void PlayAnimation(AnimSequence motion, float startTime = 0f)
    {
        if (motion == null) return;
            
        fpsAnimator.animGraph.PlayAnimation(motion, startTime);
    }
        
    public void StopAnimation(float blendTime = 0f)
    {
        fpsAnimator.animGraph.StopAnimation(blendTime);
    }
}

