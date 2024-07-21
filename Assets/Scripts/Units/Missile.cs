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
        if (m_isFired && m_isComplete == false) TravelToTargetFixedUpdate();
    }

    void TravelToTargetFixedUpdate()
    {
        //Rotate towards Target.
        Vector3 direction = m_targetPos - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, m_lookSpeed * Time.fixedDeltaTime);

        //Move Forward.
        transform.position += transform.forward * (m_projectileSpeed * Time.fixedDeltaTime);

        //Increase Lookspeed (greatly)
        var lookStep = (m_lookAcceleration + Time.fixedDeltaTime);
        m_lookSpeed += lookStep;

        //Increase Move Speed up to 50%
        var speedStep = (m_speedAcceleration + Time.fixedDeltaTime);
        m_projectileSpeed += speedStep;
    }

    void DealDamage()
    {
        //Deal Damage
        Collider[] hits = Physics.OverlapSphere(m_targetPos, m_impactRadius, m_layerMask.value);
        if (hits.Length > 0)
        {
            foreach (Collider col in hits)
            {
                EnemyController enemyHit = col.GetComponent<EnemyController>();
                enemyHit.OnTakeDamage(m_projectileDamage);
                ObjectPoolManager.SpawnObject(m_hitVFXPrefab, enemyHit.transform.position, transform.rotation, ObjectPoolManager.PoolType.ParticleSystem);

                //Apply Status Effect
                if (m_statusEffect != null)
                {
                    enemyHit.ApplyEffect(m_statusEffect);
                }
            }
        }

        //Spawn VFX
        ObjectPoolManager.SpawnObject(m_impactEffect, m_targetPos, Util.GetRandomRotation(Quaternion.identity, new Vector3(0,180,0)), ObjectPoolManager.PoolType.ParticleSystem);
    }
}