using UnityEngine;

public class RuinShrine : Ruin
{
    public ShrineRuinData m_data;
    
    public override RuinTooltipData GetTooltipData()
    {
        RuinTooltipData data = new RuinTooltipData();
        data.m_ruinName = m_data.m_ruinName;
        data.m_ruinDescription = m_data.m_ruinDescription;
        data.m_ruinDetails = null;
        return data;
    }
}
