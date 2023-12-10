using Kinemation.FPSFramework.Runtime.FPSAnimator;
using UnityEngine;

public class FPSPlayerCamera : FPSPlayerComponent
{
    [Space] [Header("Turning")] [SerializeField]
    private float turnInPlaceAngle;

    [SerializeField] private AnimationCurve turnCurve = new AnimationCurve(new Keyframe(0f, 0f));
    [SerializeField] private float turnSpeed = 1f;
    
    [Space] [Header("Camera")] 
    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Transform firstPersonCamera;
    
    [SerializeField] private Vector2 freeLookAngle;
    
    private Quaternion _moveRotation;
    private float _turnProgress = 1f;
    private bool _isTurning;
    
    private Vector2 _cameraRecoilOffset;
    
    private bool _isFiring;
    
    private Vector2 _freeLookInput;
    private Vector2 look;
    private bool _freeLook;
    
    private static readonly int TurnRight = Animator.StringToHash("TurnRight");
    private static readonly int TurnLeft = Animator.StringToHash("TurnLeft");
    
    private void Start()
    {
        _moveRotation = transform.rotation;
    }

    private void Update()
    {
        if (!Player.Pause.Active)
        {
            UpdateLookInput();
            UpdateRecoil();
        }
    }


    private void UpdateRecoil()
    {
        if (Mathf.Approximately(PlayerController._controllerRecoil.magnitude, 0f)
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

        PlayerController._controllerRecoil = Vector2.Lerp(PlayerController._controllerRecoil, Vector2.zero,
            FPSAnimLib.ExpDecayAlpha(smoothing, Time.deltaTime));

        look += PlayerController._controllerRecoil * Time.deltaTime;

        Vector2 clamp = Vector2.Lerp(Vector2.zero, new Vector2(90f, 90f), cameraWeight);
        _cameraRecoilOffset -= PlayerController._controllerRecoil * Time.deltaTime;
        _cameraRecoilOffset = Vector2.ClampMagnitude(_cameraRecoilOffset, clamp.magnitude);

        if (_isFiring) return;

        _cameraRecoilOffset = Vector2.Lerp(_cameraRecoilOffset, Vector2.zero,
            FPSAnimLib.ExpDecayAlpha(restoreSpeed, Time.deltaTime));
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

        float proneWeight = PlayerController.Animator.GetFloat("ProneWeight");
        Vector2 pitchClamp = Vector2.Lerp(new Vector2(-90f, 90f), new Vector2(-30, 0f), proneWeight);

        look.y = Mathf.Clamp(look.y, pitchClamp.x, pitchClamp.y);
        _moveRotation *= Quaternion.Euler(0f, deltaMouseX, 0f);
        TurnInPlace();

        //_jumpState = Mathf.Lerp(_jumpState, movementComponent.IsInAir() ? 1f : 0f,
        // FPSAnimLib.ExpDecayAlpha(10f, Time.deltaTime));

        float moveWeight = Mathf.Clamp01(Mathf.Abs(PlayerController._smoothMove.magnitude));
        transform.rotation = Quaternion.Slerp(transform.rotation, _moveRotation, moveWeight);
        //transform.rotation = Quaternion.Slerp(transform.rotation, moveRotation, _jumpState);
        look.x *= 1f - moveWeight;
        //look.x *= 1f - _jumpState;

        Player.CharAnimData.SetAimInput(look);
        Player.CharAnimData.AddDeltaInput(new Vector2(deltaMouseX, Player.CharAnimData.deltaAimInput.y));
    }

    public void UpdateCameraRotation()
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
            if (!_isTurning)
            {
                _turnProgress = 0f;

                PlayerController.Animator.ResetTrigger(TurnRight);
                PlayerController.Animator.ResetTrigger(TurnLeft);

                PlayerController.Animator.SetTrigger(sign > 0f ? TurnRight : TurnLeft);
            }

            _isTurning = true;
        }

        transform.rotation *= Quaternion.Euler(0f, turnInput, 0f);

        float lastProgress = turnCurve.Evaluate(_turnProgress);
        _turnProgress += Time.deltaTime * turnSpeed;
        _turnProgress = Mathf.Min(_turnProgress, 1f);

        float deltaProgress = turnCurve.Evaluate(_turnProgress) - lastProgress;

        look.x -= sign * turnInPlaceAngle * deltaProgress;

        transform.rotation *= Quaternion.Slerp(Quaternion.identity,
            Quaternion.Euler(0f, sign * turnInPlaceAngle, 0f), deltaProgress);

        if (Mathf.Approximately(_turnProgress, 1f) && _isTurning)
        {
            _isTurning = false;
        }
    }
}