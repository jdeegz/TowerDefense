using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Gatherer", menuName = "ScriptableObjects/Gatherer")]
public class GathererData : ScriptableObject
{
    public ResourceManager.ResourceType m_type;
    public string m_gathererName;
    [TextArea(5, 5)]
    public string m_gathererDescription;
    public float m_harvestDuration;
    public float m_storingDuration;
    public int m_carryCapacity;
}
