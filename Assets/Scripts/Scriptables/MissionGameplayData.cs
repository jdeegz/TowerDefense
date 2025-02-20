using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionGameplayData", menuName = "ScriptableObjects/MissionGameplayData")]
public class MissionGameplayData : ScriptableObject
{
    [Header("Game Mode")]
    public GameMode m_gameMode = GameMode.Standard;

    public enum GameMode
    {
        Standard,
        Survival
    }
    
    [Header("Early Game Health Scaling")]
    public float m_earlyGameFactor = 0.05f;         // % Of health gained in early waves.
    public float m_earlyGameCycleFactor = 0.2f;     // % Of health gained in each early-game cycle.
    
    [Space(10)]
    [Header("Mid Game Health Scaling")]
    public int m_midGameWave = 20;                  // The first wave that indicates mid-game
    public float m_midGameFactor = 0.05f;           // % Of health gained in mid-game waves.
    public float m_midGameCycleFactor = 0.1f;       // % Of health gained in each mid-game cycle.
    
    [Space(10)]
    [Header("Late Game Health Scaling")]
    public int m_lateGameWave = 40;                 // The first wave that indicates late-game
    public float m_lateGameFactor = 0.05f;          // % Of health gained in late waves.
    public float m_lateGameCycleFactor = 0.05f;     //  % Of health gained in each late-game cycle.
    
    [Space(10)]
    public int m_cycleLength = 10;                  // The wave-length of a cycle
    
    [Header("Wave Settings")]
    public float m_healthMultiplier = 0;            // Global health modifier. Possibly used for difficulty settings?
    public bool m_delayForQuest = false;            // Does this mission contain quests we need to wait for?
    public bool m_allowEndlessMode = true;          // Does this mission allow an endless mode?
    public float m_firstBuildDuraction = 15;        // The first build phase length in seconds.
    public float m_buildDuration = 6;               // Subsequent build phase lengths in seconds.
    public float m_afterBossBuildDuration = 30;     // Subsequent build phase lengths in seconds.
    public float m_survivalWaveDuration = 45f;      // The time between changing the enemy types in survival mode.
    
    [Header("Equipped Towers")]
    public List<TowerData> m_equippedTowers;        // The towers available to the player in this mission.
    public TowerData m_blueprintTower;              // Reference to the blue print tower.
    
    public float CalculateHealth(float baseHealth)
    {
        int i = GameplayManager.Instance.Wave;
        float health = baseHealth;

        float earlyGameHealth;
        float midGameHealth = 0;
        float lateGameHealth = 0;

        // EARLY GAME
        int earlyWaveNumber = Math.Min(i, m_midGameWave);
        earlyGameHealth = health * (1 + (m_earlyGameFactor * earlyWaveNumber));

        int numberOfEarlyGameCycles = earlyWaveNumber / m_cycleLength;
        float earlyGameBonusHealth = health * (1 + (numberOfEarlyGameCycles * m_earlyGameCycleFactor)) - health;
        //.Log($"Early Base Health: {earlyGameHealth}, Early Bonus Health {earlyGameBonusHealth}, Early Cycles: {numberOfEarlyGameCycles}");

        earlyGameHealth += earlyGameBonusHealth;
        
        // MID GAME
        int numberOfMidGameCycles = 0;
        float midGameBonusHealth = 0;
        if (i > m_midGameWave)
        {
            int midWaveNumber = Math.Min(i - m_midGameWave, m_lateGameWave - m_midGameWave);
            midGameHealth = health * (1 + (m_midGameFactor * midWaveNumber)) - health;

            numberOfMidGameCycles = midWaveNumber / m_cycleLength;
            midGameBonusHealth = health * (1 + (numberOfMidGameCycles * m_midGameCycleFactor)) - health;
            //Debug.Log($"Mid Base Health: {midGameHealth}, Mid Bonus Health {midGameBonusHealth}, Mid Cycles: {numberOfMidGameCycles}");

            midGameHealth += midGameBonusHealth;
        }

        // LATE GAME
        int numberOfLateGameCycles = 0;
        float lateGameBonusHealth = 0;
        if (i > m_lateGameWave)
        {
            int lateWaveNumber = i - m_lateGameWave;
            lateGameHealth = health * (1 + (m_lateGameFactor * lateWaveNumber)) - health;

            numberOfLateGameCycles = lateWaveNumber / m_cycleLength;
            lateGameBonusHealth = health * (1 + (numberOfLateGameCycles * m_lateGameCycleFactor)) - health;
            //Debug.Log($"Late Base Health: {lateGameHealth}, Late Bonus Health {lateGameBonusHealth}, Late Cycles: {numberOfLateGameCycles}");

            lateGameHealth += lateGameBonusHealth;
        }
        
        float cumHealth = (earlyGameHealth + midGameHealth + lateGameHealth);
        
        //Debug.Log($"Wave: {i}, Total Health {cumHealth}");

        return cumHealth;
    }
}
