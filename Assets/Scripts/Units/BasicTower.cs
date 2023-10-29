using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class BasicTower : Tower
{
    private float m_timeUntilFire;
    private float m_facingThreshold = 10f;
    private int m_shotsFired;
    private float m_timeUntilBurst;

    void Start()
    {
        m_timeUntilFire = 999f;
        m_timeUntilBurst = 999f;
    }

    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }

        RotateTowardsTarget();

        m_timeUntilFire += Time.deltaTime;
        m_timeUntilBurst += Time.deltaTime;

        if (m_curTarget == null)
        {
            m_shotsFired = 0;
            FindTarget();
            return;
        }

        if (!IsTargetInRange(m_curTarget.transform.position))
        {
            m_curTarget = null;
        }
        else
        {
            //If we have elapsed time, and are looking at the target, fire.
            if (m_timeUntilFire >= 1f / m_towerData.m_fireRate && m_timeUntilBurst >= m_towerData.m_burstFireRate &&
                IsTargetInSight())
            {
                Fire();
                m_timeUntilFire = 0;
                ++m_shotsFired;

                //Reset Burst Fire counters
                if (m_shotsFired >= m_towerData.m_burstSize)
                {
                    m_timeUntilBurst = 0;
                    m_shotsFired = 0;
                }
            }
        }
    }

    private void Fire()
    {
        GameObject projectileObj = Instantiate(m_towerData.m_projectilePrefab, m_muzzlePoint.position, m_muzzlePoint.rotation);
        Projectile projectileScript = projectileObj.GetComponent<Projectile>();
        projectileScript.SetProjectileData(m_curTarget, m_curTarget.m_targetPoint, m_towerData.m_baseDamage, m_muzzlePoint.position, m_modifiedStatusEffectData);

        int i = Random.Range(0, m_towerData.m_audioFireClips.Count - 1);
        m_audioSource.PlayOneShot(m_towerData.m_audioFireClips[i]);
    }

    private void FindTarget()
    {
        m_curTarget = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_targetRange, m_layerMask.value);
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

            m_curTarget = hits[closestIndex].transform.GetComponent<EnemyController>();
        }
    }

    private bool IsTargetInSight()
    {
        Vector3 directionOfTarget = m_curTarget.transform.position - transform.position;
        return Vector3.Angle(m_turretPivot.transform.forward, directionOfTarget) <= m_facingThreshold;
    }


    private bool IsTargetInRange(Vector3 targetPos)
    {
        return Vector3.Distance(transform.position, targetPos) < m_towerData.m_fireRange;
    }
}