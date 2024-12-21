using System;
using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
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
    [SerializeField] private UIEffect m_buttonUIEffect;

    private bool m_canAffordWood;
    private bool m_canAffordStone;
    private Button m_button;
    private int m_equippedTowerIndex;
    private int m_blueprintTowerIndex;

    private ButtonState m_buttonState;

    public enum ButtonState
    {
        Normal,
        Selected,
        Hovered,
    }


    // Start is called before the first frame update
    void Start()
    {
        m_button = GetComponent<Button>();
        ResourceManager.UpdateStoneBank += CheckStoneCost;
        ResourceManager.UpdateWoodBank += CheckWoodCost;
        GameplayManager.OnGameObjectSelected += GameObjectSelected;
        GameplayManager.OnGameObjectDeselected += GameObjectDeselected;
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
            m_buttonUIEffect.toneFilter = ToneFilter.None;
        }
        else
        {
            m_buttonUIEffect.toneFilter = ToneFilter.Grayscale;
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
        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
        GameplayManager.OnGameObjectDeselected -= GameObjectDeselected;
    }

    private bool m_isHovered;

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Get the selectable component related to this button.
        Selectable selectable = m_towerData.m_prefab.GetComponent<Selectable>();
        UITooltipController.Instance.SetUISelectable(selectable);
        if (m_buttonState == ButtonState.Normal) SetButtonOutline(ButtonState.Hovered);
        m_isHovered = true;
    }

    public void OnPointerExit(PointerEventData evenData)
    {
        UITooltipController.Instance.SetUISelectable(null);
        if (m_buttonState == ButtonState.Hovered) SetButtonOutline(ButtonState.Normal);
        m_isHovered = false;
    }

    private void GameObjectDeselected(GameObject obj)
    {
        if (m_isHovered)
        {
            SetButtonOutline(ButtonState.Hovered);
        }
        else
        {
            SetButtonOutline(ButtonState.Normal);
        }
    }

    private void GameObjectSelected(GameObject obj)
    {
        TowerData data = GameplayManager.Instance.GetPreconTowerData();
        if (data != null && data == m_towerData)
        {
            SetButtonOutline(ButtonState.Selected);
        }
    }

    public void SetButtonOutline(ButtonState state)
    {
        if (state != m_buttonState)
        {
            ToneFilter savedToneFilter = m_buttonUIEffect.toneFilter;

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
        }
    }
}