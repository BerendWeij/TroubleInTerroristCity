using UnityEngine;

[CreateAssetMenu(fileName = "Settings", menuName = "GameSettings", order = 1)]
public class GameSettings : ScriptableObject
{
    public float Sensitivity;
    public float Volume;
    public int Resolution;
    public int QualitySettings;

    public bool ResolutionBeenChanged;
}