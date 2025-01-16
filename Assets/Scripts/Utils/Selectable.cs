using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class Selectable : MonoBehaviour
{
    public SelectedObjectType m_selectedObjectType;
    [SerializeField] private Renderer[] m_meshRenderers;

    private string m_selectedLayerString = "Outline Selected"; //Must sync with layer name.
    private string m_hoveredLayerString = "Outline Hover"; //Must sync with layer name.
    private string m_defaultLayerString;
    private bool m_isSelected;

    public enum SelectedObjectType
    {
        ResourceWood,
        ResourceStone,
        Tower,
        Gatherer,
        Castle,
        Obelisk,
        Ruin,
        Tear,
        Building,
        UIElement
    }

    void Awake()
    {
        m_defaultLayerString = LayerMask.LayerToName(gameObject.layer);
        
        GameplayManager.OnGameObjectSelected += GameObjectSelected;
        GameplayManager.OnGameObjectDeselected += GameObjectDeselected;
        GameplayManager.OnCommandRequested += GameObjectCommandRequested;
        GameplayManager.OnGameObjectHoveredEnter += GameObjectHoveredEnter;
        GameplayManager.OnGameObjectHoveredExit += GameObjectHoveredExit;
    }

    //Not sure we need to make the distinction between Enter and Exit, could this be one action, and one function?
    private void GameObjectHoveredEnter(GameObject obj)
    {
        //we dont want to set the object's layer if it's selected.
        if (m_isSelected) return;
        
        //enable outlines on hover
        if (obj == gameObject)
        {
            //Debug.Log($"{obj} Hover Enter.");
            ToggleHoveredOutline(true);
        }
    }

    private void GameObjectHoveredExit(GameObject obj)
    {
        //we dont want to set the object's layer if it's selected.
        if (m_isSelected) return;
        
        //disable outlines when mouse leaves hover
        if (obj == gameObject)
        {
            //Debug.Log($"{obj} Hover Exit.");
            ToggleHoveredOutline(false);
        }
    }

    private void GameObjectSelected(GameObject obj)
    {
        if (obj == gameObject)
        {
            //Debug.Log($"{obj} Selected.");
            m_isSelected = true;
            
            ToggleSelectionOutline(true);

            if (m_selectedObjectType == SelectedObjectType.Tower && GameplayManager.Instance.m_interactionState == GameplayManager.InteractionState.SelectedTower)
            {
                IngameUIController.Instance.m_towerSelectHUD.SelectTower(obj);
            }
        }
        else
        {
            ToggleSelectionOutline(false);
        }
    }

    private void GameObjectDeselected(GameObject obj)
    {
        if (obj == gameObject)
        {
            //Debug.Log($"{obj} Deselected.");
            m_isSelected = false;
            
            ToggleSelectionOutline(false);

            if (m_selectedObjectType == SelectedObjectType.Tower)
            {
                IngameUIController.Instance.m_towerSelectHUD.DeselectTower();
            }
        }
    }

    public void ToggleSelectionOutline(bool enabled)
    {
        string layerName = enabled ? m_selectedLayerString : m_defaultLayerString;
        foreach (Renderer meshRenderer in m_meshRenderers)
        {
            meshRenderer.gameObject.layer = LayerMask.NameToLayer(layerName);
        }
    }

    public void ToggleHoveredOutline(bool enabled)
    {
        string layerName = enabled ? m_hoveredLayerString : m_defaultLayerString;
        foreach (Renderer meshRenderer in m_meshRenderers)
        {
            meshRenderer.gameObject.layer = LayerMask.NameToLayer(layerName);
        }
    }

    private void GameObjectCommandRequested(GameObject obj, SelectedObjectType type)
    {
        if (obj == gameObject)
        {
            ToggleSelectionOutline(true);
            gameObject.transform.DOScale(1f, .2f).OnComplete(() => ToggleSelectionOutline(false));
        }
    }

    void OnDestroy()
    {
        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
        GameplayManager.OnGameObjectDeselected -= GameObjectDeselected;
        GameplayManager.OnCommandRequested -= GameObjectCommandRequested;
        GameplayManager.OnGameObjectHoveredEnter -= GameObjectHoveredEnter;
        GameplayManager.OnGameObjectHoveredExit -= GameObjectHoveredExit;
    }
}