using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISetupView : MonoBehaviour
{
    [SerializeField] private Button m_readyButton;
    
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
        //
    }

    void Start()
    {
        m_readyButton.onClick.AddListener(OnReadyButtonClicked);
    }

    private void OnReadyButtonClicked()
    {
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Build);
        GameplayManager.Instance.UpdateInteractionState(GameplayManager.InteractionState.Idle);
        ResourceManager.Instance.StartDepositTimer();
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
