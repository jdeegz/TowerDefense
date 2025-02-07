using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingBossProjectile : PooledObject
{
    [SerializeField] private AudioSource m_audioSource;
    [SerializeField] private AudioClip m_audioImpactClip;
    public float m_moveSpeed = 4;
    private Vector3 m_goal;
    private Vector3 m_moveDirection;
    private bool m_isComplete;
    
    
    public override void OnSpawn()
    {
        base.OnSpawn();
        m_goal = GameplayManager.Instance.m_enemyGoal.position;
        m_goal.y = Random.Range(.33f, 2);
        m_isComplete = false;
        
        // Rotation
        transform.rotation = Quaternion.LookRotation((m_goal - transform.position));
    }

    void Update()
    {
        if (m_isComplete) return;
        
        m_moveDirection = (m_goal - transform.position).normalized;
        transform.Translate(m_moveSpeed * Time.deltaTime * m_moveDirection, Space.World);
        
        //If this is the exit cell, we've made it! Deal some damage to the player.
        if (Vector3.Distance(transform.position, m_goal) <= 1.5f)
        {
            m_isComplete = true;
            GameplayManager.Instance.m_castleController.TakeBossDamage(1);
            GameplayManager.Instance.m_castleController.RequestPlayAudio(m_audioImpactClip);
            ObjectPoolManager.ReturnObjectToPool(gameObject);
        }
    }
}
