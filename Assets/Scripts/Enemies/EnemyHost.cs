using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyHost : EnemyController
{
    [SerializeField] private EnemyThrall m_enemyThrall; //objects spawned as duplicates of the host
    private List<EnemyThrall> m_livingThralls;
    private BossSequenceController m_bossSequenceController;
    
    //HOST SETUP FUNCTIONS
    public override void SetupEnemy(bool active)
    {
        base.SetupEnemy(active);
        
        SetHostPosition();
        SetSequenceController(GameplayManager.Instance.GetActiveBossController());
        
        //Get the spawners in the scene.
        var spawners = GameplayManager.Instance.m_enemySpawners;
        
        //Spawn a thrall at reach spawner.
        m_livingThralls = new List<EnemyThrall>();
        foreach (StandardSpawner spawner in spawners)
        {
            Vector3 spawnPoint = spawner.GetSpawnPointTransform().position;
            Cell cell = Util.GetCellFrom3DPos(spawner.transform.position);
            Vector2Int directionToNextCell = cell.GetDirectionVector(cell.m_directionToNextCell);
            Quaternion spawnRotation = Quaternion.LookRotation(new Vector3(directionToNextCell.x, 0, directionToNextCell.y));
            float xOffset = Random.Range(-0.4f, 0.4f);
            float zOffset = Random.Range(-0.4f, 0.4f);

            spawnPoint.x += xOffset;
            spawnPoint.z += zOffset;

            GameObject thrallObj = ObjectPoolManager.SpawnObject(m_enemyThrall.gameObject, spawnPoint, spawnRotation, null, ObjectPoolManager.PoolType.Enemy);
            EnemyThrall thrall = thrallObj.GetComponent<EnemyThrall>();
            thrall.SetHost(this);
            m_livingThralls.Add(thrall);
        }
    }

    public void ThrallReachedCastle(EnemyThrall thrall)
    {
        m_livingThralls.Remove(thrall);

        // If all the thralls are destroyed and we still have health, run away!
        if (m_livingThralls.Count == 0)
        {
            OnEnemyDestroyed(transform.position);
        }
    }

    public void SetSequenceController(BossSequenceController controller)
    {
        m_bossSequenceController = controller;
    }

    void SetHostPosition()
    {
        transform.position = GameplayManager.Instance.m_enemyGoal.position;
    }

    public override void AddToGameplayList()
    {
        GameplayManager.Instance.AddBossToList(this);
    }
    
    public override void RemoveFromGameplayList()
    {
        GameplayManager.Instance.RemoveBossFromList(this);
    }

    public override void SetupUI()
    {
        //Setup the Boss health meter.
        UIHealthMeter lifeMeter = ObjectPoolManager.SpawnObject(IngameUIController.Instance.m_healthMeterBoss.gameObject, IngameUIController.Instance.m_healthMeterBossRect).GetComponent<UIHealthMeter>();
        lifeMeter.SetBoss(this, m_curMaxHealth);
    }
    //
    
    //TAKING DAMAGE AND MANAGING EFFECTS
    public override void OnTakeDamage(float dmg)
    {
        if (m_curHealth <= 0) return;
        base.OnTakeDamage(dmg);
        
        //Flash all thralls
        foreach (EnemyThrall thrall in m_livingThralls)
        {
            thrall.HostHit();
        }
    }
    
    public override void OnEnemyDestroyed(Vector3 pos)
    {
        //Destroy the thralls
        foreach (EnemyThrall thrall in m_livingThralls)
        {
            thrall.OnEnemyDestroyed(thrall.transform.position);
        }
        
        m_bossSequenceController.BossRemoved(m_curHealth);
        
        //Destroy the host
        base.OnEnemyDestroyed(pos);
    }
    //

    public override void SpawnCores()
    {
        if (m_livingThralls.Count == 0) return; //You failed, no core reward for you.
        
        Debug.Log($"Spawn Cores: Spawn Enemy Host Cores.");
        
        // What percent of defined cores should we spawn?
        int currentMaxHealth = GameplayManager.Instance.m_castleController.GetCurrentMaxHealth();
        float rewardPercent = (float)currentMaxHealth / (currentMaxHealth + GameplayManager.Instance.DamageTakenThisWave);

        // How many cores is that?
        int coresToReward = Mathf.RoundToInt(m_enemyData.m_coreRewardCount * rewardPercent);
        Debug.Log($"Spawn Cores: Cores to Reward: {coresToReward}");
        
        // Find the uncharged Obelisks.
        List<Obelisk> unchargedObelisks = FindAllUnchargedObelisks();
        
        // If we have no uncharged obelisks, we're done here. Nothing to Grant.
        if (unchargedObelisks == null || unchargedObelisks.Count == 0)
        {
            Debug.Log($"Spawn Cores: Uncharged Obelisks Null or Count is 0.");
            return;
        }
        
        // Calculate the number of cores to sent to the first obelisk in the list.
        int obeliskIndex = 0;
        Obelisk currentObelisk = unchargedObelisks[obeliskIndex];
        int currentObeliskAvailableCapacity = unchargedObelisks[obeliskIndex].GetObeliskMaxChargeCount() - unchargedObelisks[obeliskIndex].GetObeliskChargeCount();
        int currentShareOfCores = m_enemyData.m_coreRewardCount / unchargedObelisks.Count;
        GameObject coreObj = currentObelisk.m_obeliskData.m_obeliskSoulObj;
        
        // Each spawn point should spawn it's share of cores.
        // Each core should travel to its share of uncharged obelisks.
        int coresSentToCurrentObelisk = 0;
        Debug.Log($"Spawn Cores: There are {m_livingThralls.Count} valid Core Spawn locations.");
        
        for (int i = 0; i < coresToReward; ++i)
        {
            // Spawn a core at the bone position we desire, and have it travel to our current obelisk.
            Vector3 spawnPosition = m_livingThralls[i % m_livingThralls.Count].transform.position;
            GameObject obeliskSoulObject = ObjectPoolManager.SpawnObject(coreObj, spawnPosition, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
            ObeliskSoul obeliskSoul = obeliskSoulObject.GetComponent<ObeliskSoul>();
            
            obeliskSoul.SetupSoul(currentObelisk.m_targetPoint.transform.position, currentObelisk, 1);

            Debug.Log($"Spawn Cores: Core {i} Created at Living Thrall {i % m_livingThralls.Count}. Sending to Obelisk {obeliskIndex} at {currentObelisk.transform.position}");
            
            //Send Cores to obelisks until we've met the share of Cores per Obelisk or the obelisk is full. Recalculate the shares if the obelisk is filled.
            ++coresSentToCurrentObelisk;
            if (coresSentToCurrentObelisk >= Math.Min(currentObeliskAvailableCapacity, currentShareOfCores))
            {
                // Look to see if we have more obelisks to fill.
                ++obeliskIndex;
                if (obeliskIndex >= unchargedObelisks.Count)
                {
                    Debug.Log($"Spawn Cores: No more uncharged obelisks to fill.");
                    return;
                }
                
                // Calculate the new share among obelisks, and update the current obelisk.
                int coresRemaining = coresToReward - i;
                int obelisksRemaining = unchargedObelisks.Count - obeliskIndex;
                currentShareOfCores = obelisksRemaining > 0 ? coresRemaining / obelisksRemaining : coresRemaining;
                currentObelisk = unchargedObelisks[obeliskIndex];
                currentObeliskAvailableCapacity = currentObelisk.GetObeliskMaxChargeCount() - currentObelisk.GetObeliskChargeCount();
                coresSentToCurrentObelisk = 0;
            }
        }
    }
    
    
    //REMOVING BASE FUNCTIONALITY
    public override void HandleMovement()
    {
        //This unit does not move.
        return;
    }

    public override void CheckAtGoal()
    {
        //We dont care about being at the goal.
        return;
    }
    //
}
