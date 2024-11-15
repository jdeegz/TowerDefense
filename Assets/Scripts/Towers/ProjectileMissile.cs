using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ProjectileMissile : Projectile
{
    public GameObject m_impactEffect;
    public float m_impactRadius;
    public LayerMask m_areaLayerMask;
    public LayerMask m_raycastLayerMask;
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
        if (!m_isComplete && IsTargetInStoppingDistance())
        {
            DealDamage();
            RemoveProjectile();
        }

        if (m_isFired)
        {
            TravelToTargetFixedUpdate();
        }

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
        //This needs to use the missiles collider center as position instead of the missile position.
        Vector3 explosionPosition = gameObject.GetComponent<Collider>().bounds.center;
        
        //Spawn VFX
        ObjectPoolManager.SpawnObject(m_impactEffect, explosionPosition, Util.GetRandomRotation(Quaternion.identity, new Vector3(0, 180, 0)), null, ObjectPoolManager.PoolType.ParticleSystem);

        //Find affected enemies
        Collider[] hits = Physics.OverlapSphere(explosionPosition, m_impactRadius, m_areaLayerMask.value);
        if (hits.Length <= 0)
        {
            return;
        }

        foreach (Collider col in hits)
        {
            //If the explosion position is within the collider, it's a hit, regardless of shields.
            if (col.bounds.Contains(explosionPosition))
            {
                SendDamage(col);
                continue;
            }
    
            // Use ClosestPoint for ray direction
            Vector3 targetPoint = col.bounds.center;
            Vector3 rayDirection = (targetPoint - explosionPosition).normalized;
            
            
            // Offset ray origin slightly
            Vector3 rayOrigin = explosionPosition;
            Ray ray = new Ray(rayOrigin, rayDirection);

            // Perform RaycastAll
            RaycastHit[] raycastHits = Physics.RaycastAll(ray, m_impactRadius, m_raycastLayerMask.value);
            if (raycastHits.Length == 0)
            {
                continue;
            }
            
            Array.Sort(raycastHits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));
            
            //Check each hit's layer, if we hit a shield before we hit our target (ideally the last item in our list) escape.
            foreach (var hit in raycastHits)
            {
                if (hit.collider.gameObject.layer == m_shieldLayer)
                {
                    // We hit a shield before reaching the target, so exit without dealing damage
                    break;
                }

                if (hit.collider == col)
                {
                    SendDamage(col);
                    break;
                }
            }
        }
    }

    void SendDamage(Collider col)
    {
        EnemyController enemyHit = col.GetComponent<EnemyController>();
        enemyHit.OnTakeDamage(m_projectileDamage);
        ObjectPoolManager.SpawnObject(m_hitVFXPrefab, enemyHit.transform.position, transform.rotation, null, ObjectPoolManager.PoolType.ParticleSystem);

        //Apply Status Effect
        if (m_statusEffectData != null)
        {
            StatusEffect statusEffect = new StatusEffect(m_statusSender, m_statusEffectData);
            enemyHit.ApplyEffect(statusEffect);
        } 
    }


    void OnCollisionEnter(Collision collision)
    {
        if (m_isComplete) return;

        if (collision.collider.gameObject.layer == m_shieldLayer)
        {
            Quaternion spawnVFXdirection = Quaternion.LookRotation(collision.transform.position - m_startPos);
            ObjectPoolManager.SpawnObject(m_hitVFXPrefab, transform.position, spawnVFXdirection, null, ObjectPoolManager.PoolType.ParticleSystem);
            DealDamage();
            RemoveProjectile();
        }
    }
}