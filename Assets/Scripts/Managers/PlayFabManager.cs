using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab.ClientModels;
using PlayFab;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance;
    public string m_displayName;
    private List<string> m_leaderboardNames;
    public Action<bool, Dictionary<string, GetLeaderboardResult>> OnLeaderboardReceived;
    private Dictionary<string, GetLeaderboardResult> m_results = new Dictionary<string, GetLeaderboardResult>();
    private bool m_inProgress;
    private bool m_failed;

    /// <summary>
    /// move to another script later.
    /// </summary>
    
    
    private void Awake()
    {
        Instance = this;

        //Dynamically build the list of leaderboard names so i dont have to update code when we make new or remove leaderboards.
        m_leaderboardNames = new List<string>();
        foreach (MissionData data in GameManager.Instance.m_MissionContainer.m_MissionList)
        {
            m_leaderboardNames.Add(data.m_playFableaderboardId);
        }
    }

    void Start()
    {
        Login();
    }

    // Update is called once per frame
    void Update()
    {
    }

    void Login()
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnSuccess, OnError);
    }

    void OnSuccess(LoginResult result)
    {
        Debug.Log($"Login Successful.");
        
        //CheatFillLeaderboards();
    }

    void OnError(PlayFabError error)
    {
        //Debug.Log($"Login Failure.");
        Debug.Log($"{error.GenerateErrorReport()}");
    }

    public void SendLeaderboard(string leaderboardName, int score)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = leaderboardName,
                    Value = score
                }
            }
        };
        PlayFabClientAPI.UpdatePlayerStatistics(request, OnLeaderboardUpdate, OnError);
    }

    void OnLeaderboardUpdate(UpdatePlayerStatisticsResult result)
    {
        Debug.Log($"Successful Leaderboard sent.");
    }

    public void GetLeaderboard(string leaderboardName)
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = leaderboardName,
            StartPosition = 0,
            MaxResultsCount = 10
        };
        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardGet, OnError);
    }

    void OnLeaderboardGet(GetLeaderboardResult result)
    {
        foreach (var item in result.Leaderboard)
        {
            Debug.Log($"{item.Position} {item.PlayFabId} {item.StatValue}");
        }
    }

    public void GetAllLeaderboards()
    {
        if (m_inProgress)
        {
            return;
        }

        m_inProgress = true;
        m_failed = false;
        if(m_results != null) m_results.Clear();

        foreach (String leaderboardName in m_leaderboardNames)
        {
            var request = new GetLeaderboardRequest
            {
                StatisticName = leaderboardName,
                StartPosition = 0,
                MaxResultsCount = 10
            };

            PlayFabClientAPI.GetLeaderboard(
                request,
                (result) => OnAllLeaderboardGet(result, leaderboardName),
                (error) => OnErrorGetLeaderboard(error, leaderboardName));
        }
    }

    private void OnAllLeaderboardGet(GetLeaderboardResult result, string leaderboardName)
    {
        m_results[leaderboardName] = result;

        CheckFinished();
    }

    private void OnErrorGetLeaderboard(PlayFabError error, string leaderboardName)
    {
        m_failed = true;
        m_results[leaderboardName] = null;
        
        Debug.Log($"Error getting leaderboard {leaderboardName}.");
        Debug.Log($"{error.GenerateErrorReport()}");
        CheckFinished();
    }

    private void CheckFinished()
    {
        if (m_results.Count == m_leaderboardNames.Count)
        {
            OnLeaderboardReceived(m_failed, m_results);
            m_inProgress = false;
        }
    }

    public void CheatFillLeaderboards()
    {
        foreach (String leaderboardName in m_leaderboardNames)
        {
            for(int i = 0; i < 10; ++i)
            {
                int score = Random.Range(1, 99);
                SendLeaderboard(leaderboardName, score);
            }
        }
    }
}