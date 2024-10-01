using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuinData : ScriptableObject
{
    [Header("Info")]
    public string m_ruinName = "Ruin";
    [TextArea(5, 5)]
    public string m_ruinDescription;
    
    [Header("Audio")]
    public AudioClip m_discoveredAudioClip;
    public AudioClip m_unclaimedAudioClip;
    public AudioClip m_chargeConsumedAudioClip;
}




