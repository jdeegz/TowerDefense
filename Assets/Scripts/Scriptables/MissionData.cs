using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionData", menuName = "ScriptableObjects/MissionData")]
public class MissionData : ScriptableObject
{
    [Header("Scene Data")]
    [SerializeField] private MissionStats m_missionStats;
    public MissionStats m_BaseMissionStats => m_missionStats;
    public string m_missionScene;
    
    //Used in Menus
    [Header("Menu Data")]
    public string m_missionName;
    public string m_missionDescription;
    public Sprite m_missionSprite;
    
    [Header("Testing Data")]
    // For testing missions
    public bool m_isUnlockedByDefault;
}

/// <summary>
/// https://youtu.be/tE1qH8OxO2Y?t=413
/// </summary>
public struct MissionStats
{
    public int m_WaveCount;
}