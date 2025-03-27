using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionData", menuName = "ScriptableObjects/MissionData")]
public class MissionData : ScriptableObject
{
    [Header("Scene Data")]
    public string m_missionScene;
    
    //Used in Menus
    [Header("Menu Data")]
    public string m_missionID; // Code Facing
    public string m_missionName; // Player Facing
    public string m_missionDescription;
    public Sprite m_missionSprite;
    public int m_missionPlacement = 000;
    
    [Header("Testing Data")]
    // For testing missions
    public bool m_isUnlockedByDefault;
    
    [Header("Progression - Requirements")]
    public List<ProgressionUnlockableData> m_unlockRequirements;
    
    [Header("Progression - Unlockables")]
    public List<ProgressionUnlockableData> m_unlockableRewards;
}