using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "StatusEffectData", menuName = "ScriptableObjects/StatusEffectData")]
public class StatusEffectData : ScriptableObject
{
    public string m_name = "Effect Name";
    public float m_damage = 0f;
    public float m_speedModifier = 1f;
    public float m_damageModifier = 1f;
    public float m_tickSpeed = 1f;
    public float m_lifeTime = 3f;
    public GameObject m_effectVFX;

    public enum EffectType
    {
        DecreaseMoveSpeed,
        IncreaseMoveSpeed,
        DecreaseHealth,
        IncreaseHealth,
        DecreaseArmor,
        IncreaseArmor,
    }

    public EffectType m_effectType;
}