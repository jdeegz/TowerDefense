using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class Selectable : MonoBehaviour
{
    [SerializeField] private SelectionColors m_selectionColors;
    [SerializeField] private Outline[] m_outlines;
    public SelectedObjectType m_selectedObjectType;

    private bool m_isSelected;

    public enum SelectedObjectType
    {
        ResourceWood,
        ResourceStone,
        Tower,
        Gatherer
    }

    void Awake()
    {
        GameplayManager.OnObjRestricted += SetOutlineColor;
        GameplayManager.OnGameObjectSelected += GameObjectSelected;
        GameplayManager.OnGameObjectDeselected += GameObjectDeselected;
        
        for (int i = 0; i < m_outlines.Length; ++i)
        {
            m_outlines[i].OutlineColor = m_selectionColors.m_outlineBaseColor;
            m_outlines[i].OutlineWidth = m_selectionColors.m_outlineWidth;
            m_outlines[i].enabled = false;
        }
    }

    void Start()
    {
    }

    private void GameObjectSelected(GameObject obj)
    {
        EnableOutlines(obj == gameObject);
    }

    private void GameObjectDeselected(GameObject obj)
    {
        if (obj == gameObject)
        {
            EnableOutlines(false);
        }
    }

    public void EnableOutlines(bool enabled)
    {
        for (int i = 0; i < m_outlines.Length; ++i)
        {
            m_outlines[i].enabled = enabled;
        }
    }

    private void SetOutlineColor(object sender, EventArgs e)
    {
        for (int i = 0; i < m_outlines.Length; ++i)
        {
            Outline outline = m_outlines[i];
            outline.OutlineColor = GameplayManager.Instance.m_selectedObjIsRestricted ? m_selectionColors.m_outlineRestrictedColor : m_selectionColors.m_outlineBaseColor;
        }
    }

    void OnDestroy()
    {
        GameplayManager.OnObjRestricted -= SetOutlineColor;
        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
        GameplayManager.OnGameObjectDeselected -= GameObjectDeselected;
    }
}