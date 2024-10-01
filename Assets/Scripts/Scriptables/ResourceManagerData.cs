using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu (fileName = "ResourceManagerData", menuName = "ScriptableObjects/ResourceManagerData")]
public class ResourceManagerData : ScriptableObject
{
    
    [Header("Starting Resources")]
    public int m_startingStone;
    public int m_startingWood;
    
    [Header("Ruin Data")]
    public int m_maxRuinDiscovered = 4; //Total number of ruins possible in this mission.
    public int m_minWaves = 9; // The wave that needs to be met before we start displaying indicators for ruins.
    public int m_indicatorFrequency = 6; // The wave that needs to be met before we start displaying indicators for ruins.
    public int m_maxIndicators = 3; // The wave that needs to be met before we start displaying indicators for ruins.

    [Header("Ruin Objects")]
    public GameObject m_ruinIndicatorObj;
    public GameObject m_ruinShrineObj;
    public GameObject m_ruinWellObj;
    public int m_ruinWellFactor = 3; // Every Nth discovered ruin will be a well.
}
