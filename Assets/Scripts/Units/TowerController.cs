using System.Collections;
using System.Collections.Generic;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using UnityEngine;
using UnityEngine.AI;

public class TowerController : MonoBehaviour
{
    
    [SerializeField] private Transform m_turretPivot;
    [SerializeField] private Transform m_muzzlePoint;
    [SerializeField] private ScriptableTowerDataObject m_towerData;
    [SerializeField] private LayerMask m_layerMask;
    

    private bool m_isBuilt;
    private UnitEnemy m_curTarget;
    private float m_timeUntilFire;
    private float m_facingThreshold = 10f;
    
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

    private void Fire()
    {
        GameObject projectileObj =
            Instantiate(m_towerData.m_projectilePrefab, m_muzzlePoint.position, m_muzzlePoint.rotation);
        Projectile projectileScript = projectileObj.GetComponent<Projectile>();
        projectileScript.SetTarget(m_curTarget.m_targetPoint);
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
        return Vector3.Distance(transform.position, m_curTarget.transform.position) < m_towerData.m_targetRange;
    }

    public void SetupTower()
    {
        GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
        GameplayManager.Instance.AddTowerToList(this);
        gameObject.GetComponent<Collider>().enabled = true;
        gameObject.GetComponent<NavMeshObstacle>().enabled = true;
        m_isBuilt = true;
    }
}