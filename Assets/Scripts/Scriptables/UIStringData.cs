using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "UIStrings", menuName = "ScriptableObjects/StringContainer")]
public class UIStringData : ScriptableObject
{
    [Header("Alert Strings")]
    public string m_cannotAfford;
    public string m_cannotPlace;
    public string m_gathererLevelUp;
    public string m_gathererIdle;
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
    public string m_buildRestrictedOutOfBounds = "Tower is out of bounds.";
    public string m_buildRestrictedBlocksPath;
    public string m_woodRequirmentNotMet = "Not enough Wood.";
    public string m_stoneRequirementNotMet = "Not Enough Manasteel.";
    public string m_buildRestrictedQuantityNotMet;

    [Header("Ruins Restriction Reasons")]
    public string m_unlockableUnlocked = "{0} Unlocked!";
    public string m_ruinDiscovered = "{0} / {1} Ruins Discovered!";
    
    [Header("End of Game Strings")]
    public string m_victory;
    public string m_defeat;
    public string m_scoreObelisk = "Obelisk Score:";
    public string m_scoreWaves = "{0}-{1} Wave Penalty ({2} x {3}):";
    public string m_scoreLastTierWaves = "{0}+ Wave Penalty ({1} x {2}):";
    public string m_totalScore = "Total:";
    public string m_newEndlessHighScore = "Wave {0} is a new Endless Mode High Score!";
    public string m_currentEndlessHighScore = "Endless Mode High Score: Wave {0}";
    public string m_tooltipNewEndlessHighScore = "NEW BEST Highest Wave: {0}";
    public string m_tooltipCurrentEndlessHighScore = "Highest Wave: {0}";
    public string m_tooltipCurrentEndlessScore = "Current Wave: {0}";
    public string m_tooltipNewPerfectHighScore = "NEW BEST Perfect Waves: {0}";
    public string m_tooltipCurrentPerfectHighScore = "Most Perfect Waves: {0}";
    public string m_tooltipCurrentPerfectScore = "Perfect Waves: {0}";
    public string m_waveCompleted = "Cores Claimed";
    public string m_waveCompletedEndless = "Grainwraiths Slain";
    public string m_waveCompletedPerfect = "Perfect Wave!";
    public string m_waveCompletedBossWave = "Cores Claimed";
    public string m_waveCompletedBossDamage = "Damage Prevented";

    [Header("Sound Strings")]
    public string m_volumeMasterText;
    public string m_volumeMusicText;
    public string m_volumeSFXText;
    
    [Header("Options Popup Strings")]
    public string m_surrender = "Surrender";
    public string m_completeMission = "Complete Mission";
    
}
