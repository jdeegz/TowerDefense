using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;
    public PlayerData m_playerData;

    //private string m_path;
    private string m_persistantPath;
    private int m_buildNumber = 4; //Increment this to invalidate old save files. Updated Nov 4th 2024

    //Constructor
    void Awake()
    {
        Instance = this;
        SetPaths();
        HandleRead();
    }

    void SetPaths()
    {
        //m_path = Application.dataPath + Path.AltDirectorySeparatorChar + "PlayerSave.json";
        m_persistantPath = Application.persistentDataPath + Path.AltDirectorySeparatorChar + "PlayerSave.json";
    }

    //Switching to use PlayerPrefs
    public void HandleWrite()
    {
        string json = JsonUtility.ToJson(m_playerData);
        PlayerPrefs.SetString("PlayerData", json);
        PlayerPrefs.Save();
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
    }
    
    
    //Handle Writing to our own data file.
    /*public void HandleWrite()
    {
        string savePath = m_persistantPath;

        string json = JsonUtility.ToJson(m_playerData);

        using (StreamWriter writer = new StreamWriter(savePath))
        {
            writer.Write(json);
        }
    }*/
    
    //Handle Reading our own data file.
    /*public void HandleRead()
    {
        if (File.Exists(m_persistantPath))
        {
            string json;
            using (StreamReader reader = new StreamReader(m_persistantPath))
            {
                json = reader.ReadToEnd();
            }

            m_playerData = JsonUtility.FromJson<PlayerData>(json);

            //Validate Save Version.
            if (m_playerData.m_buildNumber != m_buildNumber)
            {
                //This data is not valid anymore.
                Debug.Log($"Build Number mismatch. Building new Save File.");
                ResetPlayerData();
            }
        }
        else
        {
            Debug.Log($"No Save found. Building Save File at {m_persistantPath}.");
            //No file found, let's build one.
            ResetPlayerData();
        }
    }*/

    public void BuildMissionListSaveData()
    {
        //If the read data does not have a MissionSaveData for all missions, make new ones for those missing.
        int missionListDesync = GameManager.Instance.m_MissionContainer.m_MissionList.Length - m_playerData.m_missions.Count;

        for (var i = 0; i < missionListDesync; i++)
        {
            var missionData = GameManager.Instance.m_MissionContainer.m_MissionList[i];
            MissionSaveData newMissionSaveData = new MissionSaveData(missionData.m_missionScene);
            newMissionSaveData.m_sceneName = missionData.m_missionScene;
            newMissionSaveData.m_missionAttempts = 0;
            newMissionSaveData.m_missionCompletionRank = m_playerData.m_missions.Count == 0 ? 1 : 0;

            m_playerData.m_missions.Add(newMissionSaveData);
        }
    }

    public void UpdateMissionSaveData(string missionName, int completeionRank, int wave)
    {
        MissionSaveData newMissionSaveData = new MissionSaveData(missionName);
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
        int missionListLength = GameManager.Instance.m_MissionContainer.m_MissionList.Length;
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
                    foreach (MissionData missionData in GameManager.Instance.m_MissionContainer.m_MissionList)
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
}

[System.Serializable]
public class PlayerData
{
    public int m_buildNumber;
    public List<MissionSaveData> m_missions;

    public PlayerData()
    {
        m_missions = new List<MissionSaveData>();
    }
}

[System.Serializable]
public class MissionSaveData
{
    public string m_sceneName;
    public int m_missionAttempts;
    public int m_waveHighScore;
    public int m_missionCompletionRank;

    public MissionSaveData(string sceneName)
    {
        m_sceneName = sceneName; 
    }
}