using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class UnitSpawner : MonoBehaviour
{
    public Transform m_spawnPoint;
    public List<CreepWave> m_creepWaves;

    private bool m_isSpawnerActive = false;
    private List<Creep> m_activeCreeps;


    private void Start()
    {
        m_isSpawnerActive = false;
    }


    private void Update()
    {
        //is Spawning is determine by the Gameplay State, if we're in GameplayManager.GameplayState.SpawnEnemies, spawn enemies.
        if (m_isSpawnerActive)
        {
            for (int i = 0; i < m_activeCreeps.Count; ++i)
            {
                Creep creep = m_activeCreeps[i];
                if (creep.IsCreepSpawning())
                {
                    creep.UpdateCreep();
                }
                else
                {
                    //If the creep is NOT spawning, remove it from the active creep spawner list.
                    m_activeCreeps.RemoveAt(i);
                    --i;

                    //If we have NO active creep spawners, disable this spawner.
                    if (m_activeCreeps.Count == 0)
                    {
                        m_isSpawnerActive = false;
                        GameplayManager.Instance.DisableSpawner();
                    }
                }
            }
        }
    }

    private void StartSpawning()
    {
        //Tell this spawner what creeps we'll be spawning.
        int wave = GameplayManager.Instance.m_wave % m_creepWaves.Count;
        //Debug.Log("Modulo wave number is : " + wave);

        m_activeCreeps = new List<Creep>(m_creepWaves[wave].m_creeps);
        //Debug.Log("Active Creeps List Created. Count: " + m_activeCreeps.Count);

        //Assure each creep has a point to spawn to.
        for (int i = 0; i < m_activeCreeps.Count; ++i)
        {
            m_activeCreeps[i].SetCreepVariables(m_spawnPoint);
        }

        m_isSpawnerActive = true;
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
            case GameplayManager.GameplayState.CreatePaths:
                break;
            case GameplayManager.GameplayState.Setup:
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
                StartSpawning();
                break;
            case GameplayManager.GameplayState.Combat:
                break;
            case GameplayManager.GameplayState.Build:
                break;
            case GameplayManager.GameplayState.Paused:
                break;
            case GameplayManager.GameplayState.Victory:
                break;
            case GameplayManager.GameplayState.Defeat:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }
}

[System.Serializable]
public class CreepWave
{
    public List<Creep> m_creeps;
}

[System.Serializable]
public class Creep
{
    public UnitEnemy m_enemy;
    public int m_unitsToSpawn;
    public float m_spawnDelay;
    public float m_spawnInterval;

    private bool m_isCreepSpawning = true;
    private int m_unitsSpawned;
    private float m_elapsedTime;
    private bool m_delayElapsed;
    private Transform m_creepSpawnPoint;

    public void UpdateCreep()
    {
        m_elapsedTime += Time.deltaTime;

        if (m_elapsedTime >= m_spawnDelay && !m_delayElapsed)
        {
            m_delayElapsed = true;
            m_elapsedTime = 0;
        }

        //Interval Timer
        if (m_delayElapsed && m_unitsSpawned < m_unitsToSpawn && m_elapsedTime >= m_spawnInterval)
        {
            //Debug.Log("Spawning enemy " + m_enemy.name + " : " + m_unitsSpawned + " of " + m_unitsToSpawn);
            Vector3 spawnPoint = m_creepSpawnPoint.position;
            float xOffset = Random.Range(-0.2f, 0.2f);
            float zOffset = Random.Range(-0.2f, 0.2f);

            spawnPoint.x += xOffset;
            spawnPoint.z += zOffset;

            Object.Instantiate(m_enemy.gameObject, spawnPoint, Quaternion.identity,
                GameplayManager.Instance.m_enemiesObjRoot);
            m_unitsSpawned++;
            m_elapsedTime = 0;

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