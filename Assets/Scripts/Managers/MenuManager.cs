using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;
    public static event Action<MenuState> OnMenuStateChanged;
    [SerializeField] private GameObject m_StartMenuView;
    [SerializeField] private GameObject m_MissionSelectView;
    public MenuState m_menuState;

    public enum MenuState
    {
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
        
        switch(m_menuState)
        {
            case MenuState.StartMenu:
                break;
            case MenuState.MissionSelect:
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
    
    
    
}
