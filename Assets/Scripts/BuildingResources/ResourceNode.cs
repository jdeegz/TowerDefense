
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ResourceNode : MonoBehaviour, IResourceNode
{
    
    public ResourceManager.ResourceType m_type;
    [SerializeField] private int m_resourcesRemaining;
    public event Action<ResourceNode> OnResourceNodeDepletion;
    

    void Start()
    {
    }

    public (int, List<ResourceNode>) RequestResource(int i)
    {
        int resourcesHarvested = 0;
        List<ResourceNode> nearByNodes = null;
        if (m_resourcesRemaining > 0)
        {
            //Give the gatherer how much they ask for or all that is remaining.
            int giveValue = Math.Min(i, m_resourcesRemaining);
            m_resourcesRemaining -= giveValue;
        }

        if (m_resourcesRemaining <= 0)
        {
            //If we hit 0 resources after giving some up, send the gatherer nearby nodes and start the destroy process.
            OnDepletion();
        }

        return (resourcesHarvested, nearByNodes);
    }
    

    private void OnDepletion()
    {
        OnResourceNodeDepletion?.Invoke(this);
        Destroy(gameObject);
    }
}

