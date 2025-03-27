using System.Collections.Generic;
using UnityEngine;

public class TowerShrine : Tower
{
    [Header("Shrine Fields")]
    public GameObject m_chargeObj; // The object to spawn.
    public GameObject m_chargeSpawnRoot; // The point to spawn from.

    private int m_maxCharges;
    private int m_chargesPerInterval; // How many spawn at a time.
    private int m_spawnedChargeCount; // How many we have spawned.

    private float m_intervalLength; // Time between bursts.
    private float m_intervalElapsedTime; // Time since last burst.

    private float m_burstIntervalLength; // Time between spawns.
    private float m_burstElapsedTime; // Time since last spawn.
    private int m_curChargeCount;

    public override void SetupTower()
    {
        base.SetupTower();

        TriggerShrine();
    }
    
    void TriggerShrine()
    {
        
    }

    void Start()
    {
        // Moved these out of Trigger Shrine so that I can sell this building and NOT reset the timers when we build it again.
        m_maxCharges = (int)m_towerData.m_secondaryfireRate;
        m_chargesPerInterval = (int)m_towerData.m_burstSize;
        m_intervalLength = m_towerData.m_fireRate;
        m_burstIntervalLength = m_towerData.m_burstFireRate;

        //Adding a 5s delay. If you build, click the gems, sell, then build again you can get a lot of harvest speed for the delta between build and sell costs.
        m_intervalElapsedTime = m_intervalLength - 5f; // I think 1.5f is the delay between starting the shrine and spawning orbs so its not immediately on placement.
    }

    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }
        
        if (m_spawnedChargeCount == m_chargesPerInterval || m_curChargeCount == m_maxCharges) // Reset to 0 if we've spawned enough to hit max charges or charges per interval.
        {
            m_spawnedChargeCount = 0;
            m_intervalElapsedTime = 0;
        }

        if (m_curChargeCount == m_maxCharges) return; // Dont spawn more than the maximum number of charges.

        if (m_intervalElapsedTime >= m_intervalLength) // Can we start the burst spawn?
        {
            if (m_burstElapsedTime >= m_burstIntervalLength && m_spawnedChargeCount <= m_chargesPerInterval) // Can we spawn a charge?
            {
                m_burstElapsedTime = 0;
                GrantCharges();
            }
        }

        m_intervalElapsedTime += Time.deltaTime;
        m_burstElapsedTime += Time.deltaTime;
    }

    void GrantCharges()
    {
        // SPAWN A CHARGE
        GameObject orb = ObjectPoolManager.SpawnObject(m_chargeObj, m_chargeSpawnRoot.transform.position, Quaternion.identity, null, ObjectPoolManager.PoolType.GameObject);
        orb.GetComponent<ShrineOrbController>().SetShrine(this);

        // AUDIO
        RequestPlayAudio(m_towerData.m_audioFireClips);

        // DATA RESET
        ++m_spawnedChargeCount;
        ++m_curChargeCount;
    }

    public void ChargeClicked()
    {
        --m_curChargeCount;
        SendEffect();
    }

    void SendEffect()
    {
        foreach (GathererController gatherer in GameplayManager.Instance.m_woodGathererList)
        {
            ShrineRuinEffect effect = new ShrineRuinEffect();
            gatherer.ApplyEffect(effect);
        }
    }
    
    public override TowerTooltipData GetTooltipData()
    {
        TowerTooltipData data = new TowerTooltipData();
        data.m_towerName = m_towerData.m_towerName;
        data.m_towerDescription = m_towerData.m_towerDescription;
        return data;
    }

    public override TowerUpgradeData GetUpgradeData()
    {
        TowerUpgradeData data = new TowerUpgradeData();

        data.m_turretRotation = GetTurretRotation();

        return data;
    }

    public override void SetUpgradeData(TowerUpgradeData data)
    {
        SetTurretRotation(data.m_turretRotation);
    }
}
