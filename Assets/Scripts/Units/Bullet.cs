using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : Projectile
{
    // Update is called once per frame
    void Update()
    {
        if (m_enemy) m_targetPos = m_enemy.transform.position;
        
        if (CheckTargetDistance())
        {
            DestroyProjectile();
            return;
        }

        TravelToTarget();
    }

    private bool CheckTargetDistance()
    {
        float distanceToTarget = Vector3.Distance(transform.position, m_targetPos);
        return distanceToTarget <= m_stoppingDistance;
    }

    void TravelToTarget()
    {
        float t = m_elapsedTime;

        //Straight line position at this step.
        m_directPos = Vector3.Lerp(m_startPos, m_targetPos, t);

        //Rotation -- Not needed for a basic bullet? As throwing debug.logs each frame.
        //Quaternion lookRotation = Quaternion.LookRotation((m_directPos - transform.position).normalized);
        //transform.rotation = lookRotation;

        transform.position = m_directPos;

        m_elapsedTime += Time.deltaTime * m_projectileSpeed;
    }

    void DestroyProjectile()
    {
        //Deal Damage
        if (m_enemy)
        {
            m_enemy.OnTakeDamage(m_projectileDamage);
            
            //Apply Status Effect
            if (m_statusEffect)
            {
                m_enemy.ApplyEffect(m_statusEffect);
            }
        }

        //Destroy this missile.
        Destroy(gameObject);
    }
}