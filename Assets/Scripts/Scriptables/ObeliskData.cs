using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObeliskData_Default", menuName = "ScriptableObjects/ObeliskData")]
public class ObeliskData : ScriptableObject
{
    [Header("Obelisk Data")] 
    public string m_obeliskName;
    [TextArea(5, 5)]
    public string m_obeliskDescription;

    public int m_maxChargeCount = 100;
    public float m_meterOffset = 75f;
    public LayerMask m_layerMask;
    public GameObject m_obeliskSoulObj;
    public int m_soulValue = 1;
    public float m_obeliskRange = 6f;

    public AudioClip m_soulCollected;
    public AudioClip m_obeliskCharged;
}
