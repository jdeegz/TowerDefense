using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class UnitSpawner : MonoBehaviour
{
    public Transform m_spawnPoint;
    public List<Creep> m_activeWave;
    [SerializeField] private SpawnerWaves m_spawnerWaves;

    private bool m_isSpawnerActive = false;
    private List<CreepSpawner> m_activeCreepSpawners;
    private StatusEffect m_spawnStatusEffect;
    private int m_spawnStatusEffectWaveDuration;

    public event Action<List<Creep>> OnActiveWaveSet;
    
    private void Start()
    {
        m_isSpawnerActive = false;
    }

    public void SetSpawnerStatusEffect(StatusEffect statusEffect, int duration)
    {
        m_spawnStatusEffect = statusEffect;
        m_spawnStatusEffectWaveDuration = duration;
    }

    private void Update()
    {
        //is Spawning is determine by the Gameplay State, if we're in GameplayManager.GameplayState.SpawnEnemies, spawn enemies.
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
                m_isSpawnerActive = false;
                GameplayManager.Instance.DisableSpawner();
               //Debug.Log($"{gameObject.name} done spawning.");
            }
        }
    }

    private void StartSpawning()
    {
        //We get the next Wave from GetNextCreepWave, if it is null, we dont need to start Spawning.
        if (m_activeWave == null) return;
        
        GameplayManager.Instance.ActivateSpawner();
        
        //Assure each creep has a point to spawn to.
        m_activeCreepSpawners = new List<CreepSpawner>();
        for (int i = 0; i < m_activeWave.Count; ++i)
        {
            //Build list of Active Creep Spawners.
            CreepSpawner creepSpawner = new CreepSpawner(m_activeWave[i], m_spawnPoint);
            creepSpawner.m_spawnStatusEffect = m_spawnStatusEffect;
            m_activeCreepSpawners.Add(creepSpawner);
        }
        
        //Decrement Spawn Status Effect Duration.
        --m_spawnStatusEffectWaveDuration;

        //Remove the Spawn Status Effect if we've reached 0 rounds left.
        if (m_spawnStatusEffectWaveDuration == 0)
        {
            m_spawnStatusEffect = null;
        }

        m_isSpawnerActive = true;
    }

    public List<Creep> GetNextCreepWave()
    {
        int gameplayWave = GameplayManager.Instance.m_wave + 1;
        List<Creep> creepWave = new List<Creep>();
        if (gameplayWave < m_spawnerWaves.m_trainingCreepWaves.Count)
        {
            creepWave = new List<Creep>(m_spawnerWaves.m_trainingCreepWaves[gameplayWave].m_creeps);
        }

        //Else find out if we spawn normal wave or boss wave.
        else
        {
            //Subtract the number of training ways so that we start at wave 0 in the new lists.
            gameplayWave -= m_spawnerWaves.m_trainingCreepWaves.Count;

            //Boss waves occur every 5 gameplay Waves.
            int bossWave = (gameplayWave + 1) % 5;
            if (bossWave == 0)
            {
                int wave = (gameplayWave) % m_spawnerWaves.m_bossWaves.Count;
                creepWave = new List<Creep>(m_spawnerWaves.m_bossWaves[wave].m_creeps);
            }
            else
            {
                int wave = (gameplayWave) % m_spawnerWaves.m_creepWaves.Count;
                creepWave = new List<Creep>(m_spawnerWaves.m_creepWaves[wave].m_creeps);
            }
        }

        //Debug.Log($"Getting next creep wave.");
        return creepWave;
    }

    public bool IsSpawning()
    {
        return m_isSpawnerActive;
    }

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        switch (newState)
        {
            case GameplayManager.GameplayState.BuildGrid:
                break;
            case GameplayManager.GameplayState.PlaceObstacles:
                GameplayManager.Instance.AddSpawnerToList(this);
                GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
                break;
            case GameplayManager.GameplayState.FloodFillGrid:
                break;
            case GameplayManager.GameplayState.CreatePaths:
                break;
            case GameplayManager.GameplayState.Setup:
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
                StartSpawning();
                break;
            case GameplayManager.GameplayState.BossWave:
                break;
            case GameplayManager.GameplayState.Combat:
                break;
            case GameplayManager.GameplayState.Build:
                m_activeWave = GetNextCreepWave();
                OnActiveWaveSet?.Invoke(m_activeWave);
                break;
            case GameplayManager.GameplayState.Paused:
                break;
            case GameplayManager.GameplayState.Victory:
                break;
            case GameplayManager.GameplayState.Defeat:
                break;
            default:
                //Debug.Log($"{this} has no {newState} case.");
                break;
        }
    }
}

public class CreepSpawner
{
    public EnemyData m_enemy;
    public int m_unitsToSpawn;
    public float m_spawnDelay;
    public float m_spawnInterval;
    public StatusEffect m_spawnStatusEffect;

    private bool m_isCreepSpawning = true;
    private int m_unitsSpawned;
    private float m_elapsedTime;
    private bool m_delayElapsed;
    private Transform m_creepSpawnPoint;
    private float m_nextSpawnInterval = 0;


    public CreepSpawner(Creep creep, Transform spawnPoint)
    {
        //Set the data from the passed Creep.
        m_enemy = creep.m_enemy;
        m_unitsToSpawn = creep.m_unitsToSpawn;
        m_spawnDelay = creep.m_spawnDelay;
        m_spawnInterval = creep.m_spawnInterval;
        m_creepSpawnPoint = spawnPoint;
    }

    public void UpdateCreep()
    {
        m_elapsedTime += Time.deltaTime;

        if (m_elapsedTime >= m_spawnDelay && !m_delayElapsed)
        {
            m_delayElapsed = true;
            m_elapsedTime = 0;
        }

        //Interval Timer
        if (m_delayElapsed && m_unitsSpawned < m_unitsToSpawn && m_elapsedTime >= m_nextSpawnInterval)
        {
            Vector3 spawnPoint = m_creepSpawnPoint.position;
            float xOffset = Random.Range(-0.2f, 0.2f);
            float zOffset = Random.Range(-0.2f, 0.2f);

            spawnPoint.x += xOffset;
            spawnPoint.z += zOffset;

            GameObject enemyOjb = ObjectPoolManager.SpawnObject(m_enemy.m_enemyPrefab, spawnPoint, m_creepSpawnPoint.rotation, ObjectPoolManager.PoolType.Enemy);
            EnemyController enemyController = enemyOjb.GetComponent<EnemyController>();
            enemyController.SetEnemyData(m_enemy);
            if(m_spawnStatusEffect != null) enemyController.ApplyEffect(m_spawnStatusEffect);
            
            
            m_unitsSpawned++;
            m_elapsedTime = 0;
            m_nextSpawnInterval = m_spawnInterval;

            //Are we done?
            if (m_unitsSpawned >= m_unitsToSpawn)
            {
                m_isCreepSpawning = false;
            }
        }
    }

    public bool IsCreepSpawning()
    {
        return m_isCreepSpawning;
    }

    public void SetCreepVariables(Transform transform)
    {
        m_delayElapsed = false;
        m_elapsedTime = 0;
        m_unitsSpawned = 0;
        m_isCreepSpawning = true;
        m_creepSpawnPoint = transform;
    }
}