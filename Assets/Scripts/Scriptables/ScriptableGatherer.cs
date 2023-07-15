using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Gatherer", menuName = "Gatherers/Gatherer")]
public class ScriptableGatherer : ScriptableObject
{
    public ResourceManager.ResourceType m_type;
    public float m_harvestDuration;
    public float m_storingDuration;
    public int m_carryCapacity;
}
