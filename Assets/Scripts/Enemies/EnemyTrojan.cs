using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Diagnostics;
using Random = UnityEngine.Random;

public class EnemyTrojan : EnemyController
{
    private List<EnemyController> m_livingTrojanEnemies;
    
    public override void OnEnemyDestroyed(Vector3 pos)
    {
        if (m_isComplete) return;

        if (m_curHealth <= 0) CreateTrojanSpawner();

        base.OnEnemyDestroyed(pos);
    }
    
    private void CreateTrojanSpawner()
    {
        Vector3 spawnPos = new Vector3(transform.position.x, 0, transform.position.z);
        Quaternion spawnRot = Quaternion.LookRotation(transform.forward);
        ObjectPoolManager.SpawnObject(m_enemyData.m_trojanSpawner, spawnPos, spawnRot, ObjectPoolManager.PoolType.Enemy);
    }
}