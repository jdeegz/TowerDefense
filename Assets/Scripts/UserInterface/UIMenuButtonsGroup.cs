using UnityEngine;
using UnityEngine.UI;

public class UIMenuButtonsGroup : MonoBehaviour
{
    [SerializeField] private Button m_menuButton;
    [SerializeField] private Button m_discordButton;
    [SerializeField] private Button m_unlockAllButton;
    [SerializeField] private Button m_resetAllButton;
    [SerializeField] private Button m_trainingButton;
    [SerializeField] private MissionData m_trainingZoneMissionData;
    
    private string discordInviteUrl = "https://discord.gg/PABndFnjMM";

    void Awake()
    {
        m_menuButton.onClick.AddListener(OnMenuButtonClicked);
        m_discordButton.onClick.AddListener(OnDiscordButtonClicked);
        m_unlockAllButton.onClick.AddListener(OnUnlockAllButtonClicked);
        m_resetAllButton.onClick.AddListener(OnResetAllButtonClicked);
        m_trainingButton.onClick.AddListener(OnTrainingButtonClicked);
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
    
    public void OnTrainingButtonClicked()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.RequestChangeScene(m_trainingZoneMissionData.m_missionScene, GameManager.GameState.Gameplay);
        GameManager.Instance.m_curMission = m_trainingZoneMissionData;
    }
}
