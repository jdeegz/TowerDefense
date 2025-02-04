using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIPopupTable", menuName = "ScriptableObjects/UIPopupTable")]
public class UIPopupTable : ScriptableObject
{
    [SerializeField] private List<UIPopupID> m_UIPopupTable = new List<UIPopupID>();
    
    // Internal dictionary for fast lookups (not serialized)
    private Dictionary<string, UIPopup> m_UIPopupDictionary;

    private void OnEnable()
    {
        // Build the dictionary from the serialized list
        m_UIPopupDictionary = new Dictionary<string, UIPopup>();
        foreach (var popupID in m_UIPopupTable)
        {
            if (!m_UIPopupDictionary.ContainsKey(popupID.UIopupID))
            {
                m_UIPopupDictionary.Add(popupID.UIopupID, popupID.UIPopup);
            }
            else
            {
                Debug.Log($"Duplicate UIPopupID detected: {popupID.UIopupID}");
            }
        }
    }

    public UIPopup GetUIPopupByString(string name)
    {
        // Use the dictionary for fast lookups
        if (m_UIPopupDictionary.TryGetValue(name, out UIPopup popup))
        {
            return popup;
        }

        Debug.Log($"No UIPopup found with name: {name}.");
        
        return null;
    }
}

[Serializable]
public class UIPopupID
{
    [SerializeField] private string m_UIopupID = "Popup Name";
    [SerializeField] private UIPopup m_UIPopup;

    public string UIopupID => m_UIopupID;
    public UIPopup UIPopup => m_UIPopup;
}