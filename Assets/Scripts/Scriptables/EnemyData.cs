using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "EnemyUnitData", menuName = "ScriptableObjects/EnemyUnitData")]
public class EnemyData : ScriptableObject
{
    public string m_enemyName = "Enemy";
    public GameObject m_enemyPrefab;
    
    [Header("Movement")]
    public float m_moveSpeed = 1f;
    public float m_movementWiggleValue = 1f;
    public float m_lookSpeed = 180f;
    
    [Header("Vitality")]
    public float m_damageMultiplier = 0f;
    public int m_health = 10;
    public int m_targetPriority = 0;
    public int m_challengeRating = 0;
    
    [Header("Life Meter")]
    public float m_healthMeterOffset = 35f;
    public float m_healthMeterScale = 1f;

    [Header("VFX")]
    public GameObject m_spawnVFXPrefab;
    public GameObject m_deathVFXPrefab;

    [Header("Trojan Creep Data")]
    public GameObject m_trojanSpawner;
    
    [Header("Audio")]
    public AudioClip m_audioSpawnClip;
    public AudioClip m_audioDeathClip;
    public AudioClip m_audioHealedClip;
    public List<AudioClip> m_audioDamagedClips;

}
