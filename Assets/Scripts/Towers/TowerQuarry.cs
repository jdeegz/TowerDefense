using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerQuarry : Tower
{
    [Header("Quarry Fields")]
    public List<GameObject> m_chargeObjs;
    public GameObject m_claimVFX;

    private int m_curCharges;
    private int m_maxCharges;
    private int m_chargesPerInterval;
    private int m_lastChargeWave;
    private int m_intervalLength;
    
    public override void SetupTower()
    {
        base.SetupTower();

        TriggerQuarry();
    }

    public void TriggerQuarry()
    {

        m_maxCharges = (int)m_towerData.m_secondaryfireRate;
        m_chargesPerInterval = (int)m_towerData.m_burstSize;
        m_intervalLength = (int)m_towerData.m_fireRate;
        m_curCharges = (int)m_towerData.m_burstFireRate;

        m_lastChargeWave = GameplayManager.Instance.m_wave;

    }

    public override void GameplayStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState != GameplayManager.GameplayState.Build)
        {
            return;
        }

        if (!m_isBuilt) return;
        
        AutoGrant();
    }

    void AutoGrant()
    {
        // DATA
        ResourceManager.Instance.UpdateStoneAmount(m_curCharges);
        
        // UI
        IngameUIController.Instance.SpawnCurrencyAlert(0, m_curCharges, true, transform.position);
        
        // AUDIO
        RequestPlayAudio(m_towerData.m_audioSecondaryFireClips);
        RequestStopAudioLoop();
    }
    
    public override TowerTooltipData GetTooltipData()
    {
        TowerTooltipData data = new TowerTooltipData();
        data.m_towerName = m_towerData.m_towerName;
        string description = string.Format(m_towerData.m_towerDescription, m_curCharges);
        data.m_towerDescription = description;
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
