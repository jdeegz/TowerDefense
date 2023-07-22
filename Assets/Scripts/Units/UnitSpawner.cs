using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class UnitSpawner : MonoBehaviour
{
    [SerializeField] private GameObject m_unitPrefab;
    [SerializeField] private float m_spawnInterval = 1f;
    [SerializeField] private int m_unitsPerWave = 5;
    [SerializeField] private float m_waveInterval = 30f;
    [SerializeField] private Transform m_spawnPoint;

    private int m_unitsSpawned;
    private bool m_isSpawning;
    private float m_elapsedTime;

    private void Start()
    {
        m_isSpawning = false;
        StartSpawning();
    }

    private void Update()
    {
        if (m_isSpawning)
        {
            m_elapsedTime += Time.deltaTime;

            //If there are more units to spawn in this wave, continue to spawn them at the spawn interval
            if (m_unitsSpawned < m_unitsPerWave)
            {
                if (m_elapsedTime >= m_spawnInterval)
                {
                    SpawnUnit();
                    m_elapsedTime = 0f;
                }
            }
            //If there are no more units to spawn in the wave, we wait for the next wave.
            else
            {
                if (m_elapsedTime >= m_waveInterval)
                {
                    m_unitsSpawned = 0;
                    m_elapsedTime = 0f;
                    SpawnWave();
                }
            }
        }
    }

    private void StartSpawning()
    {
        m_unitsSpawned = 0;
    }

    private void SpawnUnit()
    {
        Instantiate(m_unitPrefab, m_spawnPoint.position, Quaternion.identity);
        m_unitsSpawned++;
    }

    private void SpawnWave()
    {
        // Spawn a wave of units here
        // You can use a loop or any other mechanism to spawn multiple units
    }

    public void StopSpawning()
    {
        m_isSpawning = false;
    }
    
    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState state)
    {
        m_isSpawning = state == GameplayManager.GameplayState.Combat;
    }
}
