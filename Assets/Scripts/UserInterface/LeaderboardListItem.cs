using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class LeaderboardListItem : MonoBehaviour
{
    public TextMeshProUGUI m_listItemTitleLabel;
    public TextMeshProUGUI m_listItemValueLabel;
    
    public void SetTitleData(string leaderboardName)
    {
        m_listItemTitleLabel.SetText(leaderboardName);
    }

    public void SetPlayerData(string playerName, int position, int value)
    {
        m_listItemTitleLabel.SetText($"{position}. {playerName}");
        m_listItemValueLabel.SetText(value.ToString());
    }
}

