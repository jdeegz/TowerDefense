using System.Collections.Generic;
using UnityEngine;

public class ProgressionCheats : MonoBehaviour
{
    
    [SerializeField] private GameObject m_cheatToggleObj;
    [SerializeField] private GameObject m_cheatButtonObj;
    [SerializeField] private Transform m_rootTransform;
    [SerializeField] private List<ProgressionUnlockableData> m_unlockableData;
    private ProgressionTable m_progressionTable;
    private List<CheatToggle> m_toggles;

    //On Start build a list of buttons.
    void Start()
    {
        // Get progression info
        m_progressionTable = PlayerDataManager.m_progressionTable;
        
        // Build Buttons, Unlock/Lock all.
        // Unlock All
        GameObject buttonObj = Instantiate(m_cheatButtonObj, m_rootTransform);
        CheatButton cheatButton = buttonObj.GetComponent<CheatButton>();
        
        cheatButton.SetupButton("Unlock All", m_progressionTable.CheatProgressionData, UpdateState);
        
        buttonObj = Instantiate(m_cheatButtonObj, m_rootTransform);
        cheatButton = buttonObj.GetComponent<CheatButton>();
        cheatButton.SetupButton("Lock All", m_progressionTable.ResetProgressionData, UpdateState);
        
        // Build Toggles
        m_toggles = new List<CheatToggle>();
        foreach (ProgressionUnlockableData unlockableData in m_unlockableData)
        {
            GameObject toggleObj = Instantiate(m_cheatToggleObj, m_rootTransform);
            CheatToggle cheatToggle = toggleObj.GetComponent<CheatToggle>();
            cheatToggle.SetupToggle(unlockableData);
            m_toggles.Add(cheatToggle);
        }
    }

    void OnEnable()
    {
        UpdateState();
    }

    // Generic function that will be called to assure the toggles reflect the state of their unlockables.
    public void UpdateState()
    {
        if (m_toggles != null && m_toggles.Count != 0)
        {
            foreach (CheatToggle toggle in m_toggles)
            {
                toggle.UpdateState();
            }
        }
    }
}
