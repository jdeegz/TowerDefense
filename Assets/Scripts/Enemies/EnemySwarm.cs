using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class EnemySwarm : MonoBehaviour
{
    public List<EnemySwarmMember> m_swarmMembers;
    public int m_swarmSize;
    public EnemyData m_swarmMemberData;
    public float m_baseMoveSpeed = 2f;
    public float m_rotationSpeed = 2f;
    public float m_neighborDistance = 0.3f;
    public float m_separationDistance = 0.25f;
    public float m_jitterAmount = 0.1f;
    public float m_startingSpread = 1.5f;
    public float m_maxDistanceFromTarget = 2f;
    public Transform m_swarmMemberTarget;
    public Transform m_swarmMemberRoot;
    
    private float m_spreadTimer;
    private float m_nextSpreadTime;
    private EnemyController m_motherEnemyController;
    private float m_cumulativeMoveSpeed;
    private float m_groundClampValue = 0.2f;


    void Awake()
    {
        m_motherEnemyController = GetComponent<EnemyController>();
    }
    
    void OnEnable()
    {
        m_swarmMembers = new List<EnemySwarmMember>();
        
        for (int i = 0; i < m_swarmSize; ++i)
        {
            GameObject enemyOjb = ObjectPoolManager.SpawnObject(m_swarmMemberData.m_enemyPrefab, m_swarmMemberRoot);
            EnemySwarmMember swarmMemberController = enemyOjb.GetComponent<EnemySwarmMember>();
            swarmMemberController.SetEnemyData(m_swarmMemberData);
            swarmMemberController.SetMother(m_motherEnemyController);
            m_swarmMembers.Add(swarmMemberController);
        }
        RandomizeMembers();
    }

    void RandomizeMembers()
    {
        foreach (EnemySwarmMember member in m_swarmMembers)
        {
            Vector3 randomPosition = Random.insideUnitSphere * m_startingSpread;
            member.transform.position = m_swarmMemberTarget.position + randomPosition;
            
            if (member.transform.position.y < m_groundClampValue) // Make sure we dont spawn below the ground.
            {
                Vector3 clampedPosition = member.transform.position;
                clampedPosition.y = m_groundClampValue;
                member.transform.position = clampedPosition;
            }

            Quaternion randomRotation = Random.rotation;
            member.transform.rotation = randomRotation;
        }
    }

    void Update()
    {
        
        var speeds = m_motherEnemyController.GetMoveSpeedModifiers();
        m_cumulativeMoveSpeed = m_baseMoveSpeed * speeds.Item1 * speeds.Item2 * Time.deltaTime;
        
        foreach (EnemySwarmMember member in m_swarmMembers)
        {
            Vector3 steering = CalculateSteering(member.gameObject);
            steering += GetRandomJitter();
            HandleSwarmMove(member.gameObject, steering);
            ClampPosition(member.gameObject);
        }
    }

    void ClampPosition(GameObject member)
    {
        Vector3 directionToTarget = member.transform.position - m_swarmMemberTarget.position;
        if (directionToTarget.magnitude > m_maxDistanceFromTarget)
        {
            member.transform.position = m_swarmMemberTarget.position + directionToTarget.normalized * m_maxDistanceFromTarget;
        }
        
        if (member.transform.position.y < m_groundClampValue)
        {
            Vector3 clampedPosition = member.transform.position;
            clampedPosition.y = m_groundClampValue;
            member.transform.position = clampedPosition;
        }
    }

    Vector3 CalculateSteering(GameObject member)
    {
        Vector3 steering = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;

        int count = 0;

        foreach (EnemySwarmMember otherMember in m_swarmMembers)
        {
            if (otherMember == member) continue;

            float distance = Vector3.Distance(member.transform.position, otherMember.transform.position);

            if (distance < m_neighborDistance)
            {
                cohesion += otherMember.transform.position;
                alignment += otherMember.transform.forward;

                if (distance < m_separationDistance)
                {
                    separation -= (otherMember.transform.position - member.transform.position);
                }

                ++count;
            }
        }

        if (count > 0)
        {
            cohesion /= count;
            cohesion = (cohesion - member.transform.position).normalized;

            alignment /= count;
            alignment = alignment.normalized;
        }

        Vector3 seek = (m_swarmMemberTarget.position - member.transform.position).normalized;

        steering = seek + cohesion + separation + alignment;
        steering = steering.normalized;

        return steering;
    }

    Vector3 GetRandomJitter()
    {
        return new Vector3(
            Random.Range(-m_jitterAmount, m_jitterAmount),
            Random.Range(-m_jitterAmount, m_jitterAmount),
            Random.Range(-m_jitterAmount, m_jitterAmount));
    }

    void HandleSwarmMove(GameObject member, Vector3 steering)
    {
        Quaternion rotation = Quaternion.LookRotation(steering);
        float rotationSpeed = m_rotationSpeed * (m_cumulativeMoveSpeed / m_baseMoveSpeed);
        member.transform.rotation = Quaternion.Slerp(member.transform.rotation, rotation, rotationSpeed);
        member.transform.position += member.transform.forward * m_cumulativeMoveSpeed;
    }
}