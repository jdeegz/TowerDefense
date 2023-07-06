using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIStartView : MonoBehaviour
{
    [SerializeField] private Button m_MissionSelectionButton;
    void Awake()
    {
        MenuManager.OnMenuStateChanged += MenuManagerStateChanged;
    }
    void OnDestroy()
    {
        MenuManager.OnMenuStateChanged -= MenuManagerStateChanged;
    }

    private void MenuManagerStateChanged(MenuManager.MenuState state)
    {
        gameObject.SetActive(state == MenuManager.MenuState.StartMenu);
    }
    
    void Start()
    {
        m_MissionSelectionButton.onClick.AddListener(OnMissionSelectionButtonClick);
    }

    private void OnMissionSelectionButtonClick()
    {
        MenuManager.Instance.UpdateMenuState(MenuManager.MenuState.MissionSelect);
    }
}