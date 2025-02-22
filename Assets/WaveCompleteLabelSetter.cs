using TMPro;
using UnityEngine;

public class WaveCompleteLabelSetter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_descriptionLabel;
    [SerializeField] private UIStringData m_uiStrings;

    void OnEnable()
    {
        //Was this a boss wave?
        int wave = GameplayManager.Instance.Wave;
        
        Debug.Log($"WaveCompeteLabelSetter: Enabled. Current Wave is {wave}");

        if (GameplayManager.Instance.m_bossWaves.Contains(wave))
        {
            SetBossWaveText();
            Debug.Log($"WaveCompleteLabelSetter: This was a boss wave.");
        }
        else
        {
            SetNormalWaveText();
        }
    }

    void SetNormalWaveText()
    {
        string text;
        if (GameplayManager.Instance.IsEndlessModeActive())
        {
            text = m_uiStrings.m_waveCompletedEndless;
        }
        else
        {
            text = m_uiStrings.m_waveCompleted;
        }

        m_descriptionLabel.SetText(text);
    }

    void SetBossWaveText()
    {
        string text;
        if (GameplayManager.Instance.IsEndlessModeActive())
        {
            text = m_uiStrings.m_waveCompletedBossDamage;
        }
        else
        {
            text = m_uiStrings.m_waveCompletedBossWave;
        }

        m_descriptionLabel.SetText(text);
    }
}