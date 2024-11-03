using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrojanUnitSpawner : MonoBehaviour
{
    //The Trojan spawner is created at the X,Y position of the Enemy Trojan when it dies.
    //It needs to be added to the gameplay manager's active spawner list.
    //It needs to spawn creeps from each creep wave based on their timing data.
    [SerializeField] private TearData m_data;
    [SerializeField] private List<CreepWave> m_creepWaves;

    private List<CreepSpawner> m_activeCreepSpawners;
    private List<Creep> m_activeCreepWave;
    private bool m_isSpawnerActive;
    
    private void OnEnable()
    {
        m_isSpawnerActive = false;
        
        //Add this spawner to the list of active spawners to keep the current wave active while it spawns.
        GameplayManager.Instance.ActivateSpawner();
        
        StartCoroutine(CreateSpawner());
    }

    private IEnumerator CreateSpawner()
    {
        yield return new WaitForSeconds(0.5f);
        StartSpawning();
    }
    
    private void StartSpawning()
    {
        //Calculate which CreepWave to spawn based on mission's wave number.
        int creepWaveIndex = GameplayManager.Instance.m_wave % m_creepWaves.Count;
        
        //Assure each creep has a point to spawn to.
        m_activeCreepSpawners = new List<CreepSpawner>();
        for (int i = 0; i < m_creepWaves[creepWaveIndex].m_creeps.Count; ++i)
        {
            //Build list of Active Creep Spawners.
            CreepSpawner creepSpawner = new CreepSpawner(m_creepWaves[creepWaveIndex].m_creeps[i], transform);
            m_activeCreepSpawners.Add(creepSpawner);
        }

        m_isSpawnerActive = true;
    }
    
    private void Update()
    {
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
                    //If the creep is NOT spawning, remove it from the active creep spawner list.
                    m_activeCreepSpawners.RemoveAt(i);
                    --i;
                }
            }

            //If we have NO active creep spawners, disable this spawner.
            if (m_activeCreepSpawners.Count == 0)
            {
                m_isSpawnerActive = false;
                GameplayManager.Instance.DisableSpawner();
                RemoveTrojanUnitSpawner();
            }
        }
    }

    private void RemoveTrojanUnitSpawner()
    {
        //Trigger animation to remove spawners
        
        //Remove Obj from scene
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.Enemy);
    }
    
    public TearTooltipData GetTooltipData()
    {
        TearTooltipData data = new TearTooltipData();
        data.m_tearName = m_data.m_tearName;
        data.m_tearDescription = m_data.m_tearDescription;
        data.m_tearDetails = m_data.m_tearDetails;
        return data;
    }
}
