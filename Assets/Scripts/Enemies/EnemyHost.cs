using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

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
        var spawners = GameplayManager.Instance.m_unitSpawners;
        
        //Spawn a thrall at reach spawner.
        m_livingThralls = new List<EnemyThrall>();
        foreach (UnitSpawner spawner in spawners)
        {
            GameObject thrallObj = ObjectPoolManager.SpawnObject(m_enemyThrall.gameObject, spawner.m_spawnPoint.position, quaternion.identity, null, ObjectPoolManager.PoolType.Enemy);
            EnemyThrall thrall = thrallObj.GetComponent<EnemyThrall>();
            thrall.SetEnemyData(m_enemyData);
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

    public override void RequestRemoveEffect(GameObject sender)
    {
        //Remove effect from host
        foreach (EnemyThrall thrall in m_livingThralls)
        {
            thrall.HostRemoveEffect(sender);
        }
    }
    
    public override void ApplyEffect(StatusEffect statusEffect)
    {
        //Apply effect to host
        foreach (EnemyThrall thrall in m_livingThralls)
        {
            thrall.HostApplyEffect(statusEffect);
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
