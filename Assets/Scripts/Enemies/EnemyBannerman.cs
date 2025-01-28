using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class EnemyBannerman : MonoBehaviour
{
    // Heal Data
    [Header("Healing")]
    public GameObject m_healEffect;
    public GameObject m_thresholdEffect;
    public float m_healRadius = 3f;
    public float m_healPeriod = 2f;
    public float m_abilityPeriod = 5f;
    public float m_healPower = .2f;
    public LayerMask m_healLayerMask;
    private float m_timeUntilAbilityUseable = 0f;
    private float m_nextHealTime;

    // Status Effect Data
    public StatusEffectData m_statusEffectData;
    public List<float> m_statusEffectThresholds; //0-100 list thresholds to trigger effects.
    private HashSet<float> m_triggeredThresholds;
    private EnemyController m_enemyController;

    private void Start()
    {
        m_enemyController = GetComponentInParent<EnemyController>();
        m_enemyController.UpdateHealth += OnUpdateHealth;
        m_enemyController.DestroyEnemy += OnEnemyDestroyed;
    }

    void OnEnable()
    {
        m_triggeredThresholds = new HashSet<float>();
        m_nextHealTime = Time.time + m_healPeriod + Random.Range(0, m_healPeriod);
    }

    void OnEnemyDestroyed(Vector3 pos)
    {
        m_nextHealTime = Mathf.Infinity;
    }

    void OnDestroy()
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

                if (m_timeUntilAbilityUseable <= 0)
                {
                    m_timeUntilAbilityUseable = m_abilityPeriod;
                    
                    SendEffect();

                    ObjectPoolManager.SpawnObject(m_thresholdEffect.gameObject, m_enemyController.m_targetPoint.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
                }
            }
        }
    }
    
    private void Update()
    {
        if (m_nextHealTime <= Time.time)
        {
            m_nextHealTime += m_healPeriod;
            //Debug.Log($"Healing. Next heal at {m_nextHealTime}.");
            Heal();
        }

        m_timeUntilAbilityUseable -= Time.deltaTime;
    }

    private void Heal()
    {
        m_enemyController.RequestPlayAudio(m_enemyController.m_enemyData.m_audioEnemyFeatureClip);
        
        Collider[] colliders = Physics.OverlapSphere(m_enemyController.m_targetPoint.position, m_healRadius, m_healLayerMask);

        if (colliders.Length <= 0) return; //No one found to heal.

        //Debug.Log($"Found {colliders.Length} enemies to heal.");
        foreach (Collider col in colliders)
        {
            EnemyController enemyController = col.GetComponent<EnemyController>();

            if (enemyController == null) continue; //No controller on collider obj

            enemyController.OnHealed(m_healPower, true);
        }

        ObjectPoolManager.SpawnObject(m_healEffect.gameObject, m_enemyController.m_targetPoint.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
    }

    private void SendEffect()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_healRadius, m_healLayerMask);

        if (colliders.Length <= 0) return; //No one found to apply effects to.

        foreach (Collider col in colliders)
        {
            EnemyController enemyController = col.GetComponent<EnemyController>();
            
            if (m_statusEffectData)
            {
                StatusEffect statusEffect = new StatusEffect(gameObject, m_statusEffectData);
                enemyController.ApplyEffect(statusEffect);
            }
        }
    }
}