using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class UnitEnemy : MonoBehaviour
{
    [SerializeField] private NavMeshAgent m_navMeshAgent;
    
    public Transform m_targetPoint;
    public float m_moveSpeed;
    public int m_maxHealth = 2;
    public float m_healthMeterOffset;
    
    private int m_curHealth;
    private Transform m_goal;
    private List<MeshRenderer> m_allMeshRenderers;
    private List<Color> m_allOrigColors;
    private Coroutine m_hitFlashCoroutine;
    private Vector2Int m_curPos;
    private Cell m_curCell;
    private AudioSource m_audioSource;

    public event Action<int> UpdateHealth;
    public event Action DestroyEnemy;

    void Start()
    {
        CollectMeshRenderers(transform);
        m_goal = GameplayManager.Instance.m_enemyGoal;
        
        GameplayManager.Instance.AddEnemyToList(this);
        UIHealthMeter lifeMeter = Instantiate(IngameUIController.Instance.m_healthMeter, IngameUIController.Instance.transform);
        lifeMeter.SetEnemy(this);

        StartMoving(m_goal.position);

        //Health Scaling.
        m_curHealth = (int)MathF.Floor(m_maxHealth * Mathf.Pow(1.15f, GameplayManager.Instance.m_wave));
        UpdateHealth += OnUpdateHealth;
        DestroyEnemy += OnEnemyDestroyed;
        
        m_curPos = new Vector2Int((int)Mathf.Floor(transform.position.x + 0.5f), (int)Mathf.Floor(transform.position.z + 0.5f));
        m_curCell = Util.GetCellFromPos(m_curPos);
        m_curCell.UpdateActorCount(1);

        m_audioSource = GetComponent<AudioSource>();
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

    private Transform GetClosestTransform(Transform[] transforms)
    {
        Transform closestTransform = null;
        float closestDistance = Mathf.Infinity;
        Vector3 curPos = transform.position;

        foreach (Transform t in transforms)
        {
            float distance = Vector3.Distance(t.position, curPos);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTransform = t;
            }
        }

        return closestTransform;
    }

    void Update()
    {
        Vector2Int newPos = new Vector2Int((int)Mathf.Floor(transform.position.x + 0.5f), (int)Mathf.Floor(transform.position.z + 0.5f));
        
        if(newPos != m_curPos){
            m_curCell.UpdateActorCount(-1);
            m_curPos = newPos;
            m_curCell = Util.GetCellFromPos(m_curPos);
            m_curCell.UpdateActorCount(1);
        }
    }

    private void StartMoving(Vector3 pos)
    {
        m_navMeshAgent.SetDestination(pos);
    }

    public void TakeDamage(int dmg, AudioClip audioClip)
    {
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }

        m_hitFlashCoroutine = StartCoroutine(HitFlash());
        m_audioSource.PlayOneShot(audioClip);
        UpdateHealth?.Invoke(-dmg);
    }

    void OnUpdateHealth(int i)
    {
        m_curHealth += i;
        
        if (m_curHealth <= 0)
        {
            DestroyEnemy?.Invoke();
        }
    }

    void OnEnemyDestroyed()
    {
        GameplayManager.Instance.RemoveEnemyFromList(this);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Exit"))
        {
            GameplayManager.Instance.m_castleController.TakeDamage(1);
            DestroyEnemy?.Invoke();
        }
    }

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState state)
    {
        //m_navMeshAgent.isStopped = GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.Paused;
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