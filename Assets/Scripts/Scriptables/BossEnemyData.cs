using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (fileName = ("BossEnemyData"), menuName = "ScriptableObjects/BossEnemyData")]
public class BossEnemyData : EnemyData
{
    [Header("Boss Data")]
    public GameObject m_projectileObj;
    public int m_castleAttackRate; //The number of moves between each attack.
    public int m_strafeAttackRate; //The number of moves between each strafe.
    public float m_attackDelay;
    public float m_attackCooldown;
    
    [Header("Special Boss Audio")]
    public List<AudioClip> m_audioMovementClips;
    
    // Fireball Attack
    public AudioClip m_audioAnticFireballClip;
    public AudioClip m_audioShootFireballClip;
    
    // Strafe Attack
    public AudioClip m_audioStrafeIgniteClip;
    public AudioClip m_audioStrafeLoop;
}
