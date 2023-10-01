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
    
    
    protected Vector3 m_targetPos;
    protected UnitEnemy m_enemy;
    protected float m_projectileDamage = 1;
    protected float m_elapsedTime;
    protected float m_projectileLifetime;
    protected Vector3 m_startPos;
    protected Vector3 m_directPos;
    
    public void SetProjectileData(UnitEnemy enemy, Transform target, float dmg, Vector3 pos)
    {
        m_startPos = pos;
        m_enemy = enemy;
        m_projectileDamage = dmg;
        m_enemy.DestroyEnemy += OnEnemyDestroyed;
    }

    private void OnEnemyDestroyed(Vector3 pos)
    {
        m_enemy = null;
        m_targetPos = pos;
    }

    void Update()
    {
        if(!m_enemy) Destroy(gameObject);
    }

    void Awake()
    {
        
    }
}
