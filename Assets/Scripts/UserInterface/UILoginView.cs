using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Registration Objects")] public GameObject m_registrationDialog;
    public TMP_InputField[] m_registrationInputFields;
    public TextMeshProUGUI m_registrationMessageText;
    public TMP_InputField m_registrationEmailInput;
    public TMP_InputField m_registrationPasswordInput;
    public TMP_InputField m_registrationConfirmationPasswordInput;
    public Toggle m_registrationRememberMeToggle;
    public Button m_registrationSubmitButton;
    public Button m_showLoginButton;

    [Header("Login Objects")] public GameObject m_loginDialog;
    public TMP_InputField[] m_loginInputFields;
    public TextMeshProUGUI m_loginMessageText;
    public TMP_InputField m_loginEmailInput;
    public TMP_InputField m_loginPasswordInput;
    public Toggle m_loginRememberMeToggle;
    public Button m_loginSubmitButton;
    public Button m_loginResetPasswordButton;
    public Button m_showRegistrationButton;

    [Header("Naming Objects")] public GameObject m_nameDialog;
    public TextMeshProUGUI m_namingMessageLabel;
    public TMP_InputField m_displayNameInput;
    public Button m_namingSubmitButton;

    private string m_localId;

    private EventSystem eventSystem;
    private TMP_InputField[] m_curInputFields;

    private void Update()
    {
        HandleTab();

        HandleEnter();
    }

    private void HandleEnter()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            switch (m_loginState)
            {
                case LoginState.Incomplete:
                    break;
                case LoginState.Complete:
                    break;
                case LoginState.Registration:
                    RegisterButton();
                    break;
                case LoginState.Login:
                    LoginButton();
                    break;
                case LoginState.Naming:
                    SubmitNameButton();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    void HandleTab()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                for (int i = 0; i < m_curInputFields.Length; i++)
                {
                    if (m_curInputFields[i].gameObject == eventSystem.currentSelectedGameObject)
                    {
                        // Calculate the next index, handling negative values
                        int nextIndex = (i - 1 + m_curInputFields.Length) % m_curInputFields.Length;
                        SelectInputField(m_curInputFields[nextIndex]);
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_curInputFields.Length; i++)
                {
                    if (m_curInputFields[i].gameObject == eventSystem.currentSelectedGameObject)
                    {
                        int nextIndex = (i + 1) % m_curInputFields.Length;
                        SelectInputField(m_curInputFields[nextIndex]);
                        break;
                    }
                }
            }
        }
    }
    
    void SelectInputField(TMP_InputField inputField)
    {
        if (inputField != null)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }
    }
    
    void Awake()
    {
        MenuManager.OnMenuStateChanged += MenuStateChanged;

        //Check to see if we have a playfab manager, and if it's logged in.
        if (PlayFabManager.Instance)
        {
            PlayFabManager.Instance.OnLoginComplete += LoginComplete;
            PlayFabManager.Instance.OnLoginRequired += LoginRequired;
            PlayFabManager.Instance.OnNamingRequired += NamingRequired;
        }
    }

    private void MenuStateChanged(MenuManager.MenuState newState)
    {
        if (newState != MenuManager.MenuState.Login) return;

        //If we dont have a playfab manager, skip to regular game.
        if (!PlayFabManager.Instance)
        {
            Debug.Log($"Client Log In Completed.");
            UpdateLoginState(LoginState.Complete);
            return;
        }

        //Is this super jank? Probably.
        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.Log($"Client is logged in, skipping Log In.");
            LoginComplete();
        }
        else
        {
            Debug.Log($"Client Log In required.");
            LoginRequired();
        }
    }

    void Start()
    {
        eventSystem = FindObjectOfType<EventSystem>();
        m_registrationSubmitButton.onClick.AddListener(RegisterButton);
        m_showLoginButton.onClick.AddListener(ShowLoginDialog);

        m_loginSubmitButton.onClick.AddListener(LoginButton);
        m_loginResetPasswordButton.onClick.AddListener(ResetPasswordButton);
        m_showRegistrationButton.onClick.AddListener(ShowRegistrationDialog);

        m_namingSubmitButton.onClick.AddListener(SubmitNameButton);
    }

    private void LoginRequired()
    {
        UpdateLoginState(LoginState.Registration);
    }

    private void NamingRequired()
    {
        UpdateLoginState(LoginState.Naming);
    }

    private void LoginComplete()
    {
        Debug.Log($"Log in Complete.");
        MenuManager.Instance.UpdateMenuState(MenuManager.MenuState.StartMenu);
    }

    void UpdateLoginState(LoginState newState)
    {
        m_loginState = newState;

        m_registrationDialog.SetActive(m_loginState == LoginState.Registration);
        m_loginDialog.SetActive(m_loginState == LoginState.Login);
        m_nameDialog.SetActive(m_loginState == LoginState.Naming);

        switch (newState)
        {
            case LoginState.Incomplete:
                break;
            case LoginState.Complete:
                break;
            case LoginState.Registration:
                m_curInputFields = m_registrationInputFields;
                break;
            case LoginState.Login:
                m_curInputFields = m_loginInputFields;
                break;
            case LoginState.Naming:
                break;
            default:
                Debug.Log($"UpdateLoginState: {newState}.");
                break;
        }

    //Debug.Log($"Login State: {m_loginState}");
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
        //Check Email Validity.
        if (m_registrationPasswordInput.text.Length < 1)
        {
            m_registrationMessageText.SetText("Invalid Email.");
            return;
        }

        if (!(m_registrationEmailInput.text.IndexOf('@') > 0))
        {
            m_registrationMessageText.SetText("Invalid Email.");
            return;
        }

        //Check Password Validity.
        if (m_registrationPasswordInput.text.Length < 6)
        {
            m_registrationMessageText.SetText("Password too short.");
            return;
        }

        if (m_registrationPasswordInput.text != m_registrationConfirmationPasswordInput.text)
        {
            m_registrationMessageText.SetText("Your passwords do not match.");
            return;
        }

        PlayFabManager.Instance.RequestRegistration(m_registrationEmailInput.text, m_registrationPasswordInput.text, m_registrationRememberMeToggle.isOn, OnRegisterSuccess, OnRegisterError);
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        //If we've successfully registered a new account, we now want to name it.
        Debug.Log($"Registration Success.");
        m_localId = result.PlayFabId;

        //We assume a new account needs a new name.
        NamingRequired();
    }

    private void OnGetPlayerProfile(GetPlayerProfileResult result)
    {
        PlayFabManager.Instance.CompleteLogin(result.PlayerProfile);
    }

    private void OnRegisterError(PlayFabError error)
    {
        Debug.Log($"{error.GenerateErrorReport()}");
        m_registrationMessageText.SetText(error.ErrorMessage);
    }

    public void LoginButton()
    {
        PlayFabManager.Instance.RequestLogin(m_loginEmailInput.text, m_loginPasswordInput.text, m_loginRememberMeToggle.isOn, OnLoginSuccess, OnLoginError);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log($"Login Success.");
        m_localId = result.PlayFabId;
        PlayFabManager.Instance.CompleteLogin(result.InfoResultPayload.PlayerProfile);
    }

    private void OnLoginError(PlayFabError error)
    {
        Debug.Log($"{error.GenerateErrorReport()}");
        m_loginMessageText.SetText(error.ErrorMessage);
    }

    public void ResetPasswordButton()
    {
        PlayFabManager.Instance.RequestResetPassword(m_registrationEmailInput.text, OnPasswordReset, OnError);
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

        if (m_displayNameInput.text.Length > 13)
        {
            m_namingMessageLabel.SetText("Name too long.");
            return;
        }

        //Check Characters
        if (!CheckIsNonAlphanumeric(m_displayNameInput.text))
        {
            m_namingMessageLabel.SetText("Name cannot include special characters.");
            return;
        }

        PlayFabManager.Instance.RequestDisplayNameUpdate(m_displayNameInput.text, OnDisplayNameUpdate, OnDisplayNameError);
    }

    private void OnDisplayNameUpdate(UpdateUserTitleDisplayNameResult result)
    {
        Debug.Log($"{result.DisplayName} submitted.");
        PlayFabManager.Instance.GetPlayerProfile(m_localId, (result) => PlayFabManager.Instance.CompleteLogin(result.PlayerProfile));
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

    void OnDestroy()
    {
        MenuManager.OnMenuStateChanged -= MenuStateChanged;

        if (PlayFabManager.Instance)
        {
            PlayFabManager.Instance.OnLoginComplete -= LoginComplete;
            PlayFabManager.Instance.OnLoginRequired -= LoginRequired;
            PlayFabManager.Instance.OnNamingRequired -= NamingRequired;
        }
    }
}