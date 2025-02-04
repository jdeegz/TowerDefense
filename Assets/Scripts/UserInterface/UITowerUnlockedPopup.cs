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
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetData(object data)
    {
        Debug.Log($"no data setup needed.");

        if (data is TowerData towerData)
        {
            m_towerData = towerData;

            m_titleLabel.SetText(m_towerData.m_towerName);
            m_towerDescriptionLabel.SetText(m_towerData.m_towerDescription);
            m_towerUnlockImage.sprite = m_towerData.m_uiIcon;
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_popupGroupRoot);
        }
        else
        {
            Debug.Log($"Data Type incompatible. Expecting TowerData");
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
