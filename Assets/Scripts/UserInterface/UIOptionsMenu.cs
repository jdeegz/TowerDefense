using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIOptionsMenu : MonoBehaviour
{
    [SerializeField] private AudioClip m_volumeSliderAudioClip;
    [SerializeField] private GameObject m_cheatsGroup;
    [SerializeField] private AudioMixer m_audioMixer;

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
    [SerializeField] private Button m_closeMenuButton;
    [SerializeField] private TMP_Dropdown m_screenModeDropdown;
    [SerializeField] private UIStringData m_uiStrings;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI m_missionNameLabel;
    
    private int m_dropdownIndex;
    private AudioSource m_audioSource;
    private CanvasGroup m_canvasGroup;
    private float m_elapsedTime;
    private ProgressionCheats m_progressionCheats;

    public event Action<bool> OnMenuToggle; // Used to let CombatView if it should disable hotkeys.


    void Awake()
    {
        GameSettings.AudioMixer = m_audioMixer;
        m_audioSource = GetComponent<AudioSource>();
        m_canvasGroup = GetComponent<CanvasGroup>();
        m_canvasGroup.alpha = 0;
        m_canvasGroup.blocksRaycasts = false;
        m_elapsedTime = 0;
        m_cheatsGroup.SetActive(false);
        m_progressionCheats = m_cheatsGroup.GetComponent<ProgressionCheats>();
        
        if (GameManager.Instance != null && GameManager.Instance.m_curMission != null)
        {
            m_missionNameLabel.SetText($"{GameManager.Instance.m_curMission.m_missionName}");
        }
    }

    void OnDestroy()
    {
    }

    void Start()
    {
        m_surrenderButton.onClick.AddListener(OnSurrenderButtonClicked);
        m_restartButton.onClick.AddListener(OnRestartButtonClicked);
        m_exitApplicationButton.onClick.AddListener(OnExitApplicationButtonClicked);
        m_closeMenuButton.onClick.AddListener(OnCloseMenuButtonClicked);
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

        //Set the dropdown to show the correct option on start.
        m_dropdownIndex = 0;
        if (!Screen.fullScreen)
        {
            m_dropdownIndex = 1;
        }

        m_screenModeDropdown.value = m_dropdownIndex;

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
        Debug.Log($"playing audio clip: {clip.name}");
        m_audioSource.PlayOneShot(clip);
    }

    private void OnSurrenderButtonClicked()
    {
        Debug.Log("Surrendering Mission.");
        int wave = 0;

        // Are we surrendering from an endless match, or normal?
        if (GameplayManager.Instance.IsEndlessModeActive())
        {
            wave = GameplayManager.Instance.m_wave;
        }

        PlayerDataManager.Instance.UpdateMissionSaveData(gameObject.scene.name, 1, wave);
        GameManager.Instance.RequestChangeScene("Menus", GameManager.GameState.Menus);
    }

    private void OnRestartButtonClicked()
    {
        Debug.Log("Restarting Mission.");
        PlayerDataManager.Instance.UpdateMissionSaveData(gameObject.scene.name, 1, 0);
        GameManager.Instance.RequestSceneRestart();
    }

    private void OnExitApplicationButtonClicked()
    {
        Debug.Log("Quitting Application.");
        PlayerDataManager.Instance.UpdateMissionSaveData(gameObject.scene.name, 1, 0);
        Application.Quit();
    }

    public void OnCloseMenuButtonClicked()
    {
        //Close the menu
        ToggleMenu();
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }

        m_elapsedTime += Time.deltaTime;
    }

    public void ToggleMenu()
    {
        if (GameplayManager.Instance && GameplayManager.Instance.IsWatchingCutscene()) return;

        m_canvasGroup.alpha = m_canvasGroup.alpha == 0 ? 1 : 0;
        m_canvasGroup.blocksRaycasts = !m_canvasGroup.blocksRaycasts;

        //If we're in gameplay, pause the game while the menu is opened.
        if (GameplayManager.Instance == null) return;

        if (m_canvasGroup.blocksRaycasts)
        {
            //Debug.Log($"Trying to pause game.");
            OnMenuToggle?.Invoke(true);
            m_progressionCheats.UpdateState();
            GameplayManager.Instance.UpdateGamePlayback(GameplayManager.GameSpeed.Paused);
            GameplayManager.Instance.UpdateInteractionState(GameplayManager.InteractionState.Disabled);
        }
        else
        {
            //Debug.Log($"Trying to resume game.");
            OnMenuToggle?.Invoke(false);
            GameplayManager.Instance.UpdateGamePlayback(GameplayManager.GameSpeed.Normal);
            GameplayManager.Instance.UpdateInteractionState(GameplayManager.InteractionState.Idle);
        }
    }
}