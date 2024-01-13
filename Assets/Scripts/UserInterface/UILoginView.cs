using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UILoginView : MonoBehaviour
{
    public enum LoginState
    {
        Incomplete,
        Complete,
        Registration,
        Login,
        Naming
    }

    public LoginState m_loginState = LoginState.Incomplete;

    [Header("Registration Objects")]
    public GameObject m_registrationDialog;
    public TextMeshProUGUI m_registrationMessageText;
    public TMP_InputField m_registrationEmailInput;
    public TMP_InputField m_registrationPasswordInput;
    public Button m_registrationSubmitButton;
    public Button m_showLoginButton;
    
    [Header("Login Objects")]
    public GameObject m_loginDialog;
    public TextMeshProUGUI m_loginMessageText;
    public TMP_InputField m_loginEmailInput;
    public TMP_InputField m_loginPasswordInput;
    public Button m_loginSubmitButton;
    public Button m_loginResetPasswordButton;
    public Button m_showRegistrationButton;

    [Header("Naming Objects")]
    public GameObject m_nameDialog;
    public TextMeshProUGUI m_namingMessageLabel;
    public TMP_InputField m_displayNameInput;
    public Button m_namingSubmitButton;
    
    //Show Registration view on start.
    void Start()
    {
        m_registrationSubmitButton.onClick.AddListener(RegisterButton);
        m_showLoginButton.onClick.AddListener(ShowLoginDialog);
        
        m_loginSubmitButton.onClick.AddListener(LoginButton);
        m_loginResetPasswordButton.onClick.AddListener(ResetPasswordButton);
        m_showRegistrationButton.onClick.AddListener(ShowRegistrationDialog);
        
        m_namingSubmitButton.onClick.AddListener(SubmitNameButton);
        UpdateLoginState(LoginState.Registration);
    }

    void UpdateLoginState(LoginState newState)
    {
        m_loginState = newState;
        
        m_registrationDialog.SetActive(m_loginState == LoginState.Registration);
        m_loginDialog.SetActive(m_loginState == LoginState.Login);
        m_nameDialog.SetActive(m_loginState == LoginState.Naming);
        gameObject.SetActive(m_loginState != LoginState.Complete);
    }

    //Functions for Buttons to swap to different views.
    public void ShowRegistrationDialog()
    {
        UpdateLoginState(LoginState.Registration);
    }

    public void ShowLoginDialog()
    {
        UpdateLoginState(LoginState.Login);
    }
    
    public void RegisterButton()
    {
        if (m_registrationPasswordInput.text.Length < 6)
        {
            m_registrationMessageText.SetText("Password too short.");
            return;
        }

        if (!(m_registrationEmailInput.text.IndexOf('@') > 0))
        {
            m_registrationMessageText.SetText("Invalid Email.");
            return;
        }

        var request = new RegisterPlayFabUserRequest()
        {
            Email = m_registrationEmailInput.text,
            Password = m_registrationPasswordInput.text,
            RequireBothUsernameAndEmail = false,
        };
        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnRegisterError);
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        //If we've successfully registered a new account, we now want to name it.
        Debug.Log($"Registration Success.");
        UpdateLoginState(LoginState.Naming);
    }

    private void OnRegisterError(PlayFabError error)
    {
        Debug.Log($"{error.GenerateErrorReport()}");
        m_registrationMessageText.SetText(error.ErrorMessage);
    }

    public void LoginButton()
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = m_loginEmailInput.text,
            Password = m_loginPasswordInput.text,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnError);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log($"Login Success.");
        string name = null;
        if (result.InfoResultPayload.PlayerProfile != null)
        {
            name = result.InfoResultPayload.PlayerProfile.DisplayName;
            Debug.Log($"Logged in as {name}.");
        }

        if (name == null)
        {
            UpdateLoginState(LoginState.Naming);
        }
        else
        {
            UpdateLoginState(LoginState.Complete);
        }
    }

    public void ResetPasswordButton()
    {
        var request = new SendAccountRecoveryEmailRequest
        {
            Email = m_registrationEmailInput.text,
            TitleId = "B1C48"
        };
        PlayFabClientAPI.SendAccountRecoveryEmail(request, OnPasswordReset, OnError);
    }

    private void OnPasswordReset(SendAccountRecoveryEmailResult obj)
    {
        m_registrationMessageText.SetText("Password Reset sent to email.");
    }

    public void SubmitNameButton()
    {
        //Check length
        if (m_displayNameInput.text.Length < 3)
        {
            m_namingMessageLabel.SetText("Name not long enough.");
            return;
        }
        
        //Check Characters
        if (!CheckIsNonAlphanumeric(m_displayNameInput.text))
        {
            m_namingMessageLabel.SetText("Name cannot include special characters.");
            return;
        }
        
        
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = m_displayNameInput.text
        };
        PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnDisplayNameUpdate, OnDisplayNameError);
    }
    
    private void OnDisplayNameUpdate(UpdateUserTitleDisplayNameResult result)
    {
        Debug.Log($"{result.DisplayName} submitted.");
        UpdateLoginState(LoginState.Complete);
    }

    private void OnDisplayNameError(PlayFabError error)
    {
        Debug.Log($"{error.GenerateErrorReport()}");
        m_namingMessageLabel.SetText(error.ErrorMessage);
    }
    public bool CheckIsNonAlphanumeric(string input)
    {
        // Define a regular expression pattern to match non-alphanumeric characters
        string pattern = @"[^a-zA-Z0-9]";

        // Use Regex.IsMatch to check if the input contains non-alphanumeric characters
        if (Regex.IsMatch(input, pattern))
        {
            // Log a message if non-alphanumeric characters are found
            Debug.Log("String contains non-alphanumeric characters");
            return false;
        }
        else
        {
            // Log a message if the string is alphanumeric
            Debug.Log("String is alphanumeric");
            return true;
        }
    }

    private void OnError(PlayFabError error)
    {
        Debug.Log($"{error.GenerateErrorReport()}");
    }
}