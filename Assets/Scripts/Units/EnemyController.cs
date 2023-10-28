using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
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
    protected float m_lastSpeedModifierFaster = 1f;
    protected float m_lastSpeedModifierSlower = 1f;
    protected float m_lastDamageModifierLower = 1f;
    protected float m_lastDamageModifierHigher = 1f;
    private float m_baseDamageMultiplier;

    //Hit Flash Info
    List<Renderer> m_allRenderers;
    private List<Color> m_allOrigColors;
    private Coroutine m_hitFlashCoroutine;

    //VFX
    public GameObject m_speedTrailVFXObj;
    private GameObject m_decreaseHealthVFXOjb;
    private GameObject m_increaseHealthVFXOjb;
    private GameObject m_decreaseMoveSpeedVFXOjb;
    private GameObject m_increaseMoveSpeedVFXOjb;

    private AudioSource m_audioSource;
    private List<StatusEffect> m_statusEffects;
    private List<StatusEffect> m_newStatusEffects = new List<StatusEffect>();
    private List<StatusEffect> m_expiredStatusEffects = new List<StatusEffect>();

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
        m_curMaxHealth = (int)MathF.Floor(m_enemyData.m_health * Mathf.Pow(1.085f, GameplayManager.Instance.m_wave));
        m_curHealth = m_curMaxHealth;
        m_baseDamageMultiplier = m_enemyData.m_damageMultiplier;

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
        m_speedTrailVFXObj.SetActive(false);
        if (m_goal) StartMoving(m_goal.position);
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
        UpdateHealth?.Invoke(-dmg * m_baseDamageMultiplier * m_lastDamageModifierHigher * m_lastDamageModifierLower);

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
        //Remove Expired Effects
        if (m_expiredStatusEffects.Count > 0)
        {
            foreach (StatusEffect expiredStatusEffect in m_expiredStatusEffects)
            {
                m_statusEffects.Remove(expiredStatusEffect);
            }

            m_expiredStatusEffects.Clear();
        }

        //Add New Effects
        if (m_newStatusEffects.Count > 0)
        {
            //Check & Add new Effects to list if there are any.
            foreach (StatusEffect newStatusEffect in m_newStatusEffects)
            {
                bool senderFound = false;
                for (int i = 0; i < m_statusEffects.Count; i++)
                {
                    if (newStatusEffect.m_data.m_sender == m_statusEffects[i].m_data.m_sender)
                    {
                        //We found a sender match, update the existing effect.
                        m_statusEffects[i] = newStatusEffect;
                        senderFound = true;
                        break;
                    }
                }

                //If we didnt find a matching sender, this is a new effect. Add it to the list.
                if (!senderFound) m_statusEffects.Add(newStatusEffect);
            }

            //Reset the new status effects so it is empty.
            m_newStatusEffects.Clear();
        }

        //Update each Effect.
        foreach (StatusEffect activeEffect in m_statusEffects)
        {
            HandleEffect(activeEffect);
        }
    }

    public void ApplyEffect(StatusEffectData statusEffect)
    {
        StatusEffect newStatusEffect = new StatusEffect();
        newStatusEffect.m_data = statusEffect;

        //Add incoming status effects to a holding list. They will get added to the list then updated in UpdateStatusEffects.
        m_newStatusEffects.Add(newStatusEffect);
    }

    public void HandleEffect(StatusEffect statusEffect)
    {
        statusEffect.m_elapsedTime += Time.deltaTime;
        //Debug.Log($"{statusEffect.m_elapsedTime} / {statusEffect.m_data.m_lifeTime}");
        if (statusEffect.m_elapsedTime > statusEffect.m_data.m_lifeTime)
        {
            RemoveEffect(statusEffect);
            return;
        }

        //If we need to, spawn a vfx for this effect.
        switch (statusEffect.m_data.m_effectType)
        {
            case StatusEffectData.EffectType.DecreaseMoveSpeed:
                if (m_decreaseMoveSpeedVFXOjb) return;
                m_decreaseMoveSpeedVFXOjb = Instantiate(statusEffect.m_data.m_effectVFX, m_targetPoint.position, Quaternion.identity, transform);
                break;
            case StatusEffectData.EffectType.IncreaseMoveSpeed:
                if (m_increaseMoveSpeedVFXOjb) return;
                m_increaseMoveSpeedVFXOjb = Instantiate(statusEffect.m_data.m_effectVFX, m_targetPoint.position, Quaternion.identity, transform);
                break;
            case StatusEffectData.EffectType.DecreaseHealth:
                if (m_decreaseHealthVFXOjb) return;
                m_decreaseHealthVFXOjb = Instantiate(statusEffect.m_data.m_effectVFX, m_targetPoint.position, Quaternion.identity, transform);
                break;
            case StatusEffectData.EffectType.IncreaseHealth:
                if (m_increaseHealthVFXOjb) return;
                m_increaseHealthVFXOjb = Instantiate(statusEffect.m_data.m_effectVFX, m_targetPoint.position, Quaternion.identity, transform);
                break;
            case StatusEffectData.EffectType.DecreaseArmor:
                break;
            case StatusEffectData.EffectType.IncreaseArmor:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        //Damage & Healing per tick.
        if (statusEffect.m_elapsedTime > statusEffect.m_nextTickTime)
        {
            if (statusEffect.m_data.m_damage != 0)
            {
                //Modify health
                UpdateHealth?.Invoke(-statusEffect.m_data.m_damage);
            }

            statusEffect.m_nextTickTime += statusEffect.m_data.m_tickSpeed;
        }

        //Move Speed multipliers.
        if (statusEffect.m_data.m_speedModifier != 1)
        {
            //Modify speed
            if (statusEffect.m_data.m_speedModifier < 1 && statusEffect.m_data.m_speedModifier < m_lastSpeedModifierSlower)
            {
                m_lastSpeedModifierSlower = statusEffect.m_data.m_speedModifier;
                //Debug.Log($"Set slower speed modifier to: {m_lastSpeedModifierSlower}");
            }

            if (statusEffect.m_data.m_speedModifier > 1 && statusEffect.m_data.m_speedModifier > m_lastSpeedModifierFaster)
            {
                m_lastSpeedModifierFaster = statusEffect.m_data.m_speedModifier;
                //Debug.Log($"Set slower speed modifier to: {m_lastSpeedModifierFaster}");
            }
        }

        if (statusEffect.m_data.m_damageModifier != 1)
        {
            //Modify Damage Taken
            if (statusEffect.m_data.m_damageModifier < 1 && statusEffect.m_data.m_damageModifier < m_lastDamageModifierLower)
            {
                m_lastDamageModifierLower = statusEffect.m_data.m_damageModifier;
                //Debug.Log($"Set modifier to: {m_lastDamageModifierLower}");
            }

            if (statusEffect.m_data.m_damageModifier > 1 && statusEffect.m_data.m_damageModifier > m_lastDamageModifierHigher)
            {
                m_lastDamageModifierHigher = statusEffect.m_data.m_damageModifier;
                //Debug.Log($"Set modifier to: {m_lastDamageModifierHigher}");
            }
        }
    }

    public void RemoveEffect(StatusEffect statusEffect)
    {
        Debug.Log("Removing Effect");
        switch (statusEffect.m_data.m_effectType)
        {
            case StatusEffectData.EffectType.DecreaseMoveSpeed:
                m_lastSpeedModifierSlower = 1;
                if (m_decreaseMoveSpeedVFXOjb) Destroy(m_decreaseMoveSpeedVFXOjb);
                break;
            case StatusEffectData.EffectType.IncreaseMoveSpeed:
                m_lastSpeedModifierFaster = 1;
                if (m_increaseMoveSpeedVFXOjb) Destroy(m_increaseMoveSpeedVFXOjb);
                break;
            case StatusEffectData.EffectType.DecreaseHealth:
                if (m_decreaseHealthVFXOjb) Destroy(m_decreaseHealthVFXOjb);
                break;
            case StatusEffectData.EffectType.IncreaseHealth:
                if (m_increaseHealthVFXOjb) Destroy(m_increaseHealthVFXOjb);
                break;
            case StatusEffectData.EffectType.DecreaseArmor:
                m_lastDamageModifierLower = 1;
                break;
            case StatusEffectData.EffectType.IncreaseArmor:
                m_lastDamageModifierHigher = 1;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        m_expiredStatusEffects.Add(statusEffect);
    }
}

[Serializable]
public class StatusEffect
{
    public StatusEffectData m_data;
    public float m_elapsedTime;
    public float m_nextTickTime;
}