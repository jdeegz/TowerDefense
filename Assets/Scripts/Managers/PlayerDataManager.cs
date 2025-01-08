using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDataManager
{
    public static PlayerDataManager Instance { get; } = new PlayerDataManager();
    public static event Action<ProgressionUnlockableData> OnUnlockableUnlocked;
    public static event Action<ProgressionUnlockableData> OnUnlockableLocked;
    public PlayerData m_playerData;
    public ProgressionTable m_progressionTable;
    
    //private string m_path;
    private string m_persistantPath;
    private int m_buildNumber = 4; //Increment this to invalidate old save files. Updated Nov 4th 2024

    //Constructor
    private PlayerDataManager()
    {
        SetPaths();
        HandleRead();
        //m_progressionTable = GameManager.Instance.m_progressionTable;
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
        
        Debug.Log($"Handle Write Complete.");
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

    public void BuildMissionListSaveData()
    {
        //If the read data does not have a MissionSaveData for all missions, make new ones for those missing.
        int missionListDesync = GameManager.Instance.m_missionTable.m_MissionList.Length - m_playerData.m_missions.Count;

        for (var i = 0; i < missionListDesync; i++)
        {
            var missionData = GameManager.Instance.m_missionTable.m_MissionList[i];
            int completionRank = m_playerData.m_missions.Count == 0 ? 1 : 0;
            MissionSaveData newMissionSaveData = new MissionSaveData(missionData.m_missionScene, 0, 0, completionRank);

            m_playerData.m_missions.Add(newMissionSaveData);
        }
    }

    public void UpdateMissionSaveData(string missionName, int completeionRank, int wave)
    {
        // Make and edit a temporary Mission Save Data, use the existing one as reference, then assign it.
        MissionSaveData newMissionSaveData = new MissionSaveData(missionName, 0, 0, 0);
        newMissionSaveData.m_missionCompletionRank = completeionRank;

        for (var i = 0; i < m_playerData.m_missions.Count; i++)
        {
            MissionSaveData mission = m_playerData.m_missions[i];
            if (mission.m_sceneName == newMissionSaveData.m_sceneName)
            {
                //increment attempts in current data.
                newMissionSaveData.m_missionAttempts = mission.m_missionAttempts + 1;

                newMissionSaveData.m_waveHighScore = Math.Max(wave, mission.m_waveHighScore);

                //only save the highest completion rank.
                newMissionSaveData.m_missionCompletionRank = Math.Max(newMissionSaveData.m_missionCompletionRank, mission.m_missionCompletionRank);

                m_playerData.m_missions[i] = newMissionSaveData;

                //If completion rank is greater than 1, we want to unlock the next mission if there is one.
                if (newMissionSaveData.m_missionCompletionRank > 1 && i < m_playerData.m_missions.Count - 1)
                {
                    m_playerData.m_missions[i + 1].m_missionCompletionRank = Math.Max(1, m_playerData.m_missions[i + 1].m_missionCompletionRank);
                }
            }
        }

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
        BuildMissionListSaveData();
        HandleWrite();
    }

    void SyncMissionListSaveData()
    {
        //Validate Mission Save Data list.
        int missionListLength = GameManager.Instance.m_missionTable.m_MissionList.Length;
        if (m_playerData.m_missions.Count != missionListLength)
        {
            Debug.Log($"Mission List mismatch. Updating Mission List.");

            //We have a desync between saved missions and number of missions in game.
            if (missionListLength > m_playerData.m_missions.Count)
            {
                //We have more missions in the game, build the save data for the new ones.
                BuildMissionListSaveData();

                //TO DO
                //Build out the ability to insert new missions into the list, rather than just appending to end.
                return;
            }

            if (missionListLength < m_playerData.m_missions.Count)
            {
                //We've removed some missions from the game, clean out the ones that dont exist anymore.
                //We're not just removing from the end of the list, we're checking to see if the mission still exists, if not, removing it at its index.
                for (int i = 0; i < m_playerData.m_missions.Count; ++i)
                {
                    bool isInMissionList = false;
                    foreach (MissionData missionData in GameManager.Instance.m_missionTable.m_MissionList)
                    {
                        if (missionData.m_missionScene == m_playerData.m_missions[i].m_sceneName)
                        {
                            isInMissionList = true;
                        }
                    }

                    if (isInMissionList == false)
                    {
                        m_playerData.m_missions.RemoveAt(i);
                        --i;
                    }
                }

                HandleWrite();
            }
        }
    }
    
    // UNLOCK PROGRESSION
    public void RequestUnlockKey(ProgressionKeyData keyData)
    {
        // Is the key already unlocked?
        if (keyData.ProgressionKeyEnabled)
        {
            Debug.Log($"{keyData.name}'s value is already True.");
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

    public Dictionary<TowerData, int> GetUnlockedStructures()
    {
        Dictionary<TowerData, int> unlockedStructures = new Dictionary<TowerData, int>();
        
        foreach (ProgressionUnlockableData unlockableData in m_progressionTable.GetListUnlockableData())
        {
            ProgressionRewardStructure rewardData = unlockableData.GetRewardData() as ProgressionRewardStructure;
            if (rewardData.RewardType != "Structure")
            {
                continue;
            }
            
            UnlockProgress unlockProgress = unlockableData.GetProgress();
            if (unlockProgress.m_isUnlocked)
            {
                TowerData towerData = rewardData.GetStructureData();
                unlockedStructures[towerData] = unlockedStructures.GetValueOrDefault(towerData, 0) + rewardData.GetStructureRewardQty();
            }
        }


        Debug.Log($"Returning Unlockable Structures.");
        foreach (var kvp in unlockedStructures)
        {
            Debug.Log($"{kvp.Key.m_towerName} x{kvp.Value}");
        }
        return unlockedStructures;
    }

    public void ResetProgressionTable()
    {
        m_progressionTable.ResetProgressionData();
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
    public int m_missionCompletionRank;

    public MissionSaveData(string sceneName, int attempts, int highScore, int completionRank)
    {
        m_sceneName = sceneName;
        m_missionAttempts = attempts;
        m_waveHighScore = highScore;
        m_missionCompletionRank = completionRank;
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