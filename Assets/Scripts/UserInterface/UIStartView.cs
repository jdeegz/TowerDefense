using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIStartView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button m_MissionSelectionButton;
    [SerializeField] private Button m_menuButton;
    
    [Header("Scene References")]
    [SerializeField] private UIOptionsMenu m_menuObj;
    
    void Awake()
    {
    }
    
    void Start()
    {
        m_MissionSelectionButton.onClick.AddListener(OnMissionSelectionButtonClicked);
        m_menuButton.onClick.AddListener(OnMenuButtonClicked);
    }

    private void OnMenuButtonClicked()
    {
        m_menuObj.ToggleMenu();
    }

    private void OnMissionSelectionButtonClicked()
    {
        MenuManager.Instance.UpdateMenuState(MenuManager.MenuState.MissionSelect);
    }
}