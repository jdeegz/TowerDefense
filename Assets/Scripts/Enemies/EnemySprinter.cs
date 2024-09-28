using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemySprinter : EnemyController
{
    private float m_acceleratedSpeed;
    private float m_sprintSpeedMultiplier;
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
            
            Vector2 nextCellPosOffset = new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f)) * m_enemyData.m_movementWiggleValue;

            //Convert saved cell pos from Vector2 to Vector3
            Vector3 m_curCell3dPos = new Vector3(m_curCell.m_cellPos.x, 0, m_curCell.m_cellPos.y);

            //Get the position of the next cell.
            
            //Most common path
            m_nextCellPosition = m_curCell3dPos + new Vector3(m_curCell.m_directionToNextCell.x + nextCellPosOffset.x, 0, m_curCell.m_directionToNextCell.z + nextCellPosOffset.y);
            
            //Clamp saftey net.
            m_maxX = m_curCell.m_cellPos.x + .45f;
            m_minX = m_curCell.m_cellPos.x - .45f;
        
            m_maxZ = m_curCell.m_cellPos.y + .45f;
            m_minZ = m_curCell.m_cellPos.y - .45f;
            
            if (m_curCell.m_directionToNextCell.x < 0)
            {
                //We're going left.
                m_minX += -1;
            }
            else if (m_curCell.m_directionToNextCell.x > 0)
            {
                //we're going right.
                m_maxX += 1;
            }
        
            if (m_curCell.m_directionToNextCell.z < 0)
            {
                //We're going down.
                m_minZ += -1;
            }
            else if (m_curCell.m_directionToNextCell.z > 0)
            {
                //we're going up.
                m_maxZ += 1;
            }
        }

        m_moveDirection = (m_nextCellPosition - transform.position).normalized;

        //Send information to Animator
        float angle = Vector3.SignedAngle(transform.forward, m_moveDirection, Vector3.up);
        m_animator.SetFloat("LookRotation", angle);
        
        //Look towards the move direction.
        float cumulativeLookSpeed = m_baseLookSpeed * m_lastSpeedModifierFaster * Time.deltaTime;
        Quaternion targetRotation = Quaternion.LookRotation(m_moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, cumulativeLookSpeed);

        //Move forward.
        //Sprinter move speed adjustment, slow speed at corners.
        float turningAngle = Vector3.Angle(transform.forward, m_moveDirection);
        float speed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
        
        //Decrement accelerated speed if we're turning, and above a minimum speed. (20% of speed)
        if (turningAngle > 20 && m_acceleratedSpeed > speed * 0.2f)
        {
            Debug.Log($"We're turning, current angle: {turningAngle}. We're still faster than desired turn speed: {m_acceleratedSpeed > speed * 0.2f}.");
            m_acceleratedSpeed -= m_baseMoveSpeed * Time.deltaTime;
        }

        //Increment accelerated speed if we're not turning much.
        if (turningAngle < 20 && m_acceleratedSpeed < speed)
        {
            m_acceleratedSpeed += m_baseMoveSpeed * 0.3f * Time.deltaTime;
        }

        speed = Mathf.Min(speed, m_acceleratedSpeed);
        
        float cumulativeMoveSpeed = speed * Time.deltaTime;
        transform.position += transform.forward * cumulativeMoveSpeed;
        m_animator.SetFloat("Speed", speed);
        
        //Apply clamping
        float posX = Mathf.Clamp(transform.position.x, m_minX, m_maxX);
        float posZ = Mathf.Clamp(transform.position.z, m_minZ, m_maxZ);
        transform.position = new Vector3(posX, transform.position.y, posZ);
    }
}
