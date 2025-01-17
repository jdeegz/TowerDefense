using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionTable", menuName = "ScriptableObjects/Progression/ProgressionTable")]
public class ProgressionTable : ScriptableObject
{
    [SerializeField] private List<ProgressionUnlockableData> m_progressionUnlocks;

    public ProgressionUnlockableData GetUnlockableFromKey(ProgressionKeyData keyData)
    {
        foreach (ProgressionUnlockableData unlockableData in m_progressionUnlocks)
        {
            // Does the unlockable's list of keys contain the given key?
            if (unlockableData.RequirementsIncludesKey(keyData))
            {
                //Debug.Log($"Found {keyData.name} as a requirement of {unlockableData.name}.");
                return unlockableData;
            }
        }
        return null;
    }

    public void ResetProgressionData()
    {
        foreach (ProgressionUnlockableData unlockableData in m_progressionUnlocks)
        {
            unlockableData.ResetProgression();
        }

        Debug.Log($"Progression Reset Completed.");
    }

    public void CheatProgressionData()
    {
        foreach (ProgressionUnlockableData unlockableData in m_progressionUnlocks)
        {
            unlockableData.CheatProgression();
        }
        
        Debug.Log($"Progression Cheat Completed.");
    }

    public List<ProgressionUnlockableData> GetListUnlockableData()
    {
        return m_progressionUnlocks;
    }
}
