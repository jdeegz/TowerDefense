using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ProgressionManager : MonoBehaviour
{
    [SerializeField] private ProgressionTable m_progressionTable;

    [SerializeField] private List<ProgressionKeyData> m_keys;

    void Start()
    {
        PlayerDataManager.Instance.SetProgressionTable(m_progressionTable);
        PlayerDataManager.OnUnlockableUnlocked += UnlockableUnlocked;
        PlayerDataManager.OnUnlockableLocked += UnlockableLocked;
        GetProgressionState();
    }

    private void UnlockableLocked(ProgressionUnlockableData unlockableData)
    {
        // Is the unlockable something we need to destroy a Tray button for?
        RequestRemoveTrayButton(unlockableData);
    }

    private void UnlockableUnlocked(ProgressionUnlockableData unlockableData)
    {
        // Is the unlockable something we need to build a Tray button for?
        Debug.Log($"{unlockableData.name} earned, building a tray button if required.");
        RequestTrayButton(unlockableData);
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            HandleKeyRelease(0);
        }
        else if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            HandleKeyRelease(1);
        }
        else if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            HandleKeyRelease(2);
        }
        else if (Input.GetKeyUp(KeyCode.Alpha4))
        {
            HandleKeyRelease(3);
        }
        else if (Input.GetKeyUp(KeyCode.Alpha5))
        {
            HandleKeyRelease(4);
        }
        else if (Input.GetKeyUp(KeyCode.Alpha6))
        {
            HandleKeyRelease(5);
        }
        else if (Input.GetKeyUp(KeyCode.Alpha7))
        {
            HandleKeyRelease(6);
        }
        else if (Input.GetKeyUp(KeyCode.Alpha8))
        {
            HandleKeyRelease(7);
        }
        else if (Input.GetKeyUp(KeyCode.Alpha9))
        {
            HandleKeyRelease(8);
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            Debug.Log($"R button pressed.");
            m_progressionTable.ResetProgressionData();
        }

        if (Input.GetKeyUp(KeyCode.C))
        {
            Debug.Log($"C button pressed.");
            m_progressionTable.CheatProgressionData();
        }

        if (Input.GetKeyUp(KeyCode.P))
        {
            Debug.Log($"P button pressed.");
            PrintButtonStates();
        }
    }

    private void HandleKeyRelease(int i)
    {
        Debug.Log($"{i} button pressed.");
        if (i < 0 || i > m_keys.Count) return;
        PlayerDataManager.Instance.RequestUnlockKey(m_keys[i]);
    }

    

    private void PrintButtonStates()
    {
        Debug.Log($"Printing Available Buttons:");
        
        // Towers
        if (m_towerButtons == null || m_towerButtons.Count == 0)
        {
            Debug.Log($"No Towerss available.");
        }
        else
        {
            foreach (TowerButton towerButton in m_towerButtons)
            {
                Debug.Log($"TOWER: Button for {towerButton.m_towerData.m_towerName} available.");
            }
        }
        
        // Structures
        if (m_structureButtons == null || m_structureButtons.Count == 0)
        {
            Debug.Log($"No Structures available.");
        }
        else
        {
            foreach (StructureButton structureButton in m_structureButtons)
            {
                Debug.Log($"STRUCTURE: Button for {structureButton.m_structureData.m_towerName} available. Quantity: {structureButton.m_qty}.");
            }
        }
    }

    // UI MANAGER FUNCTIONS
    private void GetProgressionState()
    {
        foreach (ProgressionUnlockableData unlockableData in m_progressionTable.GetListUnlockableData())
        {
            UnlockProgress unlockProgress = unlockableData.GetProgress();
            if (unlockProgress.m_isUnlocked)
            {
                RequestTrayButton(unlockableData);
            }
        }
    }

    private void RequestTrayButton(ProgressionUnlockableData unlockableData)
    {
        Debug.Log($"Tray Button requested;");

        // What kind of Tray button?
        ProgressionRewardData rewardData = unlockableData.GetRewardData();

        switch (rewardData.RewardType)
        {
            case "Tower":
                ProgressionRewardTower towerRewardData = rewardData as ProgressionRewardTower;
                TowerData towerData = towerRewardData.GetTowerData();
                RequestTowerButton(towerData);
                break;
            case "Structure":
                ProgressionRewardStructure structureRewardData = rewardData as ProgressionRewardStructure;
                TowerData structureData = structureRewardData.GetStructureData();
                RequestStructureButton(structureRewardData, structureData);
                break;
            default:
                Debug.Log($"No case for {rewardData.RewardType}.");
                break;
        }
    }

    private void RequestRemoveTrayButton(ProgressionUnlockableData unlockableData)
    {
        Debug.Log($"Tray Button Removal requested;");

        // What kind of Tray button?
        ProgressionRewardData rewardData = unlockableData.GetRewardData();

        switch (rewardData.RewardType)
        {
            case "Tower":
                ProgressionRewardTower towerRewardData = rewardData as ProgressionRewardTower;
                TowerData towerData = towerRewardData.GetTowerData();
                RemoveTowerButton(towerData);
                break;
            case "Structure":
                ProgressionRewardStructure structureRewardData = rewardData as ProgressionRewardStructure;
                TowerData structureData = structureRewardData.GetStructureData();
                RemoveStructureButton(structureData, structureRewardData.GetStructureRewardQty());
                break;
            default:
                Debug.Log($"No case for {rewardData.RewardType}.");
                break;
        }
    }

    // TOWERS
    private List<TowerButton> m_towerButtons;

    private void RequestTowerButton(TowerData towerData)
    {
        // Initialize list if null.
        if (m_towerButtons == null)
        {
            m_towerButtons = new List<TowerButton>();
        }

        // Check to see if we have a button with this data already.
        foreach (TowerButton button in m_towerButtons)
        {
            if (towerData == button.m_towerData)
            {
                // We have a button already, return
                return;
            }
        }

        // Else build a new button.
        BuildTowerButton(towerData);
    }

    private void BuildTowerButton(TowerData towerData)
    {
        Debug.Log($"Building Tower Button: Starting");

        TowerButton newButton = new TowerButton(towerData);
        m_towerButtons.Add(newButton);

        Debug.Log($"Building Tower Button: Complete");
    }
    
    private void RemoveTowerButton(TowerData towerData)
    {
        if (m_towerButtons == null) return;
        
        Debug.Log($"Removing Tower Button: Starting");
        
        foreach (TowerButton towerButton in m_towerButtons)
        {
            if (towerButton.m_towerData == towerData)
            {
                m_towerButtons.Remove(towerButton);
                //Destroy(towerButton.gameObject);
                return;
            }
        }

        Debug.Log($"Removing Tower Button: Complete");
    }

    // STRUCTURES
    private List<StructureButton> m_structureButtons;

    private void RequestStructureButton(ProgressionRewardStructure rewardData, TowerData structureData)
    {
        // Initialize list if null.
        if (m_structureButtons == null)
        {
            m_structureButtons = new List<StructureButton>();
        }

        // Check to see if we have a button with this data already.
        foreach (StructureButton button in m_structureButtons)
        {
            if (structureData == button.m_structureData)
            {
                // We have a button already, increment Qty.
                button.IncrementQuantity(rewardData.GetStructureRewardQty());
                return;
            }
        }

        // Else build a new button.
        BuildStructureButton(structureData, rewardData.GetStructureRewardQty());
    }

    private void BuildStructureButton(TowerData structureData, int qty)
    {
        Debug.Log($"Building Structure Button: Starting");

        StructureButton newButton = new StructureButton(structureData, qty);
        m_structureButtons.Add(newButton);

        Debug.Log($"Building Structure Button: Complete");
    }
    
    private void RemoveStructureButton(TowerData structureData, int qty)
    {
        if (m_structureButtons == null) return;
        
        Debug.Log($"Removing Structure Button: Starting");
        
        foreach (StructureButton structureButton in m_structureButtons)
        {
            if (structureButton.m_structureData = structureData)
            {
                structureButton.m_qty -= qty;
                if (structureButton.m_qty <= 0)
                {
                    m_structureButtons.Remove(structureButton);
                    //Destroy(structureButton.gameObject);
                    return;
                }
            }
        }
        Debug.Log($"Removing Structure Button: Complete");
    }
}

public class TowerButton
{
    public TowerData m_towerData;

    public TowerButton(TowerData towerData)
    {
        m_towerData = towerData;
    }
}

public class StructureButton
{
    public TowerData m_structureData;
    public int m_qty;

    public StructureButton(TowerData towerData, int qty)
    {
        m_structureData = towerData;
        m_qty = qty;
    }

    public void IncrementQuantity(int i)
    {
        m_qty += i;

        Debug.Log($"STRUCTURE: {m_structureData.m_towerName}'s quantity increased to: {m_qty}.");
    }
}