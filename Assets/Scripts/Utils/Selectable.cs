using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class Selectable : MonoBehaviour
{
    private Outline[] m_outlines;
    private Color m_outlineBaseColor;
    private Color m_outlineRestrictedColor;
    private float m_outlineWidth;
    private bool m_isSelected;

    public SelectedObjectType m_selectedObjectType;

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
    }

    void Start()
    {
        //Get the colors from the gameplay manager and store them here.
        SetOutlineVariables();
        
        //Can probably remove this
        BuildOutlineArray();
    }

    private void BuildOutlineArray()
    {
        //Get all the outlines in the children and set their color & activity
        m_outlines = GetComponentsInChildren<Outline>();
        for (int i = 0; i < m_outlines.Length; ++i)
        {
            Outline outline = m_outlines[i];
            outline.OutlineColor = m_outlineBaseColor;
            outline.enabled = false;
        }
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
        if (m_outlines.Length <= 0)
        {
            BuildOutlineArray();
        }
        
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
            outline.OutlineColor = GameplayManager.Instance.m_selectedObjIsRestricted ? m_outlineRestrictedColor : m_outlineBaseColor;
        }
    }

    private void SetOutlineVariables()
    {
        m_outlineBaseColor = GameplayManager.Instance.m_outlineBaseColor;
        m_outlineRestrictedColor = GameplayManager.Instance.m_outlineRestrictedColor;
        m_outlineWidth = GameplayManager.Instance.m_outlineWidth;
    }


    void OnDestroy()
    {
        GameplayManager.OnObjRestricted -= SetOutlineColor;
        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
        GameplayManager.OnGameObjectDeselected -= GameObjectDeselected;
    }
}