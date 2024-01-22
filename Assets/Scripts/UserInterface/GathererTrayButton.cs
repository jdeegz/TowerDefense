using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class GathererTrayButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler 
{
    public Image m_gathererImage;
    public TextMeshProUGUI m_hotkeyLabel;
    public GameObject m_idleDisplayGroup;
    public GameObject m_selectedDisplayGroup;
    public Image m_gathererTypeImage;

    private Button m_button;
    private GathererController m_gathererController;
    private GathererController.GathererTask m_lastTask;
    private GathererData m_gathererData;

    public void SetupGathererTrayButton(GathererController gathererController, string hotkey)
    {
        m_button = GetComponent<Button>();
        m_gathererController = gathererController;
        m_button.onClick.AddListener(SelectGatherer);
        m_gathererData = m_gathererController.m_gathererData;
        m_gathererImage.sprite = m_gathererData.m_gathererIconSprite;
        m_gathererTypeImage.sprite = m_gathererData.m_gathererTypeSprite;
        m_hotkeyLabel.SetText(hotkey);
        m_lastTask = gathererController.m_gathererTask;
        ToggleIdleDisplay(m_gathererController.m_gathererTask == GathererController.GathererTask.Idling);
        GameplayManager.OnGameObjectSelected += GathererSelected;
        GameplayManager.OnGameObjectDeselected += GathererDeselected;
    }

    public void SelectGatherer()
    {
        GameplayManager.Instance.RequestSelectGatherer(m_gathererController.gameObject);
    }
    
    private void GathererSelected(GameObject selectedObj)
    {
        m_selectedDisplayGroup.SetActive(selectedObj == m_gathererController.gameObject);
    }
    
    private void GathererDeselected(GameObject selectedObj)
    {
        m_selectedDisplayGroup.SetActive(!selectedObj == m_gathererController.gameObject);
    }

    public void DeselectGatherer()
    {
        
    }

    void Update()
    {
        if (m_gathererController.m_gathererTask != m_lastTask)
        {
            m_lastTask = m_gathererController.m_gathererTask;
            ToggleIdleDisplay(m_gathererController.m_gathererTask == GathererController.GathererTask.Idling);
        }
    }
    
    public void ToggleIdleDisplay(bool b)
    {
        m_idleDisplayGroup.SetActive(b);
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        //Get the selectable component related to this button.
        Selectable selectable = m_gathererController.GetComponent<Selectable>();
        UITooltipController.Instance.SetUISelectable(selectable);
    }

    public void OnPointerExit(PointerEventData evenData)
    {
        UITooltipController.Instance.SetUISelectable(null);
    }

    private void OnDestroy()
    {
        GameplayManager.OnGameObjectSelected -= GathererSelected;
        GameplayManager.OnGameObjectDeselected -= GathererDeselected;
    }
}