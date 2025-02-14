using System.Collections;
using System.Collections.Generic;
using PlayFab;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;
using Sequence = DG.Tweening.Sequence;

public class UIMissionCompletePopup : UIPopup
{
    [SerializeField] private Button m_exitButton;
    [SerializeField] private Button m_endlessModeButton;
    [SerializeField] private Button m_retryButton;

    public TextMeshProUGUI m_resultLabel;
    public TextMeshProUGUI m_endlessHighScoreLabel;
    public UIStringData m_uiStrings;

    private int m_victoriousWave;

    void Awake()
    {
        base.Awake();

        m_exitButton.onClick.AddListener(OnExitButtonClicked);
        m_endlessModeButton.onClick.AddListener(OnEndlessModeButtonClicked);
        m_retryButton.onClick.AddListener(OnRetryButtonClicked);
        m_endlessHighScoreLabel.gameObject.SetActive(false);
    }

    public override void HandleShow()
    {
        base.HandleShow();

        GameplayManager.GameplayState curState = GameplayManager.Instance.m_gameplayState;

        Debug.Log($"Current State: {curState}");
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
        m_resultLabel.SetText(m_uiStrings.m_victory);
        m_endlessModeButton.gameObject.SetActive(!GameplayManager.Instance.IsEndlessModeActive() && GameplayManager.Instance.m_gameplayData.m_allowEndlessMode);
    }

    void SetupDefeat()
    {
        Debug.Log($"Setup Defeat UI");
        m_resultLabel.SetText(m_uiStrings.m_defeat);
        m_endlessModeButton.gameObject.SetActive(false);

        if (GameplayManager.Instance.IsEndlessModeActive()) // We lost in endless mode, we want to assign the wave we lost on to see if it's a high score.
        {
            m_victoriousWave = GameplayManager.Instance.m_wave;
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
    }

    private void OnEndlessModeButtonClicked()
    {
        UIPopupManager.Instance.ClosePopup(this);

        //Debug.Log("Endless Mode Activated.");
        GameplayManager.Instance.StartEndlessMode();
    }

    private void OnRetryButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            Debug.Log("Restarting Mission.");
            PlayerDataManager.Instance.UpdateMissionSaveData(gameObject.scene.name, 1, m_victoriousWave);
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