using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class BossManager : MonoBehaviour
{
    [SerializeField] private List<BossSequenceController> m_bossSequenceControllers;
    private int m_bossIndex = 0;

    void Start()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.BossWave)
        {
            //Spawn the boss.  
            ObjectPoolManager.SpawnObject(m_bossSequenceControllers[m_bossIndex].gameObject, Vector3.zero, quaternion.identity, ObjectPoolManager.PoolType.Enemy);
            ++m_bossIndex;

            if (m_bossIndex == m_bossSequenceControllers.Count)
            {
                m_bossIndex = 0;
            }
        }
    }
}