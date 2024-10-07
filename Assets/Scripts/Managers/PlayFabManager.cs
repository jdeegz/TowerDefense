using System;
using System.Collections;
using System.Collections.Generic;
using PlayFab.ClientModels;
using PlayFab;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance;
    public Action<bool, Dictionary<string, GetLeaderboardResult>> OnLeaderboardReceived;
    public PlayerProfileModel m_playerProfile;
    public string m_playerDisplayName;
    public event Action OnLoginComplete;
    public event Action OnLoginRequired;
    public event Action OnNamingRequired;

    private List<string> m_leaderboardNames;
    private Dictionary<string, GetLeaderboardResult> m_getLeaderboardResults = new Dictionary<string, GetLeaderboardResult>();
    private bool m_inProgress;
    private bool m_failed;
    private string m_titleId = "B1C48";
    private const string _PlayFabRememberMeIdKey = "PlayFabIDPassGuid";

    private string m_rememberMeId
    {
        get { return PlayerPrefs.GetString(_PlayFabRememberMeIdKey, ""); }
        set
        {
            var guid = string.IsNullOrEmpty(value) ? Guid.NewGuid().ToString() : value;
            PlayerPrefs.SetString(_PlayFabRememberMeIdKey, guid);
        }
    }

    private const string _LogInRememberKey = "PlayFabLoginRemember";

    public bool m_rememberMe
    {
        get { return PlayerPrefs.GetInt(_LogInRememberKey, 0) == 0 ? false : true; }
        set { PlayerPrefs.SetInt(_LogInRememberKey, value ? 1 : 0); }
    }

    private void Awake()
    {
        //Instance = this;

        //Dynamically build the list of leaderboard names so i dont have to update code when we make new or remove leaderboards.
        m_leaderboardNames = new List<string>();
        foreach (MissionData data in GameManager.Instance.m_MissionContainer.m_MissionList)
        {
            if (string.IsNullOrEmpty(data.m_playFableaderboardId))
            {
                continue;
            }

            m_leaderboardNames.Add(data.m_playFableaderboardId);
        }
    }

    // Update is called once per frame
    void Start()
    {
        //Debug.Log($"Remember Me: {m_rememberMe}");
        //Debug.Log($"Remember Me Key: {PlayerPrefs.GetString(_PlayFabRememberMeIdKey)}");

        //Try to log in.
        if (m_rememberMe && !string.IsNullOrEmpty(m_rememberMeId))
        {
            Debug.Log($"Attempting to Auto-log in.");
            PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
            {
                TitleId = m_titleId,
                CustomId = m_rememberMeId,
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true
                }
            }, RememberMeLoginSuccess, OnError);
        }
        else
        {
            OnLoginRequired?.Invoke();
        }
    }

    private void RememberMeLoginSuccess(LoginResult result)
    {
        CompleteLogin(result.InfoResultPayload.PlayerProfile);
    }

    void OnError(PlayFabError error)
    {
        //Debug.Log($"Login Failure.");
        Debug.Log($"{error.GenerateErrorReport()}");
    }
    
    public void SendLeaderboard(string leaderboardName, int score)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = leaderboardName,
                    Value = score
                }
            }
        };
        PlayFabClientAPI.UpdatePlayerStatistics(request, OnLeaderboardUpdate, OnError);
    }

    void OnLeaderboardUpdate(UpdatePlayerStatisticsResult result)
    {
        Debug.Log($"Successful Leaderboard sent.");
    }

    public void GetLeaderboard(string leaderboardName)
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = leaderboardName,
            StartPosition = 0,
            MaxResultsCount = 10
        };
        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardGet, OnError);
    }

    void OnLeaderboardGet(GetLeaderboardResult result)
    {
        foreach (var item in result.Leaderboard)
        {
            Debug.Log($"{item.Position} {item.PlayFabId} {item.StatValue}");
        }
    }

    public void GetAllLeaderboards()
    {
        if (m_inProgress)
        {
            return;
        }

        m_inProgress = true;
        m_failed = false;
        if (m_getLeaderboardResults != null) m_getLeaderboardResults.Clear();

        foreach (String leaderboardName in m_leaderboardNames)
        {
            var request = new GetLeaderboardRequest
            {
                StatisticName = leaderboardName,
                StartPosition = 0,
                MaxResultsCount = 10
            };

            PlayFabClientAPI.GetLeaderboard(
                request,
                (result) => OnAllLeaderboardGet(result, leaderboardName),
                (error) => OnErrorGetLeaderboard(error, leaderboardName));
        }
    }

    private void OnAllLeaderboardGet(GetLeaderboardResult result, string leaderboardName)
    {
        m_getLeaderboardResults[leaderboardName] = result;

        CheckFinished();
    }

    private void OnErrorGetLeaderboard(PlayFabError error, string leaderboardName)
    {
        m_failed = true;
        m_getLeaderboardResults[leaderboardName] = null;

        Debug.Log($"Error getting leaderboard {leaderboardName}.");
        Debug.Log($"{error.GenerateErrorReport()}");
        CheckFinished();
    }

    private void CheckFinished()
    {
        if (m_getLeaderboardResults.Count == m_leaderboardNames.Count)
        {
            OnLeaderboardReceived(m_failed, m_getLeaderboardResults);
            m_inProgress = false;
        }
    }

    public void CheatFillLeaderboards()
    {
        foreach (String leaderboardName in m_leaderboardNames)
        {
            for (int i = 0; i < 10; ++i)
            {
                int score = Random.Range(1, 99);
                SendLeaderboard(leaderboardName, score);
            }
        }
    }

    public void CompleteLogin(PlayerProfileModel profile)
    {
        Debug.Log($"Check for profile name: {profile.DisplayName}");
        m_playerDisplayName = profile.DisplayName;
        if (m_playerDisplayName != null)
        {
            Debug.Log($"Logged in as {m_playerDisplayName}.");
            OnLoginComplete?.Invoke();
        }
        else
        {
            Debug.Log($"Naming Required.");
            OnNamingRequired?.Invoke();
        }
    }

    public void RequestRegistration(string email, string password, bool rememberMe, Action<RegisterPlayFabUserResult> onRegistrationSuccess, Action<PlayFabError> onRegistrationError)
    {
        var request = new RegisterPlayFabUserRequest()
        {
            Email = email,
            Password = password,
            RequireBothUsernameAndEmail = false,
        };
        PlayFabClientAPI.RegisterPlayFabUser(request,

            //On Success
            (result) =>
            {
                m_rememberMe = rememberMe;
                if (rememberMe)
                {
                    m_rememberMeId = Guid.NewGuid().ToString();

                    PlayFabClientAPI.LinkCustomID(new LinkCustomIDRequest()
                    {
                        CustomId = m_rememberMeId,
                        ForceLink = false //Unsure what this should actually be. In the demo i think it was false.
                    }, null, null);
                }

                onRegistrationSuccess.Invoke(result);
            },

            //On Error
            onRegistrationError);
    }

    public void RequestLogin(string email, string password, bool rememberMe, Action<LoginResult> onLoginSuccess, Action<PlayFabError> onError)
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };

        PlayFabClientAPI.LoginWithEmailAddress(request,
            //On Success
            (result) =>
            {
                m_rememberMe = rememberMe;
                if (rememberMe)
                {
                    m_rememberMeId = Guid.NewGuid().ToString();

                    PlayFabClientAPI.LinkCustomID(new LinkCustomIDRequest()
                    {
                        CustomId = m_rememberMeId,
                        ForceLink = false //Unsure what this should actually be. In the demo i think it was false.
                    }, null, null);
                }

                onLoginSuccess.Invoke(result);
            },

            //On Error
            onError);
    }

    public void RequestResetPassword(string email, Action<SendAccountRecoveryEmailResult> onPasswordReset, Action<PlayFabError> onError)
    {
        var request = new SendAccountRecoveryEmailRequest
        {
            Email = email,
            TitleId = m_titleId
        };
        PlayFabClientAPI.SendAccountRecoveryEmail(request, onPasswordReset, onError);
    }

    public void GetPlayerProfile(string playFabId, Action<GetPlayerProfileResult> onGetPlayerProfile)
    {
        // Specify the request to get player profile
        GetPlayerProfileRequest request = new GetPlayerProfileRequest
        {
            PlayFabId = playFabId,
            ProfileConstraints = new PlayerProfileViewConstraints()
        };

        // Call the PlayFab API to get player profile
        PlayFabClientAPI.GetPlayerProfile(request, onGetPlayerProfile, OnGetProfileError);
    }

    void OnGetProfileError(PlayFabError error)
    {
        //Debug.Log($"Login Failure.");
        Debug.Log($"{error.GenerateErrorReport()}");
    }

    public void RequestDisplayNameUpdate(string name, Action<UpdateUserTitleDisplayNameResult> onDisplayNameUpdate, Action<PlayFabError> onDisplayNameError)
    {
        m_playerDisplayName = name;
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = name
        };
        PlayFabClientAPI.UpdateUserTitleDisplayName(request, onDisplayNameUpdate, onDisplayNameError);
    }

    public void RequestLogout()
    {
        PlayFabClientAPI.ForgetAllCredentials();
        OnLoginRequired?.Invoke();
    }
}