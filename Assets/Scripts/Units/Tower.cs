using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Tower : MonoBehaviour
{
    [SerializeField] protected Transform m_turretPivot;
    [SerializeField] protected Transform m_muzzlePoint;
    [SerializeField] protected ScriptableTowerDataObject m_towerData;
    [SerializeField] protected StatusEffectData m_statusEffectData;
    [SerializeField] protected LayerMask m_layerMask;
    [SerializeField] protected LineRenderer m_towerRangeCircle;
    [SerializeField] protected int m_towerRangeCircleSegments;
    [SerializeField] protected bool m_isBuilt;
    
    protected AudioSource m_audioSource;
    protected EnemyController m_curTarget;
    
    void Awake()
    {
        GameplayManager.OnGameObjectSelected += GameObjectSelected;
        GameplayManager.OnGameObjectDeselected += GameObjectDeselected;
        m_towerRangeCircle.enabled = false;
        SetupRangeCircle(m_towerRangeCircleSegments, m_towerData.m_fireRange);
        m_audioSource = GetComponent<AudioSource>();
    }
    
    private void GameObjectSelected(GameObject obj)
    {
        if (obj == gameObject)
        {
            m_towerRangeCircle.enabled = true;
        }
        else
        {
            m_towerRangeCircle.enabled = false;
        }
    }

    private void GameObjectDeselected(GameObject obj)
    {
        if (obj == gameObject)
        {
            m_towerRangeCircle.enabled = false;
        }
    }
    
    public (int, int) GetTowercost()
    {
        return (m_towerData.m_stoneCost, m_towerData.m_woodCost);
    }
    
    public (int, int) GetTowerSellCost()
    {
        return (m_towerData.m_stoneSellCost, m_towerData.m_woodSellCost);
    }
    
    public void SetupTower()
    {
        GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
        GameplayManager.Instance.AddTowerToList(this);
        gameObject.GetComponent<Collider>().enabled = true;
        gameObject.GetComponent<NavMeshObstacle>().enabled = true;
        m_isBuilt = true;
        
        m_audioSource.PlayOneShot(m_towerData.m_audioBuildClip);
        
    }

    public void OnDestroy()
    {
        if (m_audioSource.enabled)
        {
            m_audioSource.PlayOneShot(m_towerData.m_audioDestroyClip);
        }
        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
        GameplayManager.OnGameObjectDeselected -= GameObjectDeselected;
    }
    
    void SetupRangeCircle(int segments, float radius)
    {
        m_towerRangeCircle.positionCount = segments;
        m_towerRangeCircle.startWidth = 0.15f;
        m_towerRangeCircle.endWidth = 0.15f;
        for(int i = 0; i < segments; ++i)
        {
            float circumferenceProgress = (float) i / segments;
            float currentRadian = circumferenceProgress * 2 * Mathf.PI;
            float xScaled = Mathf.Cos(currentRadian);
            float yScaled = Mathf.Sin(currentRadian);

            float x = xScaled * radius;
            float y = yScaled * radius;

            Vector3 currentPosition = new Vector3(x, 0.25f, y);

            m_towerRangeCircle.SetPosition(i, currentPosition);
        }
    }
}
