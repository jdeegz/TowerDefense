using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Missile : Projectile
{
    public GameObject m_impactEffect;
    public float m_impactRadius;
    public LayerMask m_layerMask;
    public float m_lookSpeed = 10;
    public float m_lookAcceleration = 5;
    public float m_speedAcceleration = 0.3f;

    void Update()
    {
        if (m_enemy)
        {
            m_targetPos = m_enemy.m_targetPoint.position;
        }
        
        if (IsTargetInStoppingDistance() && m_isFired)
        {
            DestroyProjectile();
        }
        
    }

    void FixedUpdate()
    {
        if (m_isFired) TravelToTarget();
    }

    private bool IsTargetInStoppingDistance()
    {
        float distanceToTarget = Vector3.Distance(transform.position, m_targetPos);
        return distanceToTarget <= m_stoppingDistance;
    }

    void TravelToTarget()
    {
        //Rotate towards Target.
        Vector3 direction = m_targetPos - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, m_lookSpeed * Time.deltaTime);

        //Move Forward.
        transform.position += transform.forward * (m_projectileSpeed * Time.deltaTime);

        //Increase Lookspeed (greatly)
        var lookStep = (m_lookAcceleration + Time.deltaTime);
        m_lookSpeed += lookStep;

        //Increase Move Speed up to 50%
        var speedStep = (m_speedAcceleration + Time.deltaTime);
        m_projectileSpeed += speedStep;
    }

    void DestroyProjectile()
    {
        //Deal Damage
        Vector3 searchPos = new Vector3(transform.position.x, 0, transform.position.z);
        Collider[] hits = Physics.OverlapSphere(searchPos, m_impactRadius, m_layerMask.value);
        if (hits.Length > 0)
        {
            foreach (Collider col in hits)
            {
                EnemyController enemyHit = col.GetComponent<EnemyController>();
                enemyHit.OnTakeDamage(m_projectileDamage);

                //Apply Status Effect
                if (m_statusEffect != null)
                {
                    enemyHit.ApplyEffect(m_statusEffect);
                }
            }
        }

        //Spawn VFX
        Vector3 groundPos = transform.position;
        Instantiate(m_impactEffect, groundPos, Quaternion.identity);

        //Destroy this missile.
        Destroy(gameObject);
    }
}