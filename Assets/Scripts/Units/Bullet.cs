using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : Projectile
{
    void Awake()
    {
        m_trail = GetComponent<TrailRenderer>();
    }

    void OnEnable()
    {
        ConfigureTrail();
    }

    private void ConfigureTrail()
    {
        if (m_trail != null && m_bulletTrailData != null)
        {
            m_bulletTrailData.SetupTrail(m_trail);
        }
    }
    
    void Update()
    {
        if (m_enemy) m_targetPos = m_enemy.m_targetPoint.position;
    }

    void FixedUpdate()
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
        float distanceToTarget = Vector3.Distance(transform.position, m_targetPos);
        return distanceToTarget <= m_stoppingDistance;
    }

    void TravelToTarget()
    {
        float t = m_elapsedTime;

        //Straight line position at this step.
        m_directPos = Vector3.Lerp(m_startPos, m_targetPos, t);

        transform.position = m_directPos;
        
        //Get Direction
        Vector3 direction = m_targetPos - transform.position;
        transform.rotation = Quaternion.LookRotation(direction);

        m_elapsedTime += Time.deltaTime * m_projectileSpeed;
    }

    void DestroyProjectile()
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
            //Debug.Log($"Bullet Travelled: {distanceTravelled} & Dealt Damage: {dmg}");
        }

        //Destroy this missile.
        //Adding a delay for the trail to complete-ish
        m_renderer.enabled = false;
        float destroyDelay = 0.1f;
        if (m_trail != null && m_bulletTrailData != null)
        {
            destroyDelay = m_bulletTrailData.m_time;
        }
        Destroy(gameObject, destroyDelay);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.collider == m_enemyCollider)
        {
            DestroyProjectile();
        }
    }
}