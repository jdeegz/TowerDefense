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
    [SerializeField] private GameObject m_menuRoot;
    [SerializeField] private AudioClip m_volumeSliderAudioClip;
    [SerializeField] private AudioMixer m_audioMixer;

    [Header("Volume - Master")]
    [SerializeField] private Slider m_volumeMasterSlider;

    [SerializeField] private TextMeshProUGUI m_volumeMasterLabel;
    [SerializeField] private string m_volumeMasterString; //Exposed Parameter in the Audio Mixer.

    [Header("Volume - Music")]
    [SerializeField] private Slider m_volumeMusicSlider;

    [SerializeField] private TextMeshProUGUI m_volumeMusicLabel;
    [SerializeField] private string m_volumeMusicString; //Exposed Parameter in the Audio Mixer.

    [Header("Volume - Sound Effects")]
    [SerializeField] private Slider m_volumeSFXSlider;

    [SerializeField] private TextMeshProUGUI m_volumeSFXLabel;
    [SerializeField] private string m_volumeSFXString; //Exposed Parameter in the Audio Mixer.

    [Header("Buttons")] [SerializeField] private Button m_surrenderButton;
    [SerializeField] private Button m_restartButton;
    [SerializeField] private Button m_exitApplicationButton;
    [SerializeField] private Button m_closeMenuButton;
    [SerializeField] private Button m_logoutButton;
    [SerializeField] private TMP_Dropdown m_screenModeDropdown;
    [SerializeField] private UIStringData m_uiStrings;
    
    private int m_windowModeWidth = 1920;
    private int m_windowModeHeight = 1080;
    private int m_dropdownIndex;
    private AudioSource m_audioSource;
    private CanvasGroup m_canvasGroup;
    private float m_elapsedTime;

    void Awake()
    {
        m_audioSource = GetComponent<AudioSource>();
        m_canvasGroup = GetComponent<CanvasGroup>();
        m_canvasGroup.alpha = 0;
        m_canvasGroup.blocksRaycasts = false;
        m_elapsedTime = 0;
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
        m_logoutButton.onClick.AddListener(OnLogoutButtonClicked);

        //Get volumes
        float masterVol = PlayerPrefs.GetFloat(m_volumeMasterString, 0.5f);
        m_volumeMasterSlider.value = masterVol;
        TryChangeMasterVolume(masterVol);

        float musicVol = PlayerPrefs.GetFloat(m_volumeMusicString, 0.3f);
        m_volumeMusicSlider.value = musicVol;
        TryChangeMusicVolume(musicVol);

        float sfxVol = PlayerPrefs.GetFloat(m_volumeSFXString, 0.5f);
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
            }

            if (GameManager.Instance.m_gameState != GameManager.GameState.Menus)
            {
                m_logoutButton.gameObject.SetActive(false);
            }
        }

        //Set the dropdown to show the correct option on start.
        m_dropdownIndex = 0;
        if (!Screen.fullScreen)
        {
            m_dropdownIndex = 1;
        }

        m_screenModeDropdown.value = m_dropdownIndex;
        
#if UNITY_WEBGL
        //Disable buttons for web build.
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // Disable the feature for web platform
            m_screenModeDropdown.gameObject.SetActive(false);
            m_exitApplicationButton.gameObject.SetActive(false);
        }
#endif
    }

    void TryChangeMasterVolume(float value)
    {
        ChangeVolume(m_volumeMasterString, value, m_volumeMasterLabel, m_uiStrings.m_volumeMasterText);
    }

    void TryChangeMusicVolume(float value)
    {
        ChangeVolume(m_volumeMusicString, value, m_volumeMusicLabel, m_uiStrings.m_volumeMusicText);
    }

    void TryChangeSFXVolume(float value)
    {
        ChangeVolume(m_volumeSFXString, value, m_volumeSFXLabel, m_uiStrings.m_volumeSFXText);
    }

    void ChangeVolume(string key, float volume, TextMeshProUGUI label, string text)
    {
        // Store the current volume setting in PlayerPrefs with the dynamically generated key
        PlayerPrefs.SetFloat(key, volume);
        m_audioMixer.SetFloat(key, Mathf.Log10(volume) * 20);
        label.SetText(string.Format(text, (volume * 100).ToString("F0")));
        if (m_elapsedTime > 0.1f)
        {
            PlayAudio(m_volumeSliderAudioClip);
            m_elapsedTime = 0;
        }
    }

    void PlayAudio(AudioClip clip)
    {
        m_audioSource.PlayOneShot(clip);
    }

    private void OnSurrenderButtonClicked()
    {
        Debug.Log("Surrendering Mission.");
        PlayerDataManager.Instance.UpdateMissionSaveData(1);
        GameManager.Instance.RequestChangeScene("Menus", GameManager.GameState.Menus);
    }

    private void OnRestartButtonClicked()
    {
        Debug.Log("Restarting Mission.");
        PlayerDataManager.Instance.UpdateMissionSaveData(1);
        GameManager.Instance.RequestSceneRestart();
    }

    private void OnExitApplicationButtonClicked()
    {
        Debug.Log("Quitting Application.");
        PlayerDataManager.Instance.UpdateMissionSaveData(1);
        Application.Quit();
    }

    private void OnCloseMenuButtonClicked()
    {
        //Close the menu
        ToggleMenu();
    }

    void OnLogoutButtonClicked()
    {
        PlayFabManager.Instance.RequestLogout();
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
                Screen.SetResolution(m_windowModeWidth, m_windowModeHeight, false);
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
        m_canvasGroup.alpha = m_canvasGroup.alpha == 0 ? 1 : 0;
        m_canvasGroup.blocksRaycasts = !m_canvasGroup.blocksRaycasts;

        //If we're in gameplay, pause the game while the menu it opened.
        if (GameplayManager.Instance == null) return;

        if (m_canvasGroup.blocksRaycasts)
        {
            Debug.Log($"Trying to pause game.");
            GameplayManager.Instance.UpdateGamePlayback(GameplayManager.GameSpeed.Paused);
        }
        else
        {
            Debug.Log($"Trying to resume game.");
            GameplayManager.Instance.UpdateGamePlayback(GameplayManager.GameSpeed.Normal);
        }
    }
}