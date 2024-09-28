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
        m_startPos = transform.localPosition;
        m_endPos = m_startPos + m_relativePos;
        m_goalPos = m_endPos;
    }

    public override void SetupEnemy(bool active)
    {
        m_isComplete = false;
        
        m_curMaxHealth = (int)MathF.Floor(m_enemyData.m_health);
        m_curHealth = m_curMaxHealth;

        //Setup Hit Flash
        CollectMeshRenderers(m_enemyModelRoot.transform);
        
        //Define AudioSource
        m_audioSource = GetComponent<AudioSource>();
        m_audioSource.PlayOneShot(m_enemyData.m_audioSpawnClip);
        
        if (m_statusEffects != null) m_statusEffects.Clear();
        if (m_expiredStatusEffects != null) m_expiredStatusEffects.Clear();
        if (m_newStatusEffects != null) m_newStatusEffects.Clear();
        m_statusEffects = new List<StatusEffect>();
        
        SetEnemyActive(active);
    }
    
    public override void HandleMovement()
    {
        /*//Move forward.
        float cumulativeMoveSpeed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, m_goalPos, cumulativeMoveSpeed);

        m_currentSpeed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
        
        //Check if we're at the destination.
        CheckDestination();*/
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
