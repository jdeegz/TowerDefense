using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySwarmMember : EnemyController
{
    private EnemyController m_motherEnemyController;
    public bool m_returnToPool;
    
    public void SetMother(EnemyController mother)
    {
        m_motherEnemyController = mother;
        m_motherEnemyController.DestroyEnemy += OnEnemyDestroyed;
        m_motherEnemyController.UpdateHealth += MotherTakeDamage;
        SetupEnemy(true);
    }
    
    public override void SetupEnemy(bool active)
    {
        m_isComplete = false;

        int wave = 1;

        if (GameplayManager.Instance)
        {
            //m_goal = GameplayManager.Instance.m_enemyGoal;
            wave = GameplayManager.Instance.m_wave;
            //AddToGameplayList();
        }

        //Setup with GridManager
        //m_curPos = Vector2Int.zero;

        //Setup Data
        //m_baseMoveSpeed = m_enemyData.m_moveSpeed;
        //m_baseLookSpeed = m_enemyData.m_lookSpeed;
        m_curMaxHealth = (int)MathF.Floor(m_enemyData.m_health * Mathf.Pow(1.075f, wave));
        m_curHealth = m_curMaxHealth;
        m_baseDamageMultiplier = m_enemyData.m_damageMultiplier;

        //Setup Hit Flash
        CollectMeshRenderers(m_enemyModelRoot.transform);

        //Setup Status Effects
        //Debug.Log($"Clearing status effect lists.");
        if(m_statusEffects != null) m_statusEffects.Clear();
        if(m_expiredStatusEffects != null) m_expiredStatusEffects.Clear();
        if(m_newStatusEffects != null) m_newStatusEffects.Clear();
        m_statusEffects = new List<StatusEffect>();

        //Define AudioSource
        //m_audioSource = GetComponent<AudioSource>();
        //m_audioSource.PlayOneShot(m_enemyData.m_audioSpawnClip);

        //Setup ObeliskData if the mission has obelisks
        /*if (GameplayManager.Instance && GameplayManager.Instance.m_obelisksInMission.Count > 0)
        {
            m_obeliskData = GameplayManager.Instance.m_obelisksInMission[0].m_obeliskData;
        }*/

        //SetupUI();
        ResetDissolve();
        SetEnemyActive(active);
    }

    private void OnEnable()
    {
        if (m_motherEnemyController) // Only set-up if we have a controller.
        {
            SetupEnemy(true);
        }
    }
    
    public override void AddToGameplayList()
    {
        //
    }

    public override void RemoveFromGameplayList()
    {
        //
    }

    public override void HandleMovement()
    {
        //
    }
    
    public override void OnTakeDamage(float dmg)
    {
        m_motherEnemyController.OnTakeDamage(dmg);
    }

    private void MotherTakeDamage(float dmg)
    {
        if (m_allRenderers == null) return;
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }

        m_hitFlashCoroutine = StartCoroutine(HitFlash());
    }
    
    public override void OnHealed(float heal, bool percentage)
    {
        m_motherEnemyController.OnHealed(heal, percentage);
    }

    public override void OnEnemyDestroyed(Vector3 pos)
    {
        //Debug.Log($"OnEnemyDestroyed called on Swarm Member.");
        if (m_isComplete) return;

        //transform.SetParent(ObjectPoolManager.SetParentObject(ObjectPoolManager.PoolType.Enemy).transform);
        m_isComplete = true;
        
        m_curHealth = 0;
        
        if (m_deathVFX)
        {
            ObjectPoolManager.SpawnObject(m_deathVFX.gameObject, m_targetPoint.position, Quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
        }
        
        //Return effects to pool.
        foreach (StatusEffect activeEffect in m_statusEffects)
        {
            RemoveEffect(activeEffect);
        }

        //End the running coroutine
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }

        //Reset the coroutine tinting
        for (int i = 0; i < m_allRenderers.Count; ++i)
        {
            foreach (Material material in m_allRenderers[i].materials)
            {
                material.SetColor("_EmissionColor", m_allOrigColors[i]);
            }
        }
        
        //m_motherEnemyController.DestroyEnemy -= OnEnemyDestroyed;
        if (m_returnToPool)
        {
            ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.Enemy);
        }
        StartDissolve(null);
    }
    
    public override void ApplyEffect(StatusEffect statusEffect)
    {
        //Add incoming status effects to a holding list. They will get added to the list then updated in UpdateStatusEffects.
        if (m_curHealth <= 0) return;

        //We do not care about slow effects.
        if (statusEffect.m_data.m_effectType is StatusEffectData.EffectType.DecreaseMoveSpeed or StatusEffectData.EffectType.IncreaseMoveSpeed)
        {
            return;
        }
        
        m_newStatusEffects.Add(statusEffect);
    }
}
