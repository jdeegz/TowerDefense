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
    [SerializeField] private TextMeshProUGUI m_towerQTY;
    [SerializeField] private GameObject m_towerQTYobj;
    [SerializeField] private Image m_towerImage;
    [SerializeField] private UIEffect m_buttonUIEffect;

    private bool m_canAffordWood = true;
    private bool m_canAffordStone = true;
    private bool m_canAffordQty;
    private Button m_button;
    private int m_equippedTowerIndex;
    private int m_blueprintTowerIndex;
    private int m_qty;
    private UIEffect m_costLabelUIEffect;
    private UIEffect m_qtyLabelUIEffect;

    private int Quantity
    {
        get { return m_qty; }
        set
        {
            if (value != m_qty)
            {
                //Debug.Log($"Tray Button Quantity Updated.");
                m_qty = value;
                bool canAffordQty = m_qty is > 0 or -1;
                m_towerQTY.SetText("x{0}", m_qty);
                if (canAffordQty != m_canAffordQty)
                {
                    if (canAffordQty)
                    {
                        DOTween.To(() => m_qtyLabelUIEffect.toneIntensity, x => m_qtyLabelUIEffect.toneIntensity = x, 0, .1f)
                            .SetEase(Ease.Linear).SetUpdate(true);
                    }
                    else
                    {
                        DOTween.To(() => m_qtyLabelUIEffect.toneIntensity, x => m_qtyLabelUIEffect.toneIntensity = x, 1, .1f)
                            .SetEase(Ease.Linear).SetUpdate(true);
                    }
                }
                m_canAffordQty = canAffordQty;
                m_towerQTYobj.SetActive(m_qty != -1);
                CanAffordToBuildTower();
            }
        }
    }

    private ButtonState m_buttonState;

    public enum ButtonState
    {
        Normal,
        Selected,
        Hovered,
    }

    public TowerData GetTowerData()
    {
        return m_towerData;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_button = GetComponent<Button>();
        ResourceManager.UpdateStoneBank += CheckStoneCost;
        ResourceManager.UpdateWoodBank += CheckWoodCost;
        GameplayManager.OnGameObjectSelected += GameObjectSelected;
        GameplayManager.OnGameObjectDeselected += GameObjectDeselected;
        GameplayManager.OnTowerBuild += TowerBuilt;
        CheckStoneCost(ResourceManager.Instance.GetStoneAmount(), 0);
        CheckWoodCost(ResourceManager.Instance.GetWoodAmount(), 0);

        m_button.onClick.AddListener(SelectTowerButton);
        m_costLabelUIEffect = m_towerCost.GetComponent<UIEffect>();
        m_qtyLabelUIEffect = m_towerQTY.GetComponent<UIEffect>();
    }

    private void TowerBuilt(TowerData towerData, GameObject newTowerObj)
    {
        if (towerData != m_towerData) return;

        if (GameplayManager.Instance.m_unlockedStructures.TryGetValue(towerData, out int currentValue))
        {
            Quantity = currentValue;
        }
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

    public void UpdateHotkeyDisplay(int i)
    {
        m_towerHotkey.SetText(i.ToString());
    }

    private void CanAffordToBuildTower()
    {
        //Debug.Log($"CanAffordToBuildTower: Wood - {m_canAffordWood}, Stone - {m_canAffordStone}, Quantity - {m_canAffordQty}.");
        if (m_canAffordWood && m_canAffordStone && m_canAffordQty)
        {
            if (m_buttonUIEffect.toneIntensity > 0)
            {
                DOTween.To(() => m_buttonUIEffect.toneIntensity, x => m_buttonUIEffect.toneIntensity = x, 0, .1f)
                    .SetEase(Ease.Linear).SetUpdate(true);

                DOTween.To(() => m_costLabelUIEffect.toneIntensity, x => m_costLabelUIEffect.toneIntensity = x, 0, .1f)
                    .SetEase(Ease.Linear).SetUpdate(true);
            }
        }
        else
        {
            if (m_buttonUIEffect.toneIntensity < 1)
            {
                DOTween.To(() => m_buttonUIEffect.toneIntensity, x => m_buttonUIEffect.toneIntensity = x, 1, .1f)
                    .SetEase(Ease.Linear).SetUpdate(true);

                DOTween.To(() => m_costLabelUIEffect.toneIntensity, x => m_costLabelUIEffect.toneIntensity = x, 1, .1f)
                    .SetEase(Ease.Linear).SetUpdate(true);
            }
        }
    }

    public void SetupData(TowerData towerData, int i, int qty = -1)
    {
        m_towerData = towerData;

        //Tower Cost
        string woodSprite = "<sprite name=ResourceWood>";
        m_towerCost.SetText(towerData.m_woodCost + woodSprite);
        m_canAffordStone = ResourceManager.Instance.GetStoneAmount() >= m_towerData.m_stoneCost;
        m_canAffordWood = ResourceManager.Instance.GetWoodAmount() >= m_towerData.m_woodCost;

        //Tower Image
        m_towerImage.sprite = towerData.m_uiIcon;

        //Tower Hotkey
        string hotkeyText = i == -1 ? "B" : (i + 1).ToString();
        m_towerHotkey.SetText(hotkeyText);

        Quantity = qty;
    }

    public void SelectTowerButton()
    {
        GameplayManager.Instance.CreatePreconBuilding(m_towerData);
    }

    void OnDestroy()
    {
        ResourceManager.UpdateStoneBank -= CheckStoneCost;
        ResourceManager.UpdateWoodBank -= CheckWoodCost;
        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
        GameplayManager.OnGameObjectDeselected -= GameObjectDeselected;
        GameplayManager.OnTowerBuild -= TowerBuilt;
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

    public void UpdateQuantity(int i)
    {
        Quantity += i;
        //Debug.Log($"{m_towerData.m_towerName}'s button now has {Quantity}.");
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