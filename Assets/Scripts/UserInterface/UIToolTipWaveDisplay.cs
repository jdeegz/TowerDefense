using UnityEngine;
using UnityEngine.EventSystems;

public class UIToolTipWaveDisplay : UITooltip
{
    [SerializeField] private UIStringData m_uiStrings;
    public override void OnPointerEnter(PointerEventData eventData)
    {
        //Reformat the Description String.
        //Display Current Wave?
        
        //Display highest Wave Every if there is one.
        //High Score: 10 -- We havent past it yet.
        //NEW High Score: 11 -- We've passed the high score.
        string endlessHighScorestring;

        int curWave = GameplayManager.Instance.Wave;
        int waveHighScore = GameplayManager.Instance.GetCurrentMissionSaveData().m_waveHighScore;

        if (curWave > waveHighScore)
        {
            // New High Score!
            endlessHighScorestring = string.Format(m_uiStrings.m_tooltipNewEndlessHighScore, curWave);
        }
        else
        {
            endlessHighScorestring = string.Format(m_uiStrings.m_tooltipCurrentEndlessHighScore, waveHighScore);
        }
        
        //Display highest Perfect Wave count.
        string perfectWaveHighScoreString;
        int curPerfectWaveCount = GameplayManager.Instance.m_perfectWavesCompleted;
        int perfectWaveHighScore = GameplayManager.Instance.GetCurrentMissionSaveData().m_perfectWaveScore;

        Debug.Log($"perfect wave high score: {perfectWaveHighScore}");
        
        if (perfectWaveHighScore > 0) // If we do NOT have a saved high score, we only care about the current score.
        {
            if (curPerfectWaveCount > perfectWaveHighScore)
            {
                // New High Score!
                //NEW BEST Perfect Score: 100
                perfectWaveHighScoreString = string.Format(m_uiStrings.m_tooltipNewPerfectHighScore, curPerfectWaveCount);
            }
            else
            {
                //Perfect Wave High Score: 10
                //Perfect Waves: 9
                string currentPerfectHighScore = string.Format(m_uiStrings.m_tooltipCurrentPerfectHighScore, perfectWaveHighScore);
                string currentPerfectScore = string.Format(m_uiStrings.m_tooltipCurrentPerfectScore, curPerfectWaveCount);
                perfectWaveHighScoreString = $"{currentPerfectHighScore}<br>{currentPerfectScore}";
            }
        }
        else
        {
            perfectWaveHighScoreString = string.Format(m_uiStrings.m_tooltipCurrentPerfectScore, curPerfectWaveCount);
        }
        
        //Display current Perfect Wave count.
        m_descriptionString = $"{endlessHighScorestring}<br><br>{perfectWaveHighScoreString}";
        
        base.OnPointerEnter(eventData);
    }
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
