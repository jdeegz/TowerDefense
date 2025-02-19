using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

//This script is used to house Cutscene Data and the boss we want to spawn.
//This is like one of many within the Boss Manager of a mission.

public class BossSequenceController : MonoBehaviour
{
    [Header("Cut Scene Names")]
    [SerializeField] private String m_bossIntroCutScene;

    [SerializeField] private String m_bossDeathCutScene;
    [SerializeField] private String m_bossOutroCutScene;

    [Header("Boss Data")]
    [SerializeField] private EnemyController m_bossEnemyController;

    private EnemyController m_livingBossEnemyController;

    void OnEnable()
    {
        PlayIntroCutScene();
    }

    void PlayIntroCutScene()
    {
        if (!GameManager.Instance)
        {
            SpawnBoss();
            return;
        }

        if (!string.IsNullOrEmpty(m_bossIntroCutScene))
        {
            GameplayManager.OnCutSceneEnd += SpawnBoss;
            Debug.Log($"Boss Sequence Controller requesting Additive Scene Load to: {m_bossIntroCutScene}");
            GameManager.Instance.RequestAdditiveSceneLoad(m_bossIntroCutScene);
        }
    }
    
    void SpawnBoss()
    {
        GameplayManager.OnCutSceneEnd -= SpawnBoss;
        GameplayManager.Instance.SetActiveBossController(this);
        GameObject bossObj = ObjectPoolManager.SpawnObject(m_bossEnemyController.gameObject, Vector3.zero, quaternion.identity, null, ObjectPoolManager.PoolType.Enemy);
        EnemyController bossController = bossObj.GetComponent<EnemyController>();
        bossController.SetEnemyData(m_bossEnemyController.m_enemyData);
    }

    public void BossRemoved(float curHealth)
    {
        if (GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.Defeat) return;
        
        Debug.Log($"Boss Removed: Boss has {curHealth}.");
        
        if (curHealth > 0) // If the boss still has health, it escapes.
        {
            BossHasEscaped();   
        }
        else // Else the boss is dead.
        {
            BossHasDied();
        }
    }

    public void BossHasDied() // If the boss' hit points reached 0.
    {
        Debug.Log($"Boss Sequence Controller: Boss Has Died.");
        if (GameManager.Instance != null && !string.IsNullOrEmpty(m_bossDeathCutScene))
        {
            Debug.Log($"Boss Sequence Controller: Boss Has Died and has a cutscene.");
            GameplayManager.OnCutSceneEnd += BossDeathCutSceneEnded;
            GameManager.Instance.RequestAdditiveSceneLoad(m_bossDeathCutScene);
        }
        else
        {
            Debug.Log($"Boss Sequence Controller: Boss Has Died and has no cutscene.");
            BossDeathCutSceneEnded();
        }
    }

    public void BossHasWon() // If the boss brought the castle controller to 0 hit points.
    {
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Defeat);
    }

    public void BossHasEscaped() // If the boss reached the castle, but the castle still stands.
    {
        Debug.Log($"Boss Sequence Controller: Boss Has Escaped.");
        if (GameManager.Instance != null && !string.IsNullOrEmpty(m_bossOutroCutScene))
        {
            Debug.Log($"Boss Sequence Controller: Boss Has Escaped and has a cutscene.");
            GameplayManager.OnCutSceneEnd += BossOutroCutSceneEnded;
            GameManager.Instance.RequestAdditiveSceneLoad(m_bossOutroCutScene);
        }
        else
        {
            Debug.Log($"Boss Sequence Controller: Boss Has Escaped and has no cutscene.");
            BossOutroCutSceneEnded();
        }
    }

    //Cutscene End Responses.
    void BossDeathCutSceneEnded()
    {
        GameplayManager.OnCutSceneEnd -= BossDeathCutSceneEnded;
        Debug.Log($"Returning Boss Sequence Controller to the Object Pool.");
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.Enemy);
        
        
        GameplayManager.Instance.SetActiveBossController(null);
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Build);
    }

    void BossOutroCutSceneEnded()
    {
        GameplayManager.OnCutSceneEnd -= BossOutroCutSceneEnded;
        Debug.Log($"Returning Boss Sequence Controller to the Object Pool.");
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.Enemy);

        
        GameplayManager.Instance.SetActiveBossController(null);
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Build);
    }
}