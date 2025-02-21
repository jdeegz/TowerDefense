using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrojanUnitSpawner : EnemySpawner
{
    //The Trojan spawner is created at the X,Y position of the Enemy Trojan when it dies.
    [SerializeField] private List<CreepWave> m_creepWaves;
    
    private void OnEnable()
    {
        GameplayManager.GameplayState currentState = GameplayManager.Instance.m_gameplayState;
        if (currentState == GameplayManager.GameplayState.Victory || currentState == GameplayManager.GameplayState.Defeat)
        {
            //Cancel the request to spawn a tear.
            Destroy(gameObject);
            return;
        }
        
        base.OnEnable();
        
        m_isSpawnerActive = false;
        GameplayManager.Instance.AddTrojanSpawnerToList(this);
        RequestPlayAudio(m_data.m_audioSpawnerCreated, m_audioSource);
        StartCoroutine(CreateSpawner());
    }

    private IEnumerator CreateSpawner()
    {
        yield return new WaitForSeconds(0.5f);
        StartSpawning();
    }
    
    private void StartSpawning()
    {
        RequestPlayAudioLoop(m_data.m_audioSpawnerActiveLoops, m_audioSource);
        
        //Calculate which CreepWave to spawn based on mission's wave number.
        int creepWaveIndex = GameplayManager.Instance.Wave % m_creepWaves.Count;
        
        //Assure each creep has a point to spawn to.
        m_activeCreepSpawners = new List<CreepSpawner>();
        for (int i = 0; i < m_creepWaves[creepWaveIndex].m_creeps.Count; ++i)
        {
            //Build list of Active Creep Spawners.
            CreepSpawner creepSpawner = new CreepSpawner(m_creepWaves[creepWaveIndex].m_creeps[i], m_spawnPoint);
            m_activeCreepSpawners.Add(creepSpawner);
        }

        m_isSpawnerActive = true;
    }
    
    private void Update()
    {
        if (m_isSpawnerActive)
        {
            for (int i = 0; i < m_activeCreepSpawners.Count; ++i)
            {
                if (m_activeCreepSpawners[i].IsCreepSpawning())
                {
                    m_activeCreepSpawners[i].UpdateCreep();
                }
                else
                {
                    //If the creep is NOT spawning, remove it from the active creep spawner list.
                    m_activeCreepSpawners.RemoveAt(i);
                    --i;
                }
            }

            //If we have NO active creep spawners, disable this spawner.
            if (m_activeCreepSpawners.Count == 0)
            {
                RequestStopAudioLoop(m_audioSource);
                m_isSpawnerActive = false;
                RemoveTrojanUnitSpawner();
            }
        }
    }

    public override void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        switch (newState)
        {
            case GameplayManager.GameplayState.BuildGrid:
                break;
            case GameplayManager.GameplayState.PlaceObstacles:
                break;
            case GameplayManager.GameplayState.FloodFillGrid:
                break;
            case GameplayManager.GameplayState.CreatePaths:
                break;
            case GameplayManager.GameplayState.Setup:
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
                break;
            case GameplayManager.GameplayState.BossWave:
                break;
            case GameplayManager.GameplayState.Combat:
                break;
            case GameplayManager.GameplayState.Build:
                break;
            case GameplayManager.GameplayState.CutScene:
                break;
            case GameplayManager.GameplayState.Victory:
                RemoveTrojanUnitSpawner();
                break;
            case GameplayManager.GameplayState.Defeat:
                DeactivateSpawner();
                break;
            default:
                break;
        }
    }

    public override void UpdateCreepSpawners()
    {
        // Trojan Spawners do not need to update Creep Spawners. They only have one.
    }

    private void RemoveTrojanUnitSpawner()
    {
        DeactivateSpawner();
        
        // TO DO Trigger animation to remove spawners
        
        //Remove Obj from scene
        GameplayManager.Instance.RemoveTrojanSpawnerFromList(this);
        
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.Enemy);
        
        Debug.Log($"Spawner: {gameObject.name}'s spawner removed.");
        
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }
    
    public TearTooltipData GetTooltipData()
    {
        TearTooltipData data = new TearTooltipData();
        data.m_tearName = m_data.m_tearName;
        data.m_tearDescription = m_data.m_tearDescription;
        data.m_tearDetails = m_data.m_tearDetails;
        return data;
    }
}
