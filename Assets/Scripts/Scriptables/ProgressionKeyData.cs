using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionKeyData", menuName = "ScriptableObjects/Progression/ProgressionKeyData")]
public class ProgressionKeyData : ScriptableObject
{
    public bool ProgressionKeyEnabled
    {
        get
        {
            SerializedKVP kvp = PlayerDataManager.Instance.m_playerData.m_progressionKeys.FirstOrDefault(k => k.Key == name);

            // If this key does not exist return false
            if (kvp == null)
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

    public void LockKey()
    {
        // Write to player prefs to lock this key.
        ProgressionKeyEnabled = false;
        //Debug.Log($"KEY: {name}'s value: {ProgressionKeyEnabled}");
    }

    public void UnlockKey()
    {
        // Write to player prefs to unlock this key.
        ProgressionKeyEnabled = true;
        //Debug.Log($"KEY: {name}'s value: {ProgressionKeyEnabled}");
    }
}