using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class Selectable : MonoBehaviour
{
    [SerializeField] private SelectionColors m_selectionColors;
    [SerializeField] private Outline[] m_outlines;
    public SelectedObjectType m_selectedObjectType;

    public enum SelectedObjectType
    {
        ResourceWood,
        ResourceStone,
        Tower,
        Gatherer,
        Castle,
        Obelisk,
        Ruin
    }

    void Awake()
    {
        GameplayManager.OnObjRestricted += SetOutlineColor;
        GameplayManager.OnGameObjectSelected += GameObjectSelected;
        GameplayManager.OnGameObjectDeselected += GameObjectDeselected;
        GameplayManager.OnCommandRequested += GameObjectCommandRequested;

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
        if (obj == gameObject)
        {
            EnableOutlines(true);

            if (m_selectedObjectType == SelectedObjectType.Tower && GameplayManager.Instance.m_interactionState == GameplayManager.InteractionState.SelectedTower)
            {
                IngameUIController.Instance.m_towerSelectHUD.SelectTower(obj);
                //Put Tower Radius here.
            }
        }
        else
        {
            EnableOutlines(false);
        }
    }

    private void GameObjectDeselected(GameObject obj)
    {
        if (obj == gameObject)
        {
            EnableOutlines(false);
            
            if (m_selectedObjectType == SelectedObjectType.Tower)
            {
                IngameUIController.Instance.m_towerSelectHUD.DeselectTower();
            }
        }
    }

    public void EnableOutlines(bool enabled)
    {
        for (int i = 0; i < m_outlines.Length; ++i)
        {
            m_outlines[i].enabled = enabled;
        }
    }

    private void SetOutlineColor(GameObject obj, bool isRestricted)
    {
        if (obj != gameObject)
        {
            return;
        }

        //Debug.Log("Trying to change colors");
        for (int i = 0; i < m_outlines.Length; ++i)
        {
            Outline outline = m_outlines[i];
            outline.OutlineColor = isRestricted ? m_selectionColors.m_outlineBaseColor : m_selectionColors.m_outlineRestrictedColor;
        }
    }

    private void GameObjectCommandRequested(GameObject obj, SelectedObjectType type)
    {
        if (obj == gameObject)
        {
            EnableOutlines(true);
            gameObject.transform.DOScale(1f, .2f).OnComplete(() => EnableOutlines(false));
        }
    }

    void OnDestroy()
    {
        GameplayManager.OnObjRestricted -= SetOutlineColor;
        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
        GameplayManager.OnGameObjectDeselected -= GameObjectDeselected;
        GameplayManager.OnCommandRequested -= GameObjectCommandRequested;
    }
}