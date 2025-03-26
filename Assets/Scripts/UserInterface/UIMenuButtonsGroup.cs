using UnityEngine;
using UnityEngine.UI;

public class UIMenuButtonsGroup : MonoBehaviour
{
    [SerializeField] private Button m_menuButton;
    [SerializeField] private Button m_discordButton;
    [SerializeField] private Button m_unlockAllButton;
    [SerializeField] private Button m_resetAllButton;
    
    private string discordInviteUrl = "https://discord.gg/PABndFnjMM";

    void Awake()
    {
        m_menuButton.onClick.AddListener(OnMenuButtonClicked);
        m_discordButton.onClick.AddListener(OnDiscordButtonClicked);
        m_unlockAllButton.onClick.AddListener(OnUnlockAllButtonClicked);
        m_resetAllButton.onClick.AddListener(OnResetAllButtonClicked);
    }
    
    public void OnResetAllButtonClicked()
    {
        PlayerDataManager.Instance.ResetPlayerData();
        MissionTableController.Instance.RequestTableReset();
    }
    
    public void OnUnlockAllButtonClicked()
    {
        PlayerDataManager.Instance.CheatPlayerData();
        MissionTableController.Instance.RequestTableReset();
    }
    
    public void OnMenuButtonClicked()
    {
        UIPopupManager.Instance.ShowPopup<UIOptionsPopup>("OptionsPopup");
    }
    
    public void OnDiscordButtonClicked()
    {
        Application.OpenURL(discordInviteUrl);
    }
}
