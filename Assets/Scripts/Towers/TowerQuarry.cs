using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerQuarry : Tower
{
    [Header("Quarry Fields")]
    public List<GameObject> m_chargeObjs;
    public GameObject m_claimVFX;
    public int m_grantAmount = 1;
    
    public override void SetupTower()
    {
        base.SetupTower();
        GameplayManager.OnWaveChanged += WaveChanged;
    }

    void OnDestroy()
    {
        base.OnDestroy();
        GameplayManager.OnWaveChanged -= WaveChanged;
    }
    
    private void WaveChanged(int obj)
    {
        if (!m_isBuilt) return;
        AutoGrant();
    }
    
    void AutoGrant()
    {
        // DATA
        ResourceManager.Instance.UpdateStoneAmount(m_grantAmount);
        
        // UI
        IngameUIController.Instance.SpawnCurrencyAlert(0, m_grantAmount, true, transform.position);
        
        // AUDIO
        RequestPlayAudio(m_towerData.m_audioSecondaryFireClips);
        RequestStopAudioLoop();
    }
    
    public override TowerTooltipData GetTooltipData()
    {
        TowerTooltipData data = new TowerTooltipData();
        data.m_towerName = m_towerData.m_towerName;
        string description = string.Format(m_towerData.m_towerDescription, m_grantAmount);
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
