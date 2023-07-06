using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitEnemy : MonoBehaviour
{
    private Transform m_goal;
    private NavMeshAgent m_navMeshAgent;

    public float m_moveSpeed;

    // Start is called before the first frame update
    void Start()
    {
        //Find the closest Castle collider
        m_goal = GetClosestTransform(GameplayManager.Instance.m_enemyGoals);
        m_navMeshAgent = GetComponent<NavMeshAgent>();
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

    // Update is called once per frame
    void Update()
    {
        m_navMeshAgent.destination = m_goal.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Castle"))
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
}