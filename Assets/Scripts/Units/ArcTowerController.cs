using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcTowerController : MonoBehaviour
{
    [SerializeField] private Transform m_turretPivot;
    [SerializeField] private Transform m_muzzlePoint;
    [SerializeField] private ScriptableTowerDataObject m_towerData;
    [SerializeField] private LayerMask m_layerMask;


    public bool m_isBuilt;
    private UnitTargetDummy m_curTarget;
    private float m_timeUntilFire;
    private float m_facingThreshold = 10f;
    private AudioSource m_audioSource;
    private GameObject m_activeProjectileObj;

    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }

        if (m_curTarget == null)
        {
            //Look away from base.
            FindTarget();
            return;
        }

        RotateTowardsTarget();

        if (!IsTargetInRange(m_curTarget.transform.position))
        {
            //If target is not in range, destroy the flame cone if there is one.
            if (m_activeProjectileObj != null)
            {
                Destroy(m_activeProjectileObj);
            }

            m_curTarget = null;
        }
        else
        {
            m_timeUntilFire += Time.deltaTime;

            //If we have elapsed time, and are looking at the target, fire.
            Vector3 directionOfTarget = m_curTarget.transform.position - transform.position;
            if (m_timeUntilFire >= 1f / m_towerData.m_fireRate && Vector3.Angle(m_turretPivot.transform.forward, directionOfTarget) <= m_facingThreshold)
            {
                if (m_activeProjectileObj == null)
                {
                    m_activeProjectileObj = Instantiate(m_towerData.m_projectilePrefab, m_muzzlePoint.position, m_muzzlePoint.rotation, m_muzzlePoint.transform);
                }

                //int a = Random.Range(0, m_towerData.m_audioFireClips.Count);
                //m_audioSource.PlayOneShot(m_towerData.m_audioFireClips[0]);
                Fire();
                m_timeUntilFire = 0;
            }
        }
    }

    private void Fire()
    {
        //Get all the enemies in the cone and deal damage to them. VFX is visual layer ontop of that.
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, m_towerData.m_fireRange, transform.forward,
            m_layerMask);

        if (hits.Length <= 0) return;

        for (int i = 0; i < hits.Length; ++i)
        {
            Vector3 direction = hits[i].transform.position - transform.position;
            float coneAngleCosine = Mathf.Cos(Mathf.Deg2Rad * (m_towerData.m_fireConeAngle / 2f));

            if (Vector3.Dot(direction.normalized, m_turretPivot.forward.normalized) > coneAngleCosine && IsTargetInRange(hits[i].transform.position))
            {
                // Target is within the cone.
                UnitTargetDummy enemyHit = hits[i].transform.GetComponent<UnitTargetDummy>();
                enemyHit.TakeDamage(m_towerData.m_baseDamage);
            }
        }
    }

    private void FindTarget()
    {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, m_towerData.m_targetRange, transform.forward,
            m_layerMask);
        float closestDistance = 999;
        int closestIndex = -1;
        if (hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; ++i)
            {
                float distance = Vector3.Distance(transform.position, hits[i].transform.position);
                if (distance <= closestDistance)
                {
                    closestIndex = i;
                    closestDistance = distance;
                }
            }
            m_curTarget = hits[closestIndex].transform.GetComponent<UnitTargetDummy>();
        }
    }

    private void RotateTowardsTarget()
    {
        float angle = Mathf.Atan2(m_curTarget.transform.position.x - transform.position.x, m_curTarget.transform.position.z - transform.position.z) * Mathf.Rad2Deg;

        Quaternion targetRotation = Quaternion.Euler(new Vector3(0f, angle, 0f));

        m_turretPivot.rotation = Quaternion.RotateTowards(m_turretPivot.transform.rotation, targetRotation,
            m_towerData.m_rotationSpeed * Time.deltaTime);
    }

    private bool IsTargetInRange(Vector3 targetPos)
    {
        return Vector3.Distance(transform.position, targetPos) < m_towerData.m_fireRange;
    }
}