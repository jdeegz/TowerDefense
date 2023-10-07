using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class UnitEnemy : MonoBehaviour, IEffectable
{
    [SerializeField] private NavMeshAgent m_navMeshAgent;
    [SerializeField] private ScriptableUnitEnemy m_enemyData;

    public Transform m_targetPoint;

    private float m_curMaxHealth;
    private float m_curHealth;
    private float m_baseMoveSpeed;
    private float m_curSpeedModifier;
    private float m_lastSpeedModifierFaster = 1f;
    private float m_lastSpeedModifierSlower = 1f;
    private float m_curDamageMultiplier;
    private Vector2Int m_curPos;
    private Cell m_curCell;
    private Transform m_goal;
    private List<MeshRenderer> m_allMeshRenderers;
    private List<Color> m_allOrigColors;
    private Coroutine m_hitFlashCoroutine;
    private AudioSource m_audioSource;
    private List<StatusEffect> m_statusEffects;

    public event Action<float> UpdateHealth;
    public event Action<Vector3> DestroyEnemy;

    void Start()
    {
        //Setup with Gameplay Manager
        m_navMeshAgent.speed = m_enemyData.m_moveSpeed;
        m_goal = GameplayManager.Instance.m_enemyGoal;
        StartMoving(m_goal.position);
        GameplayManager.Instance.AddEnemyToList(this);

        //Setup with GridManager
        m_curPos = Vector2Int.zero;

        //Setup Data
        m_baseMoveSpeed = m_enemyData.m_moveSpeed;
        m_curSpeedModifier = 1;
        m_navMeshAgent.speed = m_baseMoveSpeed * m_curSpeedModifier;
        m_curMaxHealth = (int)MathF.Floor(m_enemyData.m_health * Mathf.Pow(1.15f, GameplayManager.Instance.m_wave));
        m_curHealth = m_curMaxHealth;
        m_curDamageMultiplier = m_enemyData.m_damageMultiplier;
        UIHealthMeter lifeMeter = Instantiate(IngameUIController.Instance.m_healthMeter, IngameUIController.Instance.transform);
        lifeMeter.SetEnemy(this, m_curMaxHealth, m_enemyData.m_healthMeterOffset, m_enemyData.m_healthMeterScale);
        CollectMeshRenderers(transform);

        //Setup Status Effects
        m_statusEffects = new List<StatusEffect>();

        //Subscription to events
        UpdateHealth += OnUpdateHealth;
        DestroyEnemy += OnEnemyDestroyed;

        //Define AudioSource
        m_audioSource = GetComponent<AudioSource>();
        m_audioSource.PlayOneShot(m_enemyData.m_audioSpawnClip);
    }

    private void CollectMeshRenderers(Transform parent)
    {
        //Get Parent Mesh Renderer if there is one.
        MeshRenderer meshRenderer = parent.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            if (m_allMeshRenderers == null)
            {
                m_allMeshRenderers = new List<MeshRenderer>();
            }

            if (m_allOrigColors == null)
            {
                m_allOrigColors = new List<Color>();
            }

            m_allMeshRenderers.Add(meshRenderer);
            m_allOrigColors.Add(meshRenderer.material.GetColor("_EmissionColor"));
        }

        for (int i = 0; i < parent.childCount; ++i)
        {
            //Recursivly add meshrenderers by calling this function again with the child as the transform.
            Transform child = parent.GetChild(i);
            CollectMeshRenderers(child);
        }
    }

    void Update()
    {
        //Setup Cell occupancy
        Vector2Int newPos = Util.GetVector2IntFrom3DPos(transform.position);
        if (newPos != m_curPos)
        {
            if (m_curCell != null)
            {
                m_curCell.UpdateActorCount(-1, gameObject.name);
            }

            m_curPos = newPos;
            m_curCell = Util.GetCellFromPos(m_curPos);
            m_curCell.UpdateActorCount(1, gameObject.name);
        }

        m_lastSpeedModifierFaster = 1;
        m_lastSpeedModifierSlower = 1;
        List<StatusEffect> activeEffects = new List<StatusEffect>(m_statusEffects);
        foreach (StatusEffect activeEffect in activeEffects)
        {
            HandleEffect(activeEffect);
        }

        m_navMeshAgent.speed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
    }

    private void StartMoving(Vector3 pos)
    {
        m_navMeshAgent.SetDestination(pos);
    }

    public void OnTakeDamage(float dmg)
    {
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }

        m_hitFlashCoroutine = StartCoroutine(HitFlash());
        int i = Random.Range(0, m_enemyData.m_audioDamagedClips.Count);
        m_audioSource.PlayOneShot(m_enemyData.m_audioDamagedClips[i]);
        UpdateHealth?.Invoke(-dmg * m_curDamageMultiplier);
    }

    void OnUpdateHealth(float i)
    {
        m_curHealth += i;

        if (m_curHealth <= 0)
        {
            DestroyEnemy?.Invoke(transform.position);
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Exit"))
        {
            GameplayManager.Instance.m_castleController.TakeDamage(1);
            DestroyEnemy?.Invoke(transform.position);
        }
    }

    private IEnumerator HitFlash()
    {
        //Set the color
        for (int i = 0; i < m_allMeshRenderers.Count; ++i)
        {
            m_allMeshRenderers[i].material.SetColor("_EmissionColor", Color.red);
        }

        yield return new WaitForSeconds(.15f);

        //Return to original colors.
        for (int i = 0; i < m_allMeshRenderers.Count; ++i)
        {
            m_allMeshRenderers[i].material.SetColor("_EmissionColor", m_allOrigColors[i]);
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