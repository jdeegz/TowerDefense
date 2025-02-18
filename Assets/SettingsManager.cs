using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private AudioMixer m_audioMixer;
    
    void Start()
    {
        GameSettings.AudioMixer = m_audioMixer;
        ApplyVolumeSettings();
        
    }

    void ApplyVolumeSettings()
    {
        GameSettings.MasterVolumeValue = GameSettings.MasterVolumeValue;

        GameSettings.MusicVolumeValue = GameSettings.MusicVolumeValue;

        GameSettings.SFXVolumeValue = GameSettings.SFXVolumeValue;

        GameSettings.DynamicToolTipsEnabled = GameSettings.DynamicToolTipsEnabled;
    }
}
