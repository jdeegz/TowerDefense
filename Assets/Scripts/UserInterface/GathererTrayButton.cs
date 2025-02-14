using System;
using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class GathererTrayButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image m_gathererImage;
    public TextMeshProUGUI m_hotkeyLabel;
    public TextMeshProUGUI m_gathererLevelLabel;
    public GameObject m_idleDisplayGroup;
    [SerializeField] private UIEffect m_buttonUIEffect;

    private Button m_button;
    private GathererController m_gathererController;
    private GathererController.GathererTask m_lastTask;
    private GathererData m_gathererData;

    private ButtonState m_buttonState;

    public enum ButtonState
    {
        Normal,
        Selected,
        Hovered,
    }

    public void SetupGathererTrayButton(GathererController gathererController, string hotkey)
    {
        m_button = GetComponent<Button>();
        m_gathererController = gathererController;
        m_button.onClick.AddListener(SelectGatherer);
        m_gathererData = m_gathererController.m_gathererData;
        m_gathererImage.sprite = m_gathererData.m_gathererIconSprite;
        //m_gathererTypeImage.sprite = m_gathererData.m_gathererTypeSprite;
        m_hotkeyLabel.SetText(hotkey);
        m_lastTask = gathererController.GetGathererTask();
        ToggleIdleDisplay(m_gathererController.GetGathererTask() == GathererController.GathererTask.Idling);
        GameplayManager.OnGameObjectSelected += GathererSelected;
        GameplayManager.OnGameObjectDeselected += GathererDeselected;
        m_gathererController.GathererLevelChange += UpdateGathererLevelLabel;
    }

    public void SelectGatherer()
    {
        GameplayManager.Instance.RequestSelectGatherer(m_gathererController.gameObject);
        CameraController.Instance.RequestOnRailsMove(m_gathererController.transform.position, 0.15f);
    }

    private void GathererSelected(GameObject selectedObj)
    {
        if (selectedObj == m_gathererController.gameObject)
        {
            SetButtonOutline(ButtonState.Selected);
        }
    }

    private void GathererDeselected(GameObject selectedObj)
    {
        if (selectedObj == m_gathererController.gameObject)
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
    }

    void Update()
    {
        if (m_gathererController.GetGathererTask() != m_lastTask)
        {
            m_lastTask = m_gathererController.GetGathererTask();
            ToggleIdleDisplay(m_gathererController.GetGathererTask() == GathererController.GathererTask.Idling);
        }
    }

    void UpdateGathererLevelLabel(int level)
    {
        m_gathererLevelLabel.SetText($"Lv {level}");
    }

    public void ToggleIdleDisplay(bool b)
    {
        m_idleDisplayGroup.SetActive(b);
    }

    private bool m_isHovered;

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Get the selectable component related to this button.
        Selectable selectable = m_gathererController.GetComponent<Selectable>();
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

    public void SetButtonOutline(ButtonState state)
    {
        if (state != m_buttonState)
        {
            switch (state)
            {
                case ButtonState.Normal:
                    m_buttonState = ButtonState.Normal;
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
        }
    }

    private void OnDestroy()
    {
        GameplayManager.OnGameObjectSelected -= GathererSelected;
        GameplayManager.OnGameObjectDeselected -= GathererDeselected;
        m_gathererController.GathererLevelChange -= UpdateGathererLevelLabel;
    }
}