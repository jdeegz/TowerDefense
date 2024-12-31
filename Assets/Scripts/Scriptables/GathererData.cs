using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Gatherer", menuName = "ScriptableObjects/Gatherer")]
public class GathererData : ScriptableObject
{
    [Header("Info")]
    public ResourceManager.ResourceType m_type;
    public string m_gathererName;
    [TextArea(5, 5)]
    public string m_gathererDescription;
    
    [Header("Details")]
    public float m_harvestDuration;
    public float m_storingDuration;
    public int m_carryCapacity;
    public Sprite m_gathererIconSprite;
    public Sprite m_gathererTypeSprite;
    public Color m_gathererModelColor = Color.blue;
    public Color m_gathererPathColor = Color.blue;
    public GameObject m_harvestNodeIndicatorObj;
    public GameObject m_queuedHarvestNodeIndicatorObj;

    [Header("Audio")]
    public List<AudioClip> m_commandRequestClips;
    public List<AudioClip> m_selectedGathererClips;
    public List<AudioClip> m_harvestingClips;
    public List<AudioClip> m_queueingClips;
    public AudioClip m_levelUpClip;
    public AudioClip m_idleClip;
    public List<AudioClip> m_woodDepositClips;
    public AudioClip m_critDepositClip;
}
