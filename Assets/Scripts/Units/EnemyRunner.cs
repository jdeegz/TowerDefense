using System.Collections.Generic;
using UnityEngine;

public class EnemyRunner : EnemyController
{
    private Vector3 m_moveDirection;

    public override void StartMoving(Vector3 pos)
    {
    }

    public override void HandleMovement()
    {
        //Update Cell occupancy
        Vector2Int newPos = Util.GetVector2IntFrom3DPos(transform.position);
        if (newPos != m_curPos)
        {
            //Remove self from current cell.
            if (m_curCell != null)
            {
                m_curCell.UpdateActorCount(-1, gameObject.name);
            }

            //Assign new position
            m_curPos = newPos;

            //Get new cell from new position.
            m_curCell = Util.GetCellFromPos(m_curPos);

            //Assign self to cell.
            m_curCell.UpdateActorCount(1, gameObject.name);
        }
        //Convert saved cell pos from Vector2 to Vector3
        Vector3 m_curCell3dPos = new Vector3(m_curCell.m_cellPos.x, 0, m_curCell.m_cellPos.y);

        //Get the position of the next cell.
        Vector3 m_nextCellPosition = m_curCell3dPos + new Vector3(m_curCell.m_directionToNextCell.x, 0, m_curCell.m_directionToNextCell.z);

        m_moveDirection = (m_nextCellPosition - transform.position).normalized;

        //Look towards the move direction.
        float cumulativeLookSpeed = m_baseLookSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower * Time.deltaTime;
        Quaternion targetRotation = Quaternion.LookRotation(m_moveDirection);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, cumulativeLookSpeed);

        //Move forward.
        float cumulativeMoveSpeed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower * Time.deltaTime;
        transform.position += transform.forward * cumulativeMoveSpeed;


        //Update Speed Trails
        if (cumulativeMoveSpeed > 1.0 && m_speedTrailVFXObj != null)
        {
            m_speedTrailVFXObj.SetActive(true);
        }
        else
        {
            m_speedTrailVFXObj.SetActive(false);
        }
    }
}