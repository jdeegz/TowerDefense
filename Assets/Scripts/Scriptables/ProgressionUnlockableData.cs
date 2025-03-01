using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionUnlockableData", menuName = "ScriptableObjects/Progression/ProgressionUnlockableData")]
public class ProgressionUnlockableData : ScriptableObject
{
    [SerializeField] private List<ProgressionKeyData> m_unlockRequirementKeys; // The keys required to complete the unlock.
    [SerializeField] private ProgressionRewardData m_unlockReward; // A reference to the item we unlock.
    
    [Header("Towers & Buildings")]
    [SerializeField] private RuinIndicator m_ruinIndicator; // The visual element we spawn in the mission to achieve this unlock.

    [Header("Endless Mission Req")]
    [SerializeField] private int m_waveReq = -1;
    
    public bool RequirementsIncludesKey(ProgressionKeyData keyData)
    {
        if (m_unlockRequirementKeys.Contains(keyData))
        {
            return true;
        }

        return false;
    }

    public void AwardUnlockable()
    {
        if (m_unlockReward == null) return; // Sometime we don't have an object to unlock, like for Missions that only read the keys.
        m_unlockReward.UnlockReward();
    }

    public void LockUnlockable()
    {
        if (m_unlockReward == null) return;
        m_unlockReward.LockReward();
    }
    
    public RuinIndicator GetRuinIndicator()
    {
        return m_ruinIndicator;
    }

    public ProgressionRewardData GetRewardData()
    {
        return m_unlockReward;
    }

    public List<ProgressionKeyData> GetKeyData()
    {
        return m_unlockRequirementKeys;
    }

    public int GetWaveRequirement()
    {
        return m_waveReq;
    }
    
    public void ResetProgression()
    {
        // For each unlockRequirementKey, set it's value back to locked (false)
        foreach (ProgressionKeyData progressionKeyData in m_unlockRequirementKeys)
        {
            PlayerDataManager.Instance.RequestLockKey(progressionKeyData);
        }
    }

    public UnlockProgress GetProgress()
    {
        // Return the number of unlockRequirements that have been met.
        int currentProgressValue = 0;
        
        foreach (ProgressionKeyData progressionKeyData in m_unlockRequirementKeys)
        {
            //Debug.Log($"KEY: {progressionKeyData.name}'s value: {progressionKeyData.ProgressionKeyEnabled}");
            
            if (progressionKeyData.ProgressionKeyEnabled)
            {
                ++currentProgressValue;
            }
        }
        
        UnlockProgress unlockProgress = new UnlockProgress(name, m_unlockRequirementKeys.Count, currentProgressValue, m_unlockRequirementKeys);
        return unlockProgress;
    }

    public void CheatProgression()
    {
        // For each unlockRequirementKey, set it's value back to locked (true)
        foreach (ProgressionKeyData progressionKeyData in m_unlockRequirementKeys)
        {
            PlayerDataManager.Instance.RequestUnlockKey(progressionKeyData);
        }
    }
}

public class UnlockProgress
{
    public string m_name;
    public int m_requirementTotal;
    public int m_requirementsMet;
    public List<ProgressionKeyData> m_progressionKeys;
    public bool m_isUnlocked;

    public UnlockProgress(string name, int requirementTotal, int requirementsMet, List<ProgressionKeyData> keys)
    {
        m_name = name;
        m_requirementTotal = requirementTotal;
        m_requirementsMet = requirementsMet;
        List<ProgressionKeyData> m_progressionKeys = keys;

        m_isUnlocked = m_requirementsMet == m_requirementTotal;
        //Debug.Log($"{m_name}'s progress is {m_requirementsMet} / {m_requirementTotal}. Unlocked: {m_isUnlocked}");
    }
}


