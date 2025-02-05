using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UICombatView : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private UIStringData m_uiStringData;

    [Header("Buttons")]
    [SerializeField] private Button m_pauseButton;
    [SerializeField] private Button m_menuButton;
    [SerializeField] private Button m_playButton;
    [SerializeField] private Toggle m_ffwButton;
    [SerializeField] private Button m_nextWaveButton;
    [SerializeField] private Button m_clearBlueprintsButton;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI m_stoneBankLabel;
    [SerializeField] private TextMeshProUGUI m_woodBankLabel;
    [SerializeField] private TextMeshProUGUI m_woodRateLabel;
    [SerializeField] private TextMeshProUGUI m_stoneGathererLabel;
    [SerializeField] private TextMeshProUGUI m_woodGathererLabel;
    [SerializeField] private TextMeshProUGUI m_gameClockLabel;
    [SerializeField] private TextMeshProUGUI m_waveLabel;
    [SerializeField] private TextMeshProUGUI m_obelisksChargedLabel;
    [SerializeField] private TextMeshProUGUI m_castleHealthLabel;
    [SerializeField] private TextMeshProUGUI m_debugInfoLabel;
    [SerializeField] private TextMeshProUGUI m_highScoreLabel;

    [Header("Objects")]
    [SerializeField] private GameObject m_towerTrayButtonPrefab;
    [SerializeField] private GameObject m_blueprintTowerTrayButtonPrefab;
    [SerializeField] private GameObject m_gathererTrayButtonPrefab;
    [SerializeField] private GameObject m_alertRootObj;
    [SerializeField] private GameObject m_alertPrefab;
    [SerializeField] private GameObject m_ruinIndicatedAlertPrefab;
    [SerializeField] private GameObject m_waveCompleteAlertPrefab;
    [SerializeField] private GameObject m_pausedDisplayObj;
    [SerializeField] private GameObject m_castleRepairDisplayObj;
    [SerializeField] private GameObject m_ffwActiveDisplayObj;

    [Header("Rect Transforms")]
    [SerializeField] private RectTransform m_towerTrayLayoutObj;
    [SerializeField] private RectTransform m_structureTrayLayoutObj;
    [SerializeField] private RectTransform m_gathererTrayLayoutObj;
    [SerializeField] private RectTransform m_healthDisplay;
    [SerializeField] private RectTransform m_woodBankDisplay;
    [SerializeField] private RectTransform m_stoneBankDisplay;
    [SerializeField] private RectTransform m_woodGathererDisplay;
    [SerializeField] private RectTransform m_stoneGathererDisplay;
    [SerializeField] private Image m_castleRepairFill;
    [SerializeField] private Image m_survivalWaveDurationFill;

    [Header("Scene References")]
    [SerializeField] private UIOptionsPopup m_menuObj;

    private bool m_buttonsActivated = true;
    private float m_timeToNextWave;
    private int m_curCastleHealth;
    private int m_maxCastleHealth;
    private GameObject m_blueprintButtonObj;
    private List<Button> m_buttons;
    private List<TowerTrayButton> m_towerButtons;
    private List<TowerTrayButton> m_structureButtons;
    private Tween m_healthShake;
    private Tween m_woodBankShake;
    private Tween m_stoneBankShake;
    private Tween m_woodGathererShake;
    private Tween m_stoneGathererShake;
    private CastleController m_castleController;
    private Dictionary<KeyCode, int> m_gathererKeyMap;
    private Dictionary<KeyCode, int> m_towerKeyMap;
    private int m_blueprintTowerKey;
    private int m_wave = 0;

    private float m_doubleTapTimer = 0f;
    private float m_doubleTapThreshold = 0.5f;
    private bool m_isFirstTap = false;
    private KeyCode m_lastKey = KeyCode.None;

    void Awake()
    {
        m_canvasGroup.alpha = 0;

        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        GameplayManager.OnGamePlaybackChanged += GameplayPlaybackChanged;
        GameplayManager.OnGameSpeedChanged += GameplaySpeedChanged;
        GameplayManager.OnAlertDisplayed += Alert;
        GameplayManager.OnObelisksCharged += UpdateObeliskDisplay;
        GameplayManager.OnObeliskAdded += UpdateObeliskDisplay;
        GameplayManager.OnGathererAdded += BuildGathererTrayButton;
        GameplayManager.OnGathererRemoved += RemoveGathererTrayButton;
        GameplayManager.OnDelayForQuestChanged += DelayForQuestChanged;
        GameplayManager.OnWaveCompleted += AlertWaveComplete;
        GameplayManager.OnBlueprintCountChanged += BlueprintCountChanged;
        GameplayManager.OnUnlockedStucturesUpdated += UnlockedStructuresUpdated;
        GameplayManager.OnUnlockedTowersUpdated += UnlockedTowersUpdated;
        GameplayManager.OnWaveChanged += UpdateWaveDisplay;

        ResourceManager.UpdateStoneBank += UpdateStoneDisplay;
        ResourceManager.UpdateWoodBank += UpdateWoodDisplay;
        ResourceManager.UpdateWoodRate += UpdateWoodRateDisplay;
        ResourceManager.UpdateStoneGathererCount += UpdateStoneGathererDisplay;
        ResourceManager.UpdateWoodGathererCount += UpdateWoodGathererDisplay;
        //ResourceManager.RuinIndicated += RuinIndicated;

        UIPopupManager.OnPopupManagerPopupsOpen += ToggleButtonInteractivity;

        if (m_buttons == null)
        {
            m_buttons = new List<Button>();
        }

        m_towerKeyMap = new Dictionary<KeyCode, int>
        {
            { KeyCode.Alpha1, 0 },
            { KeyCode.Alpha2, 1 },
            { KeyCode.Alpha3, 2 },
            { KeyCode.Alpha4, 3 },
            { KeyCode.Alpha5, 4 },
            { KeyCode.Alpha6, 5 },
            { KeyCode.Alpha7, 6 },
            { KeyCode.Alpha8, 7 },
            { KeyCode.Alpha9, 8 },
            { KeyCode.Alpha0, 9 },
            { KeyCode.B, -1 },
        };

        m_gathererKeyMap = new Dictionary<KeyCode, int>
        {
            { KeyCode.Q, 0 },
            { KeyCode.E, 1 },
            { KeyCode.R, 2 },
            { KeyCode.T, 3 },
            { KeyCode.Y, 4 },
        };

        m_highScoreLabel.gameObject.SetActive(false);
        m_survivalWaveDurationFill.fillAmount = 0;
    }

    private void UpdateWaveDisplay(int value)
    {
        m_wave = value;
        m_waveLabel.SetText($"Wave: {m_wave}");
    }


    private void BlueprintCountChanged(int i)
    {
        Debug.Log($"CombatVIew: Msg from Gameplay Manager. Blueprint Count is {i}");
        m_clearBlueprintsButton.gameObject.SetActive(i > 0);
    }

    private void DelayForQuestChanged(bool value)
    {
        // Use inverted values. Delay for Quest will return false when it's unset, and we want to display true.
        if (GameplayManager.Instance.m_gameplayState != GameplayManager.GameplayState.Build) return;

        m_nextWaveButton.gameObject.SetActive(!value);
    }

    private void UpdateWoodRateDisplay(float rate)
    {
        m_woodRateLabel.SetText($"{rate:F1}<sprite name=\"ResourceWood\">/Min");
    }

    private void UpdateCastleHealthDisplay(int i)
    {
        if (m_healthShake.IsActive())
        {
            m_healthShake.Kill();
            m_healthDisplay.localScale = Vector3.one;
        }

        m_curCastleHealth += i;
        m_maxCastleHealth = GameplayManager.Instance.m_castleController.GetCastleMaxHealth();
        m_castleHealthLabel.SetText($"{m_curCastleHealth}/{m_maxCastleHealth}<sprite name=\"ResourceHealth\">");
        m_healthShake = m_healthDisplay.DOPunchScale(new Vector3(0.15f, 0.3f, 0f), 0.3f, 1, .7f).SetAutoKill(true);
        m_healthShake.Play();
    }

    private void UpdateCastleMaxHealthDisplay(int i)
    {
        if (m_healthShake.IsActive())
        {
            m_healthShake.Kill();
            m_healthDisplay.localScale = Vector3.one;
        }

        m_curCastleHealth += i;
        m_maxCastleHealth = GameplayManager.Instance.m_castleController.GetCastleMaxHealth();
        m_castleHealthLabel.SetText($"{m_curCastleHealth}/{m_maxCastleHealth}<sprite name=\"ResourceHealth\">");
        m_healthShake = m_healthDisplay.DOPunchScale(new Vector3(0.15f, 0.3f, 0f), 0.3f, 1, .7f).SetAutoKill(true);
        m_healthShake.Play();
    }

    private void UpdateWoodGathererDisplay(int i)
    {
        if (m_woodGathererShake.IsActive())
        {
            m_woodGathererShake.Kill();
            m_woodGathererDisplay.localScale = Vector3.one;
        }

        m_woodGathererLabel.SetText(i.ToString());
        m_woodGathererShake = m_woodGathererDisplay.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.3f, 1, .7f).SetAutoKill(true);
        m_woodGathererShake.Play();
    }

    private void UpdateStoneGathererDisplay(int i)
    {
        if (m_stoneGathererShake.IsActive())
        {
            m_stoneGathererShake.Kill();
            ;
            m_stoneGathererDisplay.localScale = Vector3.one;
        }

        m_stoneGathererLabel.SetText(i.ToString());
        m_stoneGathererShake = m_stoneGathererDisplay.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.3f, 1, .7f).SetAutoKill(true);
        m_stoneGathererShake.Play();
    }

    private void UpdateWoodDisplay(int total, int delta)
    {
        //Debug.Log("BANK UPDATE RECEIEVED");
        if (m_woodBankShake.IsActive())
        {
            m_woodBankShake.Kill();
            m_woodBankDisplay.localScale = Vector3.one;
        }

        m_woodBankLabel.SetText($"{total}<sprite name=\"ResourceWood\">");
        m_woodBankShake = m_woodBankDisplay.DOPunchScale(new Vector3(0.15f, 0.3f, 0f), 0.15f, 1, .7f).SetAutoKill(true);
        m_woodBankShake.Play();
    }

    private void UpdateStoneDisplay(int total, int delta)
    {
        if (m_stoneBankShake.IsActive())
        {
            m_stoneBankShake.Kill();
            m_stoneBankDisplay.localScale = Vector3.one;
        }

        m_stoneBankLabel.SetText($"{total}<sprite name=\"ResourceStone\">");
        m_stoneBankShake = m_stoneBankDisplay.DOPunchScale(new Vector3(0.15f, 0.3f, 0f), 0.15f, 1, .7f).SetAutoKill(true);
        m_stoneBankShake.Play();
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
        GameplayManager.OnGamePlaybackChanged -= GameplayPlaybackChanged;
        GameplayManager.OnGameSpeedChanged -= GameplaySpeedChanged;
        GameplayManager.OnAlertDisplayed -= Alert;
        GameplayManager.OnObelisksCharged -= UpdateObeliskDisplay;
        GameplayManager.OnObeliskAdded += UpdateObeliskDisplay;
        GameplayManager.OnGathererAdded -= BuildGathererTrayButton;
        GameplayManager.OnGathererRemoved -= RemoveGathererTrayButton;
        GameplayManager.OnDelayForQuestChanged -= DelayForQuestChanged;
        GameplayManager.OnWaveCompleted -= AlertWaveComplete;
        GameplayManager.OnBlueprintCountChanged -= BlueprintCountChanged;
        GameplayManager.OnUnlockedStucturesUpdated -= UnlockedStructuresUpdated;
        GameplayManager.OnUnlockedTowersUpdated -= UnlockedTowersUpdated;
        GameplayManager.OnWaveChanged -= UpdateWaveDisplay;

        ResourceManager.UpdateStoneBank -= UpdateStoneDisplay;
        ResourceManager.UpdateWoodBank -= UpdateWoodDisplay;
        ResourceManager.UpdateWoodRate -= UpdateWoodRateDisplay;
        ResourceManager.UpdateStoneGathererCount -= UpdateStoneGathererDisplay;
        ResourceManager.UpdateWoodGathererCount -= UpdateWoodGathererDisplay;
        //ResourceManager.RuinIndicated -= RuinIndicated;

        UIPopupManager.OnPopupManagerPopupsOpen -= ToggleButtonInteractivity;

        m_castleController.UpdateHealth -= UpdateCastleHealthDisplay;
        m_castleController.UpdateMaxHealth -= UpdateCastleMaxHealthDisplay;
        m_castleController.OnIsRepairingUpdated -= IsRepairingUpdated;
    }

    private void UpdateObeliskDisplay(int x, int y)
    {
        m_obelisksChargedLabel.SetText($"Obelisk Power: {x} / {y}");
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState state)
    {
        switch (state)
        {
            case GameplayManager.GameplayState.Setup:
                m_canvasGroup.alpha = 1;
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
                break;
            case GameplayManager.GameplayState.BossWave:

                break;
            case GameplayManager.GameplayState.Combat:
                break;
            case GameplayManager.GameplayState.Build:
                m_timeToNextWave = GameplayManager.Instance.m_timeToNextWave;
                HandleHighScoreLabel();
                break;
            case GameplayManager.GameplayState.CutScene:
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

        m_castleRepairDisplayObj.SetActive(state == GameplayManager.GameplayState.Build && m_curCastleHealth < m_maxCastleHealth);
        m_nextWaveButton.gameObject.SetActive(state == GameplayManager.GameplayState.Build && !GameplayManager.Instance.m_delayForQuest);
    }

    void HandleHighScoreLabel()
    {
        m_highScoreLabel.gameObject.SetActive(GameplayManager.Instance.IsEndlessModeActive());

        if (!m_highScoreLabel.gameObject.activeSelf) return;

        if (m_wave < GameplayManager.Instance.GetCurrentMissionSaveData().m_waveHighScore)
        {
            string highScoreString = string.Format(m_uiStringData.m_displayHighScore, GameplayManager.Instance.GetCurrentMissionSaveData().m_waveHighScore);
            m_highScoreLabel.SetText(highScoreString);
        }
        else
        {
            m_highScoreLabel.SetText(m_uiStringData.m_newHighScoreDisplay);
            ColorUtility.TryParseHtmlString("#F1D24B", out Color color);
            m_highScoreLabel.color = color;
        }
    }

    private void GameplayPlaybackChanged(GameplayManager.GameSpeed newSpeed)
    {
        switch (newSpeed)
        {
            case GameplayManager.GameSpeed.Paused:
                ToggleBlueprintButton(true);
                break;
            case GameplayManager.GameSpeed.Normal:
                ToggleBlueprintButton(false);
                break;
            default:
                break;
        }


        m_pausedDisplayObj.gameObject.SetActive(newSpeed == GameplayManager.GameSpeed.Paused);

        m_pauseButton.interactable = newSpeed != GameplayManager.GameSpeed.Paused;
        m_playButton.interactable = newSpeed != GameplayManager.GameSpeed.Normal;
    }

    private void GameplaySpeedChanged(int speed)
    {
        m_ffwActiveDisplayObj.SetActive(speed != 1);
    }

    private void ToggleButtonInteractivity(bool b)
    {
        // if b is true, we disable buttons.
        m_buttonsActivated = !b;
        if (m_buttons != null)
        {
            foreach (Button button in m_buttons)
            {
                button.interactable = m_buttonsActivated;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        m_castleController = GameplayManager.Instance.m_castleController;
        m_castleController.UpdateHealth += UpdateCastleHealthDisplay;
        m_castleController.UpdateMaxHealth += UpdateCastleMaxHealthDisplay;
        m_castleController.OnIsRepairingUpdated += IsRepairingUpdated;
        m_maxCastleHealth = GameplayManager.Instance.m_castleController.m_castleData.m_maxHealth;
        m_curCastleHealth = m_maxCastleHealth;
        m_castleHealthLabel.SetText($"{m_curCastleHealth}/{m_maxCastleHealth}<sprite name=\"ResourceHealth\">");
        m_pauseButton.onClick.AddListener(OnPauseButtonClicked);
        m_menuButton.onClick.AddListener(OnMenuButtonClicked);
        m_playButton.onClick.AddListener(OnPlayButtonClicked);
        m_ffwButton.onValueChanged.AddListener(OnFFWButtonClicked);
        m_nextWaveButton.onClick.AddListener(OnNextWaveButtonClicked);
        m_clearBlueprintsButton.onClick.AddListener(OnClearBlueprintsButtonClicked);

        m_buttons.Add(m_nextWaveButton);

        BuildTowerTrayDisplay();
        m_clearBlueprintsButton.gameObject.SetActive(false);

        m_waveLabel.SetText($"Wave: {m_wave}");
    }

    private void IsRepairingUpdated(bool value)
    {
        m_castleRepairDisplayObj.SetActive(value);
    }

    private void UnlockableLocked(ProgressionUnlockableData unlockableData)
    {
        // Is the unlockable something we need to destroy a Tray button for?
    }

    private void UnlockableUnlocked(ProgressionUnlockableData unlockableData)
    {
        // Is the unlockable something we need to build a Tray button for?
    }

    private void OnClearBlueprintsButtonClicked()
    {
        GameplayManager.Instance.ClearBlueprintTowerModels();
    }

    private void OnMenuButtonClicked()
    {
        UIPopupManager.Instance.ShowPopup<UIOptionsPopup>("OptionsPopup");
    }

    private int gathererIndex;

    public void BuildGathererTrayButton(GathererController gathererController)
    {
        GameObject buttonPrefab = Instantiate(m_gathererTrayButtonPrefab, m_gathererTrayLayoutObj);
        GathererTrayButton gathererTrayButtonScript = buttonPrefab.GetComponent<GathererTrayButton>();

        string keyString = null;
        foreach (KeyValuePair<KeyCode, int> kvp in m_gathererKeyMap)
        {
            if (kvp.Value == gathererIndex)
            {
                keyString = kvp.Key.ToString();
                break;
            }
        }

        gathererTrayButtonScript.SetupGathererTrayButton(gathererController, keyString);
        ++gathererIndex;

        Button buttonScript = buttonPrefab.GetComponent<Button>();

        if (buttonScript)
        {
            //Debug.Log($"adding gatherer tray button to m_buttons");
            m_buttons.Add(buttonScript);
        }
    }

    private void RemoveGathererTrayButton(GathererController gathererController)
    {
        //To Do if i ever need to remove gatherers.
    }

    private int m_trayButtonIndex;

    private void BuildTowerTrayDisplay()
    {
        // TOWERS
        m_towerButtons = new List<TowerTrayButton>();
        for (int i = 0; i < GameplayManager.Instance.m_gameplayData.m_equippedTowers.Count; ++i)
        {
            BuildTowerTrayButton(GameplayManager.Instance.m_gameplayData.m_equippedTowers[i]);
        }

        if (GameplayManager.Instance.m_unlockedTowers != null)
        {
            foreach (var towerData in GameplayManager.Instance.m_unlockedTowers)
            {
                BuildTowerTrayButton(towerData);
            }
        }

        // BLUEPRINT TOWER
        m_blueprintButtonObj = Instantiate(m_blueprintTowerTrayButtonPrefab, m_towerTrayLayoutObj);
        TowerTrayButton blueprintTowerTrayButtonScript = m_blueprintButtonObj.GetComponent<TowerTrayButton>();

        blueprintTowerTrayButtonScript.SetupData(GameplayManager.Instance.m_gameplayData.m_blueprintTower, -1);

        Button blueprintButtonScript = m_blueprintButtonObj.GetComponent<Button>();

        m_buttons.Add(blueprintButtonScript);
        ToggleBlueprintButton(false);

        // STRUCTURES
        if (m_structureButtons == null)
        {
            m_structureButtons = new List<TowerTrayButton>();
        }

        if (GameplayManager.Instance.m_unlockedStructures != null)
        {
            foreach (var kvp in GameplayManager.Instance.m_unlockedStructures)
            {
                //Debug.Log($"building tray button for {kvp.Key.name} with {kvp.Value}");
                BuildStructureTrayButton(kvp.Key, kvp.Value);
            }
        }
    }

    void OrderTrayButtons()
    {
        m_towerButtons.Sort((a, b) => a.GetTowerData().m_woodCost.CompareTo(b.GetTowerData().m_woodCost));

        for (int i = 0; i < m_towerButtons.Count; ++i)
        {
            m_towerButtons[i].transform.SetSiblingIndex(i);
            m_towerButtons[i].UpdateHotkeyDisplay(i + 1);
        }

        if (m_structureButtons == null || m_structureButtons.Count <= 1) return;

        m_structureButtons.Sort((a, b) => a.GetTowerData().m_woodCost.CompareTo(b.GetTowerData().m_woodCost));

        for (int i = 0; i < m_structureButtons.Count; ++i)
        {
            m_structureButtons[i].transform.SetSiblingIndex(i);
            m_structureButtons[i].UpdateHotkeyDisplay(i + 5 + 1); // 5 is number of towers, 1 is starting at 6 instead of 5.
        }
    }

    private void UnlockedTowersUpdated(TowerData towerData, bool value)
    {
        // If we dont have a button, and the qty is greater than 0, add one.
        if (value)
        {
            BuildTowerTrayButton(towerData);
        }
        else
        {
            RemoveTowerTrayButton(towerData);
        }
    }

    private void UnlockedStructuresUpdated(TowerData towerData, int qty)
    {
        foreach (TowerTrayButton button in m_structureButtons)
        {
            if (button.GetTowerData() == towerData)
            {
                button.UpdateQuantity(qty);
                return;
            }
        }

        // If we dont have a button, and the qty is greater than 0, add one.
        if (qty > 0)
        {
            BuildStructureTrayButton(towerData, qty);
        }
    }

    private void BuildTowerTrayButton(TowerData towerData)
    {
        GameObject buttonPrefab = Instantiate(m_towerTrayButtonPrefab, m_towerTrayLayoutObj);
        TowerTrayButton towerTrayButtonScript = buttonPrefab.GetComponent<TowerTrayButton>();

        towerTrayButtonScript.SetupData(towerData, m_towerButtons.Count);

        Button buttonScript = buttonPrefab.GetComponent<Button>();

        m_buttons.Add(buttonScript);
        m_towerButtons.Add(towerTrayButtonScript);

        OrderTrayButtons();
    }

    private void RemoveTowerTrayButton(TowerData towerData)
    {
        List<TowerTrayButton> buttonsToRemove = new List<TowerTrayButton>(m_towerButtons);
        foreach (TowerTrayButton button in m_towerButtons)
        {
            if (towerData == button.GetTowerData())
            {
                buttonsToRemove.Remove(button);
                Button buttonScript = button.GetComponent<Button>();
                if (buttonScript)
                {
                    m_buttons.Remove(buttonScript);
                }
                Destroy(button.gameObject);
            }
        }

        m_towerButtons = buttonsToRemove;

        for (int i = 0; i < m_towerButtons.Count; ++i)
        {
            m_towerButtons[i].UpdateHotkeyDisplay(i + 1);
        }
    }

    private void BuildStructureTrayButton(TowerData towerData, int qty)
    {
        GameObject buttonPrefab = Instantiate(m_towerTrayButtonPrefab, m_structureTrayLayoutObj);
        TowerTrayButton towerTrayButtonScript = buttonPrefab.GetComponent<TowerTrayButton>();

        towerTrayButtonScript.SetupData(towerData, 5 + m_structureButtons.Count, qty);
        Button buttonScript = buttonPrefab.GetComponent<Button>();

        m_buttons.Add(buttonScript);
        m_structureButtons.Add(towerTrayButtonScript);

        OrderTrayButtons();
    }

    private void ToggleBlueprintButton(bool value)
    {
        if (m_blueprintButtonObj == null || m_blueprintButtonObj.activeSelf == value) return;

        m_blueprintButtonObj.SetActive(value);
    }

    private void Alert(string text)
    {
        UIAlert alert = ObjectPoolManager.SpawnObject(m_alertPrefab, m_alertRootObj.transform).GetComponent<UIAlert>();
        alert.SetLabelText(text, Color.white);
        alert.SetupAlert(Vector2.zero);
    }

    private void AlertWaveComplete(string text)
    {
        /*UIAlert alert = ObjectPoolManager.SpawnObject(m_waveCompleteAlertPrefab, m_alertRootObj.transform).GetComponent<UIAlert>();
        alert.SetLabelText(text, Color.white);
        alert.SetupAlert(Vector2.zero);*/
    }

    private void RuinIndicated()
    {
        UIAlert alert = ObjectPoolManager.SpawnObject(m_ruinIndicatedAlertPrefab, m_alertRootObj.transform).GetComponent<UIAlert>();
        alert.SetLabelText(m_uiStringData.m_ruinIndicatedString, Color.white);
        alert.SetupAlert(Vector2.zero);
    }

    private void OnPlayButtonClicked()
    {
        GameplayManager.Instance.UpdateGamePlayback(GameplayManager.GameSpeed.Normal);
    }

    private void OnPauseButtonClicked()
    {
        GameplayManager.Instance.UpdateGamePlayback(GameplayManager.GameSpeed.Paused);
    }

    private void OnFFWButtonClicked(bool value)
    {
        GameplayManager.Instance.UpdateGameSpeed();
    }

    private void OnNextWaveButtonClicked()
    {
        GameplayManager.Instance.m_timeToNextWave = 0f;
    }

// Update is called once per frame
    void Update()
    {
        HandleSpawnClock();

        if (GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.SpawnEnemies) HandleSurvivalWaveTimer();

        if (m_castleRepairDisplayObj.activeSelf)
        {
            m_castleRepairFill.fillAmount = m_castleController.RepairProgress();

            if (m_curCastleHealth >= m_maxCastleHealth)
            {
                m_castleRepairDisplayObj.SetActive(false);
            }
        }

        DebugMenu();

        foreach (var kvp in m_towerKeyMap)
        {
            if (Input.GetKeyDown(kvp.Key) && m_buttonsActivated)
            {
                //Only allow for precon of Blueprint towers while paused.
                if (kvp.Value == -1 && GameplayManager.Instance.m_gameSpeed != GameplayManager.GameSpeed.Paused)
                {
                    return;
                }

                if (kvp.Value == -1)
                {
                    GameplayManager.Instance.CreatePreconBuilding(GameplayManager.Instance.m_gameplayData.m_blueprintTower);
                    return;
                }

                if (kvp.Value < m_towerButtons.Count)
                {
                    //Tower buttons count is 5.
                    GameplayManager.Instance.CreatePreconBuilding(m_towerButtons[kvp.Value].GetTowerData());
                }
                else if (kvp.Value >= 5 && kvp.Value < m_structureButtons.Count + 5)
                {
                    //Pressing key 6 gives me value of 5. 5 - 
                    GameplayManager.Instance.CreatePreconBuilding(m_structureButtons[kvp.Value - 5].GetTowerData());
                }
            }
        }

        // DOUBLE TAP tracking for Gatherer Buttons (among others if we need it.)
        if (m_isFirstTap)
        {
            m_doubleTapTimer += Time.unscaledDeltaTime;

            // Reset if time exceeds threshold
            if (m_doubleTapTimer > m_doubleTapThreshold)
            {
                m_isFirstTap = false;
                m_doubleTapTimer = 0f;
                m_lastKey = KeyCode.None; // Reset last key
            }
        }

        foreach (var kvp in m_gathererKeyMap)
        {
            if (Input.GetKeyDown(kvp.Key) && m_buttonsActivated)
            {
                if (m_isFirstTap && m_lastKey == kvp.Key && m_doubleTapTimer <= m_doubleTapThreshold)
                {
                    // THIS IS A DOUBLE TAP
                    m_isFirstTap = false;
                    GathererController gathererToFocus = GameplayManager.Instance.m_woodGathererList[kvp.Value];
                    CameraController.Instance.RequestOnRailsMove(gathererToFocus.transform.position);
                }
                else
                {
                    // THIS IS A FIRST TAP
                    GameplayManager.Instance.RequestSelectGatherer(kvp.Value);
                    m_isFirstTap = true;
                    m_doubleTapTimer = 0f;
                    m_lastKey = kvp.Key;
                }
            }
        }
    }

    private void HandleSurvivalWaveTimer()
    {
        if (GameplayManager.Instance.m_gameplayData.m_gameMode != MissionGameplayData.GameMode.Survival) return;

        float normalizedTime = Mathf.Clamp01(GameplayManager.Instance.m_timeToNextWave / GameplayManager.Instance.m_gameplayData.m_survivalWaveDuration);
        m_survivalWaveDurationFill.fillAmount = 1 - normalizedTime;
    }

    void HandleSpawnClock()
    {
        if (m_nextWaveButton.enabled)
        {
            m_timeToNextWave = GameplayManager.Instance.m_timeToNextWave;
            TimeSpan timeSpan = TimeSpan.FromSeconds(m_timeToNextWave);

            //String Formatting
            string formattedTime;
            if (timeSpan.Hours > 0)
            {
                formattedTime = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            }
            else if (timeSpan.Minutes > 0)
            {
                formattedTime = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
            }
            else
            {
                formattedTime = string.Format("{0:D2}", timeSpan.Seconds);
            }

            m_gameClockLabel.SetText($"Next wave : {formattedTime}");
        }
    }

    void DebugMenu()
    {
        //Debug Info
        //Hovered Obj
        Selectable hoveredOjb = GameplayManager.Instance.m_hoveredSelectable;
        string hoveredObjString = "Null";


        //Precon Tower Position
        Vector2Int preconTowerPos = GameplayManager.Instance.m_preconstructedTowerPos;
        Cell cell = Util.GetCellFromPos(preconTowerPos);
        string actorCountString = "";
        string occupancyString = "";
        string preconTowerPosString = "";

        if (cell != null)
        {
            actorCountString = cell.m_actorCount.ToString();
            bool b = !cell.m_isOccupied;
            occupancyString = b.ToString();
            preconTowerPosString = "0,0";
        }


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
                                         "Actor Count: {4}<br>" +
                                         "Occupied: {5}<br>" +
                                         "Interaction State: {6}", hoveredObjString, preconTowerPosString, canAffordString, canPlaceString, actorCountString, occupancyString, interactionStateString);
        m_debugInfoLabel.SetText(debugInfo);
    }
}