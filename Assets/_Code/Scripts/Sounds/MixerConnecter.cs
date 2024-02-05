using UnityEngine;

public class MixerConnecter : MonoBehaviour
{
    void Start()
    {
        AudioSource source = GetComponent<AudioSource>();
        source.outputAudioMixerGroup = AudioMixerManager.Instance.SFXGroup;
    }
}
