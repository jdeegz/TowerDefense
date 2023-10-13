using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Gatherer", menuName = "ScriptableObjects/Gatherer")]
public class GathererData : ScriptableObject
{
    public ResourceManager.ResourceType m_type;
    public float m_harvestDuration;
    public float m_storingDuration;
    public int m_carryCapacity;
}
