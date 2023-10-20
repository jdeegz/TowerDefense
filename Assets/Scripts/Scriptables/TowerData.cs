using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerData", menuName = "ScriptableObjects/TowerData")]
public class TowerData : ScriptableObject
{
    [Header("Strings")]
    public string m_name;
    
    [Header("Basic Data")]
    public float m_baseDamage;
    public float m_targetRange;
    public float m_fireRange;
    public float m_fireRate;
    public float m_rotationSpeed;
    
    [Header("Arc Cone")]
    public float m_fireConeAngle;
    
    [Header("Burst Fire")]
    public float m_burstFireRate;
    public float m_burstSize;
    
    [Header("Build Costs")]
    public int m_woodCost;
    public int m_stoneCost;
    
    [Header("Sell Costs")]
    public int m_woodSellCost;
    public int m_stoneSellCost;
    
    [Header("Textures")]
    public Sprite m_uiIcon;
    
    [Header("Visual Prefabs")]
    public GameObject m_prefab;
    public GameObject m_projectilePrefab;
    
    [Header("sFX")]
    public List<AudioClip> m_audioFireClips;
    public AudioClip m_audioBuildClip;
    public AudioClip m_audioDestroyClip;

    [Header("Title")]
    public List<TowerData> m_upgradeOptions;
}
