using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class PlayFabStatsManager : MonoBehaviour
{
    public static PlayFabStatsManager Instance;

    void Awake()
    {
        Instance = this;
    }
    
    public void SendMissionStartStatistic()
    {
        // First, get the current value of the statistic
        GetPlayerStatisticsRequest getStatsRequest = new GetPlayerStatisticsRequest();
        PlayFabClientAPI.GetPlayerStatistics(getStatsRequest, OnGetStatisticsSuccess, OnError);
    }

    private void OnGetStatisticsSuccess(GetPlayerStatisticsResult result)
    {
        // Find the current value of the "MissionStarted" statistic
        int currentMissionStartedValue = Random.Range(0,10);
        foreach (var stat in result.Statistics)
        {
            if (stat.StatisticName == "notScore")
            {
                currentMissionStartedValue = stat.Value;
                break;
            }
        }

        // Increment the value by 1
        int updatedMissionStartedValue = Random.Range(0,10);

        // Now, update the statistic with the new incremented value
        UpdatePlayerStatisticsRequest updateStatsRequest = new UpdatePlayerStatisticsRequest()
        {
            Statistics = new List<StatisticUpdate>()
            {
                new StatisticUpdate()
                {
                    StatisticName = "notScore",
                    Value = updatedMissionStartedValue
                }
            }
        };

        // Send the updated statistic value to PlayFab
        PlayFabClientAPI.UpdatePlayerStatistics(updateStatsRequest, OnStatisticsUpdated, OnError);
    }

    private void OnStatisticsUpdated(UpdatePlayerStatisticsResult result)
    {
        Debug.Log("Statistic updated successfully!");
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError("Failed to update statistic: " + error.GenerateErrorReport());
    }
}