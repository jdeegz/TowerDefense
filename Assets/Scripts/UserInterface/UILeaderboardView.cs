using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;

public class UILeaderboardView : MonoBehaviour
{
    [Header("Display Objs")]
    public GameObject m_listRootObj;
    public GameObject m_leaderboardTitleObj;
    public GameObject m_leaderboardPlayerObj;
    public GameObject m_loginRequiredDisplay;
    
    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI m_loginStatusLabel;

    private bool Initialized;
    public bool m_initialized
    { 
        get { return Initialized; }
        set
        {
            // Check if the value is different to avoid unnecessary event triggers
            if (Initialized != value)
            {
                // Set the value
                Initialized = value;

                if (Initialized == false) return;

                // Trigger the event with the new value
                Debug.Log($"Subscribing Leaderboard View to PlayFabManager.");
                PlayFabManager.Instance.OnLoginComplete += GetLeaderboardData;
                PlayFabManager.Instance.OnLeaderboardReceived += UpdateLeaderboardView;
            }
        }
    }


    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log($"Leaderboard Subscribing to MenuManager.");
        MenuManager.OnMenuStateChanged += MenuStateChanged;
        m_loginStatusLabel.SetText("");
    }

    private void MenuStateChanged(MenuManager.MenuState newState)
    {
        if (newState != MenuManager.MenuState.StartMenu) return;

        Debug.Log($"We're in Start Menus. Trying to Show Leaderboards.");

        if (PlayFabManager.Instance)
        {
            if (PlayFabClientAPI.IsClientLoggedIn())
            {
                m_initialized = true;
                
                //We succeeded logging in, set the label.
                m_loginStatusLabel.SetText($"Logged in as: {PlayFabManager.Instance.m_playerDisplayName}");
                GetLeaderboardData();
            }
        }
    }

    void OnDestroy()
    {
        MenuManager.OnMenuStateChanged -= MenuStateChanged;
        if (PlayFabManager.Instance)
        {
            PlayFabManager.Instance.OnLoginComplete -= GetLeaderboardData;
            PlayFabManager.Instance.OnLeaderboardReceived -= UpdateLeaderboardView;
        }
    }

    public void GetLeaderboardData()
    {
        Debug.Log($"Leaderboard View: Get Leaderboard Data");
        PlayFabManager.Instance.GetAllLeaderboards();
    }

    private void UpdateLeaderboardView(bool failed, Dictionary<string, GetLeaderboardResult> results)
    {
        Debug.Log($"Leaderboard View: Update Leaderboard View Failed: {failed}");
        Debug.Log($"{results.Count} Results found.");
        m_loginRequiredDisplay.SetActive(failed);

        if (failed) return;

        ClearList();

        MissionData[] missionList = GameManager.Instance.m_MissionContainer.m_MissionList;

        //When we want to make tabs:
        //tab for Mission 1, it knows to use the dictionary entry dict["Mission1LeaderBoardName"]
        for (int i = 0; i < missionList.Length; ++i)
        {
            foreach (KeyValuePair<string, GetLeaderboardResult> kvp in results)
            {
                if (kvp.Key != missionList[i].m_playFableaderboardId) continue;

                //Build a title item.
                LeaderboardListItem titleListItem = Instantiate(m_leaderboardTitleObj, m_listRootObj.transform).GetComponent<LeaderboardListItem>();
                titleListItem.SetTitleData($"{missionList[i].m_missionName} Leaders");

                //Build the list item for each player & score in the value for this key.
                foreach (PlayerLeaderboardEntry item in kvp.Value.Leaderboard)
                {
                    string name;
                    bool isMe = false;

                    LeaderboardListItem playerListItem = Instantiate(m_leaderboardPlayerObj, m_listRootObj.transform).GetComponent<LeaderboardListItem>();
                    if (item.DisplayName != null)
                    {
                        isMe = item.DisplayName == PlayFabManager.Instance.m_playerDisplayName;
                        name = item.DisplayName;
                    }
                    else
                    {
                        name = "Unnamed";
                    }

                    playerListItem.SetPlayerData(name, item.Position, item.StatValue, isMe);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    void ClearList()
    {
        for (int i = m_listRootObj.transform.childCount - 1; i >= 0; --i)
        {
            Destroy(m_listRootObj.transform.GetChild(i).gameObject);
        }
    }
}