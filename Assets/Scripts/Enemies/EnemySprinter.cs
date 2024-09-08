using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemySprinter : EnemyController
{
    public override void HandleMovement() //Mostly the same as ordinary runner.
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
        //Sprinter move speed adjustment, slow speed at corners. Any angle greater than Clamped Value returns 0 move speed.
        float sprintSpeedMultiplier = Vector3.Angle(transform.forward, m_moveDirection);
        sprintSpeedMultiplier = Mathf.Min(sprintSpeedMultiplier, 60); //Clamp to 60
        sprintSpeedMultiplier = 1 - (sprintSpeedMultiplier / 60f); //Reverse normalization
        
        //Debug.Log($"{sprintSpeedMultiplier} sprinting multiplier.");
        float speed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower * sprintSpeedMultiplier;
        float cumulativeMoveSpeed = speed * Time.deltaTime;
        transform.position += transform.forward * cumulativeMoveSpeed;
        m_animator.SetFloat("Speed", speed);
        
        //Send information to Animator
        float angle = Vector3.SignedAngle(transform.forward, m_moveDirection, Vector3.up);
        m_animator.SetFloat("LookRotation", angle);
    }
}
