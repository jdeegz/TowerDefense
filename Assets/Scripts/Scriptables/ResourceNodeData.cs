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
    public int m_maxResources;
}
