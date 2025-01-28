using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "WaveDataGenerator", menuName = "ScriptableObjects/WaveDataGenerator")]
public class WaveDataGenerator : ScriptableObject
{
    
    [Header("Intro Wave Data")]
    [SerializeField] List<EnemyData> m_introEnemyDatas;
    [SerializeField] private int m_introWaveCount = 10;

    [Header("Looping Wave Data")]
    [SerializeField] List<EnemyData> m_loopingEnemyDatas;
    [SerializeField] private int m_loopingWaveCount = 20;

    [Header("Challenging Wave Data")]
    [SerializeField] List<EnemyData> m_challengingEnemyDatas;
    [SerializeField] private int m_challengingWaveCount = 5;
    
    [Header("Spawn Time Data")]
    [SerializeField] private int m_spawnInterval = 10;
    [SerializeField] private int m_delayFactor = 5;

    public void GenerateLoopingWaveData()
    {
        if (!IsInputValid()) return;
        List<CreepWave> introWaves = GenerateIntroWaveDataStatic(m_introEnemyDatas, m_introWaveCount, m_delayFactor, m_spawnInterval);
        List<CreepWave> loopingWaves = GenerateLoopingWaveDataStatic(m_loopingEnemyDatas, m_loopingWaveCount, 200, 210, m_delayFactor, m_spawnInterval);
        List<CreepWave> challengingWaves = GenerateLoopingWaveDataStatic(m_challengingEnemyDatas, m_challengingWaveCount, 250, 260, m_delayFactor, m_spawnInterval);
        GenerateWaveDataAsset(introWaves, loopingWaves, challengingWaves);
    }

    private bool IsInputValid()
    {
        if (m_introEnemyDatas.Count == 0 && m_introWaveCount > 0)
        {
            Debug.Log($"No Intro Enemy Data supplied and Wave Count is greater than 0.");
            return false;
        }

        if (m_loopingEnemyDatas.Count == 0 && m_loopingWaveCount > 0)
        {
            Debug.Log($"No Looping Enemy Data supplied and Wave Count is greater than 0.");
            return false;
        }

        if (m_challengingEnemyDatas.Count == 0 && m_challengingWaveCount > 0)
        {
            Debug.Log($"No Challenging Enemy Data supplied and Wave Count is greater than 0.");
            return false;
        }

        return true;
    }

    private static void GenerateWaveDataAsset(List<CreepWave> intro, List<CreepWave> looping, List<CreepWave> challenging)
    {
        SpawnerWaves spawnerWaves = ScriptableObject.CreateInstance<SpawnerWaves>();
        spawnerWaves.m_introWaves = intro;
        spawnerWaves.m_loopingWaves = looping;
        spawnerWaves.m_challengingWaves = challenging;

        // Only use AssetDatabase in the editor
#if UNITY_EDITOR
        string basePath = "Assets/ScriptableObjects/SurvivalWaveData/WaveData";
        string extension = ".asset";
        string path = basePath + extension;
        int counter = 1;

        while (System.IO.File.Exists(path))
        {
            path = $"{basePath}_{counter}{extension}";
            counter++;
        }

        AssetDatabase.CreateAsset(spawnerWaves, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Asset saved at: {path}");
#endif
    }

    private static List<CreepWave> GenerateLoopingWaveDataStatic(List<EnemyData> enemyDatas, int waveCount, int targetMin, int targetMax, int delayFactor, int spawnInterval)
    {
        // Function to generate a single group
        CreepWave GenerateCreepWave(List<EnemyData> enemyDatas, int min, int max)
        {
            List<Creep> creeps = new List<Creep>();
            int total = 0;

            // Pick the units to fill the wave with.
            int uniqueEnemyCount = Random.Range(2, 6);
            int uniqueEnemiesAdded = 0;
            int spawnDelay = 0;

            while (uniqueEnemiesAdded < uniqueEnemyCount && total < min)
            {
                EnemyData enemyToAddToCreep = enemyDatas[Random.Range(0, enemyDatas.Count)];
                if (total + enemyToAddToCreep.m_challengeRating <= max)
                {
                    bool found = false;
                    foreach (Creep creep in creeps)
                    {
                        if (creep.m_enemy == enemyToAddToCreep)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        spawnDelay += uniqueEnemiesAdded * (Random.Range(0, delayFactor));
                        Creep newCreep = new Creep(enemyToAddToCreep, 1, spawnDelay, spawnInterval);
                        creeps.Add(newCreep);
                        total += enemyToAddToCreep.m_challengeRating;
                        ++uniqueEnemiesAdded;
                    }
                }
            }

            // Increment unit count to meet target Challenge Rating.
            while (total < min)
            {
                Creep creepToIncrement = creeps[Random.Range(0, creeps.Count)];
                if (total + creepToIncrement.m_enemy.m_challengeRating <= max)
                {
                    ++creepToIncrement.m_unitsToSpawn;
                    total += creepToIncrement.m_enemy.m_challengeRating;
                }
            }

            CreepWave creepWave = new CreepWave();
            creepWave.m_creeps = creeps;
            return creepWave;
        }

        // Generate creepWaves
        List<CreepWave> creepWaves = new List<CreepWave>();
        for (int i = 0; i < waveCount; i++)
        {
            creepWaves.Add(GenerateCreepWave(enemyDatas, targetMin, targetMax));
        }

        return creepWaves;
    }


    private static List<CreepWave> GenerateIntroWaveDataStatic(List<EnemyData> enemyDatas, int waveCount, int delayFactor, int spawnInterval)
    {
        // Function to generate a single group
        CreepWave GenerateCreepWave(List<EnemyData> enemyDatas, int min, int max)
        {
            List<Creep> creeps = new List<Creep>();
            int total = 0;
            
            // Pick the units to fill the wave with.
            int uniqueEnemyCount = Random.Range(1, 4);
            int uniqueEnemiesAdded = 0;
            int spawnDelay = 0;

            while (uniqueEnemiesAdded < uniqueEnemyCount && total < min)
            {
                EnemyData enemyToAddToCreep = enemyDatas[Random.Range(0, enemyDatas.Count)];
                if (total + enemyToAddToCreep.m_challengeRating <= max)
                {
                    bool found = false;
                    foreach (Creep creep in creeps)
                    {
                        if (creep.m_enemy == enemyToAddToCreep)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        spawnDelay += uniqueEnemiesAdded * (Random.Range(0, delayFactor));
                        Creep newCreep = new Creep(enemyToAddToCreep, 1, spawnDelay, spawnInterval);
                        creeps.Add(newCreep);
                        total += enemyToAddToCreep.m_challengeRating;
                        ++uniqueEnemiesAdded;
                    }
                }
            }

            // Increment unit count to meet target Challenge Rating.
            while (total < min)
            {
                Creep creepToIncrement = creeps[Random.Range(0, creeps.Count)];
                if (total + creepToIncrement.m_enemy.m_challengeRating <= max)
                {
                    ++creepToIncrement.m_unitsToSpawn;
                    total += creepToIncrement.m_enemy.m_challengeRating;
                }
            }

            CreepWave creepWave = new CreepWave();
            creepWave.m_creeps = creeps;
            return creepWave;
        }

        // Generate creepWaves
        List<CreepWave> creepWaves = new List<CreepWave>();
        for (int i = 0; i < waveCount; i++)
        {
            int targetMin = (i + 1) * 20;
            int targetMax = targetMin + 10;
            creepWaves.Add(GenerateCreepWave(enemyDatas, targetMin, targetMax));
        }

        return creepWaves;
    }
}