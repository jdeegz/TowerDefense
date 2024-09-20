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

    [Header("Ruins Data")]
    public ResourceManagerData m_resourceManagerData;

    private int m_badLuckChargeCounter;
    private int m_foundRuinCounter;
    private int m_depletionCounter;
    

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
        UpdateWoodAmount(m_resourceManagerData.m_startingWood);
        UpdateStoneAmount(m_resourceManagerData.m_startingStone);
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

    public bool RequestRuin()
    {
        bool canSpawnRuin = false;

        ++m_depletionCounter;
        
        int randomNumber = Random.Range(0, m_resourceManagerData.m_ruinsChance + 1);

        if (m_depletionCounter >= m_resourceManagerData.m_minDepletions && m_foundRuinCounter < m_resourceManagerData.m_totalRuinsPossible)
        {
            ++m_badLuckChargeCounter;

            if (randomNumber == m_resourceManagerData.m_ruinsChance || m_badLuckChargeCounter == m_resourceManagerData.m_ruinsChance)
            {
                //We found a ruin!
                Debug.Log($"We Found a ruin!");
                m_badLuckChargeCounter = 0;
                ++m_foundRuinCounter;

                canSpawnRuin = true;
            }
        }
        
        return canSpawnRuin;
    }
}