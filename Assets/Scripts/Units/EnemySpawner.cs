using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class EnemySpawner : MonoBehaviour
{
    [SerializeField] protected TearData m_data;
    [SerializeField] protected Transform m_spawnPoint;
    [SerializeField] protected SpawnerWaves m_spawnerWaves;
    [SerializeField] protected AudioSource m_audioSource;

    protected CreepWave m_nextCreepWave = null;
    protected bool m_isSpawnerActive = false;
    protected List<CreepSpawner> m_activeCreepSpawners;

    // Status Effects to apply to spawned units.
    protected StatusEffect m_spawnStatusEffect;
    protected int m_spawnStatusEffectWaveDuration;

    protected Coroutine m_curCoroutine;

    public event Action<CreepWave> OnActiveWaveSet;

    public abstract void GameplayManagerStateChanged(GameplayManager.GameplayState newState);
    public abstract void UpdateCreepSpawners();

    public void DeactivateSpawner()
    {
        m_isSpawnerActive = false;
    }
    
    public void SetNextCreepWave()
    {
        if (m_spawnerWaves == null) return;

        int gameplayWave = GameplayManager.Instance.m_wave;

        //Debug.Log($"Getting wave {GameplayManager.Instance.m_wave}");

        // Derive challenge values before manipulating gameplayWave value.
        bool isChallengingWave = false;
        int challengingWaveIndex = 0;
        if (m_spawnerWaves.m_challengingWaves.Count > 0)
        {
            isChallengingWave = (gameplayWave) % 5 == 0;
            challengingWaveIndex = (gameplayWave / 5) % m_spawnerWaves.m_challengingWaves.Count;
        }

        CreepWave creepWave = new CreepWave();


        //NEW UNIT TYPE WAVES
        foreach (NewTypeCreepWave newTypeCreepWave in m_spawnerWaves.m_newEnemyTypeWaves)
        {
            if (gameplayWave == newTypeCreepWave.m_waveToSpawnOn)
            {
                creepWave = newTypeCreepWave;

                //Debug.Log($"NEW ENEMY on Wave {gameplayWave} Chosen.");

                m_nextCreepWave = creepWave;
                OnActiveWaveSet?.Invoke(m_nextCreepWave);
                return;
            }
        }

        // Now subtracting 1 for accurate indexing.
        gameplayWave -= 1;

        //INTRO WAVES
        if (gameplayWave < m_spawnerWaves.m_introWaves.Count)
        {
            creepWave = m_spawnerWaves.m_introWaves[gameplayWave];

            //Debug.Log($"Intro Wave {gameplayWave} Chosen.");

            m_nextCreepWave = creepWave;
            OnActiveWaveSet?.Invoke(m_nextCreepWave);
            return;
        }

        //LOOPING WAVE OR CHALLENGING WAVE
        //Boss waves occur every 5 gameplay Waves.
        if (isChallengingWave)
        {
            creepWave = m_spawnerWaves.m_challengingWaves[challengingWaveIndex];
            //Debug.Log($"CHALLENGING Wave {challengingWaveIndex} Chosen.");
        }
        else
        {
            //Subtract the number of training ways so that we start at wave 0 in the new lists.
            if (m_spawnerWaves.m_loopingWaves.Count == 1)
            {
                creepWave = m_spawnerWaves.m_loopingWaves[0];
            }
            else
            {
                int wave = (gameplayWave - m_spawnerWaves.m_introWaves.Count) % m_spawnerWaves.m_loopingWaves.Count;
                creepWave = m_spawnerWaves.m_loopingWaves[wave];
            }
            //Debug.Log($"LOOPING Wave {wave} Chosen.");
        }

        m_nextCreepWave = creepWave;
        OnActiveWaveSet?.Invoke(m_nextCreepWave);
    }

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    // AUDIO
    public void RequestPlayAudioLoop(List<AudioClip> clips, AudioSource audioSource = null)
    {
        if (audioSource == null) audioSource = m_audioSource;

        int i = Random.Range(0, clips.Count);
        audioSource.volume = 0;
        audioSource.loop = true;
        audioSource.clip = clips[i];
        audioSource.Play();

        if (m_curCoroutine != null) StopCoroutine(m_curCoroutine);
        m_curCoroutine = StartCoroutine(FadeInAudio(4f, audioSource));
    }

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
    }

    public void RequestStopAudioLoop(AudioSource audioSource = null)
    {
        if (audioSource == null) audioSource = m_audioSource;
        if (m_curCoroutine != null) StopCoroutine(m_curCoroutine);
        m_curCoroutine = StartCoroutine(FadeOutAudio(3f, audioSource));
    }
    // END AUDIO

    public CreepWave GetNextCreepWave()
    {
        return m_nextCreepWave;
    }

    public SpawnerWaves GetSpawnerWaves()
    {
        return m_spawnerWaves;
    }

    public Transform GetSpawnPointTransform()
    {
        return m_spawnPoint;
    }

    public void SetSpawnerStatusEffect(StatusEffect statusEffect, int duration)
    {
        m_spawnStatusEffect = statusEffect;
        m_spawnStatusEffectWaveDuration = duration;
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

public class TearTooltipData
{
    public string m_tearName;
    public string m_tearDescription;
    public string m_tearDetails;
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
    private List<Cell> m_validSpawnCells;


    public CreepSpawner(Creep creep, Transform spawnPoint)
    {
        //Set the data from the passed Creep.
        m_enemy = creep.m_enemy;
        m_unitsToSpawn = creep.m_unitsToSpawn;
        m_spawnDelay = creep.m_spawnDelay;
        m_spawnInterval = creep.m_spawnInterval;
        m_creepSpawnPoint = spawnPoint;
    }

    public CreepSpawner(Creep creep, List<Cell> validSpawnCells)
    {
        //Set the data from the passed Creep.
        m_enemy = creep.m_enemy;
        m_unitsToSpawn = creep.m_unitsToSpawn;
        m_spawnDelay = creep.m_spawnDelay;
        m_spawnInterval = creep.m_spawnInterval;
        m_validSpawnCells = new List<Cell>(validSpawnCells);
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

            Vector2Int directionToNextCell = cell.GetDirectionVector(cell.m_directionToNextCell);
            Quaternion spawnRotation = Quaternion.LookRotation(new Vector3(directionToNextCell.x, 0, directionToNextCell.y));

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

    private float m_nextIndividualEnemySpawn;
    private float m_individualEnemySpawnInterval;

    public void UpdateCreepSurvival()
    {
        // Check that we have cells to spawn in
        if (m_validSpawnCells == null || m_validSpawnCells.Count == 0)
        {
            Debug.Log($"ERROR, EXPECTING CELLS, HAVE NONE.");
            return;
        }

        m_elapsedTime += Time.deltaTime;

        // Wait for defined delay.
        if (m_elapsedTime >= m_spawnDelay && !m_delayElapsed)
        {
            m_delayElapsed = true;
            m_elapsedTime = 0;
        }

        // Reset Spawning Interval
        if (m_elapsedTime > m_spawnInterval)
        {
            m_elapsedTime = 0;
            m_unitsSpawned = 0;
            m_nextIndividualEnemySpawn = 0;
        }

        // Check to see if we've spawned all the enemies for this interval.
        if (m_unitsSpawned >= m_unitsToSpawn)
        {
            return;
        }

        // Calculate time between individual enemy spawns.
        m_individualEnemySpawnInterval = m_spawnInterval * .66f / m_unitsToSpawn;

        // Spawn units
        if (m_elapsedTime > m_nextIndividualEnemySpawn)
        {
            SpawnIndividualEnemy();
            m_nextIndividualEnemySpawn += m_individualEnemySpawnInterval;
            ++m_unitsSpawned;
            //Debug.Log($"Spawned enemy: {m_unitsSpawned} of {m_unitsToSpawn} at {Time.time}.");
        }
    }

    public bool IsCreepSpawning()
    {
        return m_isCreepSpawning;
    }

    public void SpawnIndividualEnemy()
    {
        Cell cell = m_validSpawnCells[Random.Range(0, m_validSpawnCells.Count)];

        Vector2Int cellPos = cell.m_cellPos;
        Vector3 spawnPoint = new Vector3();
        spawnPoint = new Vector3(cellPos.x, 0, cellPos.y);

        Vector2Int directionToNextCell = cell.GetDirectionVector(cell.m_directionToNextCell);
        Quaternion spawnRotation = Quaternion.LookRotation(new Vector3(directionToNextCell.x, 0, directionToNextCell.y));

        float xOffset = Random.Range(-0.2f, 0.2f);
        float zOffset = Random.Range(-0.2f, 0.2f);

        spawnPoint.x += xOffset;
        spawnPoint.z += zOffset;

        GameObject enemyOjb = ObjectPoolManager.SpawnObject(m_enemy.m_enemyPrefab, spawnPoint, spawnRotation, null, ObjectPoolManager.PoolType.Enemy);
        EnemyController enemyController = enemyOjb.GetComponent<EnemyController>();
        enemyController.SetEnemyData(m_enemy);
        if (m_spawnStatusEffect != null) enemyController.ApplyEffect(m_spawnStatusEffect);
    }

    public void SpawnAllEnemies()
    {
        for (int i = 0; i < m_unitsToSpawn; ++i)
        {
            Cell cell = m_validSpawnCells[Random.Range(0, m_validSpawnCells.Count)];

            Vector2Int cellPos = cell.m_cellPos;
            Vector3 spawnPoint = new Vector3();
            spawnPoint = new Vector3(cellPos.x, 0, cellPos.y);

            Vector2Int directionToNextCell = cell.GetDirectionVector(cell.m_directionToNextCell);
            Quaternion spawnRotation = Quaternion.LookRotation(new Vector3(directionToNextCell.x, 0, directionToNextCell.y));

            float xOffset = Random.Range(-0.2f, 0.2f);
            float zOffset = Random.Range(-0.2f, 0.2f);

            spawnPoint.x += xOffset;
            spawnPoint.z += zOffset;

            GameObject enemyOjb = ObjectPoolManager.SpawnObject(m_enemy.m_enemyPrefab, spawnPoint, spawnRotation, null, ObjectPoolManager.PoolType.Enemy);
            EnemyController enemyController = enemyOjb.GetComponent<EnemyController>();
            enemyController.SetEnemyData(m_enemy);
            if (m_spawnStatusEffect != null) enemyController.ApplyEffect(m_spawnStatusEffect);
        }
    }
}