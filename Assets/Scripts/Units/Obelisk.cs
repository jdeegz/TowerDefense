using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Diagnostics;

public class Obelisk : MonoBehaviour
{
    public int m_obeliskRadius = 1;
    public int m_startingChargeCount = 0;
    public int m_maxChargeCount = 10;
    public float m_meterOffset = 100f;
    private List<Cell> m_cellsInRange;
    private Cell m_cell;
    private int m_curChargeCount;
    public UIIngameMeter m_meter;
    public GameObject m_chargedVFXGroup;
    public LineRenderer m_obeliskRangeCircle;

    public enum ObeliskState
    {
        Charging,
        Charged
    }

    public ObeliskState m_obeliskState;
    
    public static event Action<ObeliskState> OnObeliskStateChanged;
    public static event Action<int> OnObeliskChargeChanged;

    public void UpdateObeliskState(ObeliskState newState)
    {
        m_obeliskState = newState;
        
        switch (newState)
        {
            case ObeliskState.Charging:
                m_chargedVFXGroup.SetActive(false);
                break;
            case ObeliskState.Charged:
                m_chargedVFXGroup.SetActive(true);
                GameplayManager.Instance.CheckObeliskStatus();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
        
        OnObeliskStateChanged?.Invoke(newState);
    }
    
    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        OnObeliskChargeChanged += ObeliskChargeChanged;
    }

    
    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
        OnObeliskChargeChanged -= ObeliskChargeChanged;
    }
    
    private void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.PlaceObstacles)
        {
            //Set occupancy
            GridCellOccupantUtil.SetOccupant(gameObject, true, 3, 3);
            
            m_meter = Instantiate(IngameUIController.Instance.m_ingameMeter, IngameUIController.Instance.transform);
            m_meter.SetupMeter(this.gameObject, m_meterOffset);
            
            SetupRangeCircle(30, m_obeliskRadius);

            GameplayManager.Instance.AddObeliskToList(this);
        }
    }
    
    void Start()
    {
        m_curChargeCount = m_startingChargeCount;
    }
    
    public void IncreaseObeliskCharge(int i)
    {
        switch (m_obeliskState)
        {
            case ObeliskState.Charging:
                m_curChargeCount += i;
                m_meter.SetProgress((float)m_curChargeCount / m_maxChargeCount);

                if (m_curChargeCount >= m_maxChargeCount)
                {
                    UpdateObeliskState(ObeliskState.Charged);
                }
                OnObeliskChargeChanged?.Invoke(m_curChargeCount);
                break;
            case ObeliskState.Charged:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void ObeliskChargeChanged(int i)
    {
        //This is not in use at the moment.
        Debug.Log($"Obelisk {gameObject.name} charge changed.");
    }
    
    void SetupRangeCircle(int segments, float radius)
    {
        m_obeliskRangeCircle.positionCount = segments;
        m_obeliskRangeCircle.startWidth = 0.05f;
        m_obeliskRangeCircle.endWidth = 0.05f;
        for(int i = 0; i < segments; ++i)
        {
            float circumferenceProgress = (float) i / segments;
            float currentRadian = circumferenceProgress * 2 * Mathf.PI;
            float xScaled = Mathf.Cos(currentRadian);
            float yScaled = Mathf.Sin(currentRadian);

            float x = xScaled * radius;
            float y = yScaled * radius;

            Vector3 currentPosition = new Vector3(x, 0.25f, y);

            m_obeliskRangeCircle.SetPosition(i, currentPosition);
        }
    }
}
