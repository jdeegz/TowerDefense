using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerData", menuName = "Tower/TowerData")]
public class ScriptableTowerDataObject : ScriptableObject
{
    public string m_name;
    public int m_targetRange;
    public int m_fireRange;
    public float m_fireRate;
    public float m_rotationSpeed;
    public int m_woodCost;
    public int m_stoneCost;
    public Sprite m_uiIcon;
    public GameObject m_prefab;
    public GameObject m_projectilePrefab;
}
