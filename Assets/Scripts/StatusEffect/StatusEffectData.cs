using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "StatusEffectData", menuName = "ScriptableObjects/StatusEffectData")]
public class StatusEffectData : ScriptableObject
{
    [HideInInspector] public Tower m_sender;
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

    public void Initialize(StatusEffectData data, Tower tower)
    {
        //Assign this sender
        m_sender = tower;
        
        //Assign the values
        m_name = data.m_name;
        m_damage = data.m_damage;
        m_speedModifier = data.m_speedModifier;
        m_damageModifier = data.m_damageModifier;
        m_tickSpeed = data.m_tickSpeed;
        m_lifeTime = data.m_lifeTime;
        m_effectVFX = data.m_effectVFX;
        m_effectType = data.m_effectType;
    }
}