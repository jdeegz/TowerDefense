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
    public bool m_canBeSlowed = true;
    
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
    public GameObject m_teleportDepartureVFX;
    public GameObject m_teleportArrivalVFX;

    [Header("Trojan Creep Data")]
    public GameObject m_trojanSpawner;
    
    [Header("Audio")]
    public List<AudioClip> m_audioSpawnClips;
    public List<AudioClip> m_audioDeathClips;
    public AudioClip m_audioLifeLoop;
    public AudioClip m_audioEnemyFeatureClip;

}
