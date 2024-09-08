using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyTrojanSpawnData", menuName = "ScriptableObjects/EnemyTrojanSpawnData")]
public class EnemyTrojanSpawnData : ScriptableObject
{
    public CreepWave m_enemiesToSpawn;
}
