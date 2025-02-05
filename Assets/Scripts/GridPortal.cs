using System;
using System.Collections.Generic;
using UnityEngine;

public class GridPortal : MonoBehaviour
{
    [SerializeField] private List<PortalPair> m_portalPairs;

    private void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.PlaceObstacles)
        {
            PlacePortalsInCells();
        }
    }

    private void PlacePortalsInCells()
    {
        foreach (PortalPair portalPair in m_portalPairs)
        {
            if (portalPair.m_portalEntranceObj == null || portalPair.m_portalExitObj == null) return;

            GridCellOccupantUtil.SetPortalConnectionCell(portalPair.m_portalEntranceObj, portalPair.m_portalExitObj);
        }
    }

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }


    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }
}

[Serializable]
public class PortalPair
{
    public GameObject m_portalEntranceObj;
    public GameObject m_portalExitObj;
}