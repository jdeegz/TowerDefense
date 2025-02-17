using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Profiling;
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
    private Vector3 m_direction;
    private Quaternion m_targetRotation;
    private float m_lookStep;
    private float m_speedStep;
    private int m_overlapCapsuleHitCount = 0;
    private Vector3 m_explosionPosition;
    private Vector3 m_rayOrigin;
    private Vector3 m_targetPoint;
    private Vector3 m_rayDirection;
    private EnemyController m_enemyHit;
    private Quaternion m_spawnVFXdirection;

    void Awake()
    {
        m_storedProjectileSpeed = m_projectileSpeed;
        m_storedLookSpeed = m_lookSpeed;
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        m_projectileSpeed = m_storedProjectileSpeed;
        m_lookSpeed = m_storedLookSpeed;
        m_lookStep = 0;
        m_speedStep = 0;
    }

    public override void Loaded()
    {
        //Debug.Log($"Missile Loaded");
        gameObject.transform.localScale = Vector3.zero;
        gameObject.transform.DOScale(Vector3.one, 0.15f).SetUpdate(true).SetEase(Ease.InOutBounce);
        RequestPlayAudio(m_projectileData.m_reloadClips);
    }

    void FixedUpdate()
    {
        if (m_isComplete) return;

        if (IsTargetInStoppingDistance())
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

            if (m_enemy.GetCurrentHP() <= 0 || m_enemy.m_isTeleporting)
            {
                m_enemy = null;
            }
        }
    }


    void TravelToTargetFixedUpdate()
    {
        //Rotate towards Target.
        m_direction = m_targetPos - transform.position;
        m_targetRotation = Quaternion.LookRotation(m_direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, m_targetRotation, m_lookSpeed * Time.fixedDeltaTime);

        //Move Forward.
        transform.position += transform.forward * (m_projectileSpeed * Time.fixedDeltaTime);

        //Increase Lookspeed (greatly)
        m_lookStep = m_lookAcceleration * Time.fixedDeltaTime;
        m_lookSpeed += m_lookStep;

        //Increase Move Speed up to 50%
        m_speedStep = m_speedAcceleration * Time.fixedDeltaTime;
        m_projectileSpeed += m_speedStep;
    }

    void DealDamage()
    {
        m_explosionPosition = gameObject.GetComponent<Collider>().bounds.center;
        ObjectPoolManager.SpawnObject(m_impactEffect, m_explosionPosition, Quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);

        // Find affected enemies
        Collider[] hits = Physics.OverlapSphere(m_explosionPosition, m_impactRadius, m_areaLayerMask.value);
        m_overlapCapsuleHitCount = hits.Length;

        if (m_overlapCapsuleHitCount == 0)
        {
            return;
        }

        foreach (Collider col in hits)
        {
            // Use ClosestPoint instead of center to avoid missing colliders whose center is outside the explosion
            Vector3 closestPoint = col.ClosestPoint(m_explosionPosition);

            // If explosion position is inside the collider, guarantee a hit
            if (col.bounds.Contains(m_explosionPosition))
            {
                SendDamage(col);
                continue;
            }

            // Adjust ray to go from explosion to closest point
            m_rayDirection = (closestPoint - m_explosionPosition).normalized;
            m_rayOrigin = m_explosionPosition;

            RaycastHit hit;
            Vector3 rayStart = m_rayOrigin;
            float remainingDistance = m_impactRadius;

            while (Physics.Raycast(rayStart, m_rayDirection, out hit, remainingDistance, m_raycastLayerMask.value))
            {
                //Debug.Log($"Ray hit {hit.collider.gameObject.name} at {hit.point}");

                if (hit.collider.gameObject.layer == m_shieldLayer)
                {
                    break;
                }

                if (hit.collider == col)
                {
                    SendDamage(col);
                    break;
                }

                // Move ray slightly forward to check next object
                rayStart = hit.point + m_rayDirection * 0.01f;
                remainingDistance -= hit.distance;

                if (remainingDistance <= 0)
                {
                    break;
                }
            }
        }
    }


    void SendDamage(Collider col)
    {
        m_enemyHit = col.GetComponent<EnemyController>();
        m_enemyHit.OnTakeDamage(m_projectileDamage);
        ObjectPoolManager.SpawnObject(m_hitVFXPrefab, m_enemyHit.transform.position, transform.rotation, null, ObjectPoolManager.PoolType.ParticleSystem);

        //Apply Status Effect
        if (m_statusEffectData != null)
        {
            StatusEffect statusEffect = new StatusEffect(m_statusSender, m_statusEffectData);
            m_enemyHit.ApplyEffect(statusEffect);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (m_isComplete) return;

        if (collision.collider.gameObject.layer == m_shieldLayer)
        {
            m_spawnVFXdirection = Quaternion.LookRotation(collision.transform.position - m_startPos);
            ObjectPoolManager.SpawnObject(m_hitVFXPrefab, transform.position, m_spawnVFXdirection, null, ObjectPoolManager.PoolType.ParticleSystem);
            DealDamage();
            RemoveProjectile();
        }
    }
}