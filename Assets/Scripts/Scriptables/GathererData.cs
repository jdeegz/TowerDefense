using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Audio")]
    public List<AudioClip> m_commandRequestClips;
    public List<AudioClip> m_selectedGathererClips;
    public AudioClip m_levelUpClip;
    public List<AudioClip> m_harvestingClips;
    public AudioClip m_critDepositClip;
}
