using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileBullet : Projectile
{
    void FixedUpdate()
    {
        if (!m_isComplete && IsTargetInStoppingDistance())
        {
            RemoveProjectile();
        }

        if (!m_isComplete && !m_enemy)
        {
            RemoveProjectile();
        }

        if(!m_isComplete) TravelToTargetFixedUpdate();
        
        if (m_enemy)
        {
            m_targetPos = m_enemy.m_targetPoint.position;
            
            if (m_enemy.GetCurrentHP() <= 0)
            {
                m_enemy = null;
            }
        }
    }

    void TravelToTargetFixedUpdate()
    {
        transform.position += transform.forward * (m_projectileSpeed * Time.fixedDeltaTime);

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
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (/*collision.collider == null && */ m_enemy == null) return;
        
        if (m_isComplete) return;
        
        // Also do damage if we hit our target.
        if (collision.gameObject == m_enemy.gameObject)
        {
            DealDamage();
        }
        
        if (collision.collider.gameObject.layer == m_shieldLayer || collision.gameObject == m_enemy.gameObject)
        {
            Quaternion spawnVFXdirection = Quaternion.LookRotation(collision.transform.position - m_startPos);
            ObjectPoolManager.SpawnObject(m_hitVFXPrefab, transform.position, spawnVFXdirection, null, ObjectPoolManager.PoolType.ParticleSystem);
            RemoveProjectile();
        }

        
    }
}