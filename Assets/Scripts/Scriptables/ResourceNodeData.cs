using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResourceNodeData", menuName = "ScriptableObjects/ResourceNodeData")]
public class ResourceNodeData : ScriptableObject
{
    public ResourceManager.ResourceType m_resourceType;
    public string m_resourceNodeName;
    [TextArea(5, 5)]
    public string m_resourceNodeDescription;

    [Header("Resource Data")]
    public bool m_rewardsResources = true;
    public bool m_limitedCount;
    public int m_maxResources;

    [Header("Refresh Type Data")]
    public bool m_refreshResources; // Should this tree refresh?
    public float m_refreshRate; // How often should it refresh, in seconds.
    public int m_refreshQuantity; // How many resources refresh each refresh.
}
