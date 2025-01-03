using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "CastleData", menuName = "ScriptableObjects/CastleData")]
public class CastleData : ScriptableObject
{
    public string m_castleName;
    [TextArea(5, 5)]
    public string m_castleDescription;
    
    public int m_maxHealth;
    public int m_repairHealthAmount;
    public int m_repairFrequency;
    
    //Audio
    public AudioClip m_healthGainedClip;
    public List<AudioClip> m_healthLostClips;
    public AudioClip m_repairingClip;
    public AudioClip m_destroyedClip;
}
