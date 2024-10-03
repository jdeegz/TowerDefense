using UnityEngine;

[CreateAssetMenu(fileName = "ShrineRuinData", menuName = "ScriptableObjects/ShrineRuinData")]
public class ShrineRuinData : RuinData
{
    public int m_maxCharges = 3;
    public int m_chargesPerInterval = 3; // How many spawn at a time.
    public float m_intervalLength = 60; // Time between spawns.
    public float m_burstIntervalLength = 0.3f;
}
