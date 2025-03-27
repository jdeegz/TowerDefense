using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIPopupManager : MonoBehaviour
{
    [SerializeField] private UIPopupTable m_uiPopupTable;
    private readonly Dictionary<string, UIPopup> m_popupPool = new Dictionary<string, UIPopup>();
    private readonly List<UIPopup> m_activePopups = new List<UIPopup>();

    public static UIPopupManager Instance { get; private set; }

    public static event Action<bool> OnPopupManagerPopupsOpen; // Used to let CombatView if it should disable hotkeys.

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && m_activePopups.Count > 0)
        {
            CloseTopPopup();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && m_activePopups.Count == 0)
        {
            RequestOptionsPopup();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleOutsideClick();
        }
    }

    public T ShowPopup<T>(string UIPopupID, object data = null) where T : UIPopup
    {
        UIPopup popup = GetOrCreatePopup(UIPopupID);
        if (popup == null)
        {
            Debug.Log($"No Popup found with name: {UIPopupID}");
            return null;
        }

        if (m_activePopups.Contains(popup) && !popup.SupportRefresh)
        {
            return popup as T;
        }
        
        popup.HandleShow();

        if (popup is IDataPopup dataPopup)
        {
            dataPopup.SetData(data);
        }

        m_activePopups.Add(popup);

        if (popup.PausesGame) PauseGameplay();

        return popup as T;
    }

    public void HandleOutsideClick()
    {
        if (m_activePopups.Count == 0) return;

        // Get the top-most popup.
        UIPopup topPopup = m_activePopups[^1];
        if (!topPopup.CloseOnOutsideClick) return;

        RectTransform popupRectTransform = topPopup.GetComponent<RectTransform>();

        Vector2 mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(popupRectTransform, Input.mousePosition, null, out mousePosition);

        if (!popupRectTransform.rect.Contains(mousePosition))
        {
            // Close the popup if clicked outside
            ClosePopup(topPopup);
        }
    }

    private UIPopup GetOrCreatePopup(string popupID)
    {
        if (m_popupPool.TryGetValue(popupID, out UIPopup existingPopup))
        {
            return existingPopup;
        }

        UIPopup newPopupPrefab = m_uiPopupTable.GetUIPopupByString(popupID);
        if (newPopupPrefab == null) return null;

        UIPopup newPopup = ObjectPoolManager.SpawnPopup(newPopupPrefab, transform);
        m_popupPool[popupID] = newPopup;

        return newPopup;
    }

    public void CloseTopPopup()
    {
        if (m_activePopups.Count == 0) return;

        UIPopup topPopup = m_activePopups[^1];

        if (!topPopup.CloseOnEscape) return;
        ClosePopup(topPopup);
    }

    public void ClosePopup(UIPopup popup)
    {
        if (popup == null || !m_activePopups.Contains(popup)) return;

        popup.HandleClose();

        m_activePopups.Remove(popup);

        if (m_activePopups.Count == 0)
        {
            ResumeGameplay();
        }
    }

    public void ClosePopup<T>() where T : UIPopup
    {
        UIPopup popupToClose = m_activePopups.FirstOrDefault(p => p is T);

        if (popupToClose != null)
        {
            ClosePopup(popupToClose);
        }
    }

    public void CloseAllPopups()
    {
        for (int i = m_activePopups.Count - 1; i >= 0; i--)
        {
            m_activePopups[i].HandleClose();
        }

        m_activePopups.Clear();

        ResumeGameplay();
    }

    public void PauseGameplay()
    {
        //Debug.Log($"Trying to pause game.");
        OnPopupManagerPopupsOpen?.Invoke(true);
    }

    public void ResumeGameplay()
    {
        //Debug.Log($"Trying to resume game.");
        OnPopupManagerPopupsOpen?.Invoke(false);
    }

    private void RequestOptionsPopup()
    {
        if (GameplayManager.Instance != null)
        {
            if (GameplayManager.Instance.IsWatchingCutscene()) return;

            //Dont allow the menu to OPEN if we're in victory/defeat states and the menu is currently closed.
            if (GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.Victory) return;

            if (GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.Defeat) return;
        }

        ShowPopup<UIOptionsPopup>("OptionsPopup");
    }


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}