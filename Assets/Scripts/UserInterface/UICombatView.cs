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
    

    [Header("Rect Transforms")]
    [SerializeField] private RectTransform m_towerTrayLayoutObj;
    [SerializeField] private RectTransform m_gathererTrayLayoutObj;
    [SerializeField] private RectTransform m_healthDisplay;
    [SerializeField] private RectTransform m_woodBankDisplay;
    [SerializeField] private RectTransform m_stoneBankDisplay;
    [SerializeField] private RectTransform m_woodGathererDisplay;
    [SerializeField] private RectTransform m_stoneGathererDisplay;

    [SerializeField] private Image m_castleRepairFill;
    [SerializeField] private AudioSource m_audioSource;

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
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
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
            Debug.Log($"initializing m_buttons in combat view");
            m_buttons = new List<Button>();
        }
        
        gameObject.SetActive(false);
        
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
            m_stoneGathererShake.Kill();;
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
        GameplayManager.OnGameSpeedChanged -= GameplaySpeedChanged;
        GameplayManager.OnAlertDisplayed -= Alert;
        GameplayManager.OnObelisksCharged += UpdateObeliskDisplay;
        GameplayManager.OnGathererAdded += BuildGathererTrayButton;
        GameplayManager.OnGathererRemoved += RemoveGathererTrayButton;
        
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
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
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
        m_nextWaveButton.gameObject.SetActive(state == GameplayManager.GameplayState.Build);
        m_castleRepairDisplayObj.SetActive(state == GameplayManager.GameplayState.Build && m_curCastleHealth < m_maxCastleHealth);
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
            default:
                break;
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
        m_castleController = GameplayManager.Instance.m_castleController;
        m_castleController.UpdateHealth += UpdateCastleHealthDisplay;
        m_maxCastleHealth = GameplayManager.Instance.m_castleController.m_castleData.m_maxHealth;
        m_curCastleHealth = m_maxCastleHealth;
        m_castleHealthLabel.SetText($"{m_curCastleHealth}/{m_maxCastleHealth}<sprite name=\"ResourceHealth\">");
        m_pauseButton.onClick.AddListener(OnPauseButtonClicked);
        m_playButton.onClick.AddListener(OnPlayButtonClicked);
        m_ffwButton.onClick.AddListener(OnFFWButtonClicked);
        m_nextWaveButton.onClick.AddListener(OnNextWaveButtonClicked);

        m_buttons.Add(m_nextWaveButton);

        BuildTowerTrayDisplay();
        
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
            { KeyCode.W, 1 },
            { KeyCode.E, 2 },
            { KeyCode.R, 3 },
            { KeyCode.T, 4 },
            { KeyCode.Y, 5 },
        };
    }

    
    
    public void BuildGathererTrayButton(GathererController gathererController)
    {
        GameObject buttonPrefab = Instantiate(m_gathererTrayButtonPrefab, m_gathererTrayLayoutObj);
        GathererTrayButton gathererTrayButtonScript = buttonPrefab.GetComponent<GathererTrayButton>();
        gathererTrayButtonScript.SetupGathererTrayButton(gathererController, 1);
        
        
        Button buttonScript = buttonPrefab.GetComponent<Button>();
        
        if (buttonScript)
        {
            Debug.Log($"adding gatherer tray button to m_buttons");
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
        GameplayManager.Instance.UpdateGameSpeed(GameplayManager.GameSpeed.Normal);
    }

    private void OnPauseButtonClicked()
    {
        GameplayManager.Instance.UpdateGameSpeed(GameplayManager.GameSpeed.Paused);
    }
    
    private void OnFFWButtonClicked()
    {
        GameplayManager.Instance.TogglePlaybackSpeed();
        m_ffwLabel.SetText($"{(int)GameplayManager.Instance.m_playbackSpeed}x");
        m_ffwActiveDisplayObj.SetActive(!m_ffwActiveDisplayObj.activeSelf);
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
            if (Input.GetKeyDown(kvp.Key))
            {
                GameplayManager.Instance.PreconstructTower(kvp.Value);
            }
        }

        if (m_wave != GameplayManager.Instance.m_wave)
        {
            m_wave = GameplayManager.Instance.m_wave;
            m_waveLabel.SetText($"Wave: {m_wave + 1}");
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
                                         "Interaction State: {6}", hoveredObjString, preconTowerPosString, canAffordString,  canPlaceString, actorCountString, occupancyString, interactionStateString);
        m_debugInfoLabel.SetText(debugInfo);
    }
}