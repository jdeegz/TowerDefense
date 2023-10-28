using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Tower : MonoBehaviour
{
    [SerializeField] protected Transform m_turretPivot;
    [SerializeField] protected Transform m_muzzlePoint;
    [SerializeField] protected TowerData m_towerData;
    [SerializeField] protected StatusEffectData m_statusEffectData;
    [SerializeField] protected LayerMask m_layerMask;
    [SerializeField] protected LineRenderer m_towerRangeCircle;
    [SerializeField] protected int m_towerRangeCircleSegments;
    [SerializeField] protected bool m_isBuilt;
    
    protected AudioSource m_audioSource;
    protected EnemyController m_curTarget;
    
    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayStatChanged;
        GameplayManager.OnGameObjectSelected += GameObjectSelected;
        GameplayManager.OnGameObjectDeselected += GameObjectDeselected;
        m_towerRangeCircle.enabled = false;
        SetupRangeCircle(m_towerRangeCircleSegments, m_towerData.m_fireRange);
        m_audioSource = GetComponent<AudioSource>();
    }

    private void GameplayStatChanged(GameplayManager.GameplayState newState)
    {
        
    }
    
    public void RotateTowardsTarget()
    {
        Quaternion targetRotation = new Quaternion();

        if (m_curTarget)
        {
            float angle = Mathf.Atan2(m_curTarget.transform.position.x - transform.position.x, m_curTarget.transform.position.z - transform.position.z) * Mathf.Rad2Deg;
            targetRotation = Quaternion.Euler(new Vector3(0f, angle, 0f));
        }

        //If we have no target, rotate away from the base during the Build phase. The isBuilt flag will stop this from happening when precon.
        if(GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.Build)
        {
            //Use enemy Goal as the 'target'.
            Vector3 direction = GameplayManager.Instance.m_enemyGoal.position - transform.position;

            // Calculate the rotation angle to make the new object face away from the target.
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 180f;
            targetRotation = Quaternion.Euler(0, angle, 0);
        }

        m_turretPivot.rotation = Quaternion.RotateTowards(m_turretPivot.transform.rotation, targetRotation,
            m_towerData.m_rotationSpeed * Time.deltaTime);
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

    public TowerData GetTowerData()
    {
        return m_towerData;
    }
    
    public void SetupTower()
    {
        //Grid
        GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
        GameplayManager.Instance.AddTowerToList(this);
        
        //Operational
        gameObject.GetComponent<Collider>().enabled = true;
        gameObject.GetComponent<NavMeshObstacle>().enabled = true;
        m_isBuilt = true;
        
        //Audio
        m_audioSource.PlayOneShot(m_towerData.m_audioBuildClip);
        
        //VFX
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

    public Quaternion GetTurretRotation()
    {
        return m_turretPivot.rotation;
    }

    public void SetTurretRotation(Quaternion rot)
    {
        m_turretPivot.rotation = rot;
    }

    public Transform GetTurretTransform()
    {
        return m_turretPivot;
    }
}
