
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class TowerController : MonoBehaviour
{
    
    [SerializeField] private Transform m_turretPivot;
    [SerializeField] private Transform m_muzzlePoint;
    [SerializeField] private ScriptableTowerDataObject m_towerData;
    [SerializeField] private LayerMask m_layerMask;
    [SerializeField] private LineRenderer m_towerRangeCircle;
    [SerializeField] private int m_towerRangeCircleSegments;
    

    private bool m_isBuilt;
    private UnitEnemy m_curTarget;
    private float m_timeUntilFire;
    private float m_facingThreshold = 10f;
    private AudioSource m_audioSource;
    
    

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
    
    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt) { return; }
        
        if (m_curTarget == null)
        {
            FindTarget();
            return;
        }

        RotateTowardsTarget();

        if (!IsTargetInRange())
        {
            m_curTarget = null;
        }
        else
        {
            m_timeUntilFire += Time.deltaTime;

            //If we have elapsed time, and are looking at the target, fire.
            Vector3 directionOfTarget = m_curTarget.transform.position - transform.position;
            if (m_timeUntilFire >= 1f / m_towerData.m_fireRate && Vector3.Angle(m_turretPivot.transform.forward, directionOfTarget) <= m_facingThreshold)
            {
                Fire();
                m_timeUntilFire = 0;
            }
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

    private void Fire()
    {
        GameObject projectileObj =
            Instantiate(m_towerData.m_projectilePrefab, m_muzzlePoint.position, m_muzzlePoint.rotation);
        Projectile projectileScript = projectileObj.GetComponent<Projectile>();
        projectileScript.SetTarget(m_curTarget.m_targetPoint);

        int i = Random.Range(0, m_towerData.m_audioFireClips.Count-1);
        m_audioSource.PlayOneShot(m_towerData.m_audioFireClips[i]);
    }

    private void FindTarget()
    {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, m_towerData.m_targetRange, transform.forward,
            m_layerMask);

        if (hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; ++i)
            {
                if (hits[i].transform.CompareTag("Enemy"))
                {
                    m_curTarget = hits[i].transform.GetComponent<UnitEnemy>();
                    //Just gimmie the first and gtfo. Can refine later.
                    break;
                }
            }
        }
    }

    private void RotateTowardsTarget()
    {
        float angle = Mathf.Atan2(m_curTarget.transform.position.x - transform.position.x, m_curTarget.transform.position.z - transform.position.z) * Mathf.Rad2Deg;

        Quaternion targetRotation = Quaternion.Euler(new Vector3(0f, angle, 0f));

        m_turretPivot.rotation = Quaternion.RotateTowards(m_turretPivot.transform.rotation, targetRotation,
            m_towerData.m_rotationSpeed * Time.deltaTime);
    }

    private bool IsTargetInRange()
    {
        return Vector3.Distance(transform.position, m_curTarget.transform.position) < m_towerData.m_fireRange;
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
        m_audioSource.PlayOneShot(m_towerData.m_audioDestroyClip);
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