using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Diagnostics;
using Random = UnityEngine.Random;

public class EnemyTrojan : EnemyController
{
    [SerializeField] private List<EnemyData> m_trojanEnemies;
    [SerializeField] private float m_spawnDelay = 0.3f; // Time between death and first unit spawning.
    [SerializeField] private float m_spawnBuffer = 0.0f; // Time between each unit spawning.

    private List<EnemyController> m_livingTrojanEnemies;
    private Vector3 m_offScreen = new Vector3(-20, 0, -20);

    //When we spawn the trojan, we need to also spawn it's children, add them to enemies list of easier wave management, then disable them.

    //If we reach the castle, we need to also remove the unspawned trojan children.

    //When we spawn a trojan child, we seek adjacent cells, apply small offsets, and DOTween a scale and translation with a random duration.

    public override void SetupEnemy(bool active)
    {
        CreateTrojanChildren();

        base.SetupEnemy(active);
    }

    void CreateTrojanChildren()
    {
        m_livingTrojanEnemies = new List<EnemyController>();

        foreach (EnemyData trojanEnemy in m_trojanEnemies)
        {
            GameObject enemyOjb = ObjectPoolManager.SpawnObject(trojanEnemy.m_enemyPrefab, m_offScreen, Quaternion.identity, ObjectPoolManager.PoolType.Enemy);
            EnemyController enemyController = enemyOjb.GetComponent<EnemyController>();
            enemyController.SetEnemyData(trojanEnemy, false);

            m_livingTrojanEnemies.Add(enemyController);
        }
    }

    public override void OnEnemyDestroyed(Vector3 pos)
    {
        if (m_isComplete) return;
        
        if(m_curHealth <= 0) SpawnTrojanEnemies();

        base.OnEnemyDestroyed(pos);
    }

    private void SpawnTrojanEnemies()
    {
        List<Vector2Int> emptyCellPositions = Util.GetAdjacentEmptyCellPos(Util.GetVector2IntFrom3DPos(transform.position));

        //For each living trojan enemy, pick a random cell. Then pick a random point within the cell.
        foreach (EnemyController trojanEnemy in m_livingTrojanEnemies)
        {
            Vector2Int spawnCellPos = emptyCellPositions[Random.Range(0, emptyCellPositions.Count)];
            float offset = 0.25f;
            float offsetX = Random.Range(-offset, offset);
            float offsetZ = Random.Range(-offset, offset);

            Vector3 spawnPos = new Vector3(spawnCellPos.x + offsetX, 0, spawnCellPos.y + offsetZ);

            trojanEnemy.HandleTrojanSpawn(transform.position, spawnPos);
        }
    }

    public override void ReachedCastle()
    {
        DestroyLivingChildren();

        base.ReachedCastle();
    }

    private void DestroyLivingChildren()
    {
        foreach (EnemyController trojanEnemy in m_livingTrojanEnemies)
        {
            trojanEnemy.OnEnemyDestroyed(trojanEnemy.transform.position);
        }
    }
}