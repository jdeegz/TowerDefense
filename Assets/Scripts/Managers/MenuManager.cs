using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;
    public static event Action<MenuState> OnMenuStateChanged;
    [SerializeField] private AudioSource m_audioSource;
    public MenuState m_menuState;

    public enum MenuState
    {
        Idle,
        StartMenu,
        MissionSelect,
    }

    void Start()
    {
        UpdateMenuState(MenuState.StartMenu);
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
            default:
                break;
        }

        OnMenuStateChanged?.Invoke(newState);
    }

    private void MenuManagerOnMenuStateChanged(MenuState state)
    {
        /*m_StartMenuView.SetActive(state == MenuState.StartMenu);
        m_MissionSelectView.SetActive(state == MenuState.MissionSelect);*/
    }


    void Awake()
    {
        Instance = this;
        OnMenuStateChanged += MenuManagerOnMenuStateChanged;
    }

    void OnDestroy()
    {
        OnMenuStateChanged -= MenuManagerOnMenuStateChanged;
    }

    public void RequestAudioOneShot(AudioClip audioClip)
    {
        m_audioSource.PlayOneShot(audioClip);
    }
}