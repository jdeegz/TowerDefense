using System.Collections.Generic;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

public class EnemySprinter : EnemyController
{
    private float m_acceleratedSpeed;
    private float m_sprintSpeedMultiplier;
    public List<VisualEffect> m_sprinterTrailVFX;
    
    public override void HandleMovement() //Mostly the same as ordinary runner.
    {
        //Update Cell occupancy
        m_newPos = Util.GetVector2IntFrom3DPos(transform.position);
        
        if (m_newPos != m_curPos)
        {
            //Cell prevCell = m_curCell; //Stash the previous cell incase we need to go back.
            m_newCell = Util.GetCellFromPos(m_newPos);

            //Check new cells occupancy.
            if (m_newCell.m_isOccupied)
            {
                //If it is occupied, we do NOT want to continue entering it. Ask our previous cell for it's new direction (assuming we've placed a tower and updated the grid)
            }
            else
            {
                //Remove self from current cell.
                if (m_curCell != null)
                {
                    m_curCell.UpdateActorCount(-1, gameObject.name);
                }
                
                //Have we made it to the goal?
                if (m_newCell.m_isGoal)
                {
                    ReachedCastle();
                    return;
                }
                
                // Is the new cell a portal? Is it also a portal entrance?
                if (m_newCell.m_directionToNextCell == Cell.Direction.Portal)
                {
                    Debug.Log($"{m_newCell.m_cellPos} is trying to teleport to {m_newCell.m_portalConnectionCell.m_cellPos}.");
                    Cell portalDestinationCell = m_newCell.m_portalConnectionCell;
   
                    BeginTeleport(portalDestinationCell);
                    
                    m_newPos = portalDestinationCell.m_cellPos;
                    m_newCell = portalDestinationCell;
                }

                //Assign new position, we are now in a new cell.
                m_curPos = m_newPos;

                //Get new cell from new position.
                m_curCell = m_newCell;

                //Assign self to cell.
                m_curCell.UpdateActorCount(1, gameObject.name);
                
                //Update distances
                m_cellsToGoal = m_curCell.m_cellDistanceFromGoal;
                ++m_cellsTravelled;
            }
            
            if (m_curCell == null) Debug.Log($"curCell is null.");
            if (m_goalCell == null) Debug.Log($"goal cell is null.");

            //Convert saved cell pos from Vector2 to Vector3
            m_curCell3dPos = new Vector3(m_curCell.m_cellPos.x, 0, m_curCell.m_cellPos.y);
            
            //Get the position of the next cell.
            //If the current cell has no direction, we go back to the previous cell.
            m_directionToPreviousCell = m_directionToNextCell * -1;
            m_directionToNextCell = m_curCell.GetDirectionVector(m_curCell.m_directionToNextCell);
            if (m_directionToNextCell != Vector2Int.zero)
            {
                m_nextCellPosition = m_curCell3dPos + new Vector3(m_directionToNextCell.x, 0, m_directionToNextCell.y);
            }
            else // We reverse our current direction, which will bring us to our previous cell..
            {
                Debug.Log($"We're in a cell with 0 direction, trying to reverse direction.");
                m_nextCellPosition = m_curCell3dPos + new Vector3(m_directionToPreviousCell.x, 0, m_directionToPreviousCell.y);
            }
            
            //Clamp saftey net.
            m_maxX = m_curCell.m_cellPos.x + .45f;
            m_minX = m_curCell.m_cellPos.x - .45f;
        
            m_maxZ = m_curCell.m_cellPos.y + .45f;
            m_minZ = m_curCell.m_cellPos.y - .45f;
            
            if (m_directionToNextCell.x < 0)
            {
                //We're going left.
                m_minX += -1;
            }
            else if (m_directionToNextCell.x > 0)
            {
                //we're going right.
                m_maxX += 1;
            }
        
            if (m_directionToNextCell.y < 0)
            {
                //We're going down.
                m_minZ += -1;
            }
            else if (m_directionToNextCell.y > 0)
            {
                //we're going up.
                m_maxZ += 1;
            }
        }

        m_moveDirection = (m_nextCellPosition - transform.position).normalized;

        //Send information to Animator
        m_angle = Vector3.SignedAngle(transform.forward, m_moveDirection, Vector3.up);
        m_animator.SetFloat("LookRotation", m_angle);
        
        //Look towards the move direction.
        m_cumulativeLookSpeed = m_baseLookSpeed * m_lastSpeedModifierFaster * Time.deltaTime;
        m_targetRotation = Quaternion.LookRotation(m_moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, m_targetRotation, m_cumulativeLookSpeed);
        

        //Move forward.
        //Sprinter move speed adjustment, slow speed at corners.
        float turningAngle = Vector3.Angle(transform.forward, m_moveDirection);
        m_moveSpeed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
        
        //Decrement accelerated speed if we're turning, and above a minimum speed. (20% of speed)
        if (turningAngle > 12 && m_acceleratedSpeed > m_moveSpeed * 0.1f)
        {
            //Debug.Log($"We're turning, current angle: {turningAngle}. We're still faster than desired turn speed: {m_acceleratedSpeed > speed * 0.2f}.");
            m_acceleratedSpeed -= m_baseMoveSpeed * 2 * Time.deltaTime;
        }

        //Increment accelerated speed if we're not turning much.
        if (turningAngle < 12 && m_acceleratedSpeed < m_moveSpeed)
        {
            m_acceleratedSpeed += m_baseMoveSpeed * 0.3f * Time.deltaTime;
        }

        m_moveSpeed = Mathf.Min(m_moveSpeed, m_acceleratedSpeed);
        
        m_cumulativeMoveSpeed = m_moveSpeed * Time.deltaTime;
        transform.position += transform.forward * m_cumulativeMoveSpeed;
        m_animator.SetFloat("Speed", m_moveSpeed);
        
        //Apply clamping
        float posX = Mathf.Clamp(transform.position.x, m_minX, m_maxX);
        float posZ = Mathf.Clamp(transform.position.z, m_minZ, m_maxZ);
        transform.position = new Vector3(posX, transform.position.y, posZ);

        foreach (VisualEffect visualEffect in m_sprinterTrailVFX)
        {
            visualEffect.SetFloat("MoveSpeed", m_moveSpeed);
        }
    }
}
