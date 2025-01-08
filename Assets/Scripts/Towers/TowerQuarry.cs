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

        SetVisuals();
    }

    private void SetVisuals()
    {
        foreach (GameObject obj in m_chargeObjs)
        {
            obj.SetActive(false);
        }

        if (m_curCharges == 0) return;
        
        m_chargeObjs[m_curCharges - 1].SetActive(true);
    }

    public override void GameObjectSelected(GameObject obj)
    {
        base.GameObjectSelected(obj);

        if (!m_isBuilt) return;
        
        if (obj != gameObject) return;
        
        if (m_curCharges < m_maxCharges) return;

        GrantCharges();
    }

    public override void GameplayStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState != GameplayManager.GameplayState.Build)
        {
            return;
        }

        if (m_curCharges >= m_maxCharges)
        {
            // Remind the player to collect.
            RequestPlayAudioLoop(m_towerData.m_audioLoops[0]);
            return;
        }

        if (m_curCharges < m_maxCharges && GameplayManager.Instance.m_wave - m_lastChargeWave <= m_intervalLength)
        {
            // Increment curCharges -- This will increment only once. Change the above condition to have it catch up to missing charges / waves.
            m_lastChargeWave = GameplayManager.Instance.m_wave;
            IncrementCharges();
        }
    }

    void IncrementCharges()
    {
        // DATA UPDATES
        int newChargesToAdd = Math.Min(m_chargesPerInterval, m_maxCharges - m_curCharges);
        m_curCharges += newChargesToAdd;
        SetVisuals();

        // VISUAL UPDATES

        // AUDIO
        RequestPlayAudio(m_towerData.m_audioFireClips);
    }

    void GrantCharges()
    {
        IngameUIController.Instance.SpawnCurrencyAlert(0, m_curCharges, true, transform.position);

        // Spawn VFX
        foreach (GameObject obj in m_chargeObjs)
        {
            ObjectPoolManager.SpawnObject(m_claimVFX, obj.transform.position, Quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
        }

        // AUDIO
        RequestPlayAudio(m_towerData.m_audioSecondaryFireClips);
        RequestStopAudioLoop();

        // DATA RESET
        ResourceManager.Instance.UpdateStoneAmount(m_curCharges);
        m_curCharges = 0;

        GameplayManager.Instance.DeselectObject(gameObject.GetComponent<Selectable>());

        // VISUAL UPDATES
        SetVisuals();
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
