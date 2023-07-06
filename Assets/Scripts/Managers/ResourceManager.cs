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


    private void Awake()
    {
        Instance = this;
    }

    public void UpdateStoneAmount(int amount)
    {
        m_stoneBank += amount;
        UpdateStoneBank?.Invoke(amount);
    }

    public int GetStoneAmount()
    {
        return m_stoneBank;
    }

    public void UpdateWoodAmount(int amount)
    {
        m_woodBank += amount;
        UpdateWoodBank?.Invoke(amount);
    }

    public int GetWoodAmount()
    {
        return m_woodBank;
    }

    public void UpdateStoneGathererAmount(int amount)
    {
        m_woodBank += amount;
        UpdateStoneGathererCount?.Invoke(amount);
    }

    public int GetStoneGathererAmount()
    {
        return m_stoneGathererCount;
    }

    public void UpdateWoodGathererAmount(int amount)
    {
        m_woodBank += amount;
        UpdateWoodGathererCount?.Invoke(amount);
    }

    public int GetWoodGathererAmount()
    {
        return m_woodGathererCount;
    }
}