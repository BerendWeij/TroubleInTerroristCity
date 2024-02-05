using System;
using UnityEngine;

public abstract class EquipmentItemInfo : ScriptableObject
{
    #region Internal
    [Serializable]
    public class GeneralInfo
    {
        [BHeader("( Use Settings )")]

        public bool UseWhileAirborne;
        public bool UseWhileRunning;
        public bool CanStopReloading;

        [Space(3f)]

        [BHeader("( Others )", order = 2)]

        public int CrosshairID;

        [Range(0f, 100f)]
        public float StaminaTakePerUse;

        [Range(0.01f, 2f)]
        public float MovementSpeedMod = 1f;
        
        public OverlayType overlayType;
    }

    [Serializable]
    public class AimingInfo
    {
        public bool Enabled;

        [Space]

        [EnableIf("Enabled", true)]
        public float AimThreshold;

        [EnableIf("Enabled", true)]
        public float AimCamHeadbobMod;

        [EnableIf("Enabled", true)]
        public float AimMovementSpeedMod;

        [EnableIf("Enabled", true)]
        public bool AimWhileAirborne;

        [EnableIf("Enabled", true)]
        public bool UseAimBlur;

        [EnableIf("Enabled", true)]
        public SoundPlayer AimSounds;
    }

    [Serializable]
    public class ToggleWeaponStateModule
    {
        [Range(0.1f, 5f)]
        public float Duration = 0.6f;

        [Space(4f)]

        public float AnimationSpeed = 1f;

        public DelayedSound[] Audio;
        //public DelayedCameraForce[] CameraForces = null;
    }
    #endregion

    [Group("1: ")] public GeneralInfo General;

    [Space]

    [Group("2: ")] public ToggleWeaponStateModule Equipping;
    [Group("3: ")] public ToggleWeaponStateModule Unequipping;

    [Space]

    [Group("4: ")] public AimingInfo Aiming;
}