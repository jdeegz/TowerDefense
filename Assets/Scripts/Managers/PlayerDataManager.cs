using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GameUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDataManager
{
    public static PlayerDataManager Instance { get; } = new PlayerDataManager();
    public static event Action<ProgressionUnlockableData> OnUnlockableUnlocked;
    public static event Action<ProgressionUnlockableData> OnUnlockableLocked;
    public PlayerData m_playerData;
    
    //private string m_path;
    private string m_persistantPath;
    private int m_buildNumber = 8; //Increment this to invalidate old save files. Updated Jan 30th 2025
    public int BuildNumber => m_buildNumber;
    public static ProgressionTable m_progressionTable;

    //Constructor
    private PlayerDataManager()
    {
        Debug.Log($"Player Data Manager created.");
        SetPaths();
        
        if (m_progressionTable == null)
        {
            m_progressionTable = Resources.Load<ProgressionTable>("ProgressionTable");
            Debug.Log($"Player Data Manager: Progression Table Assigned.");
        }
    }
    
    public void Initialize()
    {
        HandleRead();
    }
    
    public static ProgressionTable GetProgressionTable()
    {
        return m_progressionTable;
    }

    void SetPaths()
    {
        //m_path = Application.dataPath + Path.AltDirectorySeparatorChar + "PlayerSave.json";
        m_persistantPath = Application.persistentDataPath + Path.AltDirectorySeparatorChar + "PlayerSave.json";
    }

    public void HandleWrite()
    {
        string json = JsonUtility.ToJson(m_playerData);
        PlayerPrefs.SetString("PlayerData", json);
        PlayerPrefs.Save();
        
        //Debug.Log($"Handle Write Complete.");
    }

    
    public void HandleRead()
    {
        if (PlayerPrefs.HasKey("PlayerData"))
        {
            string json = PlayerPrefs.GetString("PlayerData");
            m_playerData = JsonUtility.FromJson<PlayerData>(json);

            // Validate Save Version.
            if (m_playerData.m_buildNumber != m_buildNumber)
            {
                // This data is not valid anymore.
                Debug.Log("Build Number mismatch. Building new Save File.");
                ResetPlayerData();
            }
        }
        else
        {
            Debug.Log("No Save found. Building Save File.");
            // No data found, let's build one.
            ResetPlayerData();
        }

        Debug.Log($"{m_playerData.m_progressionKeys.Count} Progression Keys found in Save Data");
    }
    
    public MissionSaveData GetMissionSaveDataByMissionData(MissionData missionData)
    {
        if (GameManager.Instance == null) Initialize();
        
        
        Debug.Log($"Player Data is null: {m_playerData == null}");
        Debug.Log($"Player Data Missions is null: {m_playerData.m_missions == null || m_playerData.m_missions.Count == 0}");

        
        foreach (MissionSaveData missionSaveData in m_playerData.m_missions)
        {
            if (missionSaveData.m_sceneName == missionData.m_missionScene) return missionSaveData;
        }
        
        // If we haven't found one, create a new one and add it to player data for future look-ups.
        MissionSaveData newMissionSaveData = new MissionSaveData(missionData.m_missionScene, 0, 0, 0, 0);
        bool isUnlocked = true;
        if (missionData.m_unlockRequirements == null || missionData.m_unlockRequirements.Count == 0)
        {
            isUnlocked = false; // This mission has no unlock requirements, it is not available content.
        }
        else
        {
            foreach (ProgressionUnlockableData unlockable in missionData.m_unlockRequirements)
            {
                if (!unlockable.GetProgress().m_isUnlocked)
                {
                    isUnlocked = false;
                    break;
                }
            }
        }

        if(missionData.m_isUnlockedByDefault || isUnlocked)
        {
            newMissionSaveData.m_missionCompletionRank = 1;
        }
        
        m_playerData.m_missions.Add(newMissionSaveData);

        Debug.Log($"GetMissionSaveData: Mission Name: {newMissionSaveData.m_sceneName}, Mission Completion Rank: {newMissionSaveData.m_missionCompletionRank}");

        return newMissionSaveData;
    }

    public void UpdateMissionSaveData(string missionName, int completeionRank, int wave, int perfectWaves)
    {
        Debug.Log($"UpdateMissionSaveData: {missionName}.");
        
        // Make and edit a temporary Mission Save Data, use the existing one as reference, then assign it.
        MissionSaveData newMissionSaveData = new MissionSaveData(missionName, 1, wave, completeionRank, perfectWaves);

        for (var i = 0; i < m_playerData.m_missions.Count; i++)
        {
            MissionSaveData mission = m_playerData.m_missions[i];
            if (mission.m_sceneName == newMissionSaveData.m_sceneName)
            {
                Debug.Log($"UpdateMissionSaveData: Existing MissionSaveData Found!");
                
                newMissionSaveData.m_missionAttempts = mission.m_missionAttempts + 1;

                newMissionSaveData.m_waveHighScore = Math.Max(wave, mission.m_waveHighScore);
                Debug.Log($"UpdateMissionSaveData: Evaluating High Score. Current High Score {mission.m_waveHighScore}, New Score {wave}.");
                
                newMissionSaveData.m_perfectWaveScore = Math.Max(perfectWaves, mission.m_perfectWaveScore);
                Debug.Log($"UpdateMissionSaveData: Evaluating Perfect Wave Score. Current High Score {mission.m_perfectWaveScore}, New Score {perfectWaves}.");

                newMissionSaveData.m_missionCompletionRank = Math.Max(newMissionSaveData.m_missionCompletionRank, mission.m_missionCompletionRank);
                Debug.Log($"UpdateMissionSaveData: Evaluating Completion Rank. Current High Score {mission.m_missionCompletionRank}, New Score {completeionRank}.");

                Debug.Log($"UpdateMissionSaveData: " +
                          $"Mission Attempts {newMissionSaveData.m_missionAttempts}, " +
                          $"Wave High Score {newMissionSaveData.m_waveHighScore}, " +
                          $"Perfect Wave Score {newMissionSaveData.m_perfectWaveScore}, " +
                          $"Completion Rank {newMissionSaveData.m_missionCompletionRank}.");
                
                m_playerData.m_missions[i] = newMissionSaveData;
                HandleWrite();
                return;
            }
        }

        Debug.Log($"UpdateMissionSaveData: NO Existing MissionSaveData Found!");
        m_playerData.m_missions.Add(newMissionSaveData);
        
        Debug.Log($"UpdateMissionSaveData: Creating {missionName} Mission Save Data:" +
                  $"Mission Attempts {newMissionSaveData.m_missionAttempts}, " +
                  $"Wave High Score {newMissionSaveData.m_waveHighScore}, " +
                  $"Perfect Wave Score {newMissionSaveData.m_perfectWaveScore}, " +
                  $"Completion Rank {newMissionSaveData.m_missionCompletionRank}.");
        HandleWrite();
    }

    public MissionSaveData GetMissionSaveData(string sceneName)
    {
        foreach (MissionSaveData saveData in m_playerData.m_missions)
        {
            if (saveData.m_sceneName == sceneName)
            {
                return saveData;
            }
        }
        
        return null;
    }

    public void ResetPlayerData()
    {
        m_playerData = new PlayerData();
        m_playerData.m_buildNumber = m_buildNumber;
        Debug.Log($"Player Data Manager: Resetting Progression Data");
        m_progressionTable.ResetProgressionData();
        HandleWrite();
    }
    
    // UNLOCK PROGRESSION
    public void RequestUnlockKey(ProgressionKeyData keyData)
    {
        // Is the key already unlocked?
        if (keyData.ProgressionKeyEnabled)
        {
            //Debug.Log($"{keyData.name}'s value is already True.");
            return;
        }

        // Set the key to unlocked.
        keyData.UnlockKey();

        // Get the unlockable the key is in.
        ProgressionUnlockableData unlockable = m_progressionTable.GetUnlockableFromKey(keyData);

        // Get the status of the Unlockable the key is in.
        UnlockProgress unlockProgress = unlockable.GetProgress();


        if (unlockProgress.m_isUnlocked)
        {
            unlockable.AwardUnlockable();
            OnUnlockableUnlocked?.Invoke(unlockable);
            Debug.Log($"{unlockable.name} earned.");
        }
    }

    public void RequestLockKey(ProgressionKeyData keyData)
    {
        if (!keyData.ProgressionKeyEnabled)
        {
            //Debug.Log($"{keyData.name}'s value is already False.");
            return;
        }
        
        // Set the key to locked
        keyData.LockKey();
        
        // Get the unlockable the key is in.
        ProgressionUnlockableData unlockable = m_progressionTable.GetUnlockableFromKey(keyData);

        // Get the status of the Unlockable the key is in.
        UnlockProgress unlockProgress = unlockable.GetProgress();


        if (!unlockProgress.m_isUnlocked)
        {
            unlockable.LockUnlockable();
            OnUnlockableLocked?.Invoke(unlockable);
        }
    }
    
    // Delete me??
    public void SetProgressionTable(ProgressionTable progressionTable)
    {
        m_progressionTable = progressionTable;
    }

    public SortedAndUnlocked GetSortedUnlocked()
    {
        Dictionary<TowerData, int> unlockedStructures = new Dictionary<TowerData, int>();
        List<TowerData> unlockedTowers = new List<TowerData>();

        Debug.Log($"Is player data null? {m_playerData == null}");
        foreach (ProgressionUnlockableData unlockableData in m_progressionTable.GetListUnlockableData())
        {
            //If we're still locked, go next.
            UnlockProgress unlockProgress = unlockableData.GetProgress();
            if (!unlockProgress.m_isUnlocked)
            {
                continue;
            }
            
            // Get the reward so we can determine type, then get the tower data and pack it.
            ProgressionRewardData rewardData = unlockableData.GetRewardData();
            if (rewardData == null) continue;
            
            switch (rewardData.RewardType)
            {
                case "Structure":
                    TowerData structureData = rewardData.GetReward();
                    unlockedStructures[structureData] = unlockedStructures.GetValueOrDefault(structureData, 0) + rewardData.GetRewardQty();
                    break;
                case "Tower":
                    TowerData towerData = rewardData.GetReward();
                    if (unlockedTowers.Contains(towerData)) break;
                    unlockedTowers.Add(towerData);
                    break;
                case "Mission":
                    break;
                default:
                    break;
            }
        }

        /*Debug.Log($"Returning Unlockable Structures.");
        foreach (var kvp in unlockedStructures)
        {
            Debug.Log($"{kvp.Key.m_towerName} x{kvp.Value}");
        }*/

        SortedAndUnlocked sortedAndUnlocked = new SortedAndUnlocked(unlockedStructures, unlockedTowers);
        return sortedAndUnlocked;
    }

    public void ResetProgressionTable()
    {
        m_progressionTable.ResetProgressionData();
    }
    
    public void CheatPlayerData()
    {
        foreach (MissionSaveData missionSaveData in m_playerData.m_missions)
        {
            missionSaveData.m_missionCompletionRank = Math.Max(1, missionSaveData.m_missionCompletionRank);
        }
        
        m_progressionTable.CheatProgressionData();
    }
}

public class SortedAndUnlocked
{
    public Dictionary<TowerData, int> m_unlockedStructures;
    public List<TowerData> m_unlockedTowers;

    public SortedAndUnlocked(Dictionary<TowerData, int> unlockedStructures, List<TowerData> unlockedTowers)
    {
        m_unlockedStructures = unlockedStructures;
        m_unlockedTowers = unlockedTowers;
    }
}

[System.Serializable]
public class PlayerData
{
    public int m_buildNumber;
    public List<MissionSaveData> m_missions;
    public List<SerializedKVP> m_progressionKeys;

    public PlayerData()
    {
        m_missions = new List<MissionSaveData>();
        m_progressionKeys = new List<SerializedKVP>();
    }
}

[System.Serializable]
public class MissionSaveData
{
    public string m_sceneName;
    public int m_missionAttempts;
    public int m_waveHighScore;
    public int m_perfectWaveScore;
    public int m_missionCompletionRank; // 0 - Locked, 1 - Unbeaten, 2 - Defeated, 3 - ??

    public MissionSaveData(string sceneName, int attempts, int highScore, int completionRank, int perfectWaveScore)
    {
        m_sceneName = sceneName;
        m_missionAttempts = attempts;
        m_waveHighScore = highScore;
        m_missionCompletionRank = completionRank;
        m_perfectWaveScore = perfectWaveScore;
    }
}

[System.Serializable]
public class SerializedKVP
{
    public string Key;
    public bool Value;

    public SerializedKVP(string key, bool value)
    {
        Key = key;
        Value = value;
    }
}