using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIStrings", menuName = "ScriptableObjects/StringContainer")]
public class UIStringData : ScriptableObject
{
    [Header("Alert Strings")]
    public string m_cannotAfford;
    public string m_cannotPlace;
    public string m_gathererLevelUp;
    public string m_gathererIdle;
    
    [Header("End of Game Strings")]
    public string m_victory;
    public string m_defeat;
    public string m_scoreObelisk = "Obelisk Score:";
    public string m_scoreWaves = "{0}-{1} Wave Penalty ({2} x {3}):";
    public string m_scoreLastTierWaves = "{0}+ Wave Penalty ({1} x {2}):";
    public string m_totalScore = "Total:";

    [Header("Sound Strings")]
    public string m_volumeMasterText;
    public string m_volumeMusicText;
    public string m_volumeSFXText;
}
