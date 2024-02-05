using UnityEngine;

public class FPSPlayerController : PlayerComponent
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;

    public CharacterController Controller => controller;
    public Animator Animator => animator;
}

public class FPSPlayerComponent : PlayerComponent
{
    protected FPSPlayerController PlayerController
    {
        get
        {
            if (!_playerController)
                _playerController = GetComponent<FPSPlayerController>();
            if (!_playerController)
                _playerController = GetComponentInParent<FPSPlayerController>();
            
            return _playerController;
        }
    }

    private FPSPlayerController _playerController;
}