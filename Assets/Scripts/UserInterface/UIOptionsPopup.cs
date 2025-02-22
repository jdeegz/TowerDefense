using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIOptionsPopup : UIPopup
{
    [Header("Audio Test Objects")]
    [SerializeField] private AudioClip m_volumeSliderAudioClip;
    [SerializeField] private AudioMixer m_audioMixer;

    [Header("Cheats Objects")]
    [SerializeField] private GameObject m_cheatsGroup;

    [Header("Volume - Master")]
    [SerializeField] private Slider m_volumeMasterSlider;
    [SerializeField] private TextMeshProUGUI m_volumeMasterLabel;

    [Header("Volume - Music")]
    [SerializeField] private Slider m_volumeMusicSlider;
    [SerializeField] private TextMeshProUGUI m_volumeMusicLabel;

    [Header("Volume - Sound Effects")]
    [SerializeField] private Slider m_volumeSFXSlider;
    [SerializeField] private TextMeshProUGUI m_volumeSFXLabel;

    [Header("Tooltips")]
    [SerializeField] private Toggle m_dynamicTooltipPlacement;

    [Header("Buttons")]
    [SerializeField] private Button m_surrenderButton;
    [SerializeField] private Button m_restartButton;
    [SerializeField] private Button m_exitApplicationButton;
    [SerializeField] private TMP_Dropdown m_screenModeDropdown;
    [SerializeField] private UIStringData m_uiStrings;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI m_missionNameLabel;
    [SerializeField] private TextMeshProUGUI m_surrenderButtonLabel;

    private int m_dropdownIndex;
    private float m_elapsedTime;

    void Awake()
    {
        base.Awake();

        m_elapsedTime = 0;
        m_cheatsGroup.SetActive(false);

        if (GameManager.Instance != null && GameManager.Instance.m_curMission != null)
        {
            m_missionNameLabel.SetText($"{GameManager.Instance.m_curMission.m_missionName}");
        }
    }

    public override void HandleShow()
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.m_gameState == GameManager.GameState.Menus)
            {
                m_surrenderButton.gameObject.SetActive(false);
                m_restartButton.gameObject.SetActive(false);
                m_missionNameLabel.gameObject.SetActive(false);
            }

            if (GameManager.Instance.m_gameState != GameManager.GameState.Menus)
            {
                //Out-of-game Only Options
            }
        }

        if (m_surrenderButton.gameObject.activeSelf)
        {
            string buttonString;
            if (GameplayManager.Instance.IsEndlessModeActive())
            {
                // Complete Mission
                buttonString = m_uiStrings.m_completeMission;
            }
            else
            {
                // Surrender
                buttonString = m_uiStrings.m_surrender;
            }

            m_surrenderButtonLabel.SetText(buttonString);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!GameManager.Instance || GameManager.Instance.m_gameState != GameManager.GameState.Menus)
        {
            m_cheatsGroup.SetActive(true);
        }
        else
        {
            m_cheatsGroup.SetActive(false);
        }
#endif

        base.HandleShow();
    }

    void Start()
    {
        m_surrenderButton.onClick.AddListener(OnSurrenderButtonClicked);
        m_restartButton.onClick.AddListener(OnRestartButtonClicked);
        m_exitApplicationButton.onClick.AddListener(OnExitApplicationButtonClicked);
        m_screenModeDropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        // Get tooltip setting
        bool isTooltipEnabled = GameSettings.DynamicToolTipsEnabled;
        m_dynamicTooltipPlacement.isOn = isTooltipEnabled;
        m_dynamicTooltipPlacement.onValueChanged.AddListener(ToggleDynamicTooltip);

        //Get volumes
        float masterVol = GameSettings.MasterVolumeValue;
        m_volumeMasterSlider.value = masterVol;
        TryChangeMasterVolume(masterVol);

        float musicVol = GameSettings.MusicVolumeValue;
        m_volumeMusicSlider.value = musicVol;
        TryChangeMusicVolume(musicVol);

        float sfxVol = GameSettings.SFXVolumeValue;
        m_volumeSFXSlider.value = sfxVol;
        TryChangeSFXVolume(sfxVol);

        m_volumeMasterSlider.onValueChanged.AddListener(TryChangeMasterVolume);
        m_volumeMusicSlider.onValueChanged.AddListener(TryChangeMusicVolume);
        m_volumeSFXSlider.onValueChanged.AddListener(TryChangeSFXVolume);

        //Set the dropdown to show the correct option on start.
        m_dropdownIndex = 0;
        if (!Screen.fullScreen)
        {
            m_dropdownIndex = 1;
        }

        m_screenModeDropdown.value = m_dropdownIndex;
    }

    void ToggleDynamicTooltip(bool value)
    {
        GameSettings.DynamicToolTipsEnabled = value;
    }

    void TryChangeMasterVolume(float value)
    {
        GameSettings.MasterVolumeValue = value;
        ChangeVolume(value, m_volumeMasterLabel, m_uiStrings.m_volumeMasterText);
    }

    void TryChangeMusicVolume(float value)
    {
        GameSettings.MusicVolumeValue = value;
        ChangeVolume(value, m_volumeMusicLabel, m_uiStrings.m_volumeMusicText);
    }

    void TryChangeSFXVolume(float value)
    {
        GameSettings.SFXVolumeValue = value;
        ChangeVolume(value, m_volumeSFXLabel, m_uiStrings.m_volumeSFXText);
    }

    void ChangeVolume(float volume, TextMeshProUGUI label, string text)
    {
        label.SetText(string.Format(text, (volume * 100).ToString("F0")));
        if (m_elapsedTime > 0.1f)
        {
            PlayAudio(m_volumeSliderAudioClip);
            m_elapsedTime = 0;
        }
    }

    void PlayAudio(AudioClip clip)
    {
        //Debug.Log($"playing audio clip: {clip.name}");
        m_audioSource.PlayOneShot(clip);
    }

    private void OnSurrenderButtonClicked()
    {
        //Debug.Log("Surrendering Mission.");
        int wave = 0;
        int perfectWavesCompleted = 0;

        // Are we surrendering from an endless match, or normal?
        if (GameplayManager.Instance.IsEndlessModeActive())
        {
            wave = GameplayManager.Instance.Wave;
            perfectWavesCompleted = GameplayManager.Instance.m_perfectWavesCompleted;
        }

        PlayerDataManager.Instance.UpdateMissionSaveData(gameObject.scene.name, 1, wave, perfectWavesCompleted);
        GameManager.Instance.RequestChangeScene("Menus", GameManager.GameState.Menus);
    }

    private void OnRestartButtonClicked()
    {
        //Debug.Log("Restarting Mission.");
        PlayerDataManager.Instance.UpdateMissionSaveData(gameObject.scene.name, 1, 0, 0);
        GameManager.Instance.RequestSceneRestart();
    }

    private void OnExitApplicationButtonClicked()
    {
        //Debug.Log("Quitting Application.");
        PlayerDataManager.Instance.UpdateMissionSaveData(gameObject.scene.name, 1, 0, 0);
        Application.Quit();
    }

    private void OnDropdownValueChanged(int index)
    {
        switch (index)
        {
            case 0:
                //Debug.Log($"Option {index} selected.");
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
                break;
            case 1:
                //Debug.Log($"Option {index} selected.");
                int windowHeight = Mathf.RoundToInt(Screen.currentResolution.height * 0.7f);
                int windowWidth = Mathf.RoundToInt(windowHeight * 16f / 9f);
                Screen.SetResolution(windowWidth, windowHeight, false);
                break;
            default:
                break;
        }
    }

    void Update()
    {
        base.Update();

        m_elapsedTime += Time.deltaTime;
        if (m_elapsedTime > 10f)
        {
            m_elapsedTime = 0;
        }
    }
}