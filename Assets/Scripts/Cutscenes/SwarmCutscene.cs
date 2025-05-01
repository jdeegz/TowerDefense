using System.Collections.Generic;
using Coffee.UIEffects;
using GameUtil;
using UnityEngine;

public class SwarmCutscene : MonoBehaviour
{
    public List<EnemySwarmMember> m_swarmMembers;
    public float m_baseMoveSpeed;
    public float m_rotationSpeed;
    public float m_randomTargetRange;
    public Transform m_swarmMemberTarget;
    public bool m_isCutscene;

    private float m_deltaTime;
    private bool m_allMembersReady = false;

    void Start()
    {
        for (var index = 0; index < m_swarmMembers.Count; index++)
        {
            var member = m_swarmMembers[index];
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

        Timer.DelayAction(0.1f, () => m_allMembersReady = true, null, Timer.UpdateMode.UnscaledGameTime, null);
    }

    void Update()
    {
        if (!m_allMembersReady) return;

        m_deltaTime = m_isCutscene ? Time.unscaledDeltaTime : m_deltaTime;

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
        float speed = m_baseMoveSpeed * m_deltaTime;
        member.transform.position += member.transform.forward * speed;
    }

    bool HasReachedTarget(EnemySwarmMember member, Vector3 target)
    {
        float distance = Vector3.Distance(member.transform.position, target);
        return distance <= 0.1f; // Adjust threshold as needed
    }
}