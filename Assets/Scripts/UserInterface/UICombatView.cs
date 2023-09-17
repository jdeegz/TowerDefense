using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UICombatView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button m_pauseButton;
    [SerializeField] private Button m_playButton;
    [SerializeField] private Button m_nextWaveButton;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI m_stoneBankLabel;
    [SerializeField] private TextMeshProUGUI m_woodBankLabel;
    [SerializeField] private TextMeshProUGUI m_stoneGathererLabel;
    [SerializeField] private TextMeshProUGUI m_woodGathererLabel;
    [SerializeField] private TextMeshProUGUI m_gameClockLabel;
    [SerializeField] private TextMeshProUGUI m_waveLabel;
    [SerializeField] private TextMeshProUGUI m_castleHealthLabel;
    [SerializeField] private TextMeshProUGUI m_debugInfoLabel;

    [Header("Objects")]
    [SerializeField] private GameObject m_towerTrayButtonPrefab;
    [SerializeField] private GameObject m_alertRootObj;
    [SerializeField] private GameObject m_alertPrefab;
    [SerializeField] private GameObject m_pausedDisplayObj;

    [Header("Rect Transforms")]
    [SerializeField] private RectTransform m_towerTrayLayoutObj;
    [SerializeField] private RectTransform m_healthDisplay;
    [SerializeField] private RectTransform m_woodBankDisplay;
    [SerializeField] private RectTransform m_stoneBankDisplay;
    [SerializeField] private RectTransform m_woodGathererDisplay;
    [SerializeField] private RectTransform m_stoneGathererDisplay;


    private float m_timeToNextWave;
    private int m_curCastleHealth;
    private int m_maxCastleHealth;
    private List<Button> m_buttons;
    private Tween m_healthShake;
    private Tween m_woodBankShake;
    private Tween m_stoneBankShake;
    private Tween m_woodGathererShake;
    private Tween m_stoneGathererShake;

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        GameplayManager.OnGameSpeedChanged += GameplaySpeedChanged;
        GameplayManager.OnAlertDisplayed += Alert;
        ResourceManager.UpdateStoneBank += UpdateStoneDisplay;
        ResourceManager.UpdateWoodBank += UpdateWoodDisplay;
        ResourceManager.UpdateStoneGathererCount += UpdateStoneGathererDisplay;
        ResourceManager.UpdateWoodGathererCount += UpdateWoodGathererDisplay;
        gameObject.SetActive(false);
    }

    private void UpdateCastleHealthDisplay(int i)
    {
        if (m_healthShake.IsActive())
        {
            m_healthShake.Kill();
        }

        m_curCastleHealth += i;
        m_castleHealthLabel.SetText($"{m_curCastleHealth}/{m_maxCastleHealth}<sprite name=\"ResourceHealth\">");
        m_healthShake = m_healthDisplay.DOPunchScale(new Vector3(0.15f, 0.3f, 0f), 0.3f, 1, .7f).SetAutoKill(true);
        m_healthShake.Play();
    }

    private void UpdateWoodGathererDisplay(int i)
    {
        if (m_woodGathererShake.IsActive())
        {
            m_woodGathererShake.Kill();
        }
        m_woodGathererLabel.SetText(i.ToString());
        m_woodGathererShake = m_woodGathererDisplay.DOPunchScale(new Vector3(0.15f, 0.3f, 0f), 0.3f, 1, .7f).SetAutoKill(true);
        m_woodGathererShake.Play();
    }

    private void UpdateStoneGathererDisplay(int i)
    {
        if (m_stoneGathererShake.IsActive())
        {
            m_stoneGathererShake.Kill();
        }
        m_stoneGathererLabel.SetText(i.ToString());
        m_stoneGathererShake = m_stoneGathererDisplay.DOPunchScale(new Vector3(0.15f, 0.3f, 0f), 0.3f, 1, .7f).SetAutoKill(true);
        m_stoneGathererShake.Play();
    }

    private void UpdateWoodDisplay(int total, int delta)
    {
        if (m_woodBankShake.IsActive())
        {
            m_woodBankShake.Kill();
        }

        m_woodBankLabel.SetText(total.ToString());
        m_woodBankShake = m_woodBankDisplay.DOPunchScale(new Vector3(0.15f, 0.3f, 0f), 0.3f, 1, .7f).SetAutoKill(true);
        m_woodBankShake.Play();
    }

    private void UpdateStoneDisplay(int total, int delta)
    {
        if (m_stoneBankShake.IsActive())
        {
            m_stoneBankShake.Kill();
        }
        m_stoneBankLabel.SetText(total.ToString());
        m_stoneBankShake = m_stoneBankDisplay.DOPunchScale(new Vector3(0.15f, 0.3f, 0f), 0.3f, 1, .7f).SetAutoKill(true);
        m_stoneBankShake.Play();
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
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                }

                Debug.Log("Combat View Active");
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

        //gameObject.SetActive(state != GameplayManager.GameplayState.Setup);
        m_nextWaveButton.gameObject.SetActive(state == GameplayManager.GameplayState.Build);
    }

    private void GameplaySpeedChanged(GameplayManager.GameSpeed newSpeed)
    {
        switch (newSpeed)
        {
            case GameplayManager.GameSpeed.Paused:
                ToggleButtonInteractivity(false);
                break;
            case GameplayManager.GameSpeed.Normal:
                ToggleButtonInteractivity(true);
                break;
            case GameplayManager.GameSpeed.Fast:
                ToggleButtonInteractivity(true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newSpeed), newSpeed, null);
        }


        m_playButton.gameObject.SetActive(newSpeed == GameplayManager.GameSpeed.Paused);
        m_pausedDisplayObj.gameObject.SetActive(newSpeed == GameplayManager.GameSpeed.Paused);

        m_pauseButton.gameObject.SetActive(newSpeed != GameplayManager.GameSpeed.Paused);
    }

    private void ToggleButtonInteractivity(bool b)
    {
        if (m_buttons != null)
        {
            foreach (Button button in m_buttons)
            {
                button.interactable = b;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        GameplayManager.Instance.m_castleController.UpdateHealth += UpdateCastleHealthDisplay;
        m_maxCastleHealth = GameplayManager.Instance.m_castleController.m_maxHealth;
        m_curCastleHealth = m_maxCastleHealth;
        m_castleHealthLabel.SetText($"{m_curCastleHealth}/{m_maxCastleHealth}<sprite name=\"ResourceHealth\">");
        m_pauseButton.onClick.AddListener(OnPauseButtonClicked);
        m_playButton.onClick.AddListener(OnPlayButtonClicked);
        m_nextWaveButton.onClick.AddListener(OnNextWaveButtonClicked);

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
        curAlert.GetComponent<UIAlert>().SetLabelText(text, Color.red);
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