using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SpawnerWavesData", menuName = "ScriptableObjects/SpawnerWavesData")]
public class SpawnerWaves : ScriptableObject
{
    //New Lists
    public List<CreepWave> m_introWaves;
    public List<CreepWave> m_loopingWaves; //Loops after Intro Waves completed.
    public List<CreepWave> m_challengingWaves; //Every 5th wave.
    public List<NewTypeCreepWave> m_newEnemyTypeWaves; //Spawn when the wave equals the NewTypeCreepWave value.


    private void OnValidate()
    {
        foreach (CreepWave creepWave in m_introWaves)
        {
            creepWave.m_challengeRating = 0;
            foreach (Creep creep in creepWave.m_creeps)
            {
                if (creep.m_enemy == null) return;
                creepWave.m_challengeRating += creep.m_enemy.m_challengeRating * creep.m_unitsToSpawn;
            }
        }

        foreach (CreepWave creepWave in m_loopingWaves)
        {
            creepWave.m_challengeRating = 0;
            foreach (Creep creep in creepWave.m_creeps)
            {
                if (creep.m_enemy == null) return;
                creepWave.m_challengeRating += creep.m_enemy.m_challengeRating * creep.m_unitsToSpawn;
            }
        }

        foreach (CreepWave creepWave in m_challengingWaves)
        {
            creepWave.m_challengeRating = 0;
            foreach (Creep creep in creepWave.m_creeps)
            {
                if (creep.m_enemy == null) return;
                creepWave.m_challengeRating += creep.m_enemy.m_challengeRating * creep.m_unitsToSpawn;
            }
        }
        
        foreach (CreepWave creepWave in m_newEnemyTypeWaves)
        {
            creepWave.m_challengeRating = 0;
            foreach (Creep creep in creepWave.m_creeps)
            {
                if (creep.m_enemy == null) return;
                creepWave.m_challengeRating += creep.m_enemy.m_challengeRating * creep.m_unitsToSpawn;
            }
        }
    }
}

[System.Serializable]
public class CreepWave
{
    public List<Creep> m_creeps;

    [ReadOnly] public int m_challengeRating;
}

[System.Serializable]
public class NewTypeCreepWave : CreepWave
{
    public int m_waveToSpawnOn;
    public String m_waveCutscene;
}

[System.Serializable]
public class Creep
{
    public EnemyData m_enemy;
    public int m_unitsToSpawn;
    public float m_spawnDelay;
    public float m_spawnInterval;
}