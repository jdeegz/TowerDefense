using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITowerUnlockedPopup : UIPopup, IDataPopup
{
    [SerializeField] private TextMeshProUGUI m_titleLabel;
    [SerializeField] private TextMeshProUGUI m_towerDescriptionLabel;
    [SerializeField] private Image m_towerUnlockImage;
    [SerializeField] private RectTransform m_popupGroupRoot;
    
    private TowerData m_towerData;
    

    public void SetData(object data)
    {
        if (data is TowerData towerData)
        {
            m_towerData = towerData;

            m_titleLabel.SetText(m_towerData.m_towerName);
            m_towerDescriptionLabel.SetText(m_towerData.m_towerDescription);
            m_towerUnlockImage.sprite = m_towerData.m_uiIcon;
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_popupGroupRoot);
        }
    }

    public override void CompleteClose()
    {
        base.CompleteClose();
        
        ResetData();
    }

    public void ResetData()
    {
        m_towerData = null;
        m_titleLabel.SetText("");
        m_towerDescriptionLabel.SetText("");
        m_towerUnlockImage.sprite = null;
    }
}
