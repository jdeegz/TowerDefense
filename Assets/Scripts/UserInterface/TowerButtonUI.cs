using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerButtonUI : MonoBehaviour
{
    public enum ButtonState
    {
        CanBuild,
        IsSelected,
        CannotBuild,
    }

    public ButtonState m_buttonState;
    private UIManager m_uiManager;
    
    public GameObject m_preconstructedTower;
    [SerializeField] private Button m_button;
    [SerializeField] private GameObject m_selectedVisuals;

    private void Awake()
    {
        m_uiManager = FindObjectOfType<UIManager>();
        m_button = gameObject.GetComponent<Button>();
        m_button.onClick.AddListener(OnButtonClick);
    }

    public void OnButtonClick()
    {
        m_uiManager.SelectTower(this);
    }

    private void Update()
    {
        switch (m_buttonState)
        {
            case ButtonState.CanBuild:
                m_selectedVisuals.SetActive(false);
                break;
            case ButtonState.CannotBuild:
                m_selectedVisuals.SetActive(false);
                break;
            case ButtonState.IsSelected:
                m_selectedVisuals.SetActive(true);
                break;
        }
    }

    public void SetButtonState(ButtonState state)
    {
        m_buttonState = state;
    }
}
