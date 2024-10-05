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

    private void UpdateRuinState(RuinState newState)
    {
        m_ruinState = newState;

        Debug.Log($"{gameObject.name}'s Ruin State is now {m_ruinState}");
    }

    public List<Vector3> GetValidRuinCorners()
    {
        return m_validPositionsForIndicators;
    }
    
    public List<Vector3> CheckValidRuinCorners()
    {
        Cell ruinCell = Util.GetCellFrom3DPos(transform.position);
        Vector3 cellPos = new Vector3(ruinCell.m_cellPos.x, 0, ruinCell.m_cellPos.y);
        m_validPositionsForIndicators = new List<Vector3>();

        foreach (Vector3 cornerPosition in m_cornerPositions)
        {
            Vector3 pos = cellPos + cornerPosition;
            Collider[] hits = Physics.OverlapSphere(pos, 0.5f, m_resourceNodeLayerMask);
            
            if (hits.Length == 4)
            {
                m_validPositionsForIndicators.Add(pos); // This corner is good.
            }
        }

        return m_validPositionsForIndicators;
    }

    public void IndicateThisRuin()
    {
        // Update ruin controller state.
        Debug.Log($"This ruin has been indicated by Resource Manager.");
        UpdateRuinState(RuinState.Indicated);

        // Spawn the indicator object at the desired corner.
        int i = Random.Range(0, m_validPositionsForIndicators.Count);
        Debug.Log($"Choosing the corner of {m_validPositionsForIndicators[i]} to place Indicator.");
        GameObject indicatorObj = ResourceManager.Instance.m_resourceManagerData.m_ruinIndicatorObj;
        Vector3 indicatorPos = m_validPositionsForIndicators[i];
        indicatorPos.y += 1.8f;
        m_indicatorObj = ObjectPoolManager.SpawnObject(indicatorObj, indicatorPos, Quaternion.identity, m_ruinIndicatorRoot.transform, ObjectPoolManager.PoolType.GameObject);
        
        // Get the cell the ruin is on. Subscribe to the OnDepleted event of the resource node on the cell.
        m_ruinCell = Util.GetCellFrom3DPos(transform.position);
        m_ruinCell.m_cellResourceNode.OnResourceNodeDepletion += ResourceNodeDepleted;
        ResourceManager.OnAllRuinsDiscovered += AllRuinsDiscovered;
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
        
        ResourceManager.OnAllRuinsDiscovered -= AllRuinsDiscovered;
        m_ruinCell.m_cellResourceNode.OnResourceNodeDepletion -= ResourceNodeDepleted;
    }

    private void ResourceNodeDepleted(ResourceNode obj)
    {
        // We've discovered the ruin.
        m_ruinCell.m_cellResourceNode.OnResourceNodeDepletion -= ResourceNodeDepleted;
        UpdateRuinState(RuinState.Discovered);

        GameObject ruinObj = ObjectPoolManager.SpawnObject(ResourceManager.Instance.RuinDiscovered(), transform.position, Quaternion.identity, m_ruinDiscoveredRoot.transform, ObjectPoolManager.PoolType.GameObject);
        m_ruinObj = ruinObj.GetComponent<Ruin>();
        
        ObjectPoolManager.ReturnObjectToPool(m_indicatorObj, ObjectPoolManager.PoolType.GameObject);
        m_indicatorObj = null;
        
        ResourceManager.OnAllRuinsDiscovered -= AllRuinsDiscovered;
    }

    void OnDestroy()
    {
        if (m_ruinCell != null)
        {
            m_ruinCell.m_cellResourceNode.OnResourceNodeDepletion -= ResourceNodeDepleted;
        }
        
        ResourceManager.OnAllRuinsDiscovered -= AllRuinsDiscovered;
    }
}