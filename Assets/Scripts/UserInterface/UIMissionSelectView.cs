using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIMissionSelectView : MonoBehaviour
{
    [SerializeField] private Button m_backButton;
    [SerializeField] private Button m_resetSaveDataButton;
    [SerializeField] private Button m_unlockAllButton;
    [SerializeField] private GameObject m_missionButtonRoot;
    [SerializeField] private GameObject m_experimentalMissionButtonRoot;
    [SerializeField] private CanvasGroup m_dataResetToast;
    [SerializeField] private UIMissionSelectButton m_missionButtonPrefab;

    private List<UIMissionSelectButton> m_curMissionButtons;

    void Awake()
    {
        MenuManager.OnMenuStateChanged += MenuManagerStateChanged;
    }

    void OnDestroy()
    {
        MenuManager.OnMenuStateChanged -= MenuManagerStateChanged;
    }

    private void MenuManagerStateChanged(MenuManager.MenuState state)
    {
        gameObject.SetActive(state == MenuManager.MenuState.MissionSelect);
    }


    void Start()
    {
        m_backButton.onClick.AddListener(OnBackButtonClick);/*
        m_resetSaveDataButton.onClick.AddListener(OnResetButtonClick);
        m_unlockAllButton.onClick.AddListener(OnUnlockAllButtonClick);*/

        BuildMissionList();
    }

    private void BuildMissionList()
    {
        if (!GameManager.Instance) return;

        int numberOfMissions;

        numberOfMissions = GameManager.Instance.m_missionTable.m_MissionList.Length;

        if (m_curMissionButtons == null) m_curMissionButtons = new List<UIMissionSelectButton>();
        
        // Build Standard Missions
        int standardMissionCount = 6;
        for (int i = m_curMissionButtons.Count; i < numberOfMissions; i++)
        {
            m_curMissionButtons.Add(null);
        }

        for (int i = 0; i < numberOfMissions; i++)
        {
            UIMissionSelectButton button;

            Transform buttonRoot = i < standardMissionCount ? m_missionButtonRoot.transform : m_experimentalMissionButtonRoot.transform;
            
            if (m_curMissionButtons[i] != null)
            {
                button = m_curMissionButtons[i];
            }
            else
            {
                button = Instantiate(m_missionButtonPrefab, buttonRoot);
                m_curMissionButtons[i] = button;
            }

            MissionData data = GameManager.Instance.m_missionTable.m_MissionList[i];

            //Read from player data for this mission.
            int missionCompletionRank = 0; //0 - lock, 1 - unlocked, 2 - defeated
            MissionSaveData missionSaveData = null;
            
            // Make sure we have a mission at this index.
            if (i < PlayerDataManager.Instance.m_playerData.m_missions.Count - 1)
            {
                missionSaveData = PlayerDataManager.Instance.m_playerData.m_missions[i];
            }

            // If we do, assign the completion rank.
            if (missionSaveData != null)
            {
                missionCompletionRank = missionSaveData.m_missionCompletionRank;
            }
            
            // Assure testing missions are unlocked by default, overwrite if there's defeated.
            if (data.m_isUnlockedByDefault)
            {
                missionCompletionRank = Math.Max(1, missionCompletionRank);
            }

            button.SetData(data, missionCompletionRank, i + 1);
        }
    }

    private void OnBackButtonClick()
    {
        MenuManager.Instance.UpdateMenuState(MenuManager.MenuState.StartMenu);
    }

    /*private void OnResetButtonClick()
    {
        //ClearMissionList();
        PlayerDataManager.Instance.ResetPlayerData();
        BuildMissionList();
        m_dataResetToast.alpha = 1;
        m_dataResetToast.DOFade(0, 3f);
    }*/

    /*private void OnUnlockAllButtonClick()
    {
        // Object Unlocks
        //PlayerDataManager.Instance.m_progressionTable.CheatProgressionData();
        
        // Mission Unlocks
        for (int i = 0; i < PlayerDataManager.Instance.m_playerData.m_missions.Count; ++i)
        {
            int curRank = PlayerDataManager.Instance.m_playerData.m_missions[i].m_missionCompletionRank;
            int newRank = Math.Max(1, curRank);
            PlayerDataManager.Instance.m_playerData.m_missions[i].m_missionCompletionRank = newRank;
        }
        
        BuildMissionList();
    }*/
}