using System.Collections.Generic;
using UnityEngine;

public class EnemySwarm : MonoBehaviour
{
    public List<GameObject> m_swarmMembers;
    public float m_speed = 2f;
    public float m_rotationSpeed = 2f;
    public float m_neighborDistance = 0.3f;
    public float m_separationDistance = 0.25f;
    public float m_jitterAmount = 0.1f;
    public float m_startingSpread = 1.5f;
    public float m_maxDistanceFromTarget = 2f;
    public Transform m_targetPoint;
    
    private float m_spreadTimer;
    private float m_nextSpreadTime;

    void Start()
    {
        RandomizeMembers();
    }

    void RandomizeMembers()
    {
        foreach (GameObject member in m_swarmMembers)
        {
            Vector3 randomPosition = Random.insideUnitSphere * m_startingSpread;
            member.transform.position = m_targetPoint.position + randomPosition;

            Quaternion randomRotation = Random.rotation;
            member.transform.rotation = randomRotation;
        }
    }

    void Update()
    {
        foreach (GameObject member in m_swarmMembers)
        {
            Vector3 steering = CalculateSteering(member);
            steering += GetRandomJitter();
            HandleSwarmMove(member, steering);
            ClampPosition(member);
        }
    }

    void ClampPosition(GameObject member)
    {
        Vector3 directionToTarget = member.transform.position - m_targetPoint.position;
        if (directionToTarget.magnitude > m_maxDistanceFromTarget)
        {
            member.transform.position = m_targetPoint.position + directionToTarget.normalized * m_maxDistanceFromTarget;
        }
    }

    Vector3 CalculateSteering(GameObject member)
    {
        Vector3 steering = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;

        int count = 0;

        foreach (GameObject otherMember in m_swarmMembers)
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

        Vector3 seek = (m_targetPoint.position - member.transform.position).normalized;

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
        member.transform.rotation = Quaternion.Slerp(member.transform.rotation, rotation, m_rotationSpeed * Time.deltaTime);
        member.transform.position += member.transform.forward * (m_speed * Time.deltaTime);
    }
}