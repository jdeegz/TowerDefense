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

    void Start()
    {
        m_outlines = GetComponentsInChildren<Outline>();
        SetOutlineVariables();
        GameplayManager.OnObjRestricted += SetOutlineColor;
        GameplayManager.OnGameObjectSelected += SetSelected;
    }

    private void SetSelected(GameObject obj)
    {
        EnableOutlines(obj == gameObject);
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

    public void EnableOutlines(bool enabled)
    {
        for (int i = 0; i < m_outlines.Length; ++i)
        {
            Outline outline = m_outlines[i];
            outline.enabled = enabled;
        }
    }

    void Destroy()
    {
        GameplayManager.OnObjRestricted -= SetOutlineColor;
        GameplayManager.OnGameObjectSelected -= SetSelected;
    }
}