using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public List<ScorePerWaveTier> m_scoreTiers;
    public int m_obeliskChargeScoreValue;

    private int m_totalObeliskScore;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GameplayManager.OnObelisksCharged += UpdateObeliskScore;
    }

    private void UpdateObeliskScore(int chargedObelisks, int obelisksInMission)
    {
        m_totalObeliskScore = chargedObelisks * m_obeliskChargeScoreValue;
    }

    public (List<ScoreResultsPerWaveTier>, int) GetScore()
    {
        //Generate the scores for each tier of waves.
        List<ScoreResultsPerWaveTier> tierScores = new List<ScoreResultsPerWaveTier>();
        for (int i = 0; i < m_scoreTiers.Count; ++i)
        {
            ScoreResultsPerWaveTier newScoreTier = new ScoreResultsPerWaveTier(); 
            newScoreTier.m_tierScorePerWave = m_scoreTiers[i].m_scorePerWave;
            newScoreTier.m_tierWaveBreakpoint = m_scoreTiers[i].m_waveBreakpoint;
            tierScores.Add(newScoreTier);
        }

        int tierIndex = 0;
        for (int waveIndex = 1; waveIndex < GameplayManager.Instance.m_wave + 1; ++waveIndex)
        {
            tierScores[tierIndex].m_tierScore += m_scoreTiers[tierIndex].m_scorePerWave;
            ++tierScores[tierIndex].m_tierWaveCount;

            if (waveIndex == m_scoreTiers[tierIndex].m_waveBreakpoint && tierIndex < tierScores.Count) {
                ++tierIndex;
            }
        }
        
        return (tierScores, m_totalObeliskScore);
    }

    void OnDestroy()
    {
        GameplayManager.OnObelisksCharged -= UpdateObeliskScore;
    }
}

[System.Serializable]
public class ScorePerWaveTier
{
    public int m_waveBreakpoint;
    public int m_scorePerWave;
}

[System.Serializable]
public class ScoreResultsPerWaveTier
{
    public int m_tierScore = 0;
    public int m_tierWaveCount = 0;
    public int m_tierScorePerWave;
    public int m_tierWaveBreakpoint;
}