using System;
using System.Collections.Generic;
using GameUtil;
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
    public Transform m_swarmMemberTarget;
    public Transform m_swarmMemberRoot;
    public float m_randomTargetRange;
    public bool m_isCutscene;

    private float m_deltaTime;
    private EnemyController m_motherEnemyController;
    private float m_cumulativeMoveSpeed;
    private float m_groundClampValue = 0.2f;
    private bool m_allMembersReady = false;
    
    void Awake()
    {
        m_motherEnemyController = GetComponent<EnemyController>();
    }
    
    void OnEnable()
    {
        if (m_swarmMembers == null || m_swarmMembers.Count == 0) return;

        m_allMembersReady = false;
        
        foreach (EnemySwarmMember member in m_swarmMembers)
        {
            member.transform.localPosition = Vector3.zero;
        }
        
        AssignRandomTargets();
    }
    
    void AssignRandomTargets()
    {
        for (var index = 0; index < m_swarmMembers.Count; index++)
        {
            var member = m_swarmMembers[index];
            member.GetRandomTargetAround(m_swarmMemberTarget.position, m_randomTargetRange);
        }

        Timer.DelayAction(0.33f, () => m_allMembersReady = true);
    }

    void Start()
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
        
        AssignRandomTargets();
    }

    void Update()
    {
        if (!m_allMembersReady) return;

        m_deltaTime = m_isCutscene ? Time.unscaledDeltaTime : Time.deltaTime;
        
        var speeds = m_motherEnemyController.GetMoveSpeedModifiers();
        m_cumulativeMoveSpeed = m_baseMoveSpeed * speeds.Item1 * speeds.Item2 * m_deltaTime;
        
        foreach (EnemySwarmMember member in m_swarmMembers)
        {
            Vector3 targetPos = member.GetCurrentTarget();

            if (HasReachedTarget(member, targetPos) || member.IsTargetTimedOut(m_deltaTime))
            {
                // If the member reaches the target or times out, assign a new target
                member.GetRandomTargetAround(m_swarmMemberTarget.position, m_randomTargetRange);
            }

            // Move the member towards the target
            MoveMemberTowards(member, targetPos);
        }
    }
    
    void MoveMemberTowards(EnemySwarmMember member, Vector3 target)
    {
        Vector3 direction = (target - member.transform.position).normalized;

        // Rotate towards the target
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            member.transform.rotation = Quaternion.Slerp(member.transform.rotation, targetRotation, m_rotationSpeed * m_deltaTime);
        }

        // Move 
        member.transform.position += member.transform.forward * m_cumulativeMoveSpeed;
    }
    
    bool HasReachedTarget(EnemySwarmMember member, Vector3 target)
    {
        float distance = Vector3.Distance(member.transform.position, target);
        return distance <= 0.1f; // Adjust threshold as needed
    }
}