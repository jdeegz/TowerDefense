using System.Collections;
using System.Collections.Generic;
using PlayFab;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;
using Sequence = DG.Tweening.Sequence;

public class UIVictoryView : MonoBehaviour
{
    [SerializeField] private Button m_exitButton;
    [SerializeField] private Button m_endlessModeButton;
    [SerializeField] private Button m_retryButton;

    public TextMeshProUGUI m_resultLabel;
    public TextMeshProUGUI m_endlessHighScoreLabel;
    public List<TierScoreDisplayObjects> m_scoreDisplayObjects;
    public Color m_positiveColor;
    public Color m_negativeColor;
    public Color m_neutralColor;
    public UIStringData m_uiStrings;

    private CanvasGroup m_canvasGroup;
    private int m_victoriousWave;

    void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
        m_canvasGroup.alpha = 0;
        m_canvasGroup.blocksRaycasts = false;
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState state)
    {
        if (state == GameplayManager.GameplayState.Victory || state == GameplayManager.GameplayState.Defeat)
        {
            m_canvasGroup.alpha = 1;
            m_canvasGroup.blocksRaycasts = true;
            m_endlessHighScoreLabel.gameObject.SetActive(false);
        }
        else
        {
            m_canvasGroup.alpha = 0;
            m_canvasGroup.blocksRaycasts = false;
        }

        if (state == GameplayManager.GameplayState.Victory)
        {
            m_resultLabel.SetText(m_uiStrings.m_victory);
            m_endlessModeButton.gameObject.SetActive(!GameplayManager.Instance.IsEndlessModeActive() && GameplayManager.Instance.m_gameplayData.m_allowEndlessMode);
            SetData();
        }

        if (state == GameplayManager.GameplayState.Defeat)
        {
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

            SetData();
        }
    }

    void SetData()
    {
        (List<ScoreResultsPerWaveTier>, int) data = ScoreManager.Instance.GetScore();
        List<ScoreResultsPerWaveTier> scoreTiers = data.Item1;
        int currentScoreTotal;
        Sequence tallyScoreSequence = DOTween.Sequence();
        float counterDuration = 1f;
        float counterDelay = .5f;

        //Total Score
        int index = m_scoreDisplayObjects.Count - 1;
        m_scoreDisplayObjects[index].m_titleLabel.SetText(m_uiStrings.m_totalScore);
        m_scoreDisplayObjects[index].m_valueLabel.SetText(0.ToString());

        //Obelisk Objs.
        m_scoreDisplayObjects[0].m_titleLabel.SetText(m_uiStrings.m_scoreObelisk);
        m_scoreDisplayObjects[0].m_valueLabel.SetText(0.ToString());

        currentScoreTotal = data.Item2;
        Tween obeliskScoreTween = m_scoreDisplayObjects[0].m_valueLabel.DOCounter(0, data.Item2, counterDuration).SetAutoKill(true);
        Tween addobeliskScoreToTotal = m_scoreDisplayObjects[index].m_valueLabel.DOCounter(0, currentScoreTotal, counterDuration).SetAutoKill(true);

        tallyScoreSequence.Append(obeliskScoreTween);
        tallyScoreSequence.Join(addobeliskScoreToTotal);

        tallyScoreSequence.AppendInterval(counterDelay);

        //Wave Penalties
        for (int i = 0; i < scoreTiers.Count; ++i)
        {
            //If first, min is 0, else is previous wave + 1.
            int min = i == 0 ? 1 : scoreTiers[i - 1].m_tierWaveBreakpoint + 1;
            int max = scoreTiers[i].m_tierWaveBreakpoint;
            m_scoreDisplayObjects[i + 1].m_titleLabel.SetText(string.Format(m_uiStrings.m_scoreWaves, min, max, scoreTiers[i].m_tierScorePerWave, scoreTiers[i].m_tierWaveCount));

            //If we're the last tier, pick new formatting for the string.
            if (i == scoreTiers.Count - 1)
            {
                m_scoreDisplayObjects[i + 1].m_titleLabel.SetText(string.Format(m_uiStrings.m_scoreLastTierWaves, min, scoreTiers[i].m_tierScorePerWave, scoreTiers[i].m_tierWaveCount));
            }

            m_scoreDisplayObjects[i + 1].m_valueLabel.SetText(0.ToString());

            int newTotalScore = currentScoreTotal - scoreTiers[i].m_tierScore;
            Tween wavePenaltyTween = m_scoreDisplayObjects[i + 1].m_valueLabel.DOCounter(0, -scoreTiers[i].m_tierScore, counterDuration).SetAutoKill(true);
            Tween addWavePenaltyToTotal = m_scoreDisplayObjects[index].m_valueLabel.DOCounter(currentScoreTotal, newTotalScore, counterDuration).SetAutoKill(true);
            currentScoreTotal = newTotalScore;

            tallyScoreSequence.Append(wavePenaltyTween);
            tallyScoreSequence.Join(addWavePenaltyToTotal);

            tallyScoreSequence.AppendInterval(counterDelay);
        }

        tallyScoreSequence.Play().SetUpdate(true);
        if (PlayFabManager.Instance)
        {
            SendScore(currentScoreTotal);
        }
    }

    void SendScore(int score)
    {
        //If we have playfab manager and a logged in profile, send score.
        if (PlayFabClientAPI.IsClientLoggedIn() && !string.IsNullOrEmpty(GameManager.Instance.m_curMission.m_playFableaderboardId))
        {
            Debug.Log($"Sending score as {GameManager.Instance.m_curMission.m_playFableaderboardId}.");
            PlayFabManager.Instance.SendLeaderboard(GameManager.Instance.m_curMission.m_playFableaderboardId, score);
        }
        else
        {
            Debug.Log($"Could not send score, Not logged in.");
        }
    }

    void Start()
    {
        m_exitButton.onClick.AddListener(OnExitButtonClicked);
        m_endlessModeButton.onClick.AddListener(OnEndlessModeButtonClicked);
        m_retryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    private void OnEndlessModeButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            Debug.Log("Endless Mode Activated.");
            GameplayManager.Instance.StartEndlessMode();
        }
    }
    
    
    private void OnRetryButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            Debug.Log("Restarting Mission.");
            PlayerDataManager.Instance.UpdateMissionSaveData(1, m_victoriousWave);
            GameManager.Instance.RequestSceneRestart();
        }
    }

    private void OnExitButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestChangeScene("Menus", GameManager.GameState.Menus);
        }
    }
}

[System.Serializable]
public class TierScoreDisplayObjects
{
    public CanvasGroup m_canvasGroup;
    public TextMeshProUGUI m_titleLabel;
    public TextMeshProUGUI m_valueLabel;
}