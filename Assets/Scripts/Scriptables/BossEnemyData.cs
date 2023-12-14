using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (fileName = ("BossEnemyData"), menuName = "ScriptableObjects/BossEnemyData")]
public class BossEnemyData : EnemyData
{
    public GameObject m_projectileObj;
    public GameObject m_bossShard;
    public StatusEffect m_spawnStatusEffect;
    public int m_spawnStatusEffectWaveDuration;
    public int m_castleAttackRate; //The number of moves between each attack.
    public int m_strafeAttackRate; //The number of moves between each strafe.
    public float m_attackDelay;
    public float m_attackCooldown;
}
