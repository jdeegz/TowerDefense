using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public abstract class Projectile : PooledObject
{
    [Header("Projectile Data")]
    [SerializeField] protected ProjectileData m_projectileData;
    [SerializeField] protected float m_projectileSpeed = .5f;
    [SerializeField] protected float m_stoppingDistance = .15f;
    
    [Header("Projectile Components")]
    [SerializeField] protected GameObject m_hitVFXPrefab;
    [SerializeField] protected AudioSource m_audioSource;

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
    
    protected int m_shieldLayer;
    
    void Start()
    {
        m_shieldLayer = LayerMask.NameToLayer("Shield"); //HARDCODED LAYER NAME
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
    }

    public void SetProjectileStatusEffect(StatusEffect statusEffect)
    {
        m_statusEffect = statusEffect;
    }

    private float m_distanceToTarget;
    public bool IsTargetInStoppingDistance()
    {
        m_distanceToTarget = Vector3.Distance(transform.position, m_targetPos);
        return m_distanceToTarget <= m_stoppingDistance;
    }
    
    public void RemoveProjectile()
    {
        m_isComplete = true;

        m_isFired = false;
        
        m_enemy = null;
        
        transform.rotation = Quaternion.identity;

        RequestPlayAudio(m_projectileData.m_impactClips);
        
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
    
    public virtual void Loaded()
    {
        // Base implementation (optional)
    }
    
    public void RequestPlayAudio(AudioClip clip)
    {
        if (clip == null) return;
        
        m_audioSource.PlayOneShot(clip);
    }

    public void RequestPlayAudio(List<AudioClip> clips)
    {
        if (clips == null || clips.Count == 0) return;
        
        int i = Random.Range(0, clips.Count);
        m_audioSource.PlayOneShot(clips[i]);
    }
}
