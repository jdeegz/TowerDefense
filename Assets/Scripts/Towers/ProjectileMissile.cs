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

    void OnEnable()
    {
        //Reset Data
    }

    public override void Loaded()
    {
        gameObject.transform.localScale = Vector3.zero;
        gameObject.transform.DOScale(1, 0.2f);
        RequestPlayAudio(m_projectileData.m_reloadClips);
    }

    void FixedUpdate()
    {
        if (m_isComplete) return;

        if (IsTargetInStoppingDistance())
        {
            RemoveProjectile();
            DealDamage();
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

    static readonly ProfilerMarker k_codeMarkerTravelToTargetFixedUpdate = new ProfilerMarker("TravelToTargetFixedUpdate");

    void TravelToTargetFixedUpdate()
    {
        k_codeMarkerTravelToTargetFixedUpdate.Begin();
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
        k_codeMarkerTravelToTargetFixedUpdate.End();
    }

    void DealDamage()
    {
        //This needs to use the missiles collider center as position instead of the missile position.
        m_explosionPosition = gameObject.GetComponent<Collider>().bounds.center;
        ObjectPoolManager.SpawnObject(m_impactEffect, m_explosionPosition, Util.GetRandomRotation(Quaternion.identity, new Vector3(0, 180, 0)), null, ObjectPoolManager.PoolType.ParticleSystem);

        //Find affected enemies
        Collider[] hits = Physics.OverlapSphere(m_explosionPosition, m_impactRadius, m_areaLayerMask.value);
        m_overlapCapsuleHitCount = hits.Length;

        if (m_overlapCapsuleHitCount == 0)
        {
            return;
        }

        foreach (Collider col in hits)
        {
            //If the explosion position is within the collider, it's a hit, regardless of shields.
            if (col.bounds.Contains(m_explosionPosition))
            {
                SendDamage(col);
                continue;
            }


            // Use ClosestPoint for ray direction
            m_targetPoint = col.bounds.center;
            m_rayDirection = (m_targetPoint - m_explosionPosition).normalized;
            m_rayOrigin = m_explosionPosition;

            RaycastHit hit;
            Vector3 rayStart = m_rayOrigin;
            float remainingDistance = m_impactRadius;

            while (Physics.Raycast(rayStart, m_rayDirection, out hit, remainingDistance, m_raycastLayerMask.value))
            {
                if (hit.collider.gameObject.layer == m_shieldLayer)
                {
                    // A shield blocked the damage before the target, so exit early
                    return;
                }

                if (hit.collider == col)
                {
                    SendDamage(col);
                    return;
                }

                // Move the ray origin slightly forward to continue checking beyond this collider
                rayStart = hit.point + m_rayDirection * 0.01f;
                remainingDistance -= hit.distance;

                // If remaining distance is negligible, stop checking
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