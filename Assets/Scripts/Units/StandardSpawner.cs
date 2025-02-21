using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening.Core.Enums;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class StandardSpawner : EnemySpawner
{
    private void Start()
    {
        m_isSpawnerActive = false;
        RequestPlayAudioLoop(m_data.m_audioSpawnerActiveLoops);
    }

    private void Update()
    {
        // is Spawning is determine by the Gameplay State, if we're in GameplayManager.GameplayState.SpawnEnemies, spawn enemies.
        if (m_isSpawnerActive)
        {
            for (int i = 0; i < m_activeCreepSpawners.Count; ++i)
            {
                if (m_activeCreepSpawners[i].IsCreepSpawning())
                {
                    m_activeCreepSpawners[i].UpdateCreep();
                }
                else
                {
                    // If the creep is NOT spawning, remove it from the active creep spawner list.
                    m_activeCreepSpawners.RemoveAt(i);
                    --i;
                }
            }

            // If we have NO active creep spawners, disable this spawner.
            if (m_activeCreepSpawners.Count == 0)
            {
                m_isSpawnerActive = false;
            }
        }
    }

    public override void UpdateCreepSpawners()
    {
        if (m_nextCreepWave == null) return;

        //Assure each creep has a point to spawn to.
        m_activeCreepSpawners = new List<CreepSpawner>();
        for (int i = 0; i < m_nextCreepWave.m_creeps.Count; ++i)
        {
            //Build list of Active Creep Spawners.
            CreepSpawner creepSpawner = new CreepSpawner(m_nextCreepWave.m_creeps[i], m_spawnPoint);
            creepSpawner.m_spawnStatusEffect = m_spawnStatusEffect;
            m_activeCreepSpawners.Add(creepSpawner);
        }

        //Decrement Spawn Status Effect Duration.
        --m_spawnStatusEffectWaveDuration;

        //Remove the Spawn Status Effect if we've reached 0 rounds left.
        if (m_spawnStatusEffectWaveDuration == 0)
        {
            m_spawnStatusEffect = null;
        }

        m_isSpawnerActive = true;
    }

    public override void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        switch (newState)
        {
            case GameplayManager.GameplayState.BuildGrid:
                break;
            case GameplayManager.GameplayState.PlaceObstacles:
                GameplayManager.Instance.AddSpawnerToList(this);
                GridCellOccupantUtil.SetActor(gameObject, 1, 1, 1);
                break;
            case GameplayManager.GameplayState.FloodFillGrid:
                break;
            case GameplayManager.GameplayState.CreatePaths:
                break;
            case GameplayManager.GameplayState.Setup:
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
                UpdateCreepSpawners();
                break;
            case GameplayManager.GameplayState.BossWave:
                break;
            case GameplayManager.GameplayState.Combat:
                break;
            case GameplayManager.GameplayState.Build:
                break;
            case GameplayManager.GameplayState.CutScene:
                break;
            case GameplayManager.GameplayState.Victory:
                break;
            case GameplayManager.GameplayState.Defeat:
                break;
            default:
                break;
        }
    }
}