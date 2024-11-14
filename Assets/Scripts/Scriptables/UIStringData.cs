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
    public string m_waveComplete;
    public string m_displayHighScore;
    public string m_newHighScoreDisplay;
    public string m_bossWaveComplete;
    public string m_bossDamageType;
    public string m_ruinIndicatedString;

    [Header("Building Restriction Reasons")]
    public string m_buildRestrictedDefault;
    public string m_buildRestrictedActorsOnCell;
    public string m_buildRestrictedActorsInIsland;
    public string m_buildRestrictedOccupied;
    public string m_buildRestrictedRestricted;
    public string m_buildRestrictedBlocksPath;
    
    [Header("End of Game Strings")]
    public string m_victory;
    public string m_defeat;
    public string m_scoreObelisk = "Obelisk Score:";
    public string m_scoreWaves = "{0}-{1} Wave Penalty ({2} x {3}):";
    public string m_scoreLastTierWaves = "{0}+ Wave Penalty ({1} x {2}):";
    public string m_totalScore = "Total:";
    public string m_newEndlessHighScore = "Wave {0} is a new Endless Mode High Score!";
    public string m_currentEndlessHighScore = "Endless Mode High Score: Wave {0}";

    [Header("Sound Strings")]
    public string m_volumeMasterText;
    public string m_volumeMusicText;
    public string m_volumeSFXText;
}
