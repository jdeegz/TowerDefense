using UnityEngine;
using UnityEngine.Audio;

public static class GameSettings
{
    // Dynamic Tooltip Positioning
    private const string DynamicToolTipsKey = "DynamicToolTips";
    public static bool DynamicToolTipsEnabled
    {
        get => PlayerPrefs.GetInt(DynamicToolTipsKey, 1) == 1;
        set => PlayerPrefs.SetInt(DynamicToolTipsKey, value ? 1 : 0);
    }

    // AUDIO
    // Mixer
    public static AudioMixer AudioMixer; // Set through the inspector.
    
    // Master Volume
    private const string MasterVolumeKey = "MasterVolume"; // Must match Audio Mixer string.
    public static float MasterVolumeValue
    {
        get => PlayerPrefs.GetFloat(MasterVolumeKey, 0.5f);
        set
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, value); 
            AudioMixer.SetFloat(MasterVolumeKey, Mathf.Log10(value) * 20);
        }
    }
    
    // Music Volume
    private const string MusicVolumeKey = "MusicVolume"; // Must match Audio Mixer string.
    public static float MusicVolumeValue
    {
        get => PlayerPrefs.GetFloat(MusicVolumeKey, 0.3f);
        set
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, value); 
            AudioMixer.SetFloat(MusicVolumeKey, Mathf.Log10(value) * 20);
        }
    }
    
    // SFX Volume
    private const string SFXVolumeKey = "SFXVolume"; // Must match Audio Mixer string.
    public static float SFXVolumeValue
    {
        get => PlayerPrefs.GetFloat(SFXVolumeKey, 0.5f);
        set
        {
            PlayerPrefs.SetFloat(SFXVolumeKey, value); 
            AudioMixer.SetFloat(SFXVolumeKey, Mathf.Log10(value) * 20);
        }
    }
    
    // Window Setting
    private const string WindowSettingKey = "WindowSetting";
    public static int WindowSettingValue
    {
        get => PlayerPrefs.GetInt(WindowSettingKey, 0);
        set => PlayerPrefs.SetInt(WindowSettingKey, value);
    }
    
    // Localization
    private const string SelectedLanguageKey = "SelectedLanguage";
    public static string SelectedLanguageValue
    {
        get => PlayerPrefs.GetString(SelectedLanguageKey, "en-US");
        set => PlayerPrefs.SetString(SelectedLanguageKey, value);
    }
}
