using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("Early")]
    [SerializeField] private int m_earlyCycleLength;
    [SerializeField] private int m_earlyCycleCount;
    [SerializeField] private AnimationCurve m_earlyCurve;
    [SerializeField] private float m_earlyCurveMultiplier;

    [Header("Mid")]
    [SerializeField] private int m_midCycleLength;
    [SerializeField] private int m_midCycleCount;
    [SerializeField] private AnimationCurve m_midCurve;
    [SerializeField] private float m_midCurveMultiplier;

    [Header("Late")]
    [SerializeField] private int m_lateCycleLength;
    [SerializeField] private AnimationCurve m_lateCurve;
    [SerializeField] private float m_lateCurveMultiplier;

    [Header("Wave Settings")]
    public float m_healthMultiplier = 0; // Global health modifier. Possibly used for difficulty settings?
    public bool m_delayForQuest = false; // Does this mission contain quests we need to wait for?
    public bool m_allowEndlessMode = true; // Does this mission allow an endless mode?
    public float m_firstBuildDuration = 15; // The first build phase length in seconds.
    public float m_buildDuration = 6; // Subsequent build phase lengths in seconds.
    public float m_afterBossBuildDuration = 30; // Subsequent build phase lengths in seconds.
    public float m_survivalWaveDuration = 45f; // The time between changing the enemy types in survival mode.

    [Header("Equipped Towers")]
    public List<TowerData> m_equippedTowers; // The towers available to the player in this mission.
    public TowerData m_blueprintTower; // Reference to the blue print tower.

    private int m_minute = 0;
    private float m_baseHP = 0f;
    private int m_earlyMinutesCount = 0;
    private int m_midMinutesCount = 0;
    private int m_numberOfEarlyMinutes = 0;
    private int m_minutesRemaining = 0;
    private int m_numberOfMidMinutes = 0;
    private int m_numberOfLateMinutes = 0;
    private float m_earlyHP = 0f;
    private float m_midHP = 0f;
    private float m_lateHP = 0f;
    private float m_totalHP = 0f;

    public float CalculateHealth(float baseHealth)
    {
        m_minute = GameplayManager.Instance.Minute;
        //If health multiplier is less than 1, we scale up to 1 over intro first cycle, else we scale up to health multiplier.
        float healthMultiplier = 1;
        if (m_healthMultiplier < 1)
        {
            healthMultiplier = Mathf.Lerp(m_healthMultiplier, 1, Mathf.Clamp(m_minute / m_earlyCycleLength , 0, 1));
        }
        else
        {
            healthMultiplier = Mathf.Lerp(1, m_healthMultiplier, m_minute);
        }

        m_baseHP = baseHealth * healthMultiplier;

        m_earlyMinutesCount = m_earlyCycleLength * m_earlyCycleCount;
        m_midMinutesCount = m_midCycleLength * m_midCycleCount;

        m_numberOfEarlyMinutes = Math.Min(m_earlyMinutesCount, m_minute); // Give me 0 to 10, then stop at 10.
        m_minutesRemaining = m_minute - m_numberOfEarlyMinutes;
        m_numberOfMidMinutes = Math.Min(m_minutesRemaining, m_midMinutesCount);
        m_numberOfLateMinutes = m_minutesRemaining - m_numberOfMidMinutes;

        m_earlyHP = CalculateTierHP(m_numberOfEarlyMinutes, m_earlyCycleLength, m_baseHP, m_earlyCurveMultiplier, m_earlyCurve);
        m_midHP = m_numberOfMidMinutes > 0 ? CalculateTierHP(m_numberOfMidMinutes, m_midCycleLength, m_earlyHP, m_midCurveMultiplier, m_midCurve) : 0;
        m_lateHP = m_numberOfLateMinutes > 0 ? CalculateTierHP(m_numberOfLateMinutes, m_lateCycleLength, m_midHP, m_lateCurveMultiplier, m_lateCurve) : 0;

        m_totalHP = (m_baseHP + m_earlyHP + m_midHP + m_lateHP);
        return m_totalHP;
    }

    private int m_currentCycle = 0;
    private float m_startCycleHP = 0f;
    private float m_endCycleHP = 0f;
    private float m_cycleProgress = 0f;
    private float m_curveValue = 0f;

    private float CalculateTierHP(int minuteCount, int cycleLength, float baseHP, float multiplier, AnimationCurve curve)
    {
        m_currentCycle = minuteCount / cycleLength; // Start at cycle 0 when waveCount == 0

        m_startCycleHP = baseHP * Mathf.Pow(multiplier, m_currentCycle);
        m_endCycleHP = m_startCycleHP * multiplier;

        m_cycleProgress = (minuteCount % cycleLength) / (float)(cycleLength - 1);

        m_curveValue = curve.Evaluate(m_cycleProgress);

        return Mathf.Lerp(m_startCycleHP, m_endCycleHP, m_curveValue) - baseHP;
    }

    public float CalculateHealth(float baseHealth, int minute)
    {
        m_minute = minute;
        m_baseHP = baseHealth;

        m_earlyMinutesCount = m_earlyCycleLength * m_earlyCycleCount;
        m_midMinutesCount = m_midCycleLength * m_midCycleCount;

        m_numberOfEarlyMinutes = Math.Min(m_earlyMinutesCount, m_minute); // Give me 0 to 10, then stop at 10.
        m_minutesRemaining = m_minute - m_numberOfEarlyMinutes;
        m_numberOfMidMinutes = Math.Min(m_minutesRemaining, m_midMinutesCount);
        m_numberOfLateMinutes = m_minutesRemaining - m_numberOfMidMinutes;

        m_earlyHP = CalculateTierHP(m_numberOfEarlyMinutes, m_earlyCycleLength, m_baseHP, m_earlyCurveMultiplier, m_earlyCurve);
        m_midHP = m_numberOfMidMinutes > 0 ? CalculateTierHP(m_numberOfMidMinutes, m_midCycleLength, m_earlyHP, m_midCurveMultiplier, m_midCurve) : 0;
        m_lateHP = m_numberOfLateMinutes > 0 ? CalculateTierHP(m_numberOfLateMinutes, m_lateCycleLength, m_midHP, m_lateCurveMultiplier, m_lateCurve) : 0;

        m_totalHP = (m_baseHP + m_earlyHP + m_midHP + m_lateHP) * m_healthMultiplier;
        return m_totalHP;
    }

    public TierValues GetTierValues()
    {
        return new TierValues(m_earlyCycleLength, m_earlyCycleCount, m_earlyCurveMultiplier,
            m_midCycleLength, m_midCycleCount, m_midCurveMultiplier,
            m_lateCycleLength, m_lateCurveMultiplier);
    }
}

public class TierValues
{
    public int m_earlyCycleLength;
    public int m_earlyCycleCount;
    public float m_earlyCurveMultiplier;

    public int m_midCycleLength;
    public int m_midCycleCount;
    public float m_midCurveMultiplier;

    public int m_lateCycleLength;
    public float m_lateCurveMultiplier;

    public TierValues(int earlyCycleLength, int earlyCycleCount, float earlyMultiplier, int midCycleLength, int midCycleCount, float midCycleMultiplier, int lateCycleLength, float lateCycleMultiplier)
    {
        m_earlyCycleLength = earlyCycleLength;
        m_earlyCycleCount = earlyCycleCount;
        m_earlyCurveMultiplier = earlyMultiplier;

        m_midCycleLength = midCycleLength;
        m_midCycleCount = midCycleCount;
        m_midCurveMultiplier = midCycleMultiplier;

        m_lateCycleLength = lateCycleLength;
        m_lateCurveMultiplier = lateCycleMultiplier;
    }
}