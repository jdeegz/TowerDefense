using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UITooltipController : MonoBehaviour
{
    public static UITooltipController Instance;
    public GameObject m_tooltipDisplayGroup;
    public TextMeshProUGUI m_objectNameLabel;
    public TextMeshProUGUI m_objectDescriptionLabel;
    public TextMeshProUGUI m_objectDetailsLabel;

    public float m_offsetXPositive;
    public float m_offsetYPositive;
    public float m_offsetXNegative;
    public float m_offsetYNegative;

    private float m_rectWidth;
    private float m_rectHeight;
    private RectTransform m_tooltipRect;
    private Vector2 m_mousePos;
    private Canvas m_canvas;
    private Vector2 m_defaultPivot;

    public Selectable m_curSelected;
    public Selectable m_curUISelectable;
    public Selectable m_lastUISelectable;
    public Selectable m_curWorldSelectable;
    public Selectable m_lastWorldSelectable;
    private string m_objectNameString;
    private string m_objectDescriptionString;
    private string m_objectDetailsString;
    private CanvasGroup m_canvasGroup;
    private VerticalLayoutGroup m_layoutGroup;
    private Tween m_curTween;
    private Vector2 m_lastScreenSize;
    private CanvasScaler m_canvasScaler;
    private bool m_supressToolTips;

    private string m_timeIconString = "<sprite name=\"Time\">";
    private string m_healthIconString = "<sprite name=\"ResourceHealth\">";
    private string m_resourceWoodIconString = "<sprite name=\"ResourceWood\">";
    private string m_gathererWoodIconString = "<sprite name=\"GathererWood\">";
    private string m_resourceStoneIconString = "<sprite name=\"ResourceStone\">";
    private string m_gathererStoneIconString = "<sprite name=\"GathererStone\">";

    void Awake()
    {
        Instance = this;
        m_canvasGroup = m_tooltipDisplayGroup.GetComponent<CanvasGroup>();
        m_layoutGroup = m_tooltipDisplayGroup.GetComponent<VerticalLayoutGroup>();
        m_canvasGroup.alpha = 0;
        m_tooltipRect = m_tooltipDisplayGroup.GetComponent<RectTransform>();
        m_canvas = GetComponentInParent<Canvas>();
        m_canvasScaler = m_canvas.GetComponent<CanvasScaler>();
        m_defaultPivot = m_tooltipRect.pivot;
        m_lastScreenSize = new Vector2(Screen.width, Screen.height);

        GameplayManager.OnGameObjectSelected += GameObjectSelected;
        GameplayManager.OnGameObjectDeselected += GameObjectDeselected;
    }


    void OnDestroy()
    {
        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
        GameplayManager.OnGameObjectDeselected -= GameObjectDeselected;
    }

    public void HideAndSuppressToolTips()
    {
        RequestShowTooltip(false);
        m_supressToolTips = true;
    }

    public void UnsuppressToolTips()
    {
        m_supressToolTips = false;
    }
    
    private void GameObjectDeselected(GameObject obj)
    {
        Selectable deselected = obj.GetComponent<Selectable>();
        if (deselected == m_curSelected)
        {
            m_curSelected = null;
        }
    }

    private void GameObjectSelected(GameObject obj)
    {
        m_curSelected = obj.GetComponent<Selectable>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Screen.width != m_lastScreenSize.x || Screen.height != m_lastScreenSize.y)
        {
            // Update the last known screen size
            m_lastScreenSize = new Vector2(Screen.width, Screen.height);
        }

        // If I am holding alt, do not display tooltips. If a tooltip is active, hide it.
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            m_lastUISelectable = null; // Set these null so that when we release alt the tooltip reappears if there is one to show.
            m_lastWorldSelectable = null;
            if (m_canvasGroup.alpha > 0) RequestShowTooltip(false);
            return;
        }
        
        // If I am holding alt, do not display tooltips. If a tooltip is active, hide it.
        if (GameplayManager.Instance.m_interactionState == GameplayManager.InteractionState.Disabled && GameplayManager.Instance.m_gameplayState != GameplayManager.GameplayState.Setup)
        {
            m_lastUISelectable = null; // Set these null so that when we release alt the tooltip reappears if there is one to show.
            m_lastWorldSelectable = null;
            if (m_canvasGroup.alpha > 0) RequestShowTooltip(false);
            return;
        }
        
        HandleTooltipPlacement();

        // Dont do any Tooltip display while in preconstruction.
        if (GameplayManager.Instance.m_interactionState == GameplayManager.InteractionState.PreconstructionTower)
        {
            if (m_canvasGroup.alpha > 0) RequestShowTooltip(false);
            return;
        }

        // Copy the selectable from gameplay manager.
        SetWorldSelectable(GameplayManager.Instance.m_hoveredSelectable);

        // Dont show a tooltip for a currently selected tower that is being hovered.
        if (GameplayManager.Instance.m_interactionState == GameplayManager.InteractionState.SelectedTower && m_curSelected == m_curWorldSelectable)
        {
            if (m_canvasGroup.alpha > 0) RequestShowTooltip(false);
            return;
        }

        // I want to show a tooltip if I am hovering over an object, and it's not the same object as last frame.
        if (m_curUISelectable != m_lastUISelectable)
        {
            // HandleShow a tooltip
            if (m_curUISelectable) SetTooltipData(m_curUISelectable.m_selectedObjectType, m_curUISelectable.gameObject);
            m_lastUISelectable = m_curUISelectable;
        }

        if (m_curWorldSelectable != m_lastWorldSelectable)
        {
            // HandleShow a tooltip
            if (m_curWorldSelectable) SetTooltipData(m_curWorldSelectable.m_selectedObjectType, m_curWorldSelectable.gameObject);
            m_lastWorldSelectable = m_curWorldSelectable;
        }

        if (!m_curUISelectable && !m_curWorldSelectable)
        {
            // Dont show a tooltip.
            m_lastUISelectable = null;
            m_lastWorldSelectable = null;
            RequestShowTooltip(false);
        }
    }

    public void SetUISelectable(Selectable selectable)
    {
        m_curUISelectable = selectable;
    }

    public void SetWorldSelectable(Selectable selectable)
    {
        m_curWorldSelectable = selectable;
    }
    
    public void SetTooltipData(Selectable.SelectedObjectType type, GameObject hoveredObj)
    {
        switch (type)
        {
            case Selectable.SelectedObjectType.ResourceWood:
                ResourceNodeTooltipData woodNodeData = hoveredObj.GetComponent<ResourceNode>().GetTooltipData();
                m_objectNameString = woodNodeData.m_resourceNodeName;
                m_objectDescriptionString = woodNodeData.m_resourceNodeDescription;
                m_objectDetailsString = $"Resources Remaining: {woodNodeData.m_curResources} / {woodNodeData.m_maxResources}{m_resourceWoodIconString}";
                break;
            case Selectable.SelectedObjectType.ResourceStone:
                ResourceNodeTooltipData stoneNodeData = hoveredObj.GetComponent<ResourceNode>().GetTooltipData();
                m_objectNameString = stoneNodeData.m_resourceNodeName;
                m_objectDescriptionString = stoneNodeData.m_resourceNodeDescription;
                m_objectDetailsString = $"Resources Remaining: {stoneNodeData.m_curResources} / {stoneNodeData.m_maxResources}{m_resourceStoneIconString}";
                break;
            case Selectable.SelectedObjectType.Tower:
                TowerTooltipData towerTooltipData = hoveredObj.GetComponent<Tower>().GetTooltipData();
                m_objectNameString = towerTooltipData.m_towerName;
                m_objectDescriptionString = towerTooltipData.m_towerDescription;
                m_objectDetailsString = towerTooltipData.m_towerDetails;
                break;
            case Selectable.SelectedObjectType.Building:
                TowerTooltipData buildingTooltipData = hoveredObj.GetComponent<Tower>().GetTooltipData();
                m_objectNameString = buildingTooltipData.m_towerName;
                m_objectDescriptionString = buildingTooltipData.m_towerDescription;
                m_objectDetailsString = buildingTooltipData.m_towerDetails;
                break;
            case Selectable.SelectedObjectType.Gatherer:
                GathererTooltipData gathererData = hoveredObj.GetComponent<GathererController>().GetTooltipData();

                //Define needed icons based on gatherer type.
                string resourceIconString;
                string gathererIconString;
                switch (gathererData.m_gathererType)
                {
                    case ResourceManager.ResourceType.Wood:
                        resourceIconString = m_resourceWoodIconString;
                        gathererIconString = m_gathererWoodIconString;
                        break;
                    case ResourceManager.ResourceType.Stone:
                        resourceIconString = m_resourceStoneIconString;
                        gathererIconString = m_gathererStoneIconString;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                m_objectNameString = $"{gathererIconString} {gathererData.m_gathererName} Level: {gathererData.m_gathererLevel}";
                m_objectDescriptionString = gathererData.m_gathererDescription;
                string efficiency = gathererData.m_gathererLevel == 1
                    ? $"Harvest Efficiency: {gathererData.m_carryCapacity}{resourceIconString}<br>"
                    : $"Harvest Efficiency: {gathererData.m_carryCapacity}{resourceIconString} and {(gathererData.m_gathererLevel - 1) * 25}% chance for +1{resourceIconString}<br>";
                m_objectDetailsString = efficiency +
                                        $"Harvest Speed: {gathererData.m_harvestDuration}{m_timeIconString}<br>" +
                                        $"Storage Speed: {gathererData.m_storingDuration}{m_timeIconString}";
                break;
            case Selectable.SelectedObjectType.Castle:
                CastleTooltipData castleNodeData = hoveredObj.GetComponent<CastleController>().GetTooltipData();
                m_objectNameString = castleNodeData.m_castleName;
                m_objectDescriptionString = castleNodeData.m_castleDescription;
                m_objectDetailsString = $"{castleNodeData.m_curHealth} / {castleNodeData.m_maxHealth}{m_healthIconString}<br>" +
                                        $"Repair Amount: {castleNodeData.m_repairHealthAmount}{m_healthIconString}<br>" +
                                        $"Repairs Per Wave: {castleNodeData.m_repairFrequency}";
                break;
            case Selectable.SelectedObjectType.Obelisk:
                ObeliskTooltipData obeliskNodeData = hoveredObj.GetComponent<Obelisk>().GetTooltipData();
                m_objectNameString = obeliskNodeData.m_obeliskName;
                m_objectDescriptionString = obeliskNodeData.m_obeliskDescription;
                m_objectDetailsString = $"Cores Claimed: {obeliskNodeData.m_obeliskCurCharge} / {obeliskNodeData.m_obeliskMaxCharge}";
                break;
            case Selectable.SelectedObjectType.Ruin:
                RuinTooltipData ruinData = hoveredObj.GetComponent<Ruin>().GetTooltipData();
                m_objectNameString = ruinData.m_ruinName;
                m_objectDescriptionString = ruinData.m_ruinDescription;
                m_objectDetailsString = ruinData.m_ruinDetails;
                break;
            case Selectable.SelectedObjectType.Tear: // This is gross.
                //Is it a unit spawner or a trojan spawner?
                TearTooltipData tearData;
                StandardSpawner spawner = hoveredObj.GetComponent<StandardSpawner>();
                if (spawner)
                {
                    tearData = spawner.GetTooltipData();
                }
                else
                {
                    tearData = hoveredObj.GetComponent<TrojanUnitSpawner>().GetTooltipData();
                }
                m_objectNameString = tearData.m_tearName;
                m_objectDescriptionString = tearData.m_tearDescription;
                m_objectDetailsString = tearData.m_tearDetails;
                break;
            case Selectable.SelectedObjectType.UIElement:
                UITooltip tooltip = m_curUISelectable as UITooltip;
                m_objectNameString = tooltip.m_nameString;
                m_objectDescriptionString = tooltip.m_descriptionString;
                m_objectDetailsString = tooltip.m_detailsString;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        m_objectNameLabel.gameObject.SetActive(m_objectNameString != null);
        m_objectNameLabel.SetText(m_objectNameString);

        m_objectDescriptionLabel.gameObject.SetActive(m_objectDescriptionString != null);
        m_objectDescriptionLabel.SetText(m_objectDescriptionString);

        m_objectDetailsLabel.gameObject.SetActive(m_objectDetailsString != null);
        m_objectDetailsLabel.SetText(m_objectDetailsString);

        LayoutRebuilder.ForceRebuildLayoutImmediate(m_tooltipRect);

        RequestShowTooltip(true);
    }

    void RequestShowTooltip(bool show)
    {
        if (m_supressToolTips) return;
        if (m_curTween != null) m_curTween.Kill();
        m_canvasGroup.alpha = 0;
        if (show)
        {
            m_curTween = m_canvasGroup.DOFade(1, 0.1f).SetDelay(0.25f).OnComplete(() =>
            {
                // After the first fade completes, add a 4.5-second delay
                m_curTween = m_canvasGroup.DOFade(0f, 1.5f).SetDelay(6.5f);
            });
            m_curTween.Play().SetUpdate(true);
        }
    }

    void HandleTooltipPlacement()
    {
        if (GameSettings.DynamicToolTipsEnabled) // Dynamic Placement
        {
            // Get the mouse position and convert it to screen coordinates.
            m_mousePos = Input.mousePosition;
            Vector2 rectSize = m_tooltipRect.rect.size;
            Vector2 canvasSize = m_canvasScaler.referenceResolution;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_canvas.transform as RectTransform, m_mousePos, m_canvas.worldCamera, out Vector2 localPoint);
            Vector2 newTooltipPivot = m_defaultPivot;
            Vector2 newOffset = new Vector2(m_offsetXPositive, m_offsetYPositive);
            if (localPoint.x < 0 - canvasSize.x / 2 + rectSize.x + m_offsetXPositive)
            {
                newOffset.x = -m_offsetXNegative;
                newTooltipPivot.x = 0;
            }

            if (localPoint.y > canvasSize.y / 2 - rectSize.y - m_offsetYPositive)
            {
                newOffset.y = -m_offsetYNegative;
                newTooltipPivot.y = 1;
            }

            m_tooltipRect.pivot = newTooltipPivot;
            m_tooltipRect.anchoredPosition = localPoint - newOffset;
        }
        else // Static Placement
        {
            // Set pivot to bottom-right
            m_tooltipRect.pivot = new Vector2(1, 0);

            // Get the screen's bottom-right position in screen coordinates
            Vector2 screenPosition = new Vector2(Screen.width, 0);

            // Convert screen position to the canvas position
            //RectTransform canvasRect = m_canvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_canvas.transform as RectTransform, screenPosition, m_canvas.worldCamera, out Vector2 localPoint);

            // Set the anchored position to match the bottom-right of the screen
            localPoint.x += -20;
            localPoint.y += 20;
            m_tooltipRect.anchoredPosition = localPoint;
        }
        
    }
}