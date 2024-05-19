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

    private float m_storedProjectileSpeed;
    private float m_storedLookSpeed;

    void Awake()
    {
        m_storedProjectileSpeed = m_projectileSpeed;
        m_storedLookSpeed = m_lookSpeed;
    }

    void OnEnable()
    {
        //Reset Data
        m_projectileSpeed = m_storedProjectileSpeed;
        m_lookSpeed = m_storedLookSpeed;
        if (m_renderer) m_renderer.enabled = true;
    }

    void FixedUpdate()
    {
        if (IsTargetInStoppingDistance() && m_isFired == true && m_isComplete == false)
        {
            m_isComplete = true;
            DealDamage();
            RemoveProjectile();
            return;
        }
        
        //if we're at our target, we dont need to move any longer, we've exploded.
        if (m_isFired && m_isComplete == false) TravelToTarget();
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

    void DealDamage()
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
        ObjectPoolManager.SpawnObject(m_impactEffect, groundPos, Quaternion.identity, ObjectPoolManager.PoolType.ParticleSystem);
    }
}