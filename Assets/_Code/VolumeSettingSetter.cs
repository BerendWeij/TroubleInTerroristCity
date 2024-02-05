using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class VolumeSettingSetter : MonoBehaviour
{
    private Volume _volume;
    private void Awake()
    {
        _volume = GetComponent<Volume>();
        
        SetShadowDistance(PlayerPrefs.GetFloat("Slider_ShadowDistance"));
    }
    
    public void SetShadowDistance(float value)
    {
        if (_volume.profile.TryGet(out HDShadowSettings shadows))
        {
            shadows.maxShadowDistance.value = value;
        }
    }
}
