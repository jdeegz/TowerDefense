using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ResourceManager : MonoBehaviour
{
    public enum ResourceType
    {
        Wood,
        Stone
    }

    private static ResourceType m_resourceType;
    public static ResourceManager Instance;
    private static int m_stoneBank = 0;
    private static int m_woodBank = 0;
    private static int m_stoneGathererCount = 0;
    private static int m_woodGathererCount = 0;

    public static event Action<int> UpdateStoneGathererCount;
    public static event Action<int> UpdateWoodGathererCount;

    public static event Action<int, int> UpdateWoodBank;
    public static event Action<int, int> UpdateStoneBank;

    public int m_startingStone;
    public int m_startingWood;


    private void Awake()
    {
        //Debug.Log("RESOURCE MANAGER AWAKE");
        Instance = this;
        m_stoneBank = 0;
        m_woodBank = 0;
        m_stoneGathererCount = 0;
        m_woodGathererCount = 0;
    }

    void Start()
    {
        //Debug.Log("RESOURCE MANAGER START");
        UpdateWoodAmount(m_startingWood);
        UpdateStoneAmount(m_startingStone);
        
    }

    public void UpdateWoodAmount(int amount)
    {
        m_woodBank += amount;
        UpdateWoodBank?.Invoke(m_woodBank, amount);
        //Debug.Log("BANK UPDATED");
    }
    public void UpdateStoneAmount(int amount)
    {
        m_stoneBank += amount;
        UpdateStoneBank?.Invoke(m_stoneBank, amount);
    }

    public int GetStoneAmount()
    {
        return m_stoneBank;
    }


    public int GetWoodAmount()
    {
        return m_woodBank;
    }

    public void UpdateStoneGathererAmount(int amount)
    {
        m_stoneGathererCount += amount;
        UpdateStoneGathererCount?.Invoke(m_stoneGathererCount);
    }

    public int GetStoneGathererAmount()
    {
        return m_stoneGathererCount;
    }

    public void UpdateWoodGathererAmount(int amount)
    {
        m_woodGathererCount += amount;
        UpdateWoodGathererCount?.Invoke(m_woodGathererCount);
    }

    public int GetWoodGathererAmount()
    {
        return m_woodGathererCount;
    }
}