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

    public static event Action<int> UpdateWoodBank;
    public static event Action<int> UpdateStoneBank;

    public int m_startingStone;
    public int m_startingWood;
    


    private void Awake()
    {
        Instance = this;
        UpdateWoodAmount(m_startingWood);
        UpdateStoneAmount(m_startingStone);
    }

    public void UpdateStoneAmount(int amount)
    {
        UpdateStoneBank?.Invoke(m_stoneBank += amount);
    }

    public int GetStoneAmount()
    {
        return m_stoneBank;
    }

    public void UpdateWoodAmount(int amount)
    {
        UpdateWoodBank?.Invoke(m_woodBank += amount);
    }

    public int GetWoodAmount()
    {
        return m_woodBank;
    }

    public void UpdateStoneGathererAmount(int amount)
    {
        UpdateStoneGathererCount?.Invoke(m_stoneGathererCount += amount);
    }

    public int GetStoneGathererAmount()
    {
        return m_stoneGathererCount;
    }

    public void UpdateWoodGathererAmount(int amount)
    {
        UpdateWoodGathererCount?.Invoke(m_woodGathererCount+= amount);
    }

    public int GetWoodGathererAmount()
    {
        return m_woodGathererCount;
    }
}