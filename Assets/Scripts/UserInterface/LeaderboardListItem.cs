using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class LeaderboardListItem : MonoBehaviour
{
    public TextMeshProUGUI m_listItemTitleLabel;
    public TextMeshProUGUI m_listItemValueLabel;
    public Image m_listItemBG;

    public Color m_isMeColor;
    public Color m_evenColor;
    public Color m_oddColor;
    
    public void SetTitleData(string leaderboardName)
    {
        m_listItemTitleLabel.SetText(leaderboardName);
    }

    public void SetPlayerData(string playerName, int position, int value, bool isPlayer)
    {
        m_listItemTitleLabel.SetText($"{position+1}. {playerName}");
        m_listItemValueLabel.SetText(value.ToString());
        m_listItemBG.color = position % 2 == 0 ? m_evenColor : m_oddColor;
        if (isPlayer) m_listItemBG.color = m_isMeColor;
    }
}

