using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SpawnerWavesData", menuName = "ScriptableObjects/SpawnerWavesData")]
public class SpawnerWaves : ScriptableObject
{
    public List<CreepWave> m_trainingCreepWaves;
    public List<CreepWave> m_creepWaves;
    public List<CreepWave> m_bossWaves;
}

[System.Serializable]
public class CreepWave
{
    public List<Creep> m_creeps;
}

[System.Serializable]
public class Creep
{
    public EnemyData m_enemy;
    public int m_unitsToSpawn;
    public float m_spawnDelay;
    public float m_spawnInterval;
    
}