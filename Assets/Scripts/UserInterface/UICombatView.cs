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
    [SerializeField] private CanvasGroup m_canvasGroup;
    
    [Header("Buttons")]
    [SerializeField] private Button m_pauseButton;
    [SerializeField] private Button m_menuButton;
    [SerializeField] private Button m_playButton;
    [SerializeField] private Button m_ffwButton;
    [SerializeField] private Button m_nextWaveButton;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI m_stoneBankLabel;
    [SerializeField] private TextMeshProUGUI m_woodBankLabel;
    [SerializeField] private TextMeshProUGUI m_stoneGathererLabel;
    [SerializeField] private TextMeshProUGUI m_woodGathererLabel;
    [SerializeField] private TextMeshProUGUI m_gameClockLabel;
    [SerializeField] private TextMeshProUGUI m_waveLabel;
    [SerializeField] private TextMeshProUGUI m_obelisksChargedLabel;
    [SerializeField] private TextMeshProUGUI m_castleHealthLabel;
    [SerializeField] private TextMeshProUGUI m_debugInfoLabel;
    [SerializeField] private TextMeshProUGUI m_ffwLabel;
    [SerializeField] private TextMeshProUGUI m_bossNameLabel;

    [Header("Objects")]
    [SerializeField] private GameObject m_towerTrayButtonPrefab;
    [SerializeField] private GameObject m_gathererTrayButtonPrefab;
    [SerializeField] private GameObject m_alertRootObj;
    [SerializeField] private GameObject m_alertPrefab;
    [SerializeField] private GameObject m_pausedDisplayObj;
    [SerializeField] private GameObject m_castleRepairDisplayObj;
    [SerializeField] private GameObject m_waveDisplayObj;
    [SerializeField] private GameObject m_obeliskDisplayObj;
    [SerializeField] private GameObject m_ffwActiveDisplayObj;
    [SerializeField] private GameObject m_bossHealthDisplayObj;


    [Header("Rect Transforms")]
    [SerializeField] private RectTransform m_towerTrayLayoutObj;
    [SerializeField] private RectTransform m_gathererTrayLayoutObj;
    [SerializeField] private RectTransform m_healthDisplay;
    [SerializeField] private RectTransform m_woodBankDisplay;
    [SerializeField] private RectTransform m_stoneBankDisplay;
    [SerializeField] private RectTransform m_woodGathererDisplay;
    [SerializeField] private RectTransform m_stoneGathererDisplay;

    [SerializeField] private Image m_castleRepairFill;
    [SerializeField] private Image m_bossHealthFill;
    
    [Header("Scene References")]
    [SerializeField] private UIOptionsMenu m_menuObj;

    private float m_timeToNextWave;
    private int m_curCastleHealth;
    private int m_maxCastleHealth;
    private List<Button> m_buttons;
    private Tween m_healthShake;
    private Tween m_woodBankShake;
    private Tween m_stoneBankShake;
    private Tween m_woodGathererShake;
    private Tween m_stoneGathererShake;
    private CastleController m_castleController;
    private Dictionary<KeyCode, int> m_gathererKeyMap;
    private Dictionary<KeyCode, int> m_towerKeyMap;
    private int m_wave;

    void Awake()
    {
        //Trying to remove the Enable/Disable gameobject to assure scripts run.
        m_canvasGroup.alpha = 0;
        
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        GameplayManager.OnGamePlaybackChanged += GameplayPlaybackChanged;
        GameplayManager.OnGameSpeedChanged += GameplaySpeedChanged;
        GameplayManager.OnAlertDisplayed += Alert;
        GameplayManager.OnObelisksCharged += UpdateObeliskDisplay;
        GameplayManager.OnGathererAdded += BuildGathererTrayButton;
        GameplayManager.OnGathererRemoved += RemoveGathererTrayButton;
        ResourceManager.UpdateStoneBank += UpdateStoneDisplay;
        ResourceManager.UpdateWoodBank += UpdateWoodDisplay;
        ResourceManager.UpdateStoneGathererCount += UpdateStoneGathererDisplay;
        ResourceManager.UpdateWoodGathererCount += UpdateWoodGathererDisplay;
        
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
        };

        m_gathererKeyMap = new Dictionary<KeyCode, int>
        {
            { KeyCode.Q, 0 },
            { KeyCode.E, 1 },
            { KeyCode.R, 2 },
            { KeyCode.T, 3 },
            { KeyCode.Y, 4 },
        };
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

    private void UpdateWoodGathererDisplay(int i)
    {
        if (m_woodGathererShake.IsActive())
        {
            m_woodGathererShake.Kill();
            m_woodGathererDisplay.localScale = Vector3.one;
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
            ;
            m_stoneGathererDisplay.localScale = Vector3.one;
        }

        m_stoneGathererLabel.SetText(i.ToString());
        m_stoneGathererShake = m_stoneGathererDisplay.DOPunchScale(new Vector3(0.15f, 0.3f, 0f), 0.3f, 1, .7f).SetAutoKill(true);
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

        m_woodBankLabel.SetText(total.ToString());
        m_woodBankShake = m_woodBankDisplay.DOPunchScale(new Vector3(0.15f, 0.3f, 0f), 0.3f, 1, .7f).SetAutoKill(true);
        m_woodBankShake.Play();
    }

    private void UpdateStoneDisplay(int total, int delta)
    {
        if (m_stoneBankShake.IsActive())
        {
            m_stoneBankShake.Kill();
            m_stoneBankDisplay.localScale = Vector3.one;
        }

        m_stoneBankLabel.SetText(total.ToString());
        m_stoneBankShake = m_stoneBankDisplay.DOPunchScale(new Vector3(0.15f, 0.3f, 0f), 0.3f, 1, .7f).SetAutoKill(true);
        m_stoneBankShake.Play();
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
        GameplayManager.OnGamePlaybackChanged -= GameplayPlaybackChanged;
        GameplayManager.OnGameSpeedChanged -= GameplaySpeedChanged;
        GameplayManager.OnAlertDisplayed -= Alert;
        GameplayManager.OnObelisksCharged -= UpdateObeliskDisplay;
        GameplayManager.OnGathererAdded -= BuildGathererTrayButton;
        GameplayManager.OnGathererRemoved -= RemoveGathererTrayButton;

        ResourceManager.UpdateStoneBank -= UpdateStoneDisplay;
        ResourceManager.UpdateWoodBank -= UpdateWoodDisplay;
        ResourceManager.UpdateStoneGathererCount -= UpdateStoneGathererDisplay;
        ResourceManager.UpdateWoodGathererCount -= UpdateWoodGathererDisplay;
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
                //If there obelisks in the mission we care about those instead of wave counts.
                /*if (GameplayManager.Instance.m_obeliskCount > 0)
                {
                    m_obeliskDisplayObj.SetActive(true);
                    m_waveDisplayObj.SetActive(false);
                }
                else
                {
                    m_obeliskDisplayObj.SetActive(false);
                    m_waveDisplayObj.SetActive(true);
                }*/
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
                break;
            case GameplayManager.GameplayState.Combat:
                break;
            case GameplayManager.GameplayState.Build:
                if (m_canvasGroup.alpha == 0)
                {
                    m_canvasGroup.alpha = 1;
                }

                m_timeToNextWave = GameplayManager.Instance.m_timeToNextWave;
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
        m_nextWaveButton.gameObject.SetActive(state == GameplayManager.GameplayState.Build && !GameplayManager.Instance.m_delayForQuest);
        m_castleRepairDisplayObj.SetActive(state == GameplayManager.GameplayState.Build && m_curCastleHealth < m_maxCastleHealth);
    }

    private void GameplayPlaybackChanged(GameplayManager.GameSpeed newSpeed)
    {
        switch (newSpeed)
        {
            case GameplayManager.GameSpeed.Paused:
                ToggleButtonInteractivity(false);
                break;
            case GameplayManager.GameSpeed.Normal:
                ToggleButtonInteractivity(true);
                break;
            default:
                break;
        }


        m_playButton.gameObject.SetActive(newSpeed == GameplayManager.GameSpeed.Paused);
        m_pauseButton.gameObject.SetActive(newSpeed != GameplayManager.GameSpeed.Paused);
        m_pausedDisplayObj.gameObject.SetActive(newSpeed == GameplayManager.GameSpeed.Paused);
    }

    private void GameplaySpeedChanged(int speed)
    {
        m_ffwLabel.SetText($"{speed}x");
        
        m_ffwActiveDisplayObj.SetActive(speed != 1);
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
        m_castleController = GameplayManager.Instance.m_castleController;
        m_castleController.UpdateHealth += UpdateCastleHealthDisplay;
        m_maxCastleHealth = GameplayManager.Instance.m_castleController.m_castleData.m_maxHealth;
        m_curCastleHealth = m_maxCastleHealth;
        m_castleHealthLabel.SetText($"{m_curCastleHealth}/{m_maxCastleHealth}<sprite name=\"ResourceHealth\">");
        m_pauseButton.onClick.AddListener(OnPauseButtonClicked);
        m_menuButton.onClick.AddListener(OnMenuButtonClicked);
        m_playButton.onClick.AddListener(OnPlayButtonClicked);
        m_ffwButton.onClick.AddListener(OnFFWButtonClicked);
        m_nextWaveButton.onClick.AddListener(OnNextWaveButtonClicked);

        m_buttons.Add(m_nextWaveButton);

        BuildTowerTrayDisplay();
    }

    private void OnMenuButtonClicked()
    {
        m_menuObj.ToggleMenu();
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
        GameplayManager.Instance.UpdateGamePlayback(GameplayManager.GameSpeed.Normal);
    }

    private void OnPauseButtonClicked()
    {
        GameplayManager.Instance.UpdateGamePlayback(GameplayManager.GameSpeed.Paused);
    }

    private void OnFFWButtonClicked()
    {
        GameplayManager.Instance.UpdateGameSpeed();
    }

    private void OnNextWaveButtonClicked()
    {
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.SpawnEnemies);
    }

    // Update is called once per frame
    void Update()
    {
        HandleSpawnClock();

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
            if (Input.GetKeyDown(kvp.Key) && GameplayManager.Instance.m_gameSpeed != GameplayManager.GameSpeed.Paused)
            {
                GameplayManager.Instance.PreconstructTower(kvp.Value);
            }
        }

        foreach (var kvp in m_gathererKeyMap)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                GameplayManager.Instance.RequestSelectGatherer(kvp.Value);
            }
        }

        if (m_wave != GameplayManager.Instance.m_wave)
        {
            m_wave = GameplayManager.Instance.m_wave;
            m_waveLabel.SetText($"Wave: {m_wave + 1}");
        }
    }

    void HandleSpawnClock()
    {
        if (GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.Build)
        {
            m_nextWaveButton.gameObject.SetActive(!GameplayManager.Instance.m_delayForQuest);
            

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
        string actorCountString = cell.m_actorCount.ToString();
        bool b = !cell.m_isOccupied;
        string occupancyString = b.ToString();
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
                                         "Actor Count: {4}<br>" +
                                         "Occupied: {5}<br>" +
                                         "Interaction State: {6}", hoveredObjString, preconTowerPosString, canAffordString, canPlaceString, actorCountString, occupancyString, interactionStateString);
        m_debugInfoLabel.SetText(debugInfo);
    }
}