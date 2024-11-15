using System;
using System.Collections.Generic;
using UnityEngine;

public class RuinWell : Ruin
{
    public WellRuinData m_data;

    [Header("Visual Objects")]
    public List<GameObject> m_chargeObjs;
    public GameObject m_claimVFX;

    private int m_curCharges;
    private int m_maxCharges;
    private int m_chargesPerInterval;
    private int m_lastChargeWave;
    private int m_intervalLength;

    // Setup data
    public override void Awake()
    {
        base.Awake();

        m_curCharges = m_data.m_startingCharge;
        m_maxCharges = m_data.m_maxCharges;
        m_chargesPerInterval = m_data.m_chargesPerInterval;
        m_intervalLength = m_data.m_intervalLength;

        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        GameplayManager.OnGameObjectSelected += GameObjectSelected;

        RequestPlayAudio(m_data.m_discoveredAudioClip);

        m_lastChargeWave = GameplayManager.Instance.m_wave;

        SetVisuals();
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
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

    private void GameObjectSelected(GameObject obj)
    {
        if (obj != gameObject) return;

        if (m_curCharges < m_maxCharges) return;

        GrantCharges();
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState != GameplayManager.GameplayState.Build)
        {
            return;
        }

        if (m_curCharges >= m_maxCharges)
        {
            // Remind the player to collect.
            RequestPlayAudio(m_data.m_discoveredAudioClip);
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

        /*// VISUAL UPDATES
        for (int i = 0; i < newChargesToAdd; i++)
        {
            ++m_curCharges;
            m_chargeObjs[m_curCharges - 1].SetActive(true);
        }

        if (m_curCharges >= m_maxCharges)
        {
            m_maxChargePersistantObj.SetActive(true);
        }*/

        // AUDIO
        RequestPlayAudio(m_data.m_unclaimedAudioClip);
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
        RequestPlayAudio(m_data.m_chargeConsumedAudioClip);

        // DATA RESET
        ResourceManager.Instance.UpdateStoneAmount(m_curCharges);
        m_curCharges = 0;

        GameplayManager.Instance.DeselectObject(gameObject.GetComponent<Selectable>());

        // VISUAL UPDATES
        SetVisuals();
    }

    public override RuinTooltipData GetTooltipData()
    {
        RuinTooltipData data = new RuinTooltipData();
        data.m_ruinName = m_data.m_ruinName;
        string description = string.Format(m_data.m_ruinDescription, m_curCharges);
        data.m_ruinDescription = description;
        data.m_ruinDetails = null;
        return data;
    }
}