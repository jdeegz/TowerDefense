using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICombatView : MonoBehaviour
{
    [SerializeField] private Button m_pauseButton;
    [SerializeField] private Button m_playButton;
    [SerializeField] private Button m_nextWaveButton;
    [SerializeField] private Button m_exitMapButton;
    [SerializeField] private TextMeshProUGUI m_stoneBankLabel;
    [SerializeField] private TextMeshProUGUI m_woodBankLabel;
    [SerializeField] private TextMeshProUGUI m_stoneGathererLabel;
    [SerializeField] private TextMeshProUGUI m_woodGathererLabel;
    [SerializeField] private TextMeshProUGUI m_gameClockLabel;
    [SerializeField] private TextMeshProUGUI m_waveLabel;
    [SerializeField] private TextMeshProUGUI m_castleHealthLabel;
    [SerializeField] private TextMeshProUGUI m_debugInfoLabel;
    [SerializeField] private GameObject m_towerTrayButtonPrefab;
    [SerializeField] private RectTransform m_towerTrayLayoutObj;
    [SerializeField] private GameObject m_alertRootObj;
    [SerializeField] private GameObject m_alertPrefab;
    [SerializeField] private GameObject m_pausedDisplayObj;
    private float m_timeToNextWave;
    private int m_curCastleHealth;
    private int m_maxCastleHealth;
    private List<Button> m_buttons;

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        GameplayManager.OnGameSpeedChanged += GameplaySpeedChanged;
        GameplayManager.OnAlertDisplayed += Alert;
        ResourceManager.UpdateStoneBank += UpdateStoneDisplay;
        ResourceManager.UpdateWoodBank += UpdateWoodDisplay;
        ResourceManager.UpdateStoneGathererCount += UpdateStoneGathererDisplay;
        ResourceManager.UpdateWoodGathererCount += UpdateWoodGathererDisplay;
    }

    private void UpdateCastleHealthDisplay(int i)
    {
        m_curCastleHealth += i;
        m_castleHealthLabel.SetText("Castle Health: " + m_curCastleHealth + "/" + m_maxCastleHealth);
    }

    private void UpdateWoodGathererDisplay(int i)
    {
        m_woodGathererLabel.SetText(i.ToString());
    }

    private void UpdateStoneGathererDisplay(int i)
    {
        m_stoneGathererLabel.SetText(i.ToString());
    }

    private void UpdateWoodDisplay(int i)
    {
        m_woodBankLabel.SetText(i.ToString());
    }

    private void UpdateStoneDisplay(int i)
    {
        m_stoneBankLabel.SetText(i.ToString());
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
        GameplayManager.OnGameSpeedChanged -= GameplaySpeedChanged;
        GameplayManager.OnAlertDisplayed -= Alert;
        ResourceManager.UpdateStoneBank -= UpdateStoneDisplay;
        ResourceManager.UpdateWoodBank -= UpdateWoodDisplay;
        ResourceManager.UpdateStoneGathererCount -= UpdateStoneGathererDisplay;
        ResourceManager.UpdateWoodGathererCount -= UpdateWoodGathererDisplay;
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState state)
    {
        switch (state)
        {
            case GameplayManager.GameplayState.Setup:
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
                m_waveLabel.SetText("Wave: " + (GameplayManager.Instance.m_wave + 1));
                break;
            case GameplayManager.GameplayState.Combat:
                break;
            case GameplayManager.GameplayState.Build:
                m_timeToNextWave = GameplayManager.Instance.m_buildDuration;
                break;
            case GameplayManager.GameplayState.Paused:
                break;
            case GameplayManager.GameplayState.Victory:
                Time.timeScale = 0;
                break;
            case GameplayManager.GameplayState.Defeat:
                Time.timeScale = 0;
                break;
            default:
                break;
        }

        gameObject.SetActive(state != GameplayManager.GameplayState.Setup);
        m_nextWaveButton.gameObject.SetActive(state == GameplayManager.GameplayState.Build);
    }

    private void GameplaySpeedChanged(GameplayManager.GameSpeed newSpeed)
    {
        switch (newSpeed)
        {
            case GameplayManager.GameSpeed.Paused:
                foreach (Button button in m_buttons)
                {
                    button.interactable = false;
                }
                break;
            case GameplayManager.GameSpeed.Normal:
                foreach (Button button in m_buttons)
                {
                    button.interactable = true;
                }
                break;
            case GameplayManager.GameSpeed.Fast:
                foreach (Button button in m_buttons)
                {
                    button.interactable = true;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newSpeed), newSpeed, null);
        }


        m_playButton.gameObject.SetActive(newSpeed == GameplayManager.GameSpeed.Paused);
        m_pausedDisplayObj.gameObject.SetActive(newSpeed == GameplayManager.GameSpeed.Paused);

        m_pauseButton.gameObject.SetActive(newSpeed != GameplayManager.GameSpeed.Paused);
    }

    // Start is called before the first frame update
    void Start()
    {
        GameplayManager.Instance.m_castleController.UpdateHealth += UpdateCastleHealthDisplay;
        m_maxCastleHealth = GameplayManager.Instance.m_castleController.m_maxHealth;
        m_curCastleHealth = m_maxCastleHealth;
        m_castleHealthLabel.SetText("Castle Health: " + m_curCastleHealth + "/" + m_maxCastleHealth);
        m_woodGathererLabel.SetText("0");
        m_stoneGathererLabel.SetText("0");
        m_woodBankLabel.SetText("0");
        m_stoneBankLabel.SetText("0");

        m_pauseButton.onClick.AddListener(OnPauseButtonClicked);
        m_playButton.onClick.AddListener(OnPlayButtonClicked);
        m_nextWaveButton.onClick.AddListener(OnNextWaveButtonClicked);
        m_exitMapButton.onClick.AddListener(OnExitButtonClicked);
        
        if (m_buttons == null)
        {
            m_buttons = new List<Button>();
        }
        
        m_buttons.Add(m_nextWaveButton);

        BuildTowerTrayDisplay();
    }

    private void BuildTowerTrayDisplay()
    {
        for (int i = 0; i < GameplayManager.Instance.m_equippedTowers.Length; ++i)
        {
            GameObject buttonPrefab = Instantiate(m_towerTrayButtonPrefab, m_towerTrayLayoutObj);
            TowerTrayButton towerTrayButtonScript = buttonPrefab.GetComponent<TowerTrayButton>();

            if (towerTrayButtonScript)
            {
                towerTrayButtonScript.SetupData(GameplayManager.Instance.m_equippedTowers[i], i);
            }

            Button buttonScript = buttonPrefab.GetComponent<Button>();
            
            if (buttonScript)
            {
                m_buttons.Add(buttonScript);
            }
        }
    }

    private void Alert(string text)
    {
        GameObject curAlert = Instantiate(m_alertPrefab, m_alertRootObj.transform);
        curAlert.GetComponent<UIAlert>().SetLabelText(text);
    }

    private void OnPlayButtonClicked()
    {
        GameplayManager.Instance.UpdateGameSpeed(GameplayManager.GameSpeed.Normal);
    }

    private void OnPauseButtonClicked()
    {
        GameplayManager.Instance.UpdateGameSpeed(GameplayManager.GameSpeed.Paused);
    }

    private void OnNextWaveButtonClicked()
    {
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.SpawnEnemies);
    }
    
    private void OnExitButtonClicked()
    {
        GameManager.Instance.RequestChangeScene("Menus", GameManager.GameState.Menus);
    }

    // Update is called once per frame
    void Update()
    {
        m_timeToNextWave -= Time.deltaTime;
        TimeSpan timeSpan = TimeSpan.FromSeconds(m_timeToNextWave);
        string formattedTime =
            string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        m_gameClockLabel.SetText(formattedTime);

        DebugMenu();
    }


    void DebugMenu()
    {
        //Debug Info
        //Hovered Obj
        Selectable hoveredOjb = GameplayManager.Instance.m_hoveredSelectable;
        string hoveredObjString = "Null";


        //Precon Tower Position
        Vector2Int preconTowerPos = GameplayManager.Instance.m_preconstructedTowerPos;
        string preconTowerPosString = "0,0";


        //Can Afford
        string canAffordString = "N/A";
        //Can Place
        string canPlaceString = "N/A";
        //Interaction State
        String interactionStateString = GameplayManager.Instance.m_interactionState.ToString();

        GameplayManager.InteractionState interactionState = GameplayManager.Instance.m_interactionState;
        if (interactionState == GameplayManager.InteractionState.PreconstructionTower)
        {
            if (hoveredOjb != null)
            {
                hoveredObjString = hoveredOjb.name;
            }

            if (preconTowerPos != null)
            {
                preconTowerPosString = GameplayManager.Instance.m_preconstructedTowerPos.ToString();
            }

            canAffordString = GameplayManager.Instance.m_canAfford.ToString();
            canPlaceString = GameplayManager.Instance.m_canPlace.ToString();
        }

        string debugInfo = string.Format("Hovered Obj: {0}<br>" +
                                         "Preconstructed Tower Pos: {1}<br>" +
                                         "Can Afford: {2}<br>" +
                                         "Can Place: {3}<br>" +
                                         "Interaction State: {4}", hoveredObjString, preconTowerPosString,
            canAffordString,
            canPlaceString, interactionStateString);
        m_debugInfoLabel.SetText(debugInfo);
    }
}