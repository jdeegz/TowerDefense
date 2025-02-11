using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private AudioMixer m_audioMixer;
    
    void Start()
    {
        GameSettings.AudioMixer = m_audioMixer;
        
        //Get volumes
        float masterVol = GameSettings.MasterVolumeValue;
        TryChangeMasterVolume(masterVol);

        float musicVol = GameSettings.MusicVolumeValue;
        TryChangeMusicVolume(musicVol);

        float sfxVol = GameSettings.SFXVolumeValue;
        TryChangeSFXVolume(sfxVol);
    }

    void TryChangeMasterVolume(float value)
    {
        GameSettings.MasterVolumeValue = value;
    }

    void TryChangeMusicVolume(float value)
    {
        GameSettings.MusicVolumeValue = value;
    }

    void TryChangeSFXVolume(float value)
    {
        GameSettings.SFXVolumeValue = value;
    }
}
