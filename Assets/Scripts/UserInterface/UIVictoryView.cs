using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIVictoryView : MonoBehaviour
{
    [SerializeField] private Button m_exitButton;
    
    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState state)
    {
        gameObject.SetActive(state == GameplayManager.GameplayState.Victory);
    }
    
    void Start()
    {
        m_exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    private void OnExitButtonClicked()
    {
        GameManager.Instance.RequestChangeScene("Menus", GameManager.GameState.Menus);
    }
}
