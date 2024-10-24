using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG;

public class TowerTrayButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler 
{
    [SerializeField] private TowerData m_towerData;
    [SerializeField] private TextMeshProUGUI m_towerCost;
    [SerializeField] private TextMeshProUGUI m_towerHotkey;
    [SerializeField] private Image m_towerImage;
    [SerializeField] private Image m_backgroundImage;
    [SerializeField] private Color m_backgroundBaseColor;
    [SerializeField] private Color m_backgroundCannotAffordColor;

    private bool m_canAffordWood;
    private bool m_canAffordStone;
    private Button m_button;
    private int m_equippedTowerIndex;
    private int m_blueprintTowerIndex;
    
    // Start is called before the first frame update
    void Start()
    {
        m_button = GetComponent<Button>();
        ResourceManager.UpdateStoneBank += CheckStoneCost;
        ResourceManager.UpdateWoodBank += CheckWoodCost;
        CheckStoneCost(ResourceManager.Instance.GetStoneAmount(), 0);
        CheckWoodCost(ResourceManager.Instance.GetWoodAmount(), 0);
        
       m_button.onClick.AddListener(SelectTowerButton);
    }

    private void CheckStoneCost(int total, int delta)
    {
        m_canAffordStone = total >= m_towerData.m_stoneCost;
        //Debug.Log("Check stone cost: " + i);
        CanAffordToBuildTower();
    }

    private void CheckWoodCost(int total, int delta)
    {
        m_canAffordWood = total >= m_towerData.m_woodCost;
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

    public void SetupTowerData(TowerData towerData, int i)
    {
        m_towerData = towerData;

        //Tower Cost
        string woodSprite = "<sprite name=ResourceWood>";
        m_towerCost.SetText(towerData.m_woodCost + woodSprite);

        //Tower Image
        m_towerImage.sprite = towerData.m_uiIcon;
        
        //Tower Hotkey
        m_towerHotkey.SetText((i + 1).ToString());
    }
    
    public void SelectTowerButton()
    {
        GameplayManager.Instance.PreconstructTower(m_towerData);
    }

    void OnDestroy()
    {
        ResourceManager.UpdateStoneBank -= CheckStoneCost;
        ResourceManager.UpdateWoodBank -= CheckWoodCost;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Get the selectable component related to this button.
        Selectable selectable = m_towerData.m_prefab.GetComponent<Selectable>();
        UITooltipController.Instance.SetUISelectable(selectable);
    }

    public void OnPointerExit(PointerEventData evenData)
    {
        UITooltipController.Instance.SetUISelectable(null);
    }
}
