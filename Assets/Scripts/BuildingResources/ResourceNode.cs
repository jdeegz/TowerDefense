
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
    

    public int RequestResource(int i)
    {
        int resourcesHarvested = 0;
        if (m_resourcesRemaining > 0)
        {
            //Give the gatherer how much they ask for or all that is remaining.
            resourcesHarvested = Math.Min(i, m_resourcesRemaining);
            m_resourcesRemaining -= resourcesHarvested;
        }

        if (m_resourcesRemaining <= 0)
        {
            //If we hit 0 resources after giving some up, send the gatherer nearby nodes and start the destroy process.
            OnDepletion();
        }
        return resourcesHarvested;
    }
    

    private void OnDepletion()
    {
        OnResourceNodeDepletion?.Invoke(this);
        Destroy(gameObject);
    }
}

