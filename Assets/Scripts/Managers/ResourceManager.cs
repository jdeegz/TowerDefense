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

    [Header("RequestRuinIndicator Data")]
    public ResourceManagerData m_resourceManagerData;

    public List<RuinController> m_ruinsInMission;
    public List<RuinController> m_validRuinsInMission;
    public int m_ruinDiscoveredCount;

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
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.Build)
        {
            RequestRuinIndicator();
        }
    }

    void Start()
    {
        //Debug.Log("RESOURCE MANAGER START");
        UpdateWoodAmount(m_resourceManagerData.m_startingWood);
        UpdateStoneAmount(m_resourceManagerData.m_startingStone);
        m_woodDeposits = new List<WoodDeposit>();
        m_validRuinsInMission = new List<RuinController>(m_ruinsInMission);
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

        for (int i = m_woodDeposits.Count - 1; i >= 0; --i)
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

    public void RequestRuinIndicator()
    {
        //When a gatherer harvests a resource Node, and when the wave counter increments. Wave should be a target, and reset after indicating a ruin.
        if (GameplayManager.Instance.m_wave < m_resourceManagerData.m_minWaves)
        {
            Debug.Log($"Minimum number of waves not yet passed.");
            return; // Too soon to show a ruin indicator.
        }

        if (GameplayManager.Instance.m_wave != m_resourceManagerData.m_minWaves) // If we're not the min valid wave, check if we're a factor
        {
            if (GameplayManager.Instance.m_wave % m_resourceManagerData.m_indicatorFrequency != 0)
            {
                Debug.Log($"Current wave is not a factor of {m_resourceManagerData.m_indicatorFrequency}.");
                return; // Need to wait a little longer.
            }

            Debug.Log($"Current wave is a factor of {m_resourceManagerData.m_indicatorFrequency}.");
        }
        else
        {
            Debug.Log($"Current wave is the Minimum wave.");
        }

        if (m_validRuinsInMission.Count == 0)
        {
            Debug.Log($"We're out of valid Ruins to indicate.");
            return;
        }

        // Ask the ResourceManager if there are enough ruins indicated already.
        int indicated = 0;
        foreach (RuinController ruin in m_ruinsInMission)
        {
            if (ruin.m_ruinState == RuinController.RuinState.Indicated)
            {
                ++indicated;
                if (indicated >= m_resourceManagerData.m_maxIndicators)
                {
                    Debug.Log($"We're currently indicating the max number of desired ruins: {m_resourceManagerData.m_maxIndicators}.");
                    return; // We should't indicate any more ruins.
                }

                Debug.Log($"We're ready to indicate another ruin. Current indicated: {indicated}.");
            }
        }

        // Validate the list of ruins. Has the tree on their cell been harvested? Does the ruin have 3 neighbor trees?
        List<RuinController> invalidRuins = new List<RuinController>();
        int weightSum = 0;

        Debug.Log($"Checking the Valid Ruins list for Invalid Ruins.");
        for (int i = 0; i < m_validRuinsInMission.Count; ++i) // Loop through ruins, identifying if they have at least one good corner, else add to invalid list and remove.
        {
            List<Vector3> validPositionsForIndicators = m_validRuinsInMission[i].CheckValidRuinCorners();

            if (validPositionsForIndicators.Count == 0)
            {
                invalidRuins.Add(m_validRuinsInMission[i]);
            }
            else
            {
                weightSum += m_validRuinsInMission[i].m_ruinWeight;
            }
        }

        foreach (RuinController ruinController in invalidRuins) // Trim the invalid ruins from our list so we dont keep operating on it.
        {
            Debug.Log($"{ruinController.gameObject.name} at {ruinController.transform.position} removed from Valid RequestRuinIndicator list.");
            m_validRuinsInMission.Remove(ruinController);
        }

        int chosenWeight = Random.Range(0, weightSum);
        Debug.Log($"Weighting info( Sum: {weightSum}, Chosen: {chosenWeight}, Valid Ruin Count: {m_validRuinsInMission.Count}");

        int lastTotalWeight = 0;
        for (int i = 0; i < m_validRuinsInMission.Count; ++i)
        {
            if (chosenWeight < lastTotalWeight + m_validRuinsInMission[i].m_ruinWeight)
            {
                // This is the node we have chosen.
                Debug.Log($"{m_validRuinsInMission[i]} at {m_validRuinsInMission[i].transform.position} Chosen.");
                m_validRuinsInMission[i].IndicateThisRuin();
                m_validRuinsInMission.Remove(m_validRuinsInMission[i]);
                break;
            }

            lastTotalWeight += m_validRuinsInMission[i].m_ruinWeight;
        }
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