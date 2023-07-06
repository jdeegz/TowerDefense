using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager m_buildManager;

    [SerializeField] private GameObject[] m_towerPrefabs;
    private int m_selectedTower;
    // Start is called before the first frame update
    void Awake()
    {
        m_buildManager = this;
    }

    public GameObject GetSelectedTowerPrefab()
    {
        return m_towerPrefabs[m_selectedTower];
    }
}
