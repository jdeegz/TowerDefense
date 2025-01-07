using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionUnlockableData", menuName = "ScriptableObjects/Progression/ProgressionUnlockableData")]
public class ProgressionUnlockableData : ScriptableObject
{
    [SerializeField] private List<ProgressionKeyData> m_unlockRequirementKeys;
    [SerializeField] private ProgressionRewardData m_unlockReward;
    
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
        m_unlockReward.UnlockReward();
    }

    public void LockUnlockable()
    {
        m_unlockReward.LockReward();
    }

    public ProgressionRewardData GetRewardData()
    {
        return m_unlockReward;
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
            Debug.Log($"KEY: {progressionKeyData.name}'s value: {progressionKeyData.ProgressionKeyEnabled}");
            
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
        Debug.Log($"{m_name}'s progress is {m_requirementsMet} / {m_requirementTotal}. Unlocked: {m_isUnlocked}");
    }
}


