using System;
using System.Collections;
using System.Collections.Generic;
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

    private Selectable m_lastHoveredSelectable;
    private string m_objectNameString;
    private string m_objectDescriptionString;
    private string m_objectDetailsString;
    private CanvasGroup m_canvasGroup;
    private VerticalLayoutGroup m_layoutGroup;

    private string m_timeIconString = "<sprite name=\"Time\">";
    private string m_healthIconString = "<sprite name=\"ResourceHealth\">";

    void Awake()
    {
        Instance = this;
        m_canvasGroup = m_tooltipDisplayGroup.GetComponent<CanvasGroup>();
        m_layoutGroup = m_tooltipDisplayGroup.GetComponent<VerticalLayoutGroup>();
        m_canvasGroup.alpha = 0;
    }


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        //If the last Selectable is the same as the Currenct Selectable, do nothing.
        if (m_lastHoveredSelectable == GameplayManager.Instance.m_hoveredSelectable) return;

        if (GameplayManager.Instance.m_hoveredSelectable)
        {
            //If hovered is true, SetTooltipData and then display the tooltip.
            SetTooltipData(GameplayManager.Instance.m_hoveredSelectable.m_selectedObjectType, GameplayManager.Instance.m_hoveredSelectable.gameObject);
        }
        else
        {
            //If hovered is null, Turn off the tooltip display.
            RequestShowTooltip(false);
        }

        //Update LastHoveredSelectable so we're not setting data or toggling every frame if the hovered has not changed.
        m_lastHoveredSelectable = GameplayManager.Instance.m_hoveredSelectable;
    }

    void SetTooltipData(Selectable.SelectedObjectType type, GameObject hoveredObj)
    {
        /*m_objectNameLabel.gameObject.SetActive(false);
        m_objectDescriptionLabel.gameObject.SetActive(false);
        m_objectDetailsLabel.gameObject.SetActive(false);*/

        switch (type)
        {
            case Selectable.SelectedObjectType.ResourceWood:
                ResourceNodeTooltipData woodNodeData = hoveredObj.GetComponent<ResourceNode>().GetTooltipData();
                m_objectNameString = woodNodeData.m_resourceNodeName;
                m_objectDescriptionString = woodNodeData.m_resourceNodeDescription;
                m_objectDetailsString = $"Resources Remaining: {woodNodeData.m_curResources} / {woodNodeData.m_maxResources}";
                break;
            case Selectable.SelectedObjectType.ResourceStone:
                ResourceNodeTooltipData stoneNodeData = hoveredObj.GetComponent<ResourceNode>().GetTooltipData();
                m_objectNameString = stoneNodeData.m_resourceNodeName;
                m_objectDescriptionString = stoneNodeData.m_resourceNodeDescription;
                m_objectDetailsString = $"Resources Remaining: {stoneNodeData.m_curResources} / {stoneNodeData.m_maxResources}";
                break;
            case Selectable.SelectedObjectType.Tower:
                break;
            case Selectable.SelectedObjectType.Gatherer:
                GathererTooltipData gathererData = hoveredObj.GetComponent<GathererController>().GetTooltipData();

                //Define needed icons based on gatherer type.
                string m_resourceIconString;
                string m_gathererIconString;
                switch (gathererData.m_gathererType)
                {
                    case ResourceManager.ResourceType.Wood:
                        m_resourceIconString = "<sprite name=\"ResourceWood\">";
                        m_gathererIconString = "<sprite name=\"GathererWood\">";
                        break;
                    case ResourceManager.ResourceType.Stone:
                        m_resourceIconString = "<sprite name=\"ResourceStone\">";
                        m_gathererIconString = "<sprite name=\"GathererStone\">";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                m_objectNameString = $"{m_gathererIconString} {gathererData.m_gathererName}";
                m_objectDescriptionString = gathererData.m_gathererDescription;
                m_objectDetailsString = $"Carry Capacity: {gathererData.m_carryCapacity}{m_resourceIconString}<br>" +
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
        //Immediate on and off. Could split this out into a state machine later to add animations.
        m_canvasGroup.alpha = show ? 1 : 0;
    }
}