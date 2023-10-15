using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UITowerSelectHUD : MonoBehaviour
{
    [SerializeField] private Track3dObject m_track3dObject;
    [SerializeField] private Button m_sellButton;
    [SerializeField] private TextMeshProUGUI m_sellButtonlabel;
    [SerializeField] private int m_sellStoneValue;
    [SerializeField] private int m_sellWoodValue;
    [SerializeField] private List<UpgradeTowerButton> m_upgradeButtons;
    
    private Tower m_curTower;
    private TowerData m_curTowerData;

    void Start()
    {
        ToggleTowerSelectHUD(false);
    }

    public void ToggleTowerSelectHUD(bool b)
    {
        gameObject.SetActive(b);
    }

    public void SelectTower(GameObject obj)
    {
        //References
        m_curTower = obj.GetComponent<Tower>();
        m_curTowerData = m_curTower.GetTowerData();
        m_track3dObject.SetupTracking(obj, GetComponent<RectTransform>(), 0);
        
        //Sell values
        m_sellStoneValue = m_curTowerData.m_stoneCost;
        m_sellWoodValue = m_curTowerData.m_woodCost;
        
        //Sell Button Action
        m_sellButton.onClick.RemoveAllListeners();
        m_sellButton.onClick.AddListener(RequestSellTower);
        
        //Sell Button Text
        string sellText;
        if (m_sellStoneValue > 0)
        {
            sellText = $"{m_sellStoneValue}<sprite name=\"ResourceStone\"><br>{m_sellWoodValue}<sprite name=\"ResourceWood\">";
        }
        else
        {
            sellText = $"{m_sellWoodValue}<sprite name=\"ResourceWood\">";
        }
        m_sellButtonlabel.SetText(sellText);
        
        //Upgrade Buttons
        //Disable All Upgrade Button Objects
        foreach (UpgradeTowerButton button in m_upgradeButtons)
        {
            button.gameObject.SetActive(false);
        }
        
        //Upgrade Button Setup & Activity
        for (int i = 0; i < m_curTowerData.m_upgradeOptions.Count; ++i)
        {
            m_upgradeButtons[i].gameObject.SetActive(true);
            m_upgradeButtons[i].SetUpData(m_curTower, m_curTowerData.m_upgradeOptions[i]);
        }
        
        ToggleTowerSelectHUD(true);
    }

    private void RequestSellTower()
    {
        GameplayManager.Instance.SellTower(m_curTower, m_sellStoneValue, m_sellWoodValue);
        DeselectTower();
    }

    public void DeselectTower()
    {
        m_track3dObject.StopTracking();
        m_curTower = null;
        ToggleTowerSelectHUD(false);
    }
}