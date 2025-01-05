using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RuinController : MonoBehaviour
{
    public GameObject m_ruinIndicatorRoot;
    public GameObject m_ruinDiscoveredRoot;
    public LayerMask m_resourceNodeLayerMask;

    public int m_ruinWeight = 1;
    public RuinState m_ruinState;
    public enum RuinState
    {
        Idle,
        Hidden,         // On Awake
        Indicated,      // By Resource Manager
        Discovered,     // By Harvesting
        Activated,      // Differs per type
    }
    
    private List<Vector3> m_validPositionsForIndicators;
    private List<Vector3> m_cornerPositions = new List<Vector3>
    {
        new Vector3(0.5f, 0, 0.5f),     // NE
        new Vector3(-0.5f, 0, 0.5f),    // SE
        new Vector3(-0.5f, 0, -0.5f),   // SW
        new Vector3(0.5f, 0, -0.5f)     // NW
    };

    private Cell m_ruinCell;
    private GameObject m_indicatorObj;
    private Ruin m_ruinObj;

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.FloodFillGrid)
        {
            // Get the cell the ruin is on. Subscribe to the OnDepleted event of the resource node on the cell.
            /*m_ruinCell = Util.GetCellFrom3DPos(transform.position);
            m_ruinCell.m_cellResourceNode.OnResourceNodeDepletion += ResourceNodeDepleted;*/
            ResourceManager.OnAllRuinsDiscovered += AllRuinsDiscovered;
            //Debug.Log($"{gameObject.name} subscribed to Cell {m_ruinCell.m_cellIndex}'s resource node.");
        }
    }
    
    private void UpdateRuinState(RuinState newState)
    {
        m_ruinState = newState;

        //Debug.Log($"{gameObject.name}'s Ruin State is now {m_ruinState}");
    }

    public List<Vector3> GetValidRuinCorners()
    {
        return m_validPositionsForIndicators;
    }

    public bool IsRuinCoveredByForest()
    {
        m_ruinCell = Util.GetCellFrom3DPos(transform.position);
        return m_ruinCell.m_cellResourceNode; 
    }

    public void IndicateThisRuin()
    {
        // Update ruin controller state.
        //Debug.Log($"This ruin has been indicated by Resource Manager.");
        UpdateRuinState(RuinState.Indicated);
        
        //Get the resource node in this cell and remove it.
        m_ruinCell = Util.GetCellFrom3DPos(transform.position);
        
        // Spawn the indicator object at the desired corner.
        GameObject indicatorObj = ResourceManager.Instance.m_resourceManagerData.m_ruinIndicatorObj;
        m_indicatorObj = ObjectPoolManager.SpawnObject(indicatorObj, gameObject.transform.position, Quaternion.identity, m_ruinIndicatorRoot.transform, ObjectPoolManager.PoolType.GameObject);
        GridCellOccupantUtil.SetOccupant(m_indicatorObj, true, 1, 1);
        m_indicatorObj.GetComponent<RuinIndicator>().SetUpRuinIndicator(this);
    }

    private void AllRuinsDiscovered()
    {
        if (m_ruinState != RuinState.Indicated)
        {
            return;
        }
        
        UpdateRuinState(RuinState.Idle);
        ObjectPoolManager.ReturnObjectToPool(m_indicatorObj, ObjectPoolManager.PoolType.GameObject);
        m_indicatorObj = null;
    }

    public void GathererDiscoveredRuin()
    {
        // The node was harvested, but we're not indicated so we're not discovered.
        if (m_ruinState != RuinState.Indicated) return;
        
        // We've discovered the ruin.
        UpdateRuinState(RuinState.Discovered);

        GameObject ruinObj = ObjectPoolManager.SpawnObject(ResourceManager.Instance.RuinDiscovered(), transform.position, Quaternion.identity, m_ruinDiscoveredRoot.transform, ObjectPoolManager.PoolType.GameObject);
        m_ruinObj = ruinObj.GetComponent<Ruin>();
        
        ObjectPoolManager.ReturnObjectToPool(m_indicatorObj, ObjectPoolManager.PoolType.GameObject);
        m_indicatorObj = null;
    }

    private void ResourceNodeDepleted(ResourceNode obj)
    {
        // The node was harvested, but we're not indicated so we're not discovered.
        if (m_ruinState != RuinState.Indicated) return;
        
        // We've discovered the ruin.
        UpdateRuinState(RuinState.Discovered);

        GameObject ruinObj = ObjectPoolManager.SpawnObject(ResourceManager.Instance.RuinDiscovered(), transform.position, Quaternion.identity, m_ruinDiscoveredRoot.transform, ObjectPoolManager.PoolType.GameObject);
        m_ruinObj = ruinObj.GetComponent<Ruin>();
        
        ObjectPoolManager.ReturnObjectToPool(m_indicatorObj, ObjectPoolManager.PoolType.GameObject);
        m_indicatorObj = null;
    }

    void OnDestroy()
    {
        //m_ruinCell.m_cellResourceNode.OnResourceNodeDepletion -= ResourceNodeDepleted;
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
        ResourceManager.OnAllRuinsDiscovered -= AllRuinsDiscovered;
    }
}