using System.Text;
using PlayFab;
using PlayFab.ClientModels;
using Steamworks;
using UnityEngine;

public class SteamScript : MonoBehaviour
{
    protected Callback<GetTicketForWebApiResponse_t> m_OnGetSteamAuthTicket;

    // Alternatively, you can use this callback if you choose to call SteamUser.GetAuthSessionTicket(...) instead
    // TicketIsServiceSpecific in the PlayFabLoginRequest should be false in this case
    // protected Callback<GetAuthSessionTicketResponse_t> m_OnGetSteamAuthTicketAlternate;

    private HAuthTicket m_hTicket;

    public void Awake()
    {
        m_OnGetSteamAuthTicket = Callback<GetTicketForWebApiResponse_t>.Create(OnGetSteamAuthTicket);
    }

    void Start()
    {
        Debug.Log("[DEBUG] Steam App ID: " + SteamUtils.GetAppID());
        Debug.Log("[DEBUG] Steam User ID: " + SteamUser.GetSteamID().m_SteamID);
        Debug.Log("[DEBUG] Is Steam User Logged In? " + SteamUser.BLoggedOn());
        Debug.Log("[DEBUG] PlayFab Title ID: " + PlayFabSettings.TitleId);
    }
    
    public void OnGUI()
    {
        if (GUILayout.Button("Log In") && SteamManager.Initialized)
        {
            GetSteamAuthTicket();
        }
        
        if (GUILayout.Button("Send Statistic") && SteamManager.Initialized)
        {
            PlayFabStatsManager.Instance.SendMissionStartStatistic();
        }
    }

    private void GetSteamAuthTicket()
    {
        m_hTicket = SteamUser.GetAuthTicketForWebApi("AzurePlayFab");

        if (m_hTicket == HAuthTicket.Invalid)
        {
            Debug.Log("Failed to request steam auth ticket");
        }
        else
        {
            Debug.Log("Steam auth ticket requested");
        }
    }

    private void OnGetSteamAuthTicket(GetTicketForWebApiResponse_t pCallback)
    {
        Debug.Log("Steam auth ticket callback invoked");

        if (pCallback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to get steam auth ticket: " + pCallback.m_eResult);
            return;
        }

        StringBuilder sb = new();
        for (int i = 0; i < pCallback.m_cubTicket; ++i)
        {
            sb.AppendFormat("{0:x2}", pCallback.m_rgubTicket[i]);
        }

        string steamTicket = sb.ToString();
        Debug.Log($"[DEBUG] Generated Steam Ticket: {steamTicket}");

        var request = new LoginWithSteamRequest
        {
            CreateAccount = true,
            SteamTicket = steamTicket,
            TicketIsServiceSpecific = true  // Try toggling true/false
        };

        Debug.Log($"[DEBUG] Sending PlayFab Login Request with Ticket Length: {steamTicket.Length}");

        PlayFabClientAPI.LoginWithSteam(request, OnComplete, OnFailed);
    }


    private void OnComplete(LoginResult obj)
    {
        SteamUser.CancelAuthTicket(m_hTicket);
        Debug.Log("Success!");
    }

    private void OnFailed(PlayFabError error)
    {
        SteamUser.CancelAuthTicket(m_hTicket);
        Debug.LogError($"[PlayFab ERROR] {error.GenerateErrorReport()}");

        if (error.ErrorDetails != null)
        {
            foreach (var kvp in error.ErrorDetails)
            {
                Debug.LogError($"[ERROR DETAIL] {kvp.Key}: {string.Join(", ", kvp.Value)}");
            }
        }

        if (error.HttpCode == 401)
        {
            Debug.LogError("[PlayFab ERROR] HTTP 401 Unauthorized - Check Web API Key and App ID in PlayFab settings.");
        }
    }
}