using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "EnemyUnitData", menuName = "Enemy/EnemyUnitData")]
public class ScriptableUnitEnemy : ScriptableObject
{
    public float m_moveSpeed = 1f;
    public float m_damageReduction = 0f;
    [FormerlySerializedAs("m_maxHealth")] public int m_health = 10;
    
    [Header("Life Meter")]
    public float m_healthMeterOffset = 35f;
    public float m_healthMeterScale = 1f;

    [Header("VFX")]
    public GameObject m_spawnVFXPrefab;
    public GameObject m_deathVFXPrefab;
    
    [Header("Audio")]
    public AudioClip m_audioSpawnClip;
    public AudioClip m_audioDeathClip;
    public List<AudioClip> m_audioDamagedClips;

}
