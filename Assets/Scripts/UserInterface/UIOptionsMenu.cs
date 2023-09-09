using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class UIOptionsMenu : MonoBehaviour
{
    [SerializeField] private GameObject m_menuRoot;
    [SerializeField] private Button m_surrenderButton;
    [SerializeField] private Button m_restartButton;
    [SerializeField] private Button m_exitApplicationButton;
    [SerializeField] private Button m_closeMenuButton;
    [SerializeField] private TMP_Dropdown m_screenModeDropdown;
    private int m_windowModeWidth = 1600;
    private int m_windowModeHeight = 900;
    private int m_dropdownIndex;
    
    void Awake()
    {
        m_menuRoot.SetActive(false);
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
        m_screenModeDropdown.onValueChanged.AddListener(new UnityAction<int>(OnDropdownValueChanged));

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.m_gameState == GameManager.GameState.Menus)
            {
                m_surrenderButton.gameObject.SetActive(false);
                m_restartButton.gameObject.SetActive(false);
            }
        }

        //Set the dropdown to show the correct option on start.
        m_dropdownIndex = 0;
        if (!Screen.fullScreen)
        {
            m_dropdownIndex = 1;
        }

        m_screenModeDropdown.value = m_dropdownIndex;
    }

    private void OnSurrenderButtonClicked()
    {
        Debug.Log("Surrendering Mission.");
        GameManager.Instance.RequestChangeScene("Menus", GameManager.GameState.Menus);
    }
    private void OnRestartButtonClicked()
    {
        Debug.Log("Restarting Mission.");
        GameManager.Instance.RequestSceneRestart();
    }
    private void OnExitApplicationButtonClicked()
    {
        Debug.Log("Quitting Application.");
        Application.Quit();
    }
    private void OnCloseMenuButtonClicked()
    {
        //Close the menu
        ToggleMenu();
    }

    private void OnDropdownValueChanged(int index)
    {
        switch (index)
        {
            case 0:
                Debug.Log($"Option {index} selected.");
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
                break;
            case 1:
                Debug.Log($"Option {index} selected.");
                Screen.SetResolution(m_windowModeWidth, m_windowModeHeight, false);
                break;
            default:
                break;
        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }    
    }
    
    private void ToggleMenu()
    {
        m_menuRoot.SetActive(!m_menuRoot.activeSelf);
        
        //If we're in gameplay, pause the game while the menu it opened.
        if (GameManager.Instance.m_gameState == GameManager.GameState.Gameplay)
        {
            if (m_menuRoot.activeSelf)
            {
                GameplayManager.Instance.UpdateGameSpeed(GameplayManager.GameSpeed.Paused);
            }
            else
            {
                GameplayManager.Instance.UpdateGameSpeed(GameplayManager.GameSpeed.Normal);
            }
        }
    }
}
