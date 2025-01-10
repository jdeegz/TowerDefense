using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class ProgressionRewardData : ScriptableObject
{
    public abstract string RewardType { get; }

    public bool ProgressionRewardEnabled
    {
        get
        {
            SerializedKVP kvp = PlayerDataManager.Instance.m_playerData.m_progressionKeys.FirstOrDefault(k => k.Key == name);

            // If this key does not exist return false
            if (kvp.Key == null)
            {
                Debug.Log($"No Key Found in PlayerDataManager with name: {name}.");
                return false;
            }

            return kvp.Value;
        }
        set
        {
            int i = PlayerDataManager.Instance.m_playerData.m_progressionKeys.FindIndex(k => k.Key == name);

            if (i != -1)
            {
                Debug.Log($"KVP FOUND: updating to {value}.");
                PlayerDataManager.Instance.m_playerData.m_progressionKeys[i].Value = value;
            }
            else
            {
                Debug.Log($"KVP NOT FOUND: creating and setting to {value}.");
                PlayerDataManager.Instance.m_playerData.m_progressionKeys.Add(new SerializedKVP(name, value));
            }

            PlayerDataManager.Instance.HandleWrite();
        }
    }

    public virtual void UnlockReward()
    {
        ProgressionRewardEnabled = true;
        Debug.Log($"REWARD: {name}'s value: {ProgressionRewardEnabled}");
    }

    public virtual void LockReward()
    {
        ProgressionRewardEnabled = false;
        Debug.Log($"REWARD: {name}'s value: {ProgressionRewardEnabled}");
    }

    public abstract TowerData GetReward();
    public abstract int GetRewardQty();
}