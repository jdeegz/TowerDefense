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
    public List<ProgressionUnlockableData> m_missionUnlockables;
}
