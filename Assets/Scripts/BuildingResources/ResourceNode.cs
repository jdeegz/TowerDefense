using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ResourceNode : MonoBehaviour, IResourceNode
{
    [SerializeField] private int m_resourcesRemaining;
    [SerializeField] private Animator m_animator;

    public ResourceManager.ResourceType m_type;
    public List<HarvestPoint> m_harvestPoints = new List<HarvestPoint>();

    public event Action<ResourceNode> OnResourceNodeDepletion;

    private int m_harvesters;
    private static int m_gatherersHarvestingHash = Animator.StringToHash("gatherersHarvesting");

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.PlaceObstacles)
        {
            GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
        }
    }

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

    public bool HasResources()
    {
        return m_resourcesRemaining > 0;
    }

    public void WasSelected()
    {
        m_animator.SetTrigger("isSelected");
    }

    public void SetIsHarvesting(int i)
    {
        m_harvesters += i;
        m_animator.SetInteger(m_gatherersHarvestingHash, m_harvesters);
    }

    private void OnDepletion()
    {
        GridCellOccupantUtil.SetOccupant(gameObject, false, 1, 1);
        GridManager.Instance.ResourceNodeRemoved();
        OnResourceNodeDepletion?.Invoke(this);
        Destroy(gameObject);
    }
}

[Serializable]
public class HarvestPoint
{
    public Transform m_transform;
    public bool m_isOccupied;
    public GathererController m_gatherer;
}