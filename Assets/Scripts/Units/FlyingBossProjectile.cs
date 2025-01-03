using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingBossProjectile : MonoBehaviour
{
    [SerializeField] private AudioSource m_audioSource;
    [SerializeField] private AudioClip m_audioImpactClip;
    public float m_moveSpeed = 4;
    private Vector3 m_goal;
    
    void Start()
    {
        m_goal = GameplayManager.Instance.m_enemyGoal.position;
    }
    
    void Update()
    {
        Vector3 direction = (m_goal - transform.position).normalized;
        transform.Translate(m_moveSpeed * Time.deltaTime * direction, Space.World);
        
        //If this is the exit cell, we've made it! Deal some damage to the player.
        if (Vector3.Distance(transform.position, m_goal) <= 1.5f)
        {
            GameplayManager.Instance.m_castleController.TakeBossDamage(1);
            GameplayManager.Instance.m_castleController.RequestPlayAudio(m_audioImpactClip);
            ObjectPoolManager.ReturnObjectToPool(gameObject);
        }
    }

    void OnDestroy()
    {
        Debug.Log($"castle hit by boss");
    }
}
