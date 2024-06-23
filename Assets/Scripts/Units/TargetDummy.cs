using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetDummy : EnemyController
{
    private Vector3 m_startPos;
    private Vector3 m_endPos;
    private Vector3 m_goalPos;
    
    public Vector3 m_relativePos;

    public float m_currentSpeed;
    
    void Start()
    {
        SetEnemyData(m_enemyData);
        m_startPos = transform.position;
        m_endPos = m_startPos + m_relativePos;
        m_goalPos = m_endPos;
    }
    
    public override void HandleMovement()
    {
        //Move forward.
        float cumulativeMoveSpeed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, m_goalPos, cumulativeMoveSpeed);

        m_currentSpeed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
        
        //Check if we're at the destination.
        CheckDestination();
    }

    private void CheckDestination()
    {
        if (Vector3.Distance(transform.position, m_goalPos) < 0.1f)
        {
            //We're at our goal.
            m_goalPos = m_goalPos == m_endPos ? m_startPos : m_endPos;
        }
    }
}
