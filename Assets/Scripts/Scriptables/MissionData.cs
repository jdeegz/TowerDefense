using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionData", menuName = "ScriptableObjects/MissionData")]
public class MissionData : ScriptableObject
{
    [SerializeField] private MissionStats m_missionStats;
    public MissionStats m_BaseMissionStats => m_missionStats;
    public string m_missionScene;
    public string m_missionName;
    public string m_playFableaderboardId;
    
    //Used in Mission
    
    //Used in Menus
    public string m_missionDescription;
    public Sprite m_missionSprite;

}

/// <summary>
/// https://youtu.be/tE1qH8OxO2Y?t=413
/// </summary>
public struct MissionStats
{
    public int m_WaveCount;
}