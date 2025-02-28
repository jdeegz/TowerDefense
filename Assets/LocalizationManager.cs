using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [SerializeField] private UIStringData m_defaultLanguageStringData;
    [SerializeField] private List<LanguageEntry> m_languageList;

    private Dictionary<string, UIStringData> m_languageDictionary;
    private UIStringData m_currentLanguage;
    
    public UIStringData CurrentLanguage => m_currentLanguage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLanguageDictionary();
            LoadSavedLanguage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeLanguageDictionary()
    {
        m_languageDictionary = new Dictionary<string, UIStringData>();
        foreach (var entry in m_languageList)
        {
            m_languageDictionary[entry.m_languageName] = entry.m_languageStringData;
        }
    }

    private void LoadSavedLanguage()
    {
        string savedLanguage = GameSettings.SelectedLanguageValue;
        SetLanguage(savedLanguage);
    }

    public void SetLanguage(string requestedLanguage)
    {
        if (m_languageDictionary.TryGetValue(requestedLanguage, out UIStringData newLanguage))
        {
            m_currentLanguage = newLanguage;
            GameSettings.SelectedLanguageValue = requestedLanguage;
        }
        else
        {
            Debug.Log($"Language '{requestedLanguage}' not found.");
        }
    }

    public void ResetToDefaultLanguage()
    {
        SetLanguage("en-US");
    }
}

[Serializable]
public class LanguageEntry
{
    public string m_languageName;
    public UIStringData m_languageStringData;
}
