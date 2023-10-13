
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "StatusEffectData", menuName = "ScriptableObjects/StatusEffectData")]
public class StatusEffectData : ScriptableObject
{
    [HideInInspector] public Tower m_sender;
    [HideInInspector] public float m_elapsedTime = 0f;
    [HideInInspector] public float m_nextTickTime = 0f;
    public string m_name = "Effect Name";
    public float m_damage = 0f;
    [FormerlySerializedAs("m_speed")] public float m_speedModifier = 1f;
    public float m_tickSpeed = 1f;
    public float m_lifeTime = 3f;
    public GameObject m_effectVFX;
}
