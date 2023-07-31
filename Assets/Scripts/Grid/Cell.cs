using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public enum CellState
    {
        Empty,
        Occupied,
        Hovered,
        Path,
        Island,
    }

    public CellState m_cellState;
    public bool m_isOccupied;

    private CellState m_baseState;
    private Material m_material;


    void Start()
    {
        m_material = GetComponent<MeshRenderer>().material;
        m_baseState = m_cellState;
        UpdateCellState(m_cellState);
    }

    void Update()
    {
    }

    public void ResetState()
    {
        UpdateCellState(m_baseState);
    }
    
    public void UpdateCellState(CellState newState)
    {
        m_cellState = newState;
        
        switch (newState)
        {
            case CellState.Empty:
                m_isOccupied = false;
                m_material.color = Color.white;
                break;
            case CellState.Occupied:
                m_isOccupied = true;
                m_material.color = Color.gray;
                break;
            case CellState.Hovered:
                m_isOccupied = true;
                m_material.color = Color.yellow;
                break;
            case CellState.Path:
                m_isOccupied = false;
                m_material.color = Color.green;
                break;
            case CellState.Island:
                m_isOccupied = false;
                m_material.color = Color.blue;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}