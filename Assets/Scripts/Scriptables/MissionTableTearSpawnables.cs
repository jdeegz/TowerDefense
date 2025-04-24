using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionTableTearSpawnables", menuName = "ScriptableObjects/MissionTableTearSpawnables")]
public class MissionTableTearSpawnables : ScriptableObject
{
    public List<MissionTableTearSpawnable> m_spawnables;
}
