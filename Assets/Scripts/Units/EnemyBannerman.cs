using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

public class EnemyBannerman : MonoBehaviour
{
    // Heal Data
    [Header("Healing")]
    public GameObject m_healEffect;
    public float m_healRadius = 3f;
    public float m_healPeriod = 2f;
    public float m_healPower = .2f;
    public LayerMask m_healLayerMask;
    private float m_nextHealTime;

    // Status Effect Data
    [Header("Status Effect")]
    public StatusEffect m_statusEffect;
    public List<float> m_statusEffectThresholds; //0-100 list thresholds to trigger effects.
    private HashSet<float> m_triggeredThresholds;
    private EnemyController m_enemyController;

    private void Start()
    {
        m_enemyController = GetComponentInParent<EnemyController>();
        m_enemyController.UpdateHealth += OnUpdateHealth;
        m_enemyController.DestroyEnemy += OnEnemyDestroyed;
        m_triggeredThresholds = new HashSet<float>();
        m_nextHealTime = Time.time + m_healPeriod;
    }

    void OnEnemyDestroyed(Vector3 pos)
    {
        m_enemyController.UpdateHealth -= OnUpdateHealth;
        m_enemyController.DestroyEnemy -= OnEnemyDestroyed;
    }

    void OnUpdateHealth(float i)
    {
        float maxHP = m_enemyController.GetMaxHP();
        float curHP = m_enemyController.GetCurrentHP();
        
        foreach(float threshold in m_statusEffectThresholds)
        {
            float curThreshold = maxHP * (threshold / 100f);

            if (curHP < curThreshold && !m_triggeredThresholds.Contains(threshold))
            {
                m_triggeredThresholds.Add(threshold);
                SendEffect();
                Debug.Log($"{threshold} threshold passed. Sending Effect.");
            }
        }
    }
    
    private void Update()
    {
        if (m_nextHealTime <= Time.time)
        {
            m_nextHealTime += m_healPeriod;
            Debug.Log($"Healing. Next heal at {m_nextHealTime}.");
            Heal();
        }
    }

    private void Heal()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_healRadius, m_healLayerMask);

        if (colliders.Length <= 0) return; //No one found to heal.

        foreach (Collider col in colliders)
        {
            EnemyController enemyController = col.GetComponent<EnemyController>();

            if (enemyController == null) continue; //No controller on collider obj

            enemyController.OnHealed(m_healPower, true);
        }

        ObjectPoolManager.SpawnObject(m_healEffect.gameObject, transform.position, quaternion.identity, ObjectPoolManager.PoolType.ParticleSystem);
    }

    private void SendEffect()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_healRadius, m_healLayerMask);

        if (colliders.Length <= 0) return; //No one found to apply effects to.

        foreach (Collider col in colliders)
        {
            EnemyController enemyController = col.GetComponent<EnemyController>();
            if (m_statusEffect.m_data != null)
            {
                Debug.Log($"This should not trigger if we do not have a status effect.");
                enemyController.ApplyEffect(m_statusEffect);
            }
        }
    }
}