using System;
using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class UpgradeTowerButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button m_upgradeButton;
    [SerializeField] private TextMeshProUGUI m_upgradeCostLabel;
    [SerializeField] private Image m_upgradeImage;
    [SerializeField] private UITowerSelectHUD m_parentHUD;
    [SerializeField] private UIEffect m_buttonUIEffect;
    private ButtonState m_buttonState;
    public enum ButtonState
    {
        Normal,
        Selected,
        Hovered,
    }
    
    private int m_upgradeStoneValue = 999;
    private int m_upgradeWoodValue = 999;
    private bool m_canAffordWood;
    private bool m_canAffordStone;
    private TowerData m_towerData;
    private Tower m_tower;
    private UIEffect m_upgradeCostLabelUIEffect;

    void Start()
    {
        ResourceManager.UpdateStoneBank += CheckStoneCost;
        ResourceManager.UpdateWoodBank += CheckWoodCost;
        m_upgradeCostLabelUIEffect = m_upgradeCostLabel.GetComponent<UIEffect>();
    }

    void OnEnable()
    {
        SetButtonOutline(ButtonState.Normal);
    }

    private void CheckStoneCost(int total, int delta)
    {
        m_canAffordStone = total >= m_upgradeStoneValue;
        //Debug.Log($"Upgrade Stone Cost: {m_upgradeStoneValue}, Current total: {total}.");
        CanAffordToBuildTower();
    }

    private void CheckWoodCost(int total, int delta)
    {
        m_canAffordWood = total >= m_upgradeWoodValue;
        //Debug.Log($"Upgrade Wood Cost: {m_upgradeWoodValue}, Current total: {total}.");
        CanAffordToBuildTower();
    }

    private void CanAffordToBuildTower()
    {
        if (m_canAffordWood && m_canAffordStone)
        {
            if (m_buttonUIEffect.toneIntensity > 0)
            {
                DOTween.To(() => m_buttonUIEffect.toneIntensity, x => m_buttonUIEffect.toneIntensity = x, 0, .1f)
                    .SetEase(Ease.Linear).SetUpdate(true);

                DOTween.To(() => m_upgradeCostLabelUIEffect.toneIntensity, x => m_upgradeCostLabelUIEffect.toneIntensity = x, 0, .1f)
                    .SetEase(Ease.Linear).SetUpdate(true);
            }
        }
        else
        {
            if (m_buttonUIEffect.toneIntensity < 1)
            {
                DOTween.To(() => m_buttonUIEffect.toneIntensity, x => m_buttonUIEffect.toneIntensity = x, 1, .1f)
                    .SetEase(Ease.Linear).SetUpdate(true);

                DOTween.To(() => m_upgradeCostLabelUIEffect.toneIntensity, x => m_upgradeCostLabelUIEffect.toneIntensity = x, 1, .1f)
                    .SetEase(Ease.Linear).SetUpdate(true);
            }
        }
    }
    
    public void SetUpData(Tower curTower, TowerData upgradeData)
    {
        m_tower = curTower;
        m_towerData = upgradeData;
        
        //Icon
        m_upgradeImage.sprite = upgradeData.m_uiIcon;
        
        //Cost Values
        m_upgradeStoneValue = upgradeData.m_stoneCost;
        m_upgradeWoodValue = upgradeData.m_woodCost;
        CheckStoneCost(ResourceManager.Instance.GetStoneAmount(), 0);
        CheckWoodCost(ResourceManager.Instance.GetWoodAmount(), 0);
        
        //Cost Label
        string sellText;
        if (m_upgradeStoneValue > 0)
        {
            sellText = $"{m_upgradeStoneValue}<sprite name=\"ResourceStone\"><br>{m_upgradeWoodValue}<sprite name=\"ResourceWood\">";
        }
        else
        {
            sellText = $"{m_upgradeWoodValue}<sprite name=\"ResourceWood\">";
        }
        m_upgradeCostLabel.SetText(sellText);
        
        //Sell Button Action
        m_upgradeButton.onClick.RemoveAllListeners();
        m_upgradeButton.onClick.AddListener(RequestUpgradeTower);
    }

    private void RequestUpgradeTower()
    {
        if (m_canAffordWood && m_canAffordStone)
        {
            GameplayManager.Instance.UpgradeTower(m_tower, m_towerData, m_upgradeStoneValue, m_upgradeWoodValue);
            UITooltipController.Instance.SetUISelectable(null);
            m_parentHUD.DeselectTower();
        }
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
        if(m_buttonState == ButtonState.Normal) SetButtonOutline(ButtonState.Hovered);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITooltipController.Instance.SetUISelectable(null);
        if(m_buttonState == ButtonState.Hovered) SetButtonOutline(ButtonState.Normal);
    }

    public void SetButtonOutline(ButtonState state)
    {
        if (state != m_buttonState)
        {
            ToneFilter savedToneFilter = m_buttonUIEffect.toneFilter;
            float savedToneIntensity = m_buttonUIEffect.toneIntensity;

            switch (state)
            {
                case ButtonState.Normal:
                    m_buttonState = state;
                    m_buttonUIEffect.LoadPreset("UIEffect_Normal");
                    break;
                case ButtonState.Selected:
                    m_buttonState = state;
                    m_buttonUIEffect.LoadPreset("UIEffect_Selected");
                    break;
                case ButtonState.Hovered:
                    m_buttonState = state;
                    m_buttonUIEffect.LoadPreset("UIEffect_Hovered");
                    break;
                default:
                    Debug.Log($"Not state.");
                    break;
            }

            m_buttonUIEffect.toneFilter = savedToneFilter;
            m_buttonUIEffect.toneIntensity = savedToneIntensity;
        }
    }
}
