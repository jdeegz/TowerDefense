using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : Projectile
{
    // Update is called once per frame
    void Update()
    {
        if (CheckTargetDistance())
        {
            DestroyProjectile();
            return;
        }

        TravelToTarget();
    }

    private bool CheckTargetDistance()
    {
        float distanceToTarget = Vector3.Distance(transform.position, m_target.position);
        return distanceToTarget <= m_stoppingDistance;
    }

    void TravelToTarget()
    {
        float t = m_elapsedTime;

        //Straight line position at this step.
        m_directPos = Vector3.LerpUnclamped(m_startPos, m_target.position, t);

        //Rotation
        Quaternion lookRotation = Quaternion.LookRotation((m_directPos - transform.position).normalized);
        transform.rotation = lookRotation;

        transform.position = m_directPos;

        m_elapsedTime += Time.deltaTime * m_projectileSpeed;
    }

    void DestroyProjectile()
    {
        //Deal Damage
        m_enemy.OnTakeDamage(m_projectileDamage);

        //Destroy this missile.
        Destroy(gameObject);
    }
}