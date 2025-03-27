using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class RuinArtifact : Ruin
{
    public ArtifactRuinData m_data;
    public GameObject m_chargedDisplayRoot;
    public GameObject m_artifactScrollObj;
    public VisualEffect m_chargeVFX;
    public GameObject m_claimVFX;

    private int m_chargeCount = 1;

    public override void GathererArrivedAtRuin(GathererController gathererController)
    {
        gathererController.RequestUpdateGathererLevel(1);
    }

    void Awake()
    {
        base.Awake();
        RequestPlayAudio(m_data.m_discoveredAudioClip);
        
        m_chargeVFX.Play();
        m_chargeCount = m_data.m_startingCharge;
    }

    public bool CheckForCharge()
    {
        if (m_chargeCount > 0)
        {
            UpdateChargeCount(-1);
            ObjectPoolManager.SpawnObject(m_claimVFX, m_chargedDisplayRoot.transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
            return true;
        }

        return false;
    }

    public void UpdateChargeCount(int i)
    {
        m_chargeCount += i;

        if (m_chargeCount == 0)
        {
            m_chargeVFX.Stop();
            m_artifactScrollObj.SetActive(false);
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