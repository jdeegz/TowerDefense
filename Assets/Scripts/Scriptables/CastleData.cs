using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CastleData", menuName = "ScriptableObjects/CastleData")]
public class CastleData : ScriptableObject
{
    public string m_castleName;
    [TextArea(5, 5)]
    public string m_castleDescription;
    
    public int m_maxHealth;
    public int m_repairHealthAmount;
    public float m_repairHealthInterval;
    
    //Audio
    public AudioClip m_audioHealthGained;
    public AudioClip m_audioHealthLost;
    public AudioClip m_audioResourceGained;
    public AudioClip m_audioResourceLost;
    public AudioClip m_audioWaveStart;
    public AudioClip m_audioWaveEnd;
}
