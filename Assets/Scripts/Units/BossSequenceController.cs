using System;
using Unity.Mathematics;
using UnityEngine;

//This script is used to house Cutscene Data and the boss we want to spawn.
//This is like one of many within the Boss Manager of a mission.

public class BossSequenceController : MonoBehaviour
{
    [Header("Cut Scene Names")]
    [SerializeField] private String m_bossIntroCutScene;
    [SerializeField] private String m_bossDeathCutScene;
    [SerializeField] private String m_castleDestructionCutScene;

    [Header("Boss Data")]
    [SerializeField] private EnemyController m_bossEnemyController;
    private EnemyController m_livingBossEnemyController;

    void OnEnable()
    {
        PlayIntroCutScene();
    }
    
    void PlayIntroCutScene()
    {
        if (m_bossIntroCutScene != null)
        {
            GameplayManager.OnCutSceneEnd += SpawnBoss;
            GameManager.Instance.RequestAdditiveSceneLoad(m_bossIntroCutScene);
        }    
    }
    
    public void BossHasDied()
    {
        GameplayManager.Instance.m_activeBossSequenceController = null;
        
        if (m_bossDeathCutScene != null)
        {
            GameplayManager.OnCutSceneEnd += BossDeathCutSceneEnded;
            GameManager.Instance.RequestAdditiveSceneLoad(m_bossIntroCutScene);
        }    
    }
    
    public void BossHasWon()
    {
        GameplayManager.Instance.m_activeBossSequenceController = null;
        
        if (m_castleDestructionCutScene != null)
        {
            GameplayManager.OnCutSceneEnd += CastleDestructionCutSceneEnded;
            GameManager.Instance.RequestAdditiveSceneLoad(m_bossIntroCutScene);
        }    
    }
    
    //Cutscene End Responses.
    void SpawnBoss()
    {
        GameplayManager.OnCutSceneEnd -= SpawnBoss;
        GameplayManager.Instance.m_activeBossSequenceController = this;
        GameObject bossObj = ObjectPoolManager.SpawnObject(m_bossEnemyController.gameObject, Vector3.zero, quaternion.identity, null, ObjectPoolManager.PoolType.Enemy);
        EnemyController bossController = bossObj.GetComponent<EnemyController>();
        bossController.SetEnemyData(m_bossEnemyController.m_enemyData);
    }

    void BossDeathCutSceneEnded()
    {
        GameplayManager.OnCutSceneEnd -= BossDeathCutSceneEnded;
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Build);
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.Enemy);
    }

    void CastleDestructionCutSceneEnded()
    {
        GameplayManager.OnCutSceneEnd -= CastleDestructionCutSceneEnded;
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Defeat);
    }
}
