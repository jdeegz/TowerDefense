using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class ResourceNode : MonoBehaviour, IResourceNode
{
    public ResourceNodeData m_nodeData;
    [SerializeField] private Animator m_animator;

    private int m_resourcesRemaining;
    [HideInInspector] public ResourceManager.ResourceType m_type;
    public List<HarvestPoint> m_harvestPoints;
    public event Action<ResourceNode> OnResourceNodeDepletion;

    private int m_harvesters;
    private static int m_gatherersHarvestingHash = Animator.StringToHash("gatherersHarvesting");

    void Awake()
    {
        m_resourcesRemaining = m_nodeData.m_maxResources;
        m_type = m_nodeData.m_resourceType;
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

            //Make list of neighbor positions
            ValueTuple<List<Cell>, List<Vector2Int>> vars = Util.GetNeighborHarvestPointCells(Util.GetVector2IntFrom3DPos(transform.position));
            for (var i = 0; i < vars.Item1.Count; ++i)
            {
                var cell = vars.Item1[i];
                HarvestPoint harvestPoint = new HarvestPoint();
                harvestPoint.m_harvestPointCell = cell;
                harvestPoint.m_harvestPointPos = vars.Item2[i];
                m_harvestPoints.Add(harvestPoint);
            }
        }
    }

    public (int, int) RequestResource(int i)
    {
        int resourcesHarvested = 0;
        if (m_resourcesRemaining >= 1)
        {
            //Give the gatherer how much they ask for or all that is remaining.
            resourcesHarvested = Math.Min(i, m_resourcesRemaining);
            m_resourcesRemaining -= resourcesHarvested;
        }

        if (m_resourcesRemaining <= 0)
        {
            //If we hit 0 resources after giving some up, send the gatherer nearby nodes and start the destroy process.
            OnDepletion(true);
        }

        return (resourcesHarvested, m_resourcesRemaining);
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

    private void OnDepletion(bool harvested)
    {
        //If we were harvested, check for Ruins.
        if (m_resourcesRemaining == 0 && ResourceManager.Instance.RequestRuin())
        {
            //We found a ruin!
            Instantiate(ResourceManager.Instance.m_resourceManagerData.m_ruinObj, transform.position, quaternion.identity, transform.parent);
        }
        
        GridCellOccupantUtil.SetOccupant(gameObject, false, 1, 1);
        GridManager.Instance.RefreshGrid();
        OnResourceNodeDepletion?.Invoke(this);
        Destroy(gameObject);
    }

    public ResourceNodeTooltipData GetTooltipData()
    {
        ResourceNodeTooltipData data = new ResourceNodeTooltipData();
        data.m_resourceType = m_nodeData.m_resourceType;
        data.m_resourceNodeName = m_nodeData.m_resourceNodeName;
        data.m_resourceNodeDescription = m_nodeData.m_resourceNodeDescription;
        data.m_maxResources = m_nodeData.m_maxResources;
        data.m_curResources = m_resourcesRemaining;
        return data;
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("ForestRemover"))
        {
            OnDepletion(false);
        }
    }
}

[Serializable]
public class HarvestPoint
{
    public Vector2Int m_harvestPointPos;
    public Cell m_harvestPointCell;
    //public bool m_isOccupied;
    public GathererController m_gatherer;
}

public class ResourceNodeTooltipData
{
    public ResourceManager.ResourceType m_resourceType;
    public string m_resourceNodeName;
    public string m_resourceNodeDescription;
    public int m_maxResources;
    public int m_curResources;
}