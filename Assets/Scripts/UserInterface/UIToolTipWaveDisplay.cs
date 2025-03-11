using UnityEngine;
using UnityEngine.EventSystems;

public class UIToolTipWaveDisplay : UITooltip
{
    public override void OnPointerEnter(PointerEventData eventData)
    {
        //Reformat the Description String.
        //Display Current Wave?
        
        //Display highest Wave Every if there is one.
        //High Score: 10 -- We havent past it yet.
        //NEW High Score: 11 -- We've passed the high score.
        string endlessHighScorestring;

        int curWave = GameplayManager.Instance.Wave;
        
        MissionSaveData missionSaveData = GameplayManager.Instance.GetCurrentMissionSaveData();

        if (missionSaveData == null) return;
        
        int waveHighScore = missionSaveData.m_waveHighScore;

        if (waveHighScore > 0)
        {
            if (curWave > waveHighScore)
            {
                // New High Score!
                endlessHighScorestring = string.Format(LocalizationManager.Instance.CurrentLanguage.m_tooltipNewEndlessHighScore, curWave);
            }
            else
            {
                string currentEndlessHighScore = string.Format(LocalizationManager.Instance.CurrentLanguage.m_tooltipCurrentEndlessHighScore, waveHighScore);
                string currentEndlessScore = string.Format(LocalizationManager.Instance.CurrentLanguage.m_tooltipCurrentEndlessScore, curWave);
                endlessHighScorestring = $"{currentEndlessHighScore}<br>{currentEndlessScore}";
            }
        }
        else
        {
            endlessHighScorestring = string.Format(LocalizationManager.Instance.CurrentLanguage.m_tooltipCurrentEndlessScore, curWave);
        }

        //Display highest Perfect Wave count.
        string perfectWaveHighScoreString;
        int curPerfectWaveCount = GameplayManager.Instance.m_perfectWavesCompleted;
        int perfectWaveHighScore = GameplayManager.Instance.GetCurrentMissionSaveData().m_perfectWaveScore;
        
        if (perfectWaveHighScore > 0) // If we do NOT have a saved high score, we only care about the current score.
        {
            if (curPerfectWaveCount > perfectWaveHighScore)
            {
                // New High Score!
                //NEW BEST Perfect Score: 100
                perfectWaveHighScoreString = string.Format(LocalizationManager.Instance.CurrentLanguage.m_tooltipNewPerfectHighScore, curPerfectWaveCount);
            }
            else
            {
                //Perfect Wave High Score: 10
                //Perfect Waves: 9
                string currentPerfectHighScore = string.Format(LocalizationManager.Instance.CurrentLanguage.m_tooltipCurrentPerfectHighScore, perfectWaveHighScore);
                string currentPerfectScore = string.Format(LocalizationManager.Instance.CurrentLanguage.m_tooltipCurrentPerfectScore, curPerfectWaveCount);
                perfectWaveHighScoreString = $"{currentPerfectHighScore}<br>{currentPerfectScore}";
            }
        }
        else
        {
            perfectWaveHighScoreString = string.Format(LocalizationManager.Instance.CurrentLanguage.m_tooltipCurrentPerfectScore, curPerfectWaveCount);
        }
        
        //Display current Perfect Wave count.
        m_descriptionString = $"{endlessHighScorestring}<br><br>{perfectWaveHighScoreString}";
        
        //Calculate hitpoint multiplier. Display as "Minute: 10<br>Enemy Health: x24"
        float health = GameplayManager.Instance.m_gameplayData.CalculateHealth(10);
        string formattedhealthMultiplier = (health / 10).ToString("N0");
        string multiplierString = string.Format(LocalizationManager.Instance.CurrentLanguage.m_healthMultiplierToolTip, formattedhealthMultiplier);

        int minute = GameplayManager.Instance.Minute;
        string minuteString = string.Format(LocalizationManager.Instance.CurrentLanguage.m_missionMinuteToolTip, minute);
        
        m_detailsString = $"{minuteString}<br>{multiplierString}";
        base.OnPointerEnter(eventData);
    }
}
