using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyFlier : EnemyController
{

    public override void HandleMovement()
    {
        //Movement
        float speed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
        Vector3 direction = (m_goal.position - transform.position).normalized;
        transform.Translate(speed * Time.deltaTime * direction, Space.World);
        
        //Rotation
        Quaternion lookRotation = Quaternion.LookRotation((m_goal.position - transform.position).normalized);
        transform.rotation = lookRotation;
    }
}