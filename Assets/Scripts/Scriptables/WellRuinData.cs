using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "WellRuinData", menuName = "ScriptableObjects/WellRuinData")]
public class WellRuinData : RuinData
{
    public int startingCharge = 0; // How many charges we can store
    public int m_maxCharges = 4; // How many charges we can store
    public int m_chargesPerInterval = 1; // How many charges to add per interval
    public int m_intervalLength = 1; // How many waves it takes to generate charges
}
