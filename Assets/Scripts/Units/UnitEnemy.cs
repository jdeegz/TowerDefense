using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class UnitEnemy : MonoBehaviour
{
    [SerializeField] private NavMeshAgent m_navMeshAgent;
    [SerializeField] private ScriptableUnitEnemy m_enemyData;
    
    public Transform m_targetPoint;
    
    private float m_curMaxHealth;
    private float m_curHealth;
    private float m_curDamageReduction;
    
    private Vector2Int m_curPos;
    private Cell m_curCell;
    private Transform m_goal;
    private List<MeshRenderer> m_allMeshRenderers;
    private List<Color> m_allOrigColors;
    private Coroutine m_hitFlashCoroutine;
    private AudioSource m_audioSource;

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

        //Setup Life & Damage
        m_curMaxHealth = (int)MathF.Floor(m_enemyData.m_health * Mathf.Pow(1.15f, GameplayManager.Instance.m_wave));
        m_curHealth = m_curMaxHealth;
        m_curDamageReduction = m_enemyData.m_damageReduction;
        UIHealthMeter lifeMeter = Instantiate(IngameUIController.Instance.m_healthMeter, IngameUIController.Instance.transform);
        lifeMeter.SetEnemy(this, m_curMaxHealth, m_enemyData.m_healthMeterOffset, m_enemyData.m_healthMeterScale);
        CollectMeshRenderers(transform);

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
            if(m_curCell != null) { m_curCell.UpdateActorCount(-1); }
            m_curPos = newPos;
            m_curCell = Util.GetCellFromPos(m_curPos);
            m_curCell.UpdateActorCount(1);
        }
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
        UpdateHealth?.Invoke(-dmg * m_curDamageReduction);
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
}