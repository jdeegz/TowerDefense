using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab.ClientModels;
using UnityEngine;

public class UILeaderboardView : MonoBehaviour
{
    public GameObject m_listRootObj;
    public GameObject m_leaderboardTitleObj;
    public GameObject m_leaderboardPlayerObj;
    public GameObject m_loginRequiredDisplay;


    // Start is called before the first frame update
    void Awake()
    {
        if (PlayFabManager.Instance)
        {
            PlayFabManager.Instance.OnLeaderboardReceived += UpdateLeaderboardView;
            PlayFabManager.Instance.OnLoginComplete += GetLeaderboardData;
            
            if (PlayFabManager.Instance.m_playerProfile != null)
            {
                m_loginRequiredDisplay.SetActive(false);
                GetLeaderboardData();
            }
        }
        else
        {
            m_loginRequiredDisplay.SetActive(true);
        }
    }

    public void GetLeaderboardData()
    {
        PlayFabManager.Instance.GetAllLeaderboards();
    }

    private void UpdateLeaderboardView(bool failed, Dictionary<string, GetLeaderboardResult> results)
    {
        if (failed) return;

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
                    LeaderboardListItem playerListItem = Instantiate(m_leaderboardPlayerObj, m_listRootObj.transform).GetComponent<LeaderboardListItem>();
                    bool isMe = item.DisplayName == PlayFabManager.Instance.m_playerProfile.DisplayName;
                    playerListItem.SetPlayerData(item.DisplayName, item.Position, item.StatValue, isMe);
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