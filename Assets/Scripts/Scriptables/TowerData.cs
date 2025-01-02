using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "TowerData", menuName = "ScriptableObjects/TowerData")]
public class TowerData : ScriptableObject
{
    [Header("Strings")]
    public string m_towerName;
    [TextArea(5, 5)]
    public string m_towerDescription;
    public bool m_isBlueprint;
    
    [Header("Basic Data")]
    public float m_baseDamage;
    public float m_targetRange;
    public float m_fireRange;
    public float m_fireRate;
    public float m_rotationSpeed;
    public float m_facingThreshold = 10f;
    
    [Header("Arc Cone")]
    public float m_fireConeAngle;

    [Header("Secondary Data")] 
    public bool m_hasSecondaryAttack;
    public float m_secondaryDamage;
    public float m_secondaryfireRange;
    public float m_secondaryfireRate;
    
    
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
    public GameObject m_secondaryProjectilePrefab;
    public GameObject m_towerConstructionPrefab;
    public GameObject m_muzzleFlashPrefab;
    
    [Header("sFX")]
    public List<AudioClip> m_audioFireClips;
    public List<AudioClip> m_audioSecondaryFireClips;
    public List<AudioClip> m_audioLoops;
    public AudioClip m_audioBuildClip;
    public AudioClip m_audioDestroyClip;
    public AudioClip m_audioSelectedClip;

    [Header("Upgrades")]
    public List<TowerData> m_upgradeOptions;
}
