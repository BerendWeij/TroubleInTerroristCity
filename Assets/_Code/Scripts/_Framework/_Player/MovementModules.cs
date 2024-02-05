using System;
using UnityEngine;

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

    public AnimationCurve SlopeSpeedMult = new(new Keyframe(0f, 1f), new Keyframe(1f, 1f));

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
    public bool Enabled;

    [ShowIf("Enabled", true)] [Range(20f, 90f)]
    public float SlideTreeshold = 32f;

    [ShowIf("Enabled", true)] [Range(0f, 50f)]
    public float SlideSpeed = 15f;
}