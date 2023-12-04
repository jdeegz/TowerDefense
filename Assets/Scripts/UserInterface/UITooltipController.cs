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
        m_defaultPivot = m_tooltipRect.pivot;

        GameplayManager.OnGameObjectSelected += GameObjectSelected;
        GameplayManager.OnGameObjectDeselected += GameObjectDeselected;
    }


    void OnDestroy()
    {
        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
        GameplayManager.OnGameObjectDeselected -= GameObjectDeselected;
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
        HandleTooltipPlacement();
        
        //Dont do any Tooltip display while in preconstruction.
        if (GameplayManager.Instance.m_interactionState == GameplayManager.InteractionState.PreconstructionTower)
        {
            if(m_canvasGroup.alpha > 0) RequestShowTooltip(false);
            return;
        }
        
        //Copy the selectable from gameplay manager.
        SetWorldSelectable(GameplayManager.Instance.m_hoveredSelectable);
        
        //Dont show a tooltip for a currently selected tower that is being hovered.
        if (GameplayManager.Instance.m_interactionState == GameplayManager.InteractionState.SelectedTower && m_curSelected == m_curWorldSelectable)
        {
            if(m_canvasGroup.alpha > 0) RequestShowTooltip(false);
            return;
        }
        
        //I want to show a tooltip if I am hovering over an object, and it's not the same object as last frame.
        if (m_curUISelectable != m_lastUISelectable)
        {
            //Show a tooltip
            if(m_curUISelectable) SetTooltipData(m_curUISelectable.m_selectedObjectType, m_curUISelectable.gameObject);
            m_lastUISelectable = m_curUISelectable;
        }

        if (m_curWorldSelectable != m_lastWorldSelectable)
        {
            //Show a tooltip
            if(m_curWorldSelectable) SetTooltipData(m_curWorldSelectable.m_selectedObjectType, m_curWorldSelectable.gameObject);
            m_lastWorldSelectable = m_curWorldSelectable;
        }

        if (!m_curUISelectable && !m_curWorldSelectable)
        {
            //Dont show a tooltip.
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
        Debug.Log($"Showing Tooltip.");
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

                m_objectNameString = $"{gathererIconString} {gathererData.m_gathererName}";
                m_objectDescriptionString = gathererData.m_gathererDescription;
                m_objectDetailsString = $"Carry Capacity: {gathererData.m_carryCapacity}{resourceIconString}<br>" +
                                        $"Harvest Speed: {gathererData.m_harvestDuration}{m_timeIconString}<br>" +
                                        $"Storage Speed: {gathererData.m_storingDuration}{m_timeIconString}";
                break;
            case Selectable.SelectedObjectType.Castle:
                CastleTooltipData castleNodeData = hoveredObj.GetComponent<CastleController>().GetTooltipData();
                m_objectNameString = castleNodeData.m_castleName;
                m_objectDescriptionString = castleNodeData.m_castleDescription;
                m_objectDetailsString = $"{castleNodeData.m_curHealth} / {castleNodeData.m_maxHealth}{m_healthIconString}<br>" +
                                        $"Repair Amount: {castleNodeData.m_repairHealthAmount}{m_healthIconString}<br>" +
                                        $"Repair Speed: {castleNodeData.m_repairHealthInterval}{m_timeIconString}";
                break;
            case Selectable.SelectedObjectType.Obelisk:
                ObeliskTooltipData obeliskNodeData = hoveredObj.GetComponent<Obelisk>().GetTooltipData();
                m_objectNameString = obeliskNodeData.m_obeliskName;
                m_objectDescriptionString = obeliskNodeData.m_obeliskDescription;
                m_objectDetailsString = $"Progress: {obeliskNodeData.m_obeliskCurCharge} / {obeliskNodeData.m_obeliskMaxCharge}";
                if (obeliskNodeData.m_obeliskCurCharge == obeliskNodeData.m_obeliskMaxCharge)
                {
                    m_objectDetailsString = "Progress: Complete";
                }

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

        LayoutRebuilder.ForceRebuildLayoutImmediate(m_layoutGroup.GetComponent<RectTransform>());

        RequestShowTooltip(true);
    }

    void RequestShowTooltip(bool show)
    {
        if(m_curTween != null) m_curTween.Kill();
        m_canvasGroup.alpha = 0;
        if (show)
        {
            m_curTween = m_canvasGroup.DOFade(1, 0.1f);
            m_curTween.Play();
        }
    }

    void HandleTooltipPlacement()
    {
        //Get the mouse position and convert it to screen coordinates.
        m_mousePos = Input.mousePosition;
        m_rectWidth = m_tooltipRect.rect.width;
        m_rectHeight = m_tooltipRect.rect.height;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_canvas.transform as RectTransform, m_mousePos, m_canvas.worldCamera, out Vector2 localPoint);

        Vector2 newTooltipPivot = m_defaultPivot;
        Vector2 newOffset = new Vector2(m_offsetXPositive, m_offsetYPositive);
        if (localPoint.x < 0 - Screen.width / 2 + m_rectWidth + m_offsetXPositive)
        {
            newOffset.x = -m_offsetXNegative;
            newTooltipPivot.x = 0;
        }

        if (localPoint.y > Screen.height / 2 - m_rectHeight - m_offsetYPositive)
        {
            newOffset.y = -m_offsetYNegative;
            newTooltipPivot.y = 1;
        }

        m_tooltipRect.pivot = newTooltipPivot;
        m_tooltipRect.anchoredPosition = localPoint - newOffset;
    }
}