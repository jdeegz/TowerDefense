using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICombatView : MonoBehaviour
{
    [SerializeField] private Button m_pauseButton;
    [SerializeField] private Button m_playButton;
    [SerializeField] private Button m_victoryButton;
    [SerializeField] private Button m_defeatButton;
    [SerializeField] private Button m_exitButton;
    [SerializeField] private TextMeshProUGUI m_stoneBankLabel;
    [SerializeField] private TextMeshProUGUI m_woodBankLabel;
    [SerializeField] private TextMeshProUGUI m_stoneGathererLabel;
    [SerializeField] private TextMeshProUGUI m_woodGathererLabel;


    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        ResourceManager.UpdateStoneBank += UpdateStoneDisplay;
        ResourceManager.UpdateWoodBank += UpdateWoodDisplay;
        ResourceManager.UpdateStoneGathererCount += UpdateStoneGathererDisplay;
        ResourceManager.UpdateWoodGathererCount += UpdateWoodGathererDisplay;
        m_woodGathererLabel.SetText("0");
        m_stoneGathererLabel.SetText("0");
        m_woodBankLabel.SetText("0");
        m_stoneBankLabel.SetText("0");
    }

    private void UpdateWoodGathererDisplay(int i)
    {
        int newCount = i + ResourceManager.Instance.GetWoodGathererAmount();
        m_woodGathererLabel.SetText(newCount.ToString());
    }

    private void UpdateStoneGathererDisplay(int i)
    {
        int newCount = i + ResourceManager.Instance.GetStoneGathererAmount();
        m_stoneGathererLabel.SetText(newCount.ToString());
    }

    private void UpdateWoodDisplay(int i)
    {
        int newCount = i + ResourceManager.Instance.GetWoodAmount();
        m_woodBankLabel.SetText(newCount.ToString());
    }

    private void UpdateStoneDisplay(int i)
    {
        int newCount = i + ResourceManager.Instance.GetStoneAmount();
        m_stoneBankLabel.SetText(newCount.ToString());
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState state)
    {
        gameObject.SetActive(state == GameplayManager.GameplayState.Combat ||
                             state == GameplayManager.GameplayState.Paused);
        m_playButton.gameObject.SetActive(state == GameplayManager.GameplayState.Paused);
        m_pauseButton.gameObject.SetActive(state == GameplayManager.GameplayState.Combat);
    }

    // Start is called before the first frame update
    void Start()
    {
        m_pauseButton.onClick.AddListener(OnPauseButtonClicked);
        m_playButton.onClick.AddListener(OnPlayButtonClicked);
        m_victoryButton.onClick.AddListener(OnVictoryButtonClicked);
        m_defeatButton.onClick.AddListener(OnDefeatButtonClick);
        m_exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    private void OnPlayButtonClicked()
    {
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Combat);
    }

    private void OnPauseButtonClicked()
    {
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Paused);
    }

    private void OnExitButtonClicked()
    {
        GameManager.Instance.UpdateGameState(GameManager.GameState.Menus);
    }

    private void OnDefeatButtonClick()
    {
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Defeat);
    }

    private void OnVictoryButtonClicked()
    {
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Victory);
    }

    // Update is called once per frame
    void Update()
    {
    }
}