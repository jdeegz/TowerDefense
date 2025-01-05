using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RuinShrine : Ruin
{
    public ShrineRuinData m_data;

    [Header("Temporary Display Objects")]
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

    private List<GathererController> m_gatherers;

    public override void GathererArrivedAtRuin(GathererController gathererController)
    {
        //
    }

    void Awake()
    {
        base.Awake();

        m_maxCharges = m_data.m_maxCharges;
        m_chargesPerInterval = m_data.m_chargesPerInterval;
        m_intervalLength = m_data.m_intervalLength;
        m_burstIntervalLength = m_data.m_burstIntervalLength;

        RequestPlayAudio(m_data.m_discoveredAudioClip);

        m_intervalElapsedTime = m_intervalLength - 1.5f;
        m_gatherers = GameplayManager.Instance.m_woodGathererList;
    }

    void Update()
    {
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
        RequestPlayAudio(m_data.m_chargeConsumedAudioClip);

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

    public override RuinTooltipData GetTooltipData()
    {
        RuinTooltipData data = new RuinTooltipData();
        data.m_ruinName = m_data.m_ruinName;
        data.m_ruinDescription = m_data.m_ruinDescription;
        data.m_ruinDetails = null;
        return data;
    }
}