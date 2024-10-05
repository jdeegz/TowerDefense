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
    public int m_maxRuinDiscovered = 4;     // Total number of ruins possible in this mission.
    public int m_minWaves = 9;              // The wave that needs to be met before we start displaying indicators for ruins.
    public int m_indicatorFrequency = 6;    // How many waves need to be defeated between each indicator display.
    public int m_maxIndicators = 3;         // The maximum number of indicators active at any time.

    [Header("Ruin Objects")]
    public List<Ruin> m_ruinTypes;
    public GameObject m_ruinIndicatorObj;
}
