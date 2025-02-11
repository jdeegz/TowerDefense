using UnityEngine;

public class RuinIndicator : Ruin
{
    [SerializeField] private RuinData m_data;
    [SerializeField] private GameObject m_relic;
    private RuinController m_ruinController;
    private ProgressionUnlockableData m_unlockableData;
    private TowerData m_towerData;

    public override RuinTooltipData GetTooltipData()
    {
        RuinTooltipData data = new RuinTooltipData();
        data.m_ruinName = m_data.m_ruinName;
        data.m_ruinDescription = m_towerData.m_towerRuinDescription;
        
        UnlockProgress unlockProgress = m_unlockableData.GetProgress();
        if (m_ruinController.ProgressionKey.ProgressionKeyEnabled)
        {
            data.m_ruinDetails = m_data.m_ruinDiscovered;
        }
        else
        {
            data.m_ruinDetails = m_data.m_ruinDetails;
        }

        return data;
    }

    public void SetUpRuinIndicator(RuinController ruinController)
    {
        m_ruinController = ruinController;
        m_unlockableData = PlayerDataManager.Instance.m_progressionTable.GetUnlockableFromKey(m_ruinController.ProgressionKey);
        m_towerData = m_unlockableData.GetRewardData().GetReward();
    }

    public override void GathererArrivedAtRuin(GathererController gathererController)
    {
        //Debug.Log($"Gatherer Arrived at undiscovered ruin! Requesting Unlock Key!");
        m_ruinController.GathererDiscoveredRuin();
    }

    public void ToggleRuinRelic(bool b)
    {
        m_relic.SetActive(b);
    }
}