using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;
    public static event Action<MenuState> OnMenuStateChanged;
    [SerializeField] private GameObject m_StartMenuView;
    [SerializeField] private GameObject m_MissionSelectView;
    [SerializeField] private GameObject m_loginView;
    public MenuState m_menuState;

    public enum MenuState
    {
        Idle,
        StartMenu,
        MissionSelect,
        Login
    }

    void Start()
    {
        UpdateMenuState(MenuState.Idle);

        //If we have no PlayFabManager (running edit mode stuff)
        if (!PlayFabManager.Instance)
        {
            UpdateMenuState(MenuState.StartMenu);
            return;
        }

        //If we have a connection.
        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            UpdateMenuState(MenuState.StartMenu);
            return;
        }

        //Else we ask for log in.
        UpdateMenuState(MenuState.Login);
    }

    public void UpdateMenuState(MenuState newState)
    {
        m_menuState = newState;

        switch (m_menuState)
        {
            case MenuState.Idle:
                break;
            case MenuState.StartMenu:
                break;
            case MenuState.MissionSelect:
                break;
            case MenuState.Login:
                break;
            default:
                break;
        }

        OnMenuStateChanged?.Invoke(newState);
        //Debug.Log("Menu State:" + newState);
    }

    private void MenuManagerOnMenuStateChanged(MenuState state)
    {
        m_StartMenuView.SetActive(state == MenuState.StartMenu);
        m_MissionSelectView.SetActive(state == MenuState.MissionSelect);
        m_loginView.SetActive(state == MenuState.Login);
    }


    void Awake()
    {
        Instance = this;
        OnMenuStateChanged += MenuManagerOnMenuStateChanged;

        if (PlayFabManager.Instance)
        {
            PlayFabManager.Instance.OnLoginComplete += LoginComplete;
            PlayFabManager.Instance.OnLoginRequired += LoginRequired;
            PlayFabManager.Instance.OnNamingRequired += NamingRequired;
        }
    }

    private void LoginRequired()
    {
        UpdateMenuState(MenuState.Login);
    }

    private void LoginComplete()
    {
        UpdateMenuState(MenuState.StartMenu);
    }

    private void NamingRequired()
    {
        UpdateMenuState(MenuState.Login);
    }

    void OnDestroy()
    {
        OnMenuStateChanged -= MenuManagerOnMenuStateChanged;

        if (PlayFabManager.Instance)
        {
            PlayFabManager.Instance.OnLoginComplete -= LoginComplete;
            PlayFabManager.Instance.OnLoginRequired -= LoginRequired;
            PlayFabManager.Instance.OnNamingRequired -= NamingRequired;
        }
    }
}