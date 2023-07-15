using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TowerTrayButton : MonoBehaviour
{
    [SerializeField] private ScriptableTowerDataObject m_towerData;
    [SerializeField] private TextMeshProUGUI m_towerCost;
    [SerializeField] private Image m_towerImage;
    [SerializeField] private Image m_backgroundImage;
    [SerializeField] private Color m_backgroundBaseColor;
    [SerializeField] private Color m_backgroundCannotAffordColor;

    private bool m_canAffordWood;
    private bool m_canAffordStone;
    private Button m_button;
    private int m_equippedTowerIndex;
    
    // Start is called before the first frame update
    void Start()
    {
        m_button = GetComponent<Button>();
        ResourceManager.UpdateStoneBank += CheckStoneCost;
        ResourceManager.UpdateWoodBank += CheckWoodCost;
        CheckStoneCost(ResourceManager.Instance.GetStoneAmount());
        CheckWoodCost(ResourceManager.Instance.GetWoodAmount());
        
       m_button.onClick.AddListener(SelectTowerButton);
    }

    private void CheckStoneCost(int i)
    {
        m_canAffordStone = i >= m_towerData.m_stoneCost;
        //Debug.Log("Check stone cost: " + i);
        CanAffordToBuildTower();
    }

    private void CheckWoodCost(int i)
    {
        m_canAffordWood = i >= m_towerData.m_woodCost;
        //Debug.Log("Check wood cost: " + i);
        CanAffordToBuildTower();
    }

    private void CanAffordToBuildTower()
    {
        if (m_canAffordWood && m_canAffordStone)
        {
            m_backgroundImage.color = m_backgroundBaseColor;
            m_towerCost.color = Color.white;
        }
        else
        {
            m_backgroundImage.color = m_backgroundCannotAffordColor;
            m_towerCost.color = Color.red;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetupData(ScriptableTowerDataObject towerData, int i)
    {
        m_towerData = towerData;
        m_equippedTowerIndex = i;

        //Tower Cost
        string woodSprite = "<sprite name=ResourceWood>";
        m_towerCost.SetText(towerData.m_woodCost + woodSprite);

        //Tower Image
        m_towerImage.sprite = towerData.m_uiIcon;
    }

    void OnMouseDown()
    {
        
    }
    
    public void SelectTowerButton()
    {
        GameplayManager.Instance.PreconstructTower(m_equippedTowerIndex);
    }

    public void DeselectTowerButton()
    {
        
    }
    
}
