using UnityEngine;

public abstract class Ruin : MonoBehaviour
{
    public abstract RuinTooltipData GetTooltipData();
}

public class RuinTooltipData
{
    public string m_ruinName;
    public string m_ruinDescription;
    public string m_ruinDetails;
}
