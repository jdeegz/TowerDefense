using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class SurvivalSpawner : EnemySpawner
{
    private List<Cell> m_validSpawnCells;
    private float m_elapsedTime;
    private float m_nextSpawnTime;
    private float m_nextWaveTime;


    // Update is called once per frame
    void Update()
    {
        if (GameplayManager.Instance.m_gameplayState != GameplayManager.GameplayState.SpawnEnemies) return;

        for (int i = 0; i < m_activeCreepSpawners.Count; ++i)
        {
            if (m_activeCreepSpawners[i].IsCreepSpawning())
            {
                m_activeCreepSpawners[i].UpdateCreepSurvival();
            }
            else
            {
                //If the creep is NOT spawning, remove it from the active creep spawner list.
                m_activeCreepSpawners.RemoveAt(i);
                --i;
            }
        }
    }

    public override void UpdateCreepSpawners()
    {
        SetNextCreepWave();
        
        if (m_nextCreepWave == null) return;
        
        m_activeCreepSpawners = new List<CreepSpawner>();
        for (int i = 0; i < m_nextCreepWave.m_creeps.Count; ++i)
        {
            //Build list of Active Creep Spawners.
            CreepSpawner creepSpawner = new CreepSpawner(m_nextCreepWave.m_creeps[i], m_validSpawnCells);
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
    }

    public void SetNextCreepWave()
    {
        int gameplayWave = GameplayManager.Instance.m_wave;
        
        //Debug.Log($"Getting wave {GameplayManager.Instance.m_wave}");

        CreepWave creepWave = new CreepWave();

        
        //NEW UNIT TYPE WAVES
        if (m_spawnerWaves.m_newEnemyTypeWaves.Count != 0)
        {
            foreach (NewTypeCreepWave newTypeCreepWave in m_spawnerWaves.m_newEnemyTypeWaves)
            {
                if (gameplayWave == newTypeCreepWave.m_waveToSpawnOn)
                {
                    creepWave = newTypeCreepWave;

                    m_nextCreepWave = creepWave;
                    return;
                }
            }
        }

        // Now subtracting 1 for accurate indexing.
        gameplayWave -= 1;
        
        //INTRO WAVES
        if (gameplayWave < m_spawnerWaves.m_introWaves.Count)
        {
            creepWave = m_spawnerWaves.m_introWaves[gameplayWave];

            //Debug.Log($"INTRO Wave {gameplayWave} Chosen.");
            m_nextCreepWave = creepWave;
            return;
        }


        //Calculate challenging wave BEFORE subtracting intro waves, to assure player see multiple of 5 and gets a hard wave.
        int challengingWave = (gameplayWave + 1) % 5;
        
        //Subtract the number of training ways so that we start at wave 0 in the new lists.
        gameplayWave -= m_spawnerWaves.m_introWaves.Count;

        //LOOPING WAVE OR CHALLENGING WAVE
        //Boss waves occur every 5 gameplay Waves.
        if (challengingWave == 0 && m_spawnerWaves.m_challengingWaves.Count > 0)
        {
            int wave = (gameplayWave) % m_spawnerWaves.m_challengingWaves.Count;
            creepWave = m_spawnerWaves.m_challengingWaves[wave];
            //Debug.Log($"CHALLENGING Wave {wave} Chosen.");
        }
        else
        {
            int wave = (gameplayWave) % m_spawnerWaves.m_loopingWaves.Count;
            creepWave = m_spawnerWaves.m_loopingWaves[wave];
            //Debug.Log($"LOOPING Wave {wave} Chosen.");
        }
        m_nextCreepWave = creepWave;
    }

    private void GetValidSpawnCells()
    {
        List<Cell> outofBoundsCells = GridManager.Instance.GetOutOfBoundsSpawnCells();
        
        //m_validSpawnCells = Util.FindInteriorCells(outofBoundsCells);
        List<Cell> interiorCells = Util.FindInteriorCells(outofBoundsCells);

        // Remove occupied cells.
        List<Cell> unoccupiedCell = new List<Cell>();
        foreach (Cell cell in interiorCells)
        {
            if(!cell.m_isOccupied) unoccupiedCell.Add(cell);
        }

        m_validSpawnCells = unoccupiedCell;
    }

    public override void GameplayManagerStateChanged(GameplayManager.GameplayState obj)
    {
        switch (obj)
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
                // Get the build restricted cells from Grid Manager.
                // Subtract 1 from the perimeter and assign result to valid Spawn Cells.
                GetValidSpawnCells();
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
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