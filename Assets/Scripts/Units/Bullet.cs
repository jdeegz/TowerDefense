using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : Projectile
{
    void FixedUpdate()
    {
        if (IsTargetInStoppingDistance() && m_isComplete == false)
        {
            m_isComplete = true;
            DealDamage();
            RemoveProjectile();
            return;
        }
        
        TravelToTarget();
    }

    void TravelToTarget()
    {
        transform.position += transform.forward * (m_projectileSpeed * Time.deltaTime);
        
        //Get Direction
        Vector3 direction = m_targetPos - transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    void DealDamage()
    {
        //Deal Damage
        if (m_enemy)
        {
            //Apply Status Effect
            if (m_statusEffect != null)
            {
                m_enemy.ApplyEffect(m_statusEffect);
            }

            //Calculate distance travelled & Damage Falloff.
            float distanceTravelled = Vector3.Distance(transform.position, m_startPos);
            float dmg = m_projectileDamage - distanceTravelled * (m_projectileDamage / 10);
            if (dmg <= 0.0)
            {
                dmg = 0.0f;
            }

            m_enemy.OnTakeDamage(dmg);
            Quaternion spawnVFXdirection = Quaternion.LookRotation(m_targetPos - m_startPos);
            ObjectPoolManager.SpawnObject(m_hitVFXPrefab, m_enemy.m_targetPoint.transform.position, spawnVFXdirection, ObjectPoolManager.PoolType.ParticleSystem);
        }
    }
}