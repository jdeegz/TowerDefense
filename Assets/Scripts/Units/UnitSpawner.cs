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
    [SerializeField] private SpawnerWaves m_spawnerWaves;

    public GameObject m_testCube;
    public List<GameObject> m_testCubeList;
    
    private bool m_isSpawnerActive = false;
    private List<Creep> m_activeWave;
    private List<CreepSpawner> m_activeCreepSpawners;


    private void Start()
    {
        m_isSpawnerActive = false;
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
                Debug.Log($"{gameObject.name} done spawning.");
            }
        }
    }

    private void StartSpawning()
    {
       
        //Creep waves start at wave 0, are shown as wave 1.
        //If we have training waves, use them.
        GameplayManager.Instance.ActivateSpawner();
        int gameplayWave = GameplayManager.Instance.m_wave;
        if (gameplayWave < m_spawnerWaves.m_trainingCreepWaves.Count)
        {
            m_activeWave = new List<Creep>(m_spawnerWaves.m_trainingCreepWaves[gameplayWave].m_creeps);
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
                m_activeWave = new List<Creep>(m_spawnerWaves.m_bossWaves[wave].m_creeps);
            }
            else
            {
                int wave = (gameplayWave) % m_spawnerWaves.m_creepWaves.Count;
                m_activeWave = new List<Creep>(m_spawnerWaves.m_creepWaves[wave].m_creeps);
            }
        }

        //Assure each creep has a point to spawn to.
        m_activeCreepSpawners = new List<CreepSpawner>();
        for (int i = 0; i < m_activeWave.Count; ++i)
        {
            //Build list of Active Creep Spawners.
            CreepSpawner creepSpawner = new CreepSpawner(m_activeWave[i], m_spawnPoint);
            m_activeCreepSpawners.Add(creepSpawner);
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
            case GameplayManager.GameplayState.FloodFillGrid:
                break;
            case GameplayManager.GameplayState.CreatePaths:
                List<Vector2Int> cubePositions = new List<Vector2Int>();
                Vector2Int endPos = Util.GetVector2IntFrom3DPos(GameplayManager.Instance.m_enemyGoal.position);
                Vector2Int startPos = Util.GetVector2IntFrom3DPos(m_spawnPoint.position);
                cubePositions = AStar.GetExitPath(startPos, endPos);

                for (int i = 0; i < cubePositions.Count; i++)
                {
                    Vector3 pos = new Vector3(cubePositions[i].x, 0, cubePositions[i].y);
                    GameObject obj = Instantiate(m_testCube, pos, quaternion.identity);
                    obj.name = i.ToString();
                }
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

public class CreepSpawner
{
    public EnemyData m_enemy;
    public int m_unitsToSpawn;
    public float m_spawnDelay;
    public float m_spawnInterval;

    private bool m_isCreepSpawning = true;
    private int m_unitsSpawned;
    private float m_elapsedTime;
    private bool m_delayElapsed;
    private Transform m_creepSpawnPoint;

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
        if (m_delayElapsed && m_unitsSpawned < m_unitsToSpawn && m_elapsedTime >= m_spawnInterval)
        {
            //Debug.Log("Spawning enemy " + m_enemy.name + " : " + m_unitsSpawned + " of " + m_unitsToSpawn);
            Vector3 spawnPoint = m_creepSpawnPoint.position;
            float xOffset = Random.Range(-0.2f, 0.2f);
            float zOffset = Random.Range(-0.2f, 0.2f);

            spawnPoint.x += xOffset;
            spawnPoint.z += zOffset;

            GameObject enemyOjb = GameObject.Instantiate(m_enemy.m_enemyPrefab, spawnPoint, m_creepSpawnPoint.rotation, GameplayManager.Instance.m_enemiesObjRoot);
            enemyOjb.GetComponent<EnemyController>().SetEnemyData(m_enemy);
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