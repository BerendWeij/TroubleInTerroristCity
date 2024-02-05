using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerManager : Singleton<AudioMixerManager>
{
    [SerializeField]
    private AudioMixer _masterMixer;
       
    [SerializeField]
    private AudioMixerGroup _SFXGroup;
    [SerializeField]
    private AudioMixerGroup _musicGroup;
     
    public AudioMixer MasterMixer
    {
        get => _masterMixer;
    }
     
    public AudioMixerGroup SFXGroup
    {
        get => _SFXGroup;
    }
     
    public AudioMixerGroup MusicGroup
    {
        get => _musicGroup;
    }
}

