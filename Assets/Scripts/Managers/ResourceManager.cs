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
    public static event Action<float> UpdateWoodRate;
    public static event Action<int, int> UpdateStoneBank;

    [Header("Ruins Data")]
    public ResourceManagerData m_resourceManagerData;

    [Header("Tree Resource Node Prefabs")]
    public List<GameObject> m_treePrefabs;
    private List<ResourceNode> m_treesInScene;
    
    private int m_badLuckChargeCounter;
    private int m_foundRuinCounter;
    private int m_depletionCounter;

    //each time wood is deposited, add quantity and timestamp to a list.
    
    //When a request to update the gpm display, collect all of the items in the list, within the last 60 seconds.
    //Sum the quantities in the list and divide by 60.
    private float m_depositTimer;
    private List<WoodDeposit> m_woodDeposits;
    private float m_woodPerMinute;
    

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
        m_woodDeposits = new List<WoodDeposit>();
    }

    void Update()
    {
        m_depositTimer += Time.deltaTime;

        if (m_depositTimer % 60 == 0)
        {
            CalculateWoodRate();
        }
    }
    
    public void UpdateWoodAmount(int amount, GathererController gatherer = null)
    {
        m_woodBank += amount;
        UpdateWoodBank?.Invoke(m_woodBank, amount);
        //Debug.Log("BANK UPDATED");

        if (gatherer != null && amount > 0) //Check if this is a deposit and not a sell/build/upgrade
        {
            WoodDeposit newDeposit = new WoodDeposit();
            newDeposit.m_quantity = amount;
            newDeposit.m_timeStamp = m_depositTimer;
            m_woodDeposits.Add(newDeposit);
            CalculateWoodRate();
        }
    }

    private void CalculateWoodRate()
    {
        float currentTime = m_depositTimer;
        int woodSum = 0;
        float factor;
        float firstDepositThisMinute = 0;
        bool minimumTimeMet = false;
            
        for (int i = m_woodDeposits.Count -1 ; i >= 0; --i)
        {
            if (m_woodDeposits[i].m_timeStamp >= currentTime - 60)
            {
                woodSum += m_woodDeposits[i].m_quantity;
                firstDepositThisMinute = m_woodDeposits[i].m_timeStamp;
                //last time
            }
            else
            {
                minimumTimeMet = true;
                break;
            }
        }

        if (minimumTimeMet)
        {
            factor = (60 - (currentTime - 60 - firstDepositThisMinute)) / 60;
            m_woodPerMinute = woodSum * factor;
        }
        else
        {
            //How long has it been? 30s, 60 / 30 = 2, multiply sum by 2 to get estimated WPM.
            factor = 60 / currentTime;
            m_woodPerMinute = woodSum * factor;
        }
        
        UpdateWoodRate?.Invoke(m_woodPerMinute);
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

    public void StartDepositTimer()
    {
        m_depositTimer = 0f;
    }
}

[System.Serializable]
public class WoodDeposit
{
    public int m_quantity;
    public float m_timeStamp;
}