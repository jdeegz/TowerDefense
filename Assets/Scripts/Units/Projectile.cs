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

    protected bool m_isFired = false;
    protected Vector3 m_targetPos;
    protected EnemyController m_enemy;
    protected float m_projectileDamage = 1;
    protected float m_elapsedTime;
    protected float m_projectileLifetime;
    protected Vector3 m_startPos;
    protected Vector3 m_directPos;
    protected StatusEffect m_statusEffect;
    protected Collider m_enemyCollider;

    void Awake()
    {
    }
    
    public void SetProjectileData(EnemyController enemy, Transform target, float dmg, Vector3 pos)
    {
        m_startPos = pos;
        m_enemy = enemy;
        m_projectileDamage = dmg;
        m_enemyCollider = enemy.GetComponent<Collider>();
        m_enemy.DestroyEnemy += OnEnemyDestroyed;
        m_isFired = true;
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

    void Update()
    {
        if(!m_enemy) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if(m_enemy) m_enemy.DestroyEnemy -= OnEnemyDestroyed;
    }
}
