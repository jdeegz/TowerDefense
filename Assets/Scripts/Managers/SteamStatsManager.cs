using Steamworks;
using UnityEngine;

public static class SteamStatsManager
{
    public static void DecrementStat(string statName)
    {
        if (!SteamManager.Initialized) return;

        int currentValue = 0;
        SteamUserStats.GetStat(statName, out currentValue);
        SteamUserStats.SetStat(statName, currentValue - 1);
        SteamUserStats.StoreStats();

        Debug.Log($"SteamStatsManager: {statName} decremented from {currentValue} to {currentValue - 1}.");
    }
    
    public static void IncrementStat(string statName)
    {
        if (!SteamManager.Initialized) return;

        int currentValue = 0;
        SteamUserStats.GetStat(statName, out currentValue);
        SteamUserStats.SetStat(statName, currentValue + 1);
        SteamUserStats.StoreStats();

        Debug.Log($"SteamStatsManager: {statName} incremented from {currentValue} to {currentValue + 1}.");
    }


    public static void SetStat(string statName, int value)
    {
        if (!SteamManager.Initialized) return;

        SteamUserStats.SetStat(statName, value);
        SteamUserStats.StoreStats();

        Debug.Log($"SteamStatsManager: {statName} set to {value}.");
    }
    
    public static void SetStat(string statName, float value)
    {
        if (!SteamManager.Initialized) return;

        SteamUserStats.SetStat(statName, value);
        SteamUserStats.StoreStats();

        Debug.Log($"SteamStatsManager: {statName} set to {value}.");
    }

    public static int GetStat(string statName)
    {
        if (!SteamManager.Initialized) return 0;

        int value = 0;
        SteamUserStats.GetStat(statName, out value);
        Debug.Log($"SteamStatsManager: {statName} recieved. Value of {value}.");

        return value;
    }

    public static void SetAchievement(string achievementName)
    {
        SteamUserStats.SetAchievement(achievementName);
        SteamUserStats.StoreStats();
        
        Debug.Log($"SteamStatsManager: {achievementName} achievement earned.");
    }
}