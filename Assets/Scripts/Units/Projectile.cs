using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class Projectile : MonoBehaviour
{
    [SerializeField] protected float m_projectileSpeed = .5f;
    [SerializeField] protected float m_stoppingDistance = .1f;
    [SerializeField] protected GameObject m_hitVFXPrefab;

    protected bool m_isFired = false;
    protected bool m_isComplete;
    protected Vector3 m_targetPos;
    protected EnemyController m_enemy;
    protected float m_projectileDamage = 1;
    protected float m_elapsedTime;
    protected float m_projectileLifetime;
    protected Vector3 m_startPos;
    protected Vector3 m_directPos;
    protected StatusEffect m_statusEffect;
    public Renderer m_renderer;
    public BulletTrailData m_bulletTrailData;
    protected TrailRenderer m_trail;

    void Start()
    {
        m_trail = GetComponent<TrailRenderer>();
        ConfigureTrail();
    }

    private void ConfigureTrail()
    {
        if (m_trail != null && m_bulletTrailData != null)
        {
            m_bulletTrailData.SetupTrail(m_trail);
        }
    }
    
    public void SetProjectileData(EnemyController enemy, Transform target, float dmg, Vector3 pos)
    {
        m_startPos = pos;
        m_enemy = enemy;
        m_targetPos = target.position;
        m_projectileDamage = dmg;
        m_isFired = true;
        m_isComplete = false;
        if (m_renderer) m_renderer.enabled = true;
        if (m_trail != null ) m_trail.Clear();
    }

    public void SetProjectileStatusEffect(StatusEffect statusEffect)
    {
        m_statusEffect = statusEffect;
    }

    private void OnEnemyDestroyed(Vector3 pos)
    {
        m_enemy = null;
        m_targetPos = pos;
    }
    
    public bool IsTargetInStoppingDistance()
    {
        float distanceToTarget = Vector3.Distance(transform.position, m_targetPos);
        return distanceToTarget <= m_stoppingDistance;
    }

    void Update()
    {
        if (m_enemy)
        {
            m_targetPos = m_enemy.m_targetPoint.position;
            
            if (m_enemy.GetCurrentHP() <= 0)
            {
                OnEnemyDestroyed(m_enemy.m_targetPoint.position);
            }
        }
    }
    
    public void RemoveProjectile()
    {
        if (m_renderer)
        {
            m_renderer.enabled = false;
        }

        if (m_trail != null && m_bulletTrailData != null)
        {
            ObjectPoolManager.OrphanObject(gameObject, m_bulletTrailData.m_time);
        }
        
        else
        {
            ObjectPoolManager.ReturnObjectToPool(gameObject);
        }

    }
}
