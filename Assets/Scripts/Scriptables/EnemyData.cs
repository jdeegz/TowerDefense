using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "EnemyUnitData", menuName = "ScriptableObjects/EnemyUnitData")]
public class EnemyData : ScriptableObject
{
    public string m_enemyName;
    public GameObject m_enemyPrefab;
    
    public float m_moveSpeed = 1f;
    public float m_lookSpeed = 180f;
    public float m_damageMultiplier = 0f;
    public int m_health = 10;
    
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
