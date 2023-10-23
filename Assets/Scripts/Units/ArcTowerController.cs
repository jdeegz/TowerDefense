using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcTowerController : Tower
{
    private float m_timeUntilFire;
    private float m_facingThreshold = 10f;
    private GameObject m_activeProjectileObj;
    private float m_rotationModifier = 1;

    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }

        if (m_towerData.m_hasSecondaryAttack) HandleSecondaryAttack();
        
        if (m_curTarget == null)
        {
            //If target is not in range, destroy the flame cone if there is one.
            if (m_activeProjectileObj != null)
            {
                Destroy(m_activeProjectileObj);
            }

            FindTarget();
            return;
        }

        RotateTowardsTarget();
        m_rotationModifier = IsTargetInSight() ? 0.2f : 1.0f;

        if (!IsTargetInRange(m_curTarget.transform.position))
        {
            m_rotationModifier = 1;
            m_curTarget = null;
        }
        else
        {
            m_timeUntilFire += Time.deltaTime;

            //If we have elapsed time, and are looking at the target, fire.
            if (m_timeUntilFire >= 1f / m_towerData.m_fireRate && IsTargetInSight())
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

    private float m_timeUntilSecondaryFire;

    private void HandleSecondaryAttack()
    {
        m_timeUntilSecondaryFire += Time.deltaTime;

        if (m_timeUntilSecondaryFire >= 1f / m_towerData.m_secondaryfireRate)
        {
            //Reset Counter
            m_timeUntilSecondaryFire = 0f;
            
            //Spawn VFX
            Instantiate(m_towerData.m_secondaryProjectilePrefab, transform.position, Quaternion.identity);
            Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_secondaryfireRange, m_layerMask.value);
            
            //Find enemies and deal damage
            if (hits.Length <= 0) return;
            for (int i = 0; i < hits.Length; ++i)
            {
                // Target is within the cone.
                EnemyController enemyHit = hits[i].transform.GetComponent<EnemyController>();
                enemyHit.OnTakeDamage(m_towerData.m_secondaryDamage);
                
            }
        }
    }

    private void Fire()
    {
        //Get all the enemies in the cone and deal damage to them. VFX is visual layer ontop of that.
        Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_targetRange, m_layerMask.value);
        if (hits.Length <= 0) return;

        for (int i = 0; i < hits.Length; ++i)
        {
            Vector3 direction = hits[i].transform.position - transform.position;
            float coneAngleCosine = Mathf.Cos(Mathf.Deg2Rad * (m_towerData.m_fireConeAngle / 2f));

            if (Vector3.Dot(direction.normalized, m_turretPivot.forward.normalized) > coneAngleCosine && IsTargetInRange(hits[i].transform.position))
            {
                // Target is within the cone.
                EnemyController enemyHit = hits[i].transform.GetComponent<EnemyController>();
                enemyHit.OnTakeDamage(m_towerData.m_baseDamage);
                if (m_statusEffectData)
                {
                    m_statusEffectData.m_sender = this;
                    enemyHit.ApplyEffect(m_statusEffectData);
                }
            }
        }
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

            m_curTarget = hits[closestIndex].transform.GetComponent<EnemyController>();
        }
    }

    private void RotateTowardsTarget()
    {
        float angle = Mathf.Atan2(m_curTarget.transform.position.x - transform.position.x, m_curTarget.transform.position.z - transform.position.z) * Mathf.Rad2Deg;

        Quaternion targetRotation = Quaternion.Euler(new Vector3(0f, angle, 0f));

        m_turretPivot.rotation = Quaternion.RotateTowards(m_turretPivot.transform.rotation, targetRotation,
            m_towerData.m_rotationSpeed * Time.deltaTime * m_rotationModifier);
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