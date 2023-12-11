using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Sequence = DG.Tweening.Sequence;

public class EnemyFlierBoss : EnemyController
{
    [HideInInspector]public BossSequenceController m_bossSequenceController;
    public AnimationCurve m_moveCurve;
    public AnimationCurve m_attackRotationCurve;
    public float m_maxRotationRotation = 2.5f; //Degrees per second
    public GameObject m_coneObj;
    public GameObject m_dragonObj;
    public Vector3 m_dragonAttackRotation;

    private Vector3 m_introductionPosition = Vector3.zero;
    private int m_lastGoal;
    private int m_curGoal;
    private int m_nextGoal;
    private Vector3 m_nextGoalPos;
    private float m_coneTimer = 99f;
    private float m_stoppingDistance;
    private float m_coneStartDelay;
    private float m_coneEndBuffer;
    private bool m_isStrafing = false;
    
    public override void HandleMovement()
    {
        //If we just spawned, travel to N units away from the castle.
        if (!m_isStrafing)
        {
            if (m_introductionPosition == Vector3.zero)
            {
                m_introductionPosition = GameplayManager.Instance.m_castleController.transform.position;
                m_introductionPosition.z += 4.5f;
            }
            
            //Movement
            float speed = m_baseMoveSpeed;
            Vector3 direction = (m_introductionPosition - transform.position).normalized;
            transform.Translate(speed * Time.deltaTime * direction, Space.World);
        
            //Rotation
            Quaternion lookRotation = Quaternion.LookRotation((m_introductionPosition - transform.position).normalized);
            transform.rotation = lookRotation;
            
            //Check to see if we should stop this movement.
            if (Vector3.Distance(transform.position, m_introductionPosition) <= 0.1f)
            {
                BeginStrafe();
            }
        }
    }

    public void Update()
    {
        m_coneTimer += Time.deltaTime;
        HandleCone();
    }

    void BeginStrafe()
    {
        m_isStrafing = true;
        //Get the closest cell and set it as last and current goals. Then find a new goal.
        int pos = m_bossSequenceController.GetCellIndexFromVector3(m_introductionPosition);
        m_lastGoal = pos;
        m_curGoal = pos;
        m_nextGoal = m_bossSequenceController.GetNextGridCell(m_lastGoal, m_curGoal);
        m_nextGoalPos = m_bossSequenceController.GetNextGoalPosition(m_nextGoal);

        Vector3 targetDirection = m_nextGoalPos - transform.position;
        float degreesToRotate = Vector3.Angle(transform.forward, targetDirection);
        float rotationDuration = degreesToRotate / m_baseLookSpeed;
        rotationDuration = Math.Min(rotationDuration, m_maxRotationRotation);
        transform.DOLookAt(m_nextGoalPos, rotationDuration, AxisConstraint.Y).SetEase(Ease.InOutQuad).OnComplete(HandleStrafe);
        
        HandleStrafe();
    }
    
    void UpdateStrafeDestination()
    {
        Debug.Log($"Updating Strafe Destination");
        m_lastGoal = m_curGoal;
        m_curGoal = m_nextGoal;
        m_nextGoal = m_bossSequenceController.GetNextGridCell(m_lastGoal, m_curGoal);
        m_nextGoalPos = m_bossSequenceController.GetNextGoalPosition(m_nextGoal);
        HandleStrafe();
    }

    void HandleStrafe()
    {
        Debug.Log($"Handling Strafe to {m_nextGoalPos}");
        //Define the duration of the DOMove to create a consistent movespeed per path point.
        Vector3 m_curGoalPos = new Vector3(m_bossSequenceController.m_bossGridCellPositions[m_curGoal].x, 0, m_bossSequenceController.m_bossGridCellPositions[m_curGoal].y);
        float moveDistance = Vector3.Distance(transform.position, m_curGoalPos);
        float moveDuration = moveDistance / m_baseMoveSpeed;
        
        //Define the rotation speed. Goal is to rotate the dragon a consistent amount of degrees/second rather than faster rotations for larger degree deltas.
        Vector3 targetDirection = m_nextGoalPos - transform.position;
        float degreesToRotate = Vector3.Angle(transform.forward, targetDirection);
        float rotationDuration = degreesToRotate / m_baseLookSpeed;
        rotationDuration = Math.Min(rotationDuration, m_maxRotationRotation);
        float rotationDelay = moveDuration * 0.7f;
        
        SetConeTimes(moveDuration);
        
        //Build the Dotween sequence. We want Move to start and for rotate to play with a delay relative to the duration of the move.
        Sequence curSequence = DOTween.Sequence();
    
        //Move to our next goal.
        curSequence.Append(transform.DOMove(m_curGoalPos, moveDuration).SetEase(m_moveCurve));
        
        //Rotate the dragon object down during the move.
        curSequence.Join(m_dragonObj.transform.DOLocalRotate(m_dragonAttackRotation, moveDuration).SetEase(m_attackRotationCurve));

        //Look towards the next goal as we come to the end of our movement.
        curSequence.Join(transform.DOLookAt(m_nextGoalPos, rotationDuration, AxisConstraint.Y).SetEase(Ease.InOutQuad).SetDelay(rotationDelay));

        
        //Find the next destination when we're done.
        curSequence.OnComplete(() => UpdateStrafeDestination());
        
        curSequence.Play();
    }

    void SetConeTimes(float moveDuration)
    {
        //Define the start delay. We want the smallest between our defined delay or a % of the move duration.
        float startDelay = moveDuration * 0.20f;
        m_coneStartDelay = startDelay;

        //We definitely want the cone to be disabled as we start to rotate.
        m_coneEndBuffer = moveDuration * 0.65f;
        
        //Reset the cone timer.
        m_coneTimer = 0;
    }
    
    void HandleCone()
    {
        //If the cone is disabled, and we're after start, before end, turn on cone.
        if (!m_coneObj.activeSelf && m_coneTimer > m_coneStartDelay && m_coneTimer < m_coneEndBuffer)
        {
            m_coneObj.SetActive(true);
        }
        else if(m_coneObj.activeSelf && (m_coneTimer < m_coneStartDelay || m_coneTimer > m_coneEndBuffer))
        {
            m_coneObj.SetActive(false);
        }
    }
}
