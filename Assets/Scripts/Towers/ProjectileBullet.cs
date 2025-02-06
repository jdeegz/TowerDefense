using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileBullet : Projectile
{
    void FixedUpdate()
    {
        if (m_isComplete) return;
        
        if (IsTargetInStoppingDistance())
        {
            DealDamage();
            RemoveProjectile();
        }

        if (!m_isComplete && !m_enemy)
        {
            RemoveProjectile();
        }

        TravelToTargetFixedUpdate();
        
        if (m_enemy)
        {
            m_targetPos = m_enemy.m_targetPoint.position;
            
            if (m_enemy.GetCurrentHP() <= 0 || m_enemy.m_isTeleporting)
            {
                m_enemy = null;
            }
        }
    }

    private Vector3 m_directionThisFrame;
    void TravelToTargetFixedUpdate()
    {
        transform.position += transform.forward * (m_projectileSpeed * Time.fixedDeltaTime);

        //Get Direction
        m_directionThisFrame = m_targetPos - transform.position;
        transform.rotation = Quaternion.LookRotation(m_directionThisFrame);
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

    private Quaternion m_spawnVFXDirection;
    void OnCollisionEnter(Collision collision)
    {
        if (m_isComplete) return;
        
        if (collision.collider.gameObject.layer == m_shieldLayer)
        {
            m_spawnVFXDirection = Quaternion.LookRotation(collision.transform.position - m_startPos);
            ObjectPoolManager.SpawnObject(m_hitVFXPrefab, transform.position, m_spawnVFXDirection, null, ObjectPoolManager.PoolType.ParticleSystem);
            RemoveProjectile();
        }

        
    }
}