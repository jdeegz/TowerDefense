
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class BasicTower : Tower
{
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

        if (!IsTargetInRange(m_curTarget.transform.position))
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

    private void Fire()
    {
        GameObject projectileObj = Instantiate(m_towerData.m_projectilePrefab, m_muzzlePoint.position, m_muzzlePoint.rotation);
        Projectile projectileScript = projectileObj.GetComponent<Projectile>();
        if(m_statusEffectData) m_statusEffectData.m_sender = this;
        projectileScript.SetProjectileData(m_curTarget, m_curTarget.m_targetPoint, m_towerData.m_baseDamage, m_muzzlePoint.position, m_statusEffectData);

        int i = Random.Range(0, m_towerData.m_audioFireClips.Count-1);
        m_audioSource.PlayOneShot(m_towerData.m_audioFireClips[i]);
    }

    private void FindTarget()
    {
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
            m_curTarget = hits[closestIndex].transform.GetComponent<UnitEnemy>();
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