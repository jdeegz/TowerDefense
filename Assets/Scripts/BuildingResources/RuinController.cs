using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuinController : MonoBehaviour
{
    public RuinData m_ruinData;
    public GameObject m_ruinChargeVisualObj;
    public GameObject m_ruinIndicatorObj;
    public GameObject m_ruinDiscoveredObj;
    public LayerMask m_resourceNodeLayerMask;

    public int m_ruinWeight = 1;
    public RuinState m_ruinState;
    public enum RuinState
    {
        Idle,
        Hidden, // On Awake
        Indicated, // By Resource Manager
        Discovered, // By Harvesting
        Activated, // Differs per type
    }
    
    private AudioSource m_audioSource;
    private bool m_ruinIsCharged;

    private List<Vector3> m_validPositionsForIndicators;
    private List<Vector3> m_cornerPositions = new List<Vector3>
    {
        new Vector3(0.5f, 0, 0.5f), //NE
        new Vector3(-0.5f, 0, 0.5f), //SE
        new Vector3(-0.5f, 0, -0.5f), //SW
        new Vector3(0.5f, 0, -0.5f) //NW
    };

    /*void Start()
    {
        GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
        GridManager.Instance.RefreshGrid();
        m_ruinIsCharged = true;
        m_ruinChargeVisualObj.SetActive(true);
        m_audioSource = GetComponent<AudioSource>();
        m_audioSource.PlayOneShot(m_ruinData.m_discoveredAudioClip);

        m_audioSource.clip = m_ruinData.m_unclaimedAudioClip;
        m_audioSource.Play();
    }*/

    private void UpdateRuinState(RuinState newState)
    {
        m_ruinState = newState;

        Debug.Log($"{gameObject.name}'s Ruin State is now {m_ruinState}");
    }

    public bool RequestPowerUp()
    {
        if (m_ruinIsCharged)
        {
            m_audioSource.Stop();
            m_ruinChargeVisualObj.SetActive(false);
            m_ruinIsCharged = false;
            m_audioSource.PlayOneShot(m_ruinData.m_chargeConsumedAudioClip);
            return true;
        }
        else
        {
            return false;
        }
    }

    public RuinTooltipData GetTooltipData()
    {
        RuinTooltipData data = new RuinTooltipData();
        data.m_ruinName = m_ruinData.m_ruinName;
        data.m_ruinDescription = m_ruinData.m_ruinDescription;
        data.m_ruinDetails = m_ruinIsCharged ? "Gatherer Level-up Available." : "Level-up Consumed.";
        return data;
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
        ObjectPoolManager.SpawnObject(indicatorObj, indicatorPos, Quaternion.identity, m_ruinIndicatorObj.transform, ObjectPoolManager.PoolType.GameObject);
        
        // Get the cell the ruin is on. Subscribe to the OnDepleted event of the resource node on the cell.
        Cell ruinCell = Util.GetCellFrom3DPos(transform.position);
        
        
    }
}

public class RuinTooltipData
{
    public string m_ruinName;
    public string m_ruinDescription;
    public string m_ruinDetails;
}