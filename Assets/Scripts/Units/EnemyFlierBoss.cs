using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Sequence = DG.Tweening.Sequence;

public class EnemyFlierBoss : EnemyController
{
    [HideInInspector] public BossSequenceController m_bossSequenceController;
    public GameObject m_muzzleObj;

    private int m_curGoal;
    private Vector3 m_curGoalPos;
    private Vector3 m_castlePos;
    
    private bool m_isStrafing = false;
    private float m_coneStartDelay;
    private float m_coneEndBuffer;
    private float m_moveDistance;
    private float m_distanceTravelled;
    private int m_moveCounter;
    private float m_rotationThreadhold = 0.999f;
    private Coroutine m_curCoroutine;
    
    private BossState m_bossState;
    private enum BossState
    {
        Idle,
        RotateToDestination,
        MoveToDestination,
        RotateToTarget,
        AttackTarget,
        Death,
    }

    void Start()
    {
        //If we just spawned, travel to N units away from the castle.
        m_curGoal = 0;
        m_curGoalPos = m_bossSequenceController.GetNextGoalPosition(m_curGoal);
        m_castlePos = GameplayManager.Instance.m_castleController.transform.position;
        transform.rotation = Quaternion.LookRotation(m_curGoalPos - transform.position);
        UpdateBossState(BossState.MoveToDestination);
    }

    public override void HandleMovement()
    {
        
    }

    void UpdateBossState(BossState newState)
    {
        m_bossState = newState;
        switch (m_bossState)
        {
            case BossState.Idle:
                break;
            case BossState.RotateToDestination:
                break;
            case BossState.MoveToDestination:
                break;
            case BossState.RotateToTarget:
                break;
            case BossState.AttackTarget:
                m_curCoroutine = StartCoroutine(Attack());
                break;
            case BossState.Death:
                if(m_curCoroutine != null) StopCoroutine(m_curCoroutine);
                //Do boss death stuff.
                //Spawn 4 seekers.
                //Have to keep gameplay state from switching due to not having alive enemies.
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private IEnumerator Attack()
    {
        yield return new WaitForSeconds(((BossEnemyData)m_enemyData).m_attackDelay);
        HandleAttack();
        yield return new WaitForSeconds(((BossEnemyData)m_enemyData).m_attackCooldown);
        UpdateBossState(BossState.RotateToDestination);
    }
    
    private IEnumerator UpdateStateAfterDelay(float i, BossState newState)
    {
        yield return new WaitForSeconds(i);
        UpdateBossState(newState);
    }
    
    private void HandleAttack()
    {
        GameObject projectileObj = Instantiate(((BossEnemyData)m_enemyData).m_projectileObj, m_muzzleObj.transform.position, m_muzzleObj.transform.rotation);
    }
    
    public void Update()
    {
        switch (m_bossState)
        {
            case BossState.Idle:
                break;
            case BossState.RotateToDestination:
                // Calculate the rotation to face the target
                Quaternion rotationToDestination = Quaternion.LookRotation(m_curGoalPos - transform.position);
                float rotationToDestinationDotProduct = Mathf.Abs(Quaternion.Dot(transform.rotation, rotationToDestination));
                
                transform.rotation = Quaternion.Slerp(transform.rotation, rotationToDestination, m_baseLookSpeed * Time.deltaTime);
                
                if (rotationToDestinationDotProduct >= m_rotationThreadhold)
                {
                    m_curCoroutine = StartCoroutine(UpdateStateAfterDelay(1, BossState.MoveToDestination));   
                }
                break;
            case BossState.MoveToDestination:
                if (m_moveCounter % ((BossEnemyData)m_enemyData).m_strafeAttackRate != 0) HandleCone();
                
                //Movement
                float speed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
                Vector3 direction = (m_curGoalPos - transform.position).normalized;
                transform.Translate(speed * Time.deltaTime * direction, Space.World);

                //Set the distance travelled
                m_distanceTravelled = m_moveDistance - Vector3.Distance(transform.position, m_curGoalPos);

                //Check if we're at our destination.
                if (Vector3.Distance(transform.position, m_curGoalPos) <= 0.05f)
                {
                    //Get the next destination, even if we're attacking right now.
                    UpdateMoveDestination();
                    SetConeDistances();
                    
                    
                    //Do we need to Rotate to a new Destination, or Rotate to attack the castle?
                    if (m_moveCounter % ((BossEnemyData)m_enemyData).m_castleAttackRate == 0)
                    {
                        UpdateBossState(BossState.RotateToTarget);
                    }
                    else
                    {
                        UpdateBossState(BossState.RotateToDestination);
                    }
                }
                break;
            case BossState.RotateToTarget:
                // Calculate the rotation to face the target
                Quaternion rotationToCastle = Quaternion.LookRotation(m_castlePos - transform.position);
                float rotationToCastledotProduct = Mathf.Abs(Quaternion.Dot(transform.rotation, rotationToCastle));
                
                transform.rotation = Quaternion.Slerp(transform.rotation, rotationToCastle, m_baseLookSpeed * Time.deltaTime);
                
                if (rotationToCastledotProduct >= m_rotationThreadhold)
                {
                    UpdateBossState(BossState.AttackTarget);    
                }
                break;
            case BossState.AttackTarget:
                break;
            case BossState.Death:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void UpdateMoveDestination()
    {
        m_isStrafing = true;
        ++m_curGoal;
        if (m_curGoal == m_bossSequenceController.m_bossGridCellPositions.Count)
        {
            m_curGoal = 0;
        }
        ++m_moveCounter;
        m_curGoalPos = m_bossSequenceController.GetNextGoalPosition(m_curGoal);
        m_moveDistance = Vector3.Distance(transform.position, m_curGoalPos);
    }


    void SetConeDistances()
    {
        //Starting distance
        m_coneStartDelay = m_moveDistance * .2f; // Distance we need to travel before turning on cone.
        
        //Ending distance
        m_coneEndBuffer = m_moveDistance * .8f; //Distance we need to travel to turn the cone off.

        m_distanceTravelled = 0f;
    }

    void HandleCone()
    {
        //If the cone is disabled, and we're after start, before end, turn on cone.
        if (!m_muzzleObj.activeSelf && m_distanceTravelled > m_coneStartDelay && m_distanceTravelled < m_coneEndBuffer)
        {
            m_muzzleObj.SetActive(true);
        }
        else if (m_muzzleObj.activeSelf && (m_distanceTravelled < m_coneStartDelay || m_distanceTravelled > m_coneEndBuffer))
        {
            m_muzzleObj.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (m_curHealth >= 0) return;

        foreach (UnitSpawner spawner in GameplayManager.Instance.m_unitSpawners)
        {
            GameObject bossShardObj = Instantiate(((BossEnemyData)m_enemyData).m_bossShard, transform.position, quaternion.identity);
            
            bossShardObj.GetComponent<BossShard>().SetupBossShard(spawner.transform.position);
            spawner.SetSpawnerStatusEffect(((BossEnemyData)m_enemyData).m_spawnStatusEffect, ((BossEnemyData)m_enemyData).m_spawnStatusEffectWaveDuration);
        }
    }
}