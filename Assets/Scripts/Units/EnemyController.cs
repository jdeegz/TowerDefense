using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public abstract class EnemyController : MonoBehaviour, IEffectable
{
    //Enemy Scriptable Data
    private EnemyData m_enemyData;
    public Transform m_targetPoint;
    
    //Enemy Objective & Position
    protected Vector2Int m_curPos;
    protected Cell m_curCell;
    protected Transform m_goal;
    
    //Enemy Stats
    private float m_curMaxHealth;
    private float m_curHealth;
    protected float m_baseMoveSpeed;
    private float m_curSpeedModifier;
    protected float m_lastSpeedModifierFaster = 1f;
    protected float m_lastSpeedModifierSlower = 1f;
    private float m_curDamageMultiplier;
    
    //Hit Flash Info
    List<Renderer> m_allRenderers;
    private List<Color> m_allOrigColors;
    private Coroutine m_hitFlashCoroutine;
    
    //VFX
    public GameObject m_speedTrailObj;
    
    private AudioSource m_audioSource;
    private List<StatusEffect> m_statusEffects;

    protected NavMeshAgent m_navMeshAgent;
    
    public event Action<float> UpdateHealth;
    public event Action<Vector3> DestroyEnemy;

    public void SetEnemyData(EnemyData data)
    {
        m_enemyData = data;
    }
    
    void Start()
    {
        //Setup with Gameplay Manager
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        m_goal = GameplayManager.Instance.m_enemyGoal;
        GameplayManager.Instance.AddEnemyToList(this);

        //Setup with GridManager
        m_curPos = Vector2Int.zero;

        //Setup Data
        m_baseMoveSpeed = m_enemyData.m_moveSpeed;
        m_curSpeedModifier = 1;
        m_curMaxHealth = (int)MathF.Floor(m_enemyData.m_health * Mathf.Pow(1.15f, GameplayManager.Instance.m_wave));
        m_curHealth = m_curMaxHealth;
        m_curDamageMultiplier = m_enemyData.m_damageMultiplier;
        
        //Setup Life Meter
        UIHealthMeter lifeMeter = Instantiate(IngameUIController.Instance.m_healthMeter, IngameUIController.Instance.transform);
        lifeMeter.SetEnemy(this, m_curMaxHealth, m_enemyData.m_healthMeterOffset, m_enemyData.m_healthMeterScale);
        
        //Setup Hit Flash
        CollectMeshRenderers(transform);

        //Setup Status Effects
        m_statusEffects = new List<StatusEffect>();

        //Subscription to events
        UpdateHealth += OnUpdateHealth;
        DestroyEnemy += OnEnemyDestroyed;

        //Define AudioSource
        m_audioSource = GetComponent<AudioSource>();
        m_audioSource.PlayOneShot(m_enemyData.m_audioSpawnClip);
        
        //Create Speed Trail Object
        m_speedTrailObj.SetActive(false);
        if(m_goal) StartMoving(m_goal.position);
    }
    
    void Update()
    {
        UpdateStatusEffects();
        HandleMovement();
    }
    
    //Movement
    //Functions
    public abstract void StartMoving(Vector3 pos);
    public abstract void HandleMovement();
    
    //Taking Damage
    //Functions
    void CollectMeshRenderers(Transform parent)
    {
        //Get Parent Mesh Renderer if there is one.
        Renderer Renderer = parent.GetComponent<Renderer>();
        if (Renderer != null)
        {
            if (m_allRenderers == null)
            {
                m_allRenderers = new List<Renderer>();
            }

            if (m_allOrigColors == null)
            {
                m_allOrigColors = new List<Color>();
            }

            m_allRenderers.Add(Renderer);
            m_allOrigColors.Add(Renderer.material.GetColor("_EmissionColor"));
        }

        for (int i = 0; i < parent.childCount; ++i)
        {
            //Recursivly add meshrenderers by calling this function again with the child as the transform.
            Transform child = parent.GetChild(i);
            CollectMeshRenderers(child);
        }
    }
    
    public void OnTakeDamage(float dmg)
    {
        //Deal Damage
        UpdateHealth?.Invoke(-dmg * m_curDamageMultiplier);
        
        //Audio
        int i = Random.Range(0, m_enemyData.m_audioDamagedClips.Count);
        m_audioSource.PlayOneShot(m_enemyData.m_audioDamagedClips[i]);
        
        //VFX
        
        //Hit Flash
        if (m_allRenderers == null) return;
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }
        m_hitFlashCoroutine = StartCoroutine(HitFlash());
    }

    void OnUpdateHealth(float i)
    {
        m_curHealth += i;

        if (m_curHealth <= 0)
        {
            DestroyEnemy?.Invoke(transform.position);
        }
    }
    
    private IEnumerator HitFlash()
    {
        //Set the color
        for (int i = 0; i < m_allRenderers.Count; ++i)
        {
            m_allRenderers[i].material.SetColor("_EmissionColor", Color.red);
        }

        yield return new WaitForSeconds(.15f);

        //Return to original colors.
        for (int i = 0; i < m_allRenderers.Count; ++i)
        {
            m_allRenderers[i].material.SetColor("_EmissionColor", m_allOrigColors[i]);
        }
    }

    void OnEnemyDestroyed(Vector3 pos)
    {
        if (m_curCell != null)
        {
            m_curCell.UpdateActorCount(-1, gameObject.name);
        }
        GameplayManager.Instance.RemoveEnemyFromList(this);
        Destroy(gameObject);
    }
    
    //Enemy Escape
    //Functions
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Exit"))
        {
            GameplayManager.Instance.m_castleController.TakeDamage(1);
            DestroyEnemy?.Invoke(transform.position);
        }
    }
    
    //Status Effect
    //Functions
    public void UpdateStatusEffects()
    {
        //Update Status Effects
        m_lastSpeedModifierFaster = 1;
        m_lastSpeedModifierSlower = 1;
        List<StatusEffect> activeEffects = new List<StatusEffect>(m_statusEffects);
        foreach (StatusEffect activeEffect in activeEffects)
        {
            HandleEffect(activeEffect);
        }
    }
    
    public void ApplyEffect(StatusEffectData data)
    {
        StatusEffect newStatusEffect = new StatusEffect();
        newStatusEffect.m_data = data;
        if (m_statusEffects.Count >= 1)
        {
            for (int i = 0; i < m_statusEffects.Count; i++)
            {
                var activeEffect = m_statusEffects[i];
                if (data.m_sender == activeEffect.m_data.m_sender)
                {
                    m_statusEffects[i] = newStatusEffect;
                    Debug.Log($"Replacing Effect. Remaining time:{m_statusEffects[i].m_elapsedTime}");
                    return;
                }
            }
        }

        Debug.Log("Adding Effect.");
        
        m_statusEffects.Add(newStatusEffect);
        
    }

    public void HandleEffect(StatusEffect statusEffect)
    {
        if (statusEffect.m_elapsedTime > statusEffect.m_nextTickTime)
        {
            if (statusEffect.m_data.m_damage != 0)
            {
                //Modify health
                UpdateHealth?.Invoke(-statusEffect.m_data.m_damage);
            }

            statusEffect.m_nextTickTime += statusEffect.m_data.m_tickSpeed;
        }

        if (statusEffect.m_data.m_speedModifier != 1)
        {
            //Modify speed
            if (statusEffect.m_data.m_speedModifier < 1 && statusEffect.m_data.m_speedModifier < m_lastSpeedModifierSlower)
            {
                m_lastSpeedModifierSlower = statusEffect.m_data.m_speedModifier;
            }

            if (statusEffect.m_data.m_speedModifier > 1 && statusEffect.m_data.m_speedModifier > m_lastSpeedModifierFaster)
            {
                m_lastSpeedModifierFaster = statusEffect.m_data.m_speedModifier;
            }
        }

        statusEffect.m_elapsedTime += Time.deltaTime;

        Debug.Log($"{statusEffect.m_elapsedTime} / {statusEffect.m_data.m_lifeTime}");
        if (statusEffect.m_elapsedTime > statusEffect.m_data.m_lifeTime) RemoveEffect(statusEffect);
    }

    public void RemoveEffect(StatusEffect data)
    {
        Debug.Log("Removing Effect");
        m_statusEffects.Remove(data);
    }
}

public class StatusEffect
{
    public StatusEffectData m_data;
    public float m_elapsedTime;
    public float m_nextTickTime;
}

