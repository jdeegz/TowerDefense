using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class UnitEnemy : MonoBehaviour
{
    private Transform m_goal;
    private NavMeshAgent m_navMeshAgent;
    public Transform m_targetPoint;

    public float m_moveSpeed;
    [SerializeField] private int m_hitPoints = 2;
    private List<MeshRenderer> m_allMeshRenderers;
    private List<Color> m_allOrigColors;
    private Coroutine m_hitFlashCoroutine;


    void Start()
    {
        CollectMeshRenderers(transform);

        //Find the closest Castle collider
        if (GameplayManager.Instance != null)
        {
            m_goal = GetClosestTransform(GameplayManager.Instance.m_enemyGoals);
        }

        m_navMeshAgent = GetComponent<NavMeshAgent>();
        StartMoving(m_goal.position);
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
        if (m_navMeshAgent.remainingDistance <= m_navMeshAgent.stoppingDistance)
        {
            Destroy(gameObject);
        }
    }

    private void StartMoving(Vector3 pos)
    {
        m_navMeshAgent.SetDestination(pos);
    }

    public void TakeDamage(int dmg)
    {
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }

        m_hitFlashCoroutine = StartCoroutine(HitFlash());

        m_hitPoints -= dmg;

        if (m_hitPoints <= 0)
        {
            Destroy(gameObject);
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
        m_navMeshAgent.isStopped = GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.Paused;
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