using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening.Core.Enums;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class UnitSpawner : MonoBehaviour
{
    [SerializeField] private TearData m_data;
    [SerializeField] private Transform m_spawnPoint;
    [SerializeField] private SpawnerWaves m_spawnerWaves;
    [SerializeField] private AudioSource m_audioSource;

    private CreepWave m_activeWave = null;
    private bool m_isSpawnerActive = false;
    private List<CreepSpawner> m_activeCreepSpawners;
    private StatusEffect m_spawnStatusEffect;
    private int m_spawnStatusEffectWaveDuration;
    private Cell m_spawnerCell;

    public event Action<CreepWave> OnActiveWaveSet;

    private void Start()
    {
        m_isSpawnerActive = false;
        RequestPlayAudioLoop(m_data.m_audioSpawnerActiveLoops);
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
                Debug.Log($"{gameObject.name} done spawning.");
                m_isSpawnerActive = false;
                GameplayManager.Instance.DisableSpawner();

                //RequestStopAudioLoop();
            }
        }
    }

    public CreepWave GetActiveWave()
    {
        //CreepWave activeWave = new CreepWave(m_activeWave);
        return m_activeWave;
    }

    public SpawnerWaves GetSpawnerWaves()
    {
        return m_spawnerWaves;
    }

    public Transform GetSpawnPointTransform()
    {
        return m_spawnPoint;
    }

    public void RequestPlayAudioLoop(List<AudioClip> clips, AudioSource audioSource = null)
    {
        if (audioSource == null) audioSource = m_audioSource;

        int i = Random.Range(0, clips.Count);
        audioSource.volume = 0;
        audioSource.loop = true;
        audioSource.clip = clips[i];
        audioSource.Play();

        if (m_curCoroutine != null) StopCoroutine(m_curCoroutine);
        Debug.Log($"{gameObject.name} starting fade in coroutine");
        m_curCoroutine = StartCoroutine(FadeInAudio(4f, audioSource));
    }

    private Coroutine m_curCoroutine;

    private IEnumerator FadeInAudio(float duration, AudioSource audioSource)
    {
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Clamp01(startVolume + (elapsedTime / duration));
            yield return null;
        }

        audioSource.volume = 1f;
        Debug.Log($"{gameObject.name} fade in coroutine completed.");
    }

    private IEnumerator FadeOutAudio(float duration, AudioSource audioSource)
    {
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Clamp01(startVolume - (elapsedTime / duration));
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        Debug.Log($"{gameObject.name} fade out coroutine completed.");
    }

    public void RequestStopAudioLoop(AudioSource audioSource = null)
    {
        if (audioSource == null) audioSource = m_audioSource;
        Debug.Log($"starting fade out coroutine.");
        if (m_curCoroutine != null) StopCoroutine(m_curCoroutine);
        m_curCoroutine = StartCoroutine(FadeOutAudio(3f, audioSource));
    }

    private void StartSpawning()
    {
        //RequestPlayAudioLoop(m_data.m_audioSpawnerActiveLoops);

        GameplayManager.Instance.ActivateSpawner();

        //Assure each creep has a point to spawn to.
        m_activeCreepSpawners = new List<CreepSpawner>();
        for (int i = 0; i < m_activeWave.m_creeps.Count; ++i)
        {
            //Build list of Active Creep Spawners.
            CreepSpawner creepSpawner = new CreepSpawner(m_activeWave.m_creeps[i], m_spawnPoint);
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

    public CreepWave GetNextCreepWave()
    {
        int gameplayWave = GameplayManager.Instance.m_wave + 1;

        CreepWave creepWave = new CreepWave();

        //INTRO WAVES
        if (gameplayWave < m_spawnerWaves.m_introWaves.Count)
        {
            creepWave = m_spawnerWaves.m_introWaves[gameplayWave];

            //Does this wave have units in it?
            return creepWave;
        }

        //NEW UNIT TYPE WAVES
        foreach (NewTypeCreepWave newTypeCreepWave in m_spawnerWaves.m_newEnemyTypeWaves)
        {
            if (gameplayWave == newTypeCreepWave.m_waveToSpawnOn)
            {
                creepWave = newTypeCreepWave;

                return creepWave;
            }
        }

        //Subtract the number of training ways so that we start at wave 0 in the new lists.
        gameplayWave -= m_spawnerWaves.m_introWaves.Count;

        //LOOPING WAVE OR CHALLENGING WAVE
        //Boss waves occur every 5 gameplay Waves.
        int challengingWave = gameplayWave % 5;
        if (challengingWave == 0 && m_spawnerWaves.m_challengingWaves.Count > 0)
        {
            int wave = (gameplayWave) % m_spawnerWaves.m_challengingWaves.Count;
            creepWave = m_spawnerWaves.m_challengingWaves[wave];

            return creepWave;
        }
        else
        {
            int wave = (gameplayWave) % m_spawnerWaves.m_loopingWaves.Count;
            creepWave = m_spawnerWaves.m_loopingWaves[wave];
            
            return creepWave;
        }
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
                //GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
                GridCellOccupantUtil.SetActor(gameObject, 1, 1, 1);
                m_spawnerCell = Util.GetCellFrom3DPos(transform.position);
                break;
            case GameplayManager.GameplayState.FloodFillGrid:
                break;
            case GameplayManager.GameplayState.CreatePaths:
                break;
            case GameplayManager.GameplayState.Setup:
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
                if (m_activeWave.m_creeps != null && m_activeWave.m_creeps.Count > 0)
                {
                    StartSpawning();
                }
                break;
            case GameplayManager.GameplayState.BossWave:
                break;
            case GameplayManager.GameplayState.Combat:
                break;
            case GameplayManager.GameplayState.Build:
                m_activeWave = GetNextCreepWave();
                OnActiveWaveSet?.Invoke(m_activeWave);
                break;
            case GameplayManager.GameplayState.CutScene:
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

    public TearTooltipData GetTooltipData()
    {
        TearTooltipData data = new TearTooltipData();
        data.m_tearName = m_data.m_tearName;
        data.m_tearDescription = m_data.m_tearDescription;
        data.m_tearDetails = m_data.m_tearDetails;
        return data;
    }
}

public class CreepSpawner
{
    public EnemyData m_enemy;
    public int m_unitsToSpawn;
    public float m_spawnDelay;
    public float m_spawnInterval;
    public StatusEffect m_spawnStatusEffect;

    private float m_elapsedTime;
    private float m_nextSpawnInterval = 0;
    private bool m_isCreepSpawning = true;
    private bool m_delayElapsed;
    private int m_unitsSpawned;
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
        if (m_delayElapsed && m_unitsSpawned < m_unitsToSpawn && m_elapsedTime >= m_nextSpawnInterval)
        {
            Vector3 spawnPoint = m_creepSpawnPoint.position;
            Cell cell = Util.GetCellFrom3DPos(m_creepSpawnPoint.position);
            Quaternion spawnRotation = Quaternion.LookRotation(cell.m_directionToNextCell);
            float xOffset = Random.Range(-0.2f, 0.2f);
            float zOffset = Random.Range(-0.2f, 0.2f);

            spawnPoint.x += xOffset;
            spawnPoint.z += zOffset;

            GameObject enemyOjb = ObjectPoolManager.SpawnObject(m_enemy.m_enemyPrefab, spawnPoint, spawnRotation, null, ObjectPoolManager.PoolType.Enemy);
            EnemyController enemyController = enemyOjb.GetComponent<EnemyController>();
            enemyController.SetEnemyData(m_enemy);
            if (m_spawnStatusEffect != null) enemyController.ApplyEffect(m_spawnStatusEffect);


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

public class TearTooltipData
{
    public string m_tearName;
    public string m_tearDescription;
    public string m_tearDetails;
}