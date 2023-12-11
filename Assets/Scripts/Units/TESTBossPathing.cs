using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class TESTBossPathing : MonoBehaviour
{
    public GameObject[] m_pathPoints;
    public GameObject m_bossObj;
    public float m_moveSpeed = 3f;
    public AnimationCurve m_customCurve;
    public AnimationCurve m_attackRotationCurve;
    public float m_rotationSpeed = 90f; //Degrees per second
    public float m_maxRotationRotation = 2.5f; //Degrees per second
    public float m_maxConeStartDelay = 0.25f;
    public GameObject m_coneObj;
    public GameObject m_dragonObj;
    public Vector3 m_dragonAttackRotation;

    private int m_curGoal;
    private int m_nextGoal;
    private float m_coneTimer;
    private float m_stoppingDistance;
    private float m_coneStartDelay;
    private float m_coneEndBuffer;
    
    // Start is called before the first frame update
    void Start()
    {
        UpdateDestination();
    }

    // Update is called once per frame
    void Update()
    {
        m_coneTimer += Time.deltaTime;
        HandleCone();
    }

    void UpdateDestination()
    {
        m_curGoal = m_nextGoal;
        ++m_nextGoal;
        if (m_nextGoal >= m_pathPoints.Length) m_nextGoal = 0;
        HandleMovement();
    }

    void HandleMovement()
    {
        //Define the duration of the DOMove to create a consistent movespeed per path point.
        float moveDistance = Vector3.Distance(m_bossObj.transform.position, m_pathPoints[m_curGoal].transform.position);
        float moveDuration = moveDistance / m_moveSpeed;
        
        //Define the rotation speed. Goal is to rotate the dragon a consistent amount of degrees/second rather than faster rotations for larger degree deltas.
        Vector3 targetDirection = m_pathPoints[m_nextGoal].transform.position - m_bossObj.transform.position;
        float degreesToRotate = Vector3.Angle(m_bossObj.transform.forward, targetDirection);
        float rotationDuration = degreesToRotate / m_rotationSpeed;
        rotationDuration = Math.Min(rotationDuration, m_maxRotationRotation);
        float rotationDelay = moveDuration * 0.7f;
        
        SetConeTimes(moveDuration);
        
        //Build the Dotween sequence. We want Move to start and for rotate to play with a delay relative to the duration of the move.
        Sequence curSequence = DOTween.Sequence();
    
        //Move to our next goal.
        curSequence.Append(m_bossObj.transform.DOMove(m_pathPoints[m_curGoal].transform.position, moveDuration).SetEase(m_customCurve));
        
        //Rotate the dragon object down during the move.
        curSequence.Join(m_dragonObj.transform.DOLocalRotate(m_dragonAttackRotation, moveDuration).SetEase(m_attackRotationCurve));

        //Look towards the next goal as we come to the end of our movement.
        curSequence.Join(m_bossObj.transform.DOLookAt(m_pathPoints[m_nextGoal].transform.position, rotationDuration, AxisConstraint.Y).SetEase(Ease.InOutQuad).SetDelay(rotationDelay));

        
        //Find the next destination when we're done.
        curSequence.OnComplete(() => UpdateDestination());
        
        curSequence.Play();
    }

    void SetConeTimes(float moveDuration)
    {
        //Define the start delay. We want the smallest between our defined delay or a % of the move duration.
        float startDelay = moveDuration * 0.25f;
        m_coneStartDelay = startDelay;

        //We definitely want the cone to be disabled as we start to rotate.
        m_coneEndBuffer = moveDuration * 0.7f;
        
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