using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine.Serialization;
using Sequence = DG.Tweening.Sequence;

public class UIMissionCompletePopup : UIPopup
{
    [Header("EOG Button Objects")]
    [SerializeField] private Button m_exitButton;
    [SerializeField] private Button m_endlessModeButton;
    [SerializeField] private Button m_retryButton;
    
    [Header("Victory / Defeat Canvas Groups")]
    [SerializeField] private CanvasGroup m_victoryCanvasGroup;
    [SerializeField] private CanvasGroup m_defeatCanvasGroup;
    
    [Header("Result Labels")]
    [SerializeField] private  TextMeshProUGUI m_victoryResultLabel;
    [SerializeField] private  TextMeshProUGUI m_defeatResultLabel;
    [SerializeField] private  TextMeshProUGUI m_endlessHighScoreLabel;
    
    [Header("String Data")]
    [SerializeField] private  UIStringData m_uiStrings;

    private int m_victoriousWave;

    void Awake()
    {
        base.Awake();

        m_exitButton.onClick.AddListener(OnExitButtonClicked);
        m_endlessModeButton.onClick.AddListener(OnEndlessModeButtonClicked);
        m_retryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    public override void HandleShow()
    {
        base.HandleShow();

        m_endlessHighScoreLabel.gameObject.SetActive(false);
        m_defeatCanvasGroup.alpha = 0;
        m_victoryCanvasGroup.alpha = 0;
        
        GameplayManager.GameplayState curState = GameplayManager.Instance.m_gameplayState;

        Debug.Log($"Current State: {curState}");
        WaveReportCard.Instance.MoveDisplayTo(transform);
        if (curState == GameplayManager.GameplayState.Victory)
        {
            SetupVictory();
            return;
        }

        if (curState == GameplayManager.GameplayState.Defeat)
        {
            SetupDefeat();
            return;
        }
        
    }

    void SetupVictory()
    {
        Debug.Log($"Setup Victory UI");
        m_victoryCanvasGroup.alpha = 1;
        m_victoryResultLabel.SetText(m_uiStrings.m_victory);
        m_endlessModeButton.gameObject.SetActive(!GameplayManager.Instance.IsEndlessModeActive() && GameplayManager.Instance.m_gameplayData.m_allowEndlessMode);

        if (GameplayManager.Instance.IsEndlessModeActive() && GameplayManager.Instance.m_gameplayData.m_allowEndlessMode) // We lost in endless mode, we want to assign the wave we lost on to see if it's a high score.
        {
            ConfigureEndlessHighScoreLabel();
        }
    }

    void SetupDefeat()
    {
        m_defeatCanvasGroup.alpha = 1;
        Debug.Log($"Setup Defeat UI");
        m_defeatResultLabel.SetText(m_uiStrings.m_defeat);
        m_endlessModeButton.gameObject.SetActive(false);

        if (GameplayManager.Instance.IsEndlessModeActive()) // We lost in endless mode, we want to assign the wave we lost on to see if it's a high score.
        {
            ConfigureEndlessHighScoreLabel();
        }
    }

    void ConfigureEndlessHighScoreLabel()
    {
        m_victoriousWave = GameplayManager.Instance.Wave;
        int curHighScore = GameplayManager.Instance.GetCurrentMissionSaveData().m_waveHighScore;

        string endlessHighScorestring;

        if (m_victoriousWave > curHighScore)
        {
            // New High Score!
            endlessHighScorestring = string.Format(m_uiStrings.m_newEndlessHighScore, m_victoriousWave);
        }
        else
        {
            endlessHighScorestring = string.Format(m_uiStrings.m_currentEndlessHighScore, curHighScore);
        }

        m_endlessHighScoreLabel.SetText(endlessHighScorestring);
        m_endlessHighScoreLabel.gameObject.SetActive(true);
    }

    private void OnEndlessModeButtonClicked()
    {
        WaveReportCard.Instance.ReturnDisplayTo();
        
        UIPopupManager.Instance.ClosePopup(this);

        //Debug.Log("Endless Mode Activated.");
        GameplayManager.Instance.StartEndlessMode();
    }

    private void OnRetryButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            Debug.Log("Restarting Mission.");
            GameManager.Instance.RequestSceneRestart();
        }
    }

    private void OnExitButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestChangeScene("Menus", GameManager.GameState.Menus, MenuManager.MenuState.MissionSelect);
        }
    }
}