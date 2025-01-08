using UnityEngine;

public class RuinIndicator : Ruin
{
    [SerializeField] private RuinData m_data;
    [SerializeField] private ProgressionKeyData m_unlockKeyData;
    private RuinController m_ruinController;
    
    public override RuinTooltipData GetTooltipData()
    {
        RuinTooltipData data = new RuinTooltipData();
        data.m_ruinName = m_data.m_ruinName;
        data.m_ruinDescription = m_data.m_ruinDescription;
        data.m_ruinDetails = null;
        return data;
    }

    public void SetUpRuinIndicator(RuinController ruinController)
    {
        m_ruinController = ruinController;
    }
    public override void GathererArrivedAtRuin(GathererController gathererController)
    {
        Debug.Log($"Gatherer Arrived at undiscovered ruin! Requesting Unlock Key!");
        m_ruinController.GathererDiscoveredRuin();
    }
}
