using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using UnityEngine;
using UnityEngine.Serialization;
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
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
    protected StatusEffectData m_statusEffectData;
    protected GameObject m_statusSender;
    
    public Renderer m_renderer;
    public BulletTrailData m_bulletTrailData;
    protected TrailRenderer m_trail;
    protected int m_shieldLayer;
    
    void Start()
    {
        m_shieldLayer = LayerMask.NameToLayer("Shield"); //HARDCODED LAYER NAME
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
    
    public void SetProjectileData(EnemyController enemy, Transform target, float dmg, Vector3 pos,  GameObject sender, StatusEffectData statusEffectData = null)
    {
        //if(m_isFired) Debug.Log($"We are Setting data for a fired missile.");
        m_startPos = pos;
        m_enemy = enemy;
        m_statusSender = sender;
        m_statusEffectData = statusEffectData;
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
    
    public bool IsTargetInStoppingDistance()
    {
        float distanceToTarget = Vector3.Distance(transform.position, m_targetPos);
        return distanceToTarget <= m_stoppingDistance;
    }
    
    public void RemoveProjectile()
    {
        m_isComplete = true;

        m_isFired = false;
        
        m_enemy = null;
        
        if (m_renderer)
        {
            m_renderer.enabled = false;
        }

        transform.rotation = Quaternion.identity;
        
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
