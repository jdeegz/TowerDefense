using System.Collections.Generic;
using UnityEngine;

public class EnemyRunner :  EnemyController
{
    public override void StartMoving(Vector3 pos)
    {
        m_navMeshAgent.SetDestination(pos);
    }

    public override void HandleMovement()
    {
        //Update Cell occupancy
        Vector2Int newPos = Util.GetVector2IntFrom3DPos(transform.position);
        if (newPos != m_curPos)
        {
            if (m_curCell != null)
            {
                m_curCell.UpdateActorCount(-1, gameObject.name);
            }

            m_curPos = newPos;
            m_curCell = Util.GetCellFromPos(m_curPos);
            m_curCell.UpdateActorCount(1, gameObject.name);
        }

        m_navMeshAgent.speed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
        
        //Update Speed Trails
        if (m_navMeshAgent.speed > 1.0 && m_speedTrailObj != null)
        {
            m_speedTrailObj.SetActive(true);
        }
        else
        {
            m_speedTrailObj.SetActive(false);
        }
    }
}