using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObeliskData_Default", menuName = "ScriptableObjects/ObeliskData")]
public class ObeliskData : ScriptableObject
{
    [Header("Obelisk Data")] 
    public LayerMask m_layerMask;
    public GameObject m_obeliskSoulObj;
    public int m_soulValue = 1;
    public float m_obeliskRange = 6f;
}
