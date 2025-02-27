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
    [SerializeField] private Button m_discordButton;
    
    private string discordInviteUrl = "https://discord.gg/PABndFnjMM";
    
    void Start()
    {
        m_MissionSelectionButton.onClick.AddListener(OnMissionSelectionButtonClicked);
        m_menuButton.onClick.AddListener(OnMenuButtonClicked);
        m_discordButton.onClick.AddListener(OnDiscordButtonClicked);
    }

    private void OnDiscordButtonClicked()
    {
        Application.OpenURL(discordInviteUrl);
    }

    private void OnMenuButtonClicked()
    {
        UIPopupManager.Instance.ShowPopup<UIOptionsPopup>("OptionsPopup");
    }

    private void OnMissionSelectionButtonClicked()
    {
        MenuManager.Instance.UpdateMenuState(MenuManager.MenuState.MissionSelect);
    }
}