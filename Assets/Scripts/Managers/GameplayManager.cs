using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.TextCore.Text;
using Object = System.Object;
using Random = UnityEngine.Random;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance;
    public GameplayState m_gameplayState;
    public GameSpeed m_gameSpeed;
    public InteractionState m_interactionState;

    public static event Action<GameplayState> OnGameplayStateChanged;
    public static event Action<GameSpeed> OnGamePlaybackChanged;
    public static event Action<int> OnGameSpeedChanged;
    public static event Action<int> OnWaveChanged;
    public static event Action<int, int, int> OnEnemyCountChanged;
    public static event Action<GameObject> OnGameObjectSelected;
    public static event Action<GameObject> OnGameObjectDeselected;
    public static event Action<GameObject> OnGameObjectHoveredEnter;
    public static event Action<GameObject> OnGameObjectHoveredExit;
    public static event Action<GameObject, Selectable.SelectedObjectType> OnCommandRequested;
    public static event Action<GameObject, bool> OnObjRestricted;
    public static event Action<String> OnAlertDisplayed;
    public static event Action<CompletedWave> OnWaveCompleted;
    public static event Action<List<Cell>> OnPreconBuildingMoved;
    public static event Action OnPreconBuildingClear;
    public static event Action<TowerData, GameObject> OnTowerBuild;
    public static event Action<TowerData> OnStructureSold;
    public static event Action OnTowerUpgrade;
    public static event Action<int, int> OnObelisksCharged;
    public static event Action<int, int> OnObeliskAdded;
    public static event Action<GathererController> OnGathererAdded;
    public static event Action<GathererController> OnGathererRemoved;
    public static event Action OnCutSceneEnd;
    public static event Action<bool> OnGossamerHealed;
    public static event Action<bool> OnSpireDestroyed;
    public static event Action<bool> OnDelayForQuestChanged;
    public static event Action<int> OnBlueprintCountChanged;
    public static event Action<TowerData, int> OnUnlockedStucturesUpdated;
    public static event Action<TowerData, bool> OnUnlockedTowersUpdated;


    [Header("Progression")]
    [SerializeField] private ProgressionTable m_progressionTable;
    private int m_qty;
    public Dictionary<TowerData, int> m_unlockedStructures;
    public List<TowerData> m_unlockedTowers;
    private UICombatView m_combatHUD;

    public UICombatView CombatHUD
    {
        get { return m_combatHUD; }
        set { m_combatHUD = value; }
    }

    [Header("Wave Settings")]
    public MissionGameplayData m_gameplayData;
    public GameplayAudioData m_gameplayAudioData;
    public AudioSource m_audioSource;

    private int m_wave;

    public int Wave
    {
        get { return m_wave; }
        set
        {
            m_wave = value;
            m_minute = Mathf.FloorToInt(m_totalTime / 60f);
            Debug.Log($"Wave {m_wave}, Total Time: {m_totalTime}, Minute: {m_minute}");
            OnWaveChanged?.Invoke(m_wave);
        }
    }

    private float m_totalTime;
    private int m_minute;
    public int Minute => m_minute;

    [HideInInspector] public float m_timeToNextWave;
    [HideInInspector] public bool delayForQuest;

    [Header("Spire")]
    public CastleController m_castleController;
    public Transform m_enemyGoal;

    [HideInInspector]
    public Vector2Int m_goalPointPos;

    [Header("Obelisks")]
    [FormerlySerializedAs("m_activeObelisks")]
    public List<Obelisk> m_obelisksInMission;

    [Header("Unit Spawners")]
    [ReadOnly] public List<EnemySpawner> m_enemySpawners;
    [ReadOnly] public List<EnemySpawner> m_enemyTrojanSpawners;

    [Header("Active Enemies")]
    public List<EnemyController> m_enemyList;
    public List<EnemyController> m_enemyBossList;

    public List<CompletedWave> m_wavesCompleted;
    public int m_perfectWavesCompleted;

    private int m_enemiesCreatedThisWave;

    public int EnemiesCreatedThisWave
    {
        get { return m_enemiesCreatedThisWave; }
        set
        {
            if (value != m_enemiesCreatedThisWave)
            {
                m_enemiesCreatedThisWave = value;
            }
        }
    }

    private int m_enemiesKilledThisWave;

    public int EnemiesKilledThisWave
    {
        get { return m_enemiesKilledThisWave; }
        set
        {
            if (value != m_enemiesKilledThisWave)
            {
                m_enemiesKilledThisWave = value;
            }
        }
    }

    private int m_coresClaimedThisWave;

    public int CoresClaimedThisWave
    {
        get { return m_coresClaimedThisWave; }
        set
        {
            if (value != m_coresClaimedThisWave)
            {
                m_coresClaimedThisWave = value;
            }
        }
    }

    private int m_damageTakenThisWave;

    public int DamageTakenThisWave
    {
        get { return m_damageTakenThisWave; }
        set
        {
            if (value != m_damageTakenThisWave)
            {
                m_damageTakenThisWave = value;
            }
        }
    }


    [Header("Player Constructed")]
    public List<GathererController> m_woodGathererList;
    public List<GathererController> m_stoneGathererList;
    public List<Tower> m_towerList;
    private List<TowerBlueprint> m_blueprintList;

    [Header("Selected Object Info")]
    public SelectionColors m_selectionColors;
    private Material m_selectedOutlineMaterial; //Assigned on awake
    private Selectable m_curSelectable;
    public Selectable m_hoveredSelectable;

    // Precon Tower Info
    public InvalidCell m_invalidCellObj;
    public List<InvalidCell> m_invalidCellObjs;
    public LayerMask m_buildSurface;
    public Vector2Int m_preconstructedTowerPos;

    [HideInInspector] public bool m_canAfford;
    [HideInInspector] public bool m_canPlace;
    [HideInInspector] public bool m_canPath;
    [HideInInspector] public bool m_canBuild;

    private GameObject m_preconstructedTowerObj;
    private Tower m_preconstructedTower;
    private TowerData m_preconstructedTowerData;

    //Boss Testing Ground
    [Header("Boss Wave Info")]
    public List<int> m_bossWaves; // What wave does this mission spawn a boss
    public BossSequenceController m_bossSequenceController; // What boss does this mission spawn
    private BossSequenceController m_activeBossSequenceController; // Assigned by the BossSequence Controller
    private bool m_watchingCutScene;

    //Ooze Cell Info
    public OozeManager m_oozeManager;

    [Header("Strings")]
    [SerializeField] private UIStringData m_UIStringData;
    private Camera m_mainCamera;
    private MissionSaveData m_curMissionSaveData = null;

    public enum GameplayState
    {
        BuildGrid,
        PlaceObstacles,
        FloodFillGrid,
        CreatePaths,
        Setup,
        SpawnEnemies,
        BossWave,
        Combat,
        Build,
        CutScene,
        Victory,
        Defeat
    }

    public enum InteractionState
    {
        Disabled,
        Idle,
        SelectedGatherer,
        SelectedTower,
        PreconstructionTower,
        SelectedCastle,
        SelecteObelisk,
        SelectRuin
    }

    public bool m_delayForQuest
    {
        get { return delayForQuest; }

        set
        {
            if (delayForQuest != value)
            {
                delayForQuest = value;
                OnDelayForQuestChanged?.Invoke(delayForQuest);
            }
        }
    }


    void Update()
    {
        // Do not run while in Cutscenes.
        if (m_watchingCutScene) return;

        // Input updates.
        HandleHotkeys();
        HandleMouseInput();

        // Preconstructed Building updates.
        PreconPeriodicTimer();
        PositionPrecon();
        HandlePreconMousePosition();

        NextWaveTimer();
        GameTimer();
    }

    void GameTimer()
    {
        m_totalTime += Time.deltaTime;
    }

    void NextWaveTimer()
    {
        if (m_delayForQuest) return;

        // When should we be counting
        switch (m_gameplayData.m_gameMode)
        {
            case MissionGameplayData.GameMode.Standard:
                if (m_gameplayState != GameplayState.Build) return;
                break;
            case MissionGameplayData.GameMode.Survival:
                if (m_gameplayState != GameplayState.SpawnEnemies && m_gameplayState != GameplayState.Build) return;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // Update time
        m_timeToNextWave -= Time.deltaTime;

        // Trigger next wave
        if (m_timeToNextWave <= 0)
        {
            ++Wave;
            StartNextWave();
        }
    }

    void StartNextWave()
    {
        switch (m_gameplayData.m_gameMode)
        {
            case MissionGameplayData.GameMode.Standard:

                // Is this a boss wave?
                if (m_bossWaves.Contains(Wave) && m_bossSequenceController)
                {
                    Debug.Log($"BOSS Wave {Wave} Chosen.");
                    UpdateGameplayState(GameplayState.BossWave);
                    return;
                }

                // Does a spawner have a cutscene request?
                string cutsceneName = null;
                foreach (StandardSpawner spawner in m_enemySpawners)
                {
                    spawner.SetNextCreepWave();

                    if (GameManager.Instance && spawner.GetNextCreepWave() is NewTypeCreepWave newTypeWave && !string.IsNullOrEmpty(newTypeWave.m_waveCutscene))
                    {
                        cutsceneName = newTypeWave.m_waveCutscene;
                    }
                }

                if (!string.IsNullOrEmpty(cutsceneName))
                {
                    UpdateGameplayState(GameplayState.CutScene);
                    OnCutSceneEnd += ResumeSpawnWave;
                    GameManager.Instance.RequestAdditiveSceneLoad(cutsceneName);
                    return;
                }

                // Else just spawn enemies.
                UpdateGameplayState(GameplayState.SpawnEnemies);
                break;


            case MissionGameplayData.GameMode.Survival:
                /*OnWaveCompleted?.Invoke(EnemiesCreatedThisWave, EnemiesKilledThisWave, CoresClaimedThisWave, DamageTakenThisWave);
                ResetWaveCompleteValues();*/
                foreach (EnemySpawner spawner in m_enemySpawners)
                {
                    spawner.UpdateCreepSpawners();
                }

                UpdateGameplayState(GameplayState.SpawnEnemies);

                m_castleController.RepairCastle();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    void ResumeSpawnWave()
    {
        OnCutSceneEnd -= ResumeSpawnWave;
        UpdateGameplayState(GameplayState.SpawnEnemies);
    }

    void HandleMouseInput()
    {
        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObj = hit.collider.gameObject;

            //If we hit a UI Game Object.
            if (EventSystem.current.IsPointerOverGameObject())
            {
                //Tell hovered selectable to deselect, we're on UI.
                if (m_hoveredSelectable != null)
                {
                    OnGameObjectHoveredExit?.Invoke(m_hoveredSelectable.gameObject);
                }

                HandleUIInteraction();
            }
            else //We hit a World Space Object.
            {
                HoverInteraction(hitObj);
                Mouse1Interaction();
                Mouse2Interaction();
            }
        }
    }

    void HandleHotkeys()
    {
        if (m_interactionState == InteractionState.Disabled) return;

        //Toggle Pause
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PauseHotkeyPressed();
        }

        //Toggle FFW
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            FastForwardHotkeyPressed();
        }
    }

    void HandleUIInteraction()
    {
        m_hoveredSelectable = null;
    }

    void Mouse1Interaction()
    {
        if (Input.GetMouseButtonUp(0) && m_interactionState != InteractionState.Disabled)
        {
            //If the object we're hovering is not currently the selected object.
            if (m_interactionState == InteractionState.PreconstructionTower)
            {
                if (!m_canAfford)
                {
                    Debug.Log("Build Tower Attempt: Can Afford is False.");
                    OnAlertDisplayed?.Invoke(m_pathRestrictedReason);
                    RequestPlayAudio(m_gameplayAudioData.m_cannotPlaceClip);
                    return;
                }

                if (!m_canPlace)
                {
                    Debug.Log("Build Tower Attempt: Can Place is False.");
                    OnAlertDisplayed?.Invoke(m_pathRestrictedReason);
                    RequestPlayAudio(m_gameplayAudioData.m_cannotPlaceClip);
                    return;
                }

                if (!m_canPath)
                {
                    Debug.Log("Build Tower Attempt: Can Path is False.");
                    OnAlertDisplayed?.Invoke(m_pathRestrictedReason);
                    RequestPlayAudio(m_gameplayAudioData.m_cannotPlaceClip);
                    return;
                }

                //Reset outline color. This will be overridden by Tower Precon functions.
                SetOutlineColor(true);
                BuildTower();
                return;
            }

            // && m_curSelectable != m_hoveredSelectable
            if (m_hoveredSelectable != null)
            {
                if (m_hoveredSelectable.m_selectedObjectType == Selectable.SelectedObjectType.ResourceWood)
                {
                    return;
                }

                //Debug.Log(m_hoveredSelectable + " : selected.");
                OnGameObjectSelected?.Invoke(m_hoveredSelectable.gameObject);
                //Clear Hoverable because we've selected.
                m_hoveredSelectable = null;
            }
            else if (m_curSelectable && m_hoveredSelectable == null)
            {
                //If we click, and we dont have a hoverable, and current selected is NOT a gatherer, deselect the object.
                //This allows us to pan while having the gatherer selected.
                if (m_curSelectable.m_selectedObjectType != Selectable.SelectedObjectType.Gatherer)
                {
                    OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
                }
            }
        }
    }

    void StartMission()
    {
        Debug.Log($"Mission Started.");
        
        m_totalTime = 0;
        
        m_missionStartTime = Time.time;
        Invoke(nameof(SendSteamMissionStartedInfo), m_minimumMissionLength);
        
        UpdateGameplayState(GameplayState.Build);
        ResourceManager.Instance.StartDepositTimer();
    }

    void Mouse2Interaction()
    {
        if (m_interactionState == InteractionState.Disabled) return;

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(1))
        {
            if (!m_curSelectable || m_curSelectable.m_selectedObjectType != Selectable.SelectedObjectType.Gatherer) return;

            if (!m_hoveredSelectable || m_hoveredSelectable.m_selectedObjectType != Selectable.SelectedObjectType.ResourceWood) return;

            if (m_gameplayState == GameplayState.Setup) StartMission();

            m_curSelectable.GetComponent<GathererController>().AddNodeToHarvestQueue(m_hoveredSelectable.GetComponent<ResourceNode>());
        }
        else if (!Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(1))
        {
            //If something is selected.
            if (m_hoveredSelectable != null || m_preconstructedTowerObj != null)
            {
                switch (m_interactionState)
                {
                    case InteractionState.Disabled:
                        break;
                    case InteractionState.Idle:
                        break;
                    case InteractionState.SelectedGatherer:
                        //Debug.Log("Command Requested on Gatherer.");
                        if (m_gameplayState == GameplayState.Setup) StartMission();
                        OnCommandRequested?.Invoke(m_hoveredSelectable.gameObject, m_hoveredSelectable.m_selectedObjectType);
                        break;
                    case InteractionState.SelectedTower:
                        OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
                        break;
                    case InteractionState.PreconstructionTower:
                        SetOutlineColor(true);
                        ClearPreconstructedTower();
                        m_interactionState = InteractionState.Idle;
                        break;
                    case InteractionState.SelectedCastle:
                        OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
                        break;
                    case InteractionState.SelecteObelisk:
                        OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
                        break;
                    case InteractionState.SelectRuin:
                        OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // If we right click on a Blueprint, remove it.
                if (m_hoveredSelectable != null && m_hoveredSelectable.m_selectedObjectType == Selectable.SelectedObjectType.Tower)
                {
                    TowerBlueprint blueprint = m_hoveredSelectable.GetComponent<TowerBlueprint>();
                    if (blueprint)
                    {
                        RemoveBlueprintFromList(blueprint);
                    }
                }
            }
            else if (m_curSelectable)
            {
                OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
                m_hoveredSelectable = null;
            }
        }
    }

    public void DeselectObject(Selectable selectedObj)
    {
        if (m_curSelectable == selectedObj)
        {
            OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
        }
    }

    void HoverInteraction(GameObject obj)
    {
        //DO WE CARE ABOUT HOVERING?
        if (m_interactionState == InteractionState.PreconstructionTower)
        {
            if (m_hoveredSelectable != null)
            {
                OnGameObjectHoveredExit?.Invoke(m_hoveredSelectable.gameObject);
                m_hoveredSelectable = null;
                return;
            }

            {
                return;
            }
        }


        //HOVERING

        //Is the object selectable?
        Selectable hitSelectable = obj.GetComponent<Selectable>();


        //If the object is selected, we dont want to handle Hover state.
        if (m_curSelectable != null && m_curSelectable == hitSelectable)
        {
            return;
        }

        //Exit hover if the hit obj is null, or if the obj is new
        //If we're hovering over something new.
        if (m_hoveredSelectable != hitSelectable && m_hoveredSelectable != null)
        {
            OnGameObjectHoveredExit?.Invoke(m_hoveredSelectable.gameObject);
        }

        //If we're not hovering anything we dont need to go further.
        if (hitSelectable == null)
        {
            m_hoveredSelectable = null;
            return;
        }

        //Assign new hovered object.
        if (m_hoveredSelectable != hitSelectable)
        {
            m_hoveredSelectable = hitSelectable;
            OnGameObjectHoveredEnter?.Invoke(m_hoveredSelectable.gameObject);
        }
    }

    private void SetOutlineColor(bool canBuild)
    {
        //Debug.Log($"Trying to change color: {canBuild}");
        Color color = canBuild ? m_selectionColors.m_outlineBaseColor : m_selectionColors.m_outlineRestrictedColor;
        m_selectedOutlineMaterial.SetColor("_Outline_Color", color);

        if (m_preconstructedTower)
        {
            m_preconstructedTower.SetRangeCircleColor(color);
        }
    }

    private void Awake()
    {
        m_unlockedStructures = new Dictionary<TowerData, int>();
        PlayerDataManager.Instance.SetProgressionTable(m_progressionTable);
        PlayerDataManager.OnUnlockableUnlocked += UnlockableUnlocked;
        PlayerDataManager.OnUnlockableLocked += UnlockableLocked;

        SortedAndUnlocked sortedAndUnlocked = PlayerDataManager.Instance.GetSortedUnlocked();

        m_unlockedStructures = sortedAndUnlocked.m_unlockedStructures;
        m_unlockedTowers = sortedAndUnlocked.m_unlockedTowers;

        m_selectedOutlineMaterial = Resources.Load<Material>("Materials/Mat_OutlineSelected");

        if (!Instance)
        {
            Instance = this;
        }

        m_mainCamera = Camera.main;

        if (m_enemyGoal != null)
        {
            m_goalPointPos = new Vector2Int((int)m_enemyGoal.position.x, (int)m_enemyGoal.position.z);
        }

        OnGameplayStateChanged += GameplayManagerStateChanged;
        OnGamePlaybackChanged += GameplayPlaybackChanged;
        OnGameObjectSelected += GameObjectSelected;
        OnGameObjectDeselected += GameObjectDeselected;

        UIPopupManager.OnPopupManagerPopupsOpen += PopupManagerPopupsOpen;

        m_delayForQuest = m_gameplayData.m_delayForQuest;
        m_blueprintList = new List<TowerBlueprint>();
    }

    private void PopupManagerPopupsOpen(bool value)
    {
        if (value)
        {
            GameplayManager.Instance.UpdateGamePlayback(GameplayManager.GameSpeed.Paused);
            GameplayManager.Instance.UpdateInteractionState(GameplayManager.InteractionState.Disabled);
        }
        else
        {
            GameplayManager.Instance.UpdateGamePlayback(GameplayManager.GameSpeed.Normal);
            GameplayManager.Instance.UpdateInteractionState(GameplayManager.InteractionState.Idle);
        }
    }

    public MissionSaveData GetCurrentMissionSaveData()
    {
        return m_curMissionSaveData;
    }

    private void UnlockableLocked(ProgressionUnlockableData unlockableData)
    {
        ProgressionRewardData rewardData = unlockableData.GetRewardData();

        switch (rewardData.RewardType)
        {
            // TOWERS
            case "Tower":
                TowerData towerData = rewardData.GetReward();
                m_unlockedTowers.Remove(towerData);
                OnUnlockedTowersUpdated?.Invoke(towerData, false);
                break;

            // STRUCTURES
            case "Structure":
                // Define the data that has been revoked.
                TowerData structureData = rewardData.GetReward();

                int qty = rewardData.GetRewardQty();

                //Remove the QTY.
                if (m_unlockedStructures.ContainsKey(structureData))
                {
                    //Assure we don't go below 0 quantity, subtract the smallest of qty locked or current qty.
                    qty = Math.Min(qty, m_unlockedStructures.GetValueOrDefault(structureData, 0));
                    m_unlockedStructures[structureData] += -qty;

                    //Invoke the addition of type & stock
                    OnUnlockedStucturesUpdated?.Invoke(structureData, -qty);
                }

                break;

            // TYPE NOT FOUND
            default:
                break;
        }
    }

    private void UnlockableUnlocked(ProgressionUnlockableData unlockableData)
    {
        ProgressionRewardData rewardData = unlockableData.GetRewardData();

        if (rewardData == null) return;

        switch (rewardData.RewardType)
        {
            // TOWERS
            case "Tower":

                ProgressionRewardTower towerRewardData = rewardData as ProgressionRewardTower;
                TowerData towerData = towerRewardData.GetReward();
                OnUnlockedTowersUpdated?.Invoke(towerData, true);
                break;

            // STRUCTURES
            case "Structure":

                //Type specification
                ProgressionRewardStructure structureRewardData = rewardData as ProgressionRewardStructure;

                // Define the data that has been awarded.
                TowerData structureData = structureRewardData.GetReward();

                //Does Unlocked Stuctures already have an entry for this? If so, update the quantity. If not add it.
                int qty = structureRewardData.GetRewardQty();
                if (m_unlockedStructures.ContainsKey(structureData))
                {
                    m_unlockedStructures[structureData] += qty;
                }
                else
                {
                    m_unlockedStructures[structureData] = m_unlockedStructures.GetValueOrDefault(structureData, 0) + qty;
                }

                //Invoke the addition of type & stock
                OnUnlockedStucturesUpdated?.Invoke(structureData, qty);
                break;

            // TYPE NOT FOUND
            default:

                Debug.Log($"No case for {rewardData.RewardType}.");
                break;
        }
    }

    void OnDestroy()
    {
        OnGameplayStateChanged -= GameplayManagerStateChanged;
        OnGamePlaybackChanged -= GameplayPlaybackChanged;
        OnGameObjectSelected -= GameObjectSelected;
        OnGameObjectDeselected -= GameObjectDeselected;
        PlayerDataManager.OnUnlockableUnlocked -= UnlockableUnlocked;
        PlayerDataManager.OnUnlockableLocked -= UnlockableLocked;
        UIPopupManager.OnPopupManagerPopupsOpen -= PopupManagerPopupsOpen;
    }

    private void GameplayPlaybackChanged(GameSpeed state)
    {
        //
    }

    private void GameplayManagerStateChanged(GameplayState state)
    {
        //
    }

    void Start()
    {
        UpdateGameplayState(GameplayState.BuildGrid);
        m_timeToNextWave = m_gameplayData.m_firstBuildDuration;

        /*var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        m_scriptableRenderer = pipeline.scriptableRenderer;
        m_scriptableRendererFeature = m_scriptableRenderer.supportedRenderingFeatures.Find(feature => feature is FullscreenEffect);*/
        m_curMissionSaveData = PlayerDataManager.Instance.GetMissionSaveData(gameObject.scene.name);
    }

    public enum GameSpeed
    {
        Paused,
        Normal,
    }

    private int m_playbackSpeed = 1;

    public void UpdateGameSpeed()
    {
        // If the game is paused, store m_playbackSpeed which will be set when unpaused.
        // If the game is in play, update playback speed immediately.

        int minSpeed = 1;
        int maxSpeed = 2;
        m_playbackSpeed = m_playbackSpeed == minSpeed ? maxSpeed : minSpeed;

        if (m_gameSpeed == GameSpeed.Normal) Time.timeScale = m_playbackSpeed;

        // AUDIO
        if (m_playbackSpeed == minSpeed)
        {
            RequestPlayAudio(m_gameplayAudioData.m_ffwOff);
        }
        else
        {
            RequestPlayAudio(m_gameplayAudioData.m_ffwOn);
        }

        OnGameSpeedChanged?.Invoke(m_playbackSpeed);
    }

    public void UpdateGamePlayback(GameSpeed newSpeed)
    {
        if (newSpeed == m_gameSpeed) return;

        m_gameSpeed = newSpeed;
        Debug.Log($"Gameplay Manager: Game Speed is now {m_gameSpeed}.");

        //Include FFW state
        //When we get a request check current game speed to identify if we return to normal or return to FFW when leaving pause.
        //What do we send from the hotkey/button presses?
        //
        switch (newSpeed)
        {
            case GameSpeed.Paused:
                ToggleBlueprintPathDirections(true); // Re-enable path directions to include blueprint towers.
                Time.timeScale = 0;
                RequestPlayAudio(m_gameplayAudioData.m_pause);
                break;
            case GameSpeed.Normal:
                if (m_interactionState == InteractionState.PreconstructionTower && m_preconstructedTower is TowerBlueprint)
                {
                    OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
                    ClearPreconstructedTower(); // Clear precon tower if we're currently placing blueprints.
                }

                ToggleBlueprintPathDirections(false); // Disable blueprint tower path directions.
                Time.timeScale = m_playbackSpeed;
                Debug.Log($"Time Scale now : {Time.timeScale}");
                RequestPlayAudio(m_gameplayAudioData.m_play);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newSpeed), newSpeed, null);
        }

        OnGamePlaybackChanged?.Invoke(newSpeed);
    }

    void PauseHotkeyPressed()
    {
        if (m_gameSpeed == GameSpeed.Normal)
        {
            UpdateGamePlayback(GameSpeed.Paused);
        }
        else
        {
            UpdateGamePlayback(GameSpeed.Normal);
        }
    }

    void FastForwardHotkeyPressed()
    {
        UpdateGameSpeed();
    }

    public void UpdateGameplayState(GameplayState newState)
    {
        m_gameplayState = newState;
        Debug.Log($"Gameplay State is now: {m_gameplayState}");

        switch (m_gameplayState)
        {
            case GameplayState.BuildGrid:
                break;
            case GameplayState.PlaceObstacles:
                break;
            case GameplayState.FloodFillGrid:
                break;
            case GameplayState.CreatePaths:
                break;
            case GameplayState.Setup:
                UpdateInteractionState(InteractionState.Idle);
                UpdateGamePlayback(GameSpeed.Normal);
                break;
            case GameplayState.SpawnEnemies:


                switch (m_gameplayData.m_gameMode)
                {
                    case MissionGameplayData.GameMode.Standard:
                        RequestPlayAudio(m_gameplayAudioData.m_waveStartClip);
                        m_timeToNextWave = m_gameplayData.m_buildDuration;
                        break;
                    case MissionGameplayData.GameMode.Survival:
                        m_timeToNextWave = m_gameplayData.m_survivalWaveDuration;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
            case GameplayState.BossWave:
                m_timeToNextWave = m_gameplayData.m_afterBossBuildDuration;
                ObjectPoolManager.SpawnObject(m_bossSequenceController.gameObject, Vector3.zero, Quaternion.identity, null, ObjectPoolManager.PoolType.Enemy);
                break;
            case GameplayState.Combat:
                break;
            case GameplayState.Build:
                break;
            case GameplayState.CutScene:
                break;
            case GameplayState.Victory:
                SendSteamCompletionInfo();
                HandleMissionProgression();
                PlayerDataManager.Instance.UpdateMissionSaveData(gameObject.scene.name, 2, Wave, m_perfectWavesCompleted);
                HandleVictorySequence();
                break;
            case GameplayState.Defeat:
                SendSteamCompletionInfo();
                PlayerDataManager.Instance.UpdateMissionSaveData(gameObject.scene.name, 1, 0, 0);
                HandleDefeatSequence();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        OnGameplayStateChanged?.Invoke(newState);
    }

    private float m_missionStartTime;
    private float m_minimumMissionLength = 60;

    // When the mission starts, tell steam that this mission has been attempted.
    private void SendSteamMissionStartedInfo()
    {
        //Did the mission last at least N seconds? We want to weed out short sessions.
        //Increment "_Started" for this mission stat.
        if (GameManager.Instance == null)
        {
            Debug.Log($"No Game Manager. Not updating Steam Stats.");
            return;
        }

        string missionID = GameManager.Instance.m_curMission.m_missionID;

        Debug.Log($"SteamStats: Attempting to increment {missionID}_Started");
        
        SteamStatsManager.IncrementStat(missionID + "_Started"); // Result
    }

    // When a mission is completed, determine the state of completion.
    private void SendSteamCompletionInfo()
    {
        if (GameManager.Instance == null)
        {
            Debug.Log($"SendSteamStats: No Game Manager. Not updating Steam Stats.");
            return;
        }
        
        float missionDuration = Time.time - m_missionStartTime;
        if (missionDuration < m_minimumMissionLength)
        {
            Debug.Log($"SendSteamStats: Mission Length of {missionDuration} seconds not valid.");
            return;
        }
        
        Debug.Log($"SendSteamStats: Begin Sending Steam Stats.");

        // Race condition, im not sure if the curMissionSaveData is already updated before this function fires. Maybe send a copy to this function?
        string missionID = GameManager.Instance.m_curMission.m_missionID;

        // This chunk will only log victories and defeats until a mission is defeated.
        if (m_gameplayState == GameplayState.Victory && m_curMissionSaveData.m_missionCompletionRank == 1) // If we won, and the mission is previously unbeaten.
        {
            // _WonCount
            SteamStatsManager.IncrementStat(missionID + "_WonCount");
            // _WavesToWin
            SteamStatsManager.SetStat(missionID + "_WavesToWin", Wave);
            // _TimeToWin
            SteamStatsManager.SetStat(missionID + "_TimeToWin", m_missionStartTime - Time.time);
            // _AttemptsToWin
            SteamStatsManager.SetStat(missionID + "_AttemptsToWin", m_curMissionSaveData.m_missionAttempts);
        }
        else if (m_gameplayState == GameplayState.Defeat && m_curMissionSaveData.m_missionCompletionRank == 1)
        {
            // _LostCount
            SteamStatsManager.IncrementStat(missionID + "_LostCount");
            // _WavesToLose
            SteamStatsManager.SetStat(missionID + "_WavesToLose", Wave);
            // _TimeToLose
            SteamStatsManager.SetStat(missionID + "_TimeToLose", m_missionStartTime - Time.time);
        }

        // Endless ends when the spire is destroyed, and is always a victory.
        if (m_endlessModeActive)
        {
            if (m_curMissionSaveData.m_waveHighScore < Wave)
            {
                SteamStatsManager.SetStat(missionID + "_WaveHighScore", Wave);
                
                SteamStatsManager.SetStat(missionID + "_TimeToWinEndless", m_missionStartTime - Time.time);
            }

            if (m_curMissionSaveData.m_perfectWaveScore < m_perfectWavesCompleted)
            {
                SteamStatsManager.SetStat(missionID + "_PerfectWavesCompleted", m_perfectWavesCompleted);
            }
        }

        Debug.Log($"SendSteamStats: Completed Sending Steam Stats.");
    }

    private void ResetWaveCompleteValues()
    {
        EnemiesCreatedThisWave = 0;
        EnemiesKilledThisWave = 0;
        CoresClaimedThisWave = 0;
        DamageTakenThisWave = 0;
    }

    public void UpdateInteractionState(InteractionState newState)
    {
        switch (newState)
        {
            case InteractionState.Disabled:
                // Unhook selectables.
                if (m_curSelectable) OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);

                // Remove Precon tower.
                ClearPreconstructedTower();
                break;
            case InteractionState.Idle:
                break;
            case InteractionState.SelectedGatherer:
                break;
            case InteractionState.SelectedTower:
                break;
            case InteractionState.PreconstructionTower:
                break;
            case InteractionState.SelectedCastle:
                break;
            case InteractionState.SelecteObelisk:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        m_interactionState = newState;
        Debug.Log($"Interaction state is now: {m_interactionState}");
    }

    private void GameObjectSelected(GameObject obj)
    {
        //Deselect current selection.
        if (m_curSelectable)
        {
            if (m_curSelectable.gameObject != obj)
            {
                OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
            }
        }

        Selectable objSelectable = obj.GetComponent<Selectable>();
        m_curSelectable = objSelectable;

        switch (objSelectable.m_selectedObjectType)
        {
            case Selectable.SelectedObjectType.ResourceWood:
                break;
            case Selectable.SelectedObjectType.ResourceStone:
                break;
            case Selectable.SelectedObjectType.Tower:
                if (m_preconstructedTowerObj)
                {
                    UpdateInteractionState(InteractionState.PreconstructionTower);
                }
                else
                {
                    UpdateInteractionState(InteractionState.SelectedTower);
                }

                break;
            case Selectable.SelectedObjectType.Building:
                if (m_preconstructedTowerObj)
                {
                    UpdateInteractionState(InteractionState.PreconstructionTower);
                }
                else
                {
                    UpdateInteractionState(InteractionState.SelectedTower);
                }

                break;
            case Selectable.SelectedObjectType.Gatherer:
                UpdateInteractionState(InteractionState.SelectedGatherer);
                break;
            case Selectable.SelectedObjectType.Castle:
                UpdateInteractionState(InteractionState.SelectedCastle);
                break;
            case Selectable.SelectedObjectType.Obelisk:
                UpdateInteractionState(InteractionState.SelecteObelisk);
                break;
            case Selectable.SelectedObjectType.Ruin:
                break;
            default:
                // No function setup for this Selectable Type.
                break;
        }
    }

    private void GameObjectDeselected(GameObject obj)
    {
        m_curSelectable = null;
        m_interactionState = InteractionState.Idle;
    }

    public void AddGathererToList(GathererController gatherer, ResourceManager.ResourceType type)
    {
        switch (type)
        {
            case ResourceManager.ResourceType.Wood:
                m_woodGathererList.Add(gatherer);
                ResourceManager.Instance.UpdateWoodGathererAmount(1);
                break;
            case ResourceManager.ResourceType.Stone:
                m_stoneGathererList.Add(gatherer);
                ResourceManager.Instance.UpdateStoneGathererAmount(1);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        OnGathererAdded?.Invoke(gatherer);
    }

    public void RemoveGathererFromList(GathererController gatherer, ResourceManager.ResourceType type)
    {
        switch (type)
        {
            case ResourceManager.ResourceType.Wood:

                for (int i = 0; i < m_woodGathererList.Count; ++i)
                {
                    if (m_woodGathererList[i] == gatherer)
                    {
                        m_woodGathererList.RemoveAt(i);
                    }
                }

                break;
            case ResourceManager.ResourceType.Stone:
                for (int i = 0; i < m_stoneGathererList.Count; ++i)
                {
                    if (m_stoneGathererList[i] == gatherer)
                    {
                        m_stoneGathererList.RemoveAt(i);
                    }
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        OnGathererRemoved?.Invoke(gatherer);
    }

    public void CreatePreconBuilding(TowerData towerData)
    {
        ClearPreconstructedTower();

        m_preconstructedTowerData = towerData;
        m_preconBuildingWidth = towerData.m_buildingSize.x;
        m_preconBuildingHeight = towerData.m_buildingSize.y;
        m_preconxOffset = m_preconBuildingWidth % 2 == 0 ? 0.5f : 0;
        m_preconzOffset = m_preconBuildingHeight % 2 == 0 ? 0.5f : 0;

        // Spawn the tower at the mouse position.
        Vector3 spawnPosition = new Vector3();
        Vector2Int gridPos = new Vector2Int();
        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);

        if (ray.direction.y == 0) return; // Ensure the ray isn't parallel to the XZ plane

        float t = -ray.origin.y / ray.direction.y; // Solve for t where Y = 0
        Vector3 worldPos = ray.origin + t * ray.direction; // Get intersection point

        Debug.Log($"Precon Mouse Position: {worldPos}.");

        worldPos.x += m_preconxOffset;
        worldPos.z += m_preconzOffset;

        Debug.Log($"Precon Building Position: {worldPos}.");

        gridPos = Util.FindNearestValidCellPos(worldPos, m_preconstructedTowerData.m_buildingSize);

        spawnPosition = new Vector3(gridPos.x, 0.7f, gridPos.y);

        Debug.Log($"Precon Building Spawn Position: {spawnPosition}");

        // Create the new object.
        m_preconstructedTowerObj = Instantiate(towerData.m_prefab, spawnPosition, Quaternion.identity);
        m_preconstructedTowerPos = gridPos;
        m_preconstructedTower = m_preconstructedTowerObj.GetComponent<Tower>();

        // Set the precon Cells and neighbors.
        List<Cell> newCells = Util.GetCellsFromPos(m_preconstructedTowerPos, m_preconBuildingWidth, m_preconBuildingHeight);

        if (newCells == null)
        {
            Debug.Log($"newCells is null.");
            return;
        }

        m_preconstructedTowerCells = newCells;
        m_preconNeighborCells = GetNeighborCells(m_preconstructedTowerPos, m_preconBuildingWidth, m_preconBuildingHeight);
        OnPreconBuildingMoved?.Invoke(m_preconstructedTowerCells);

        // Tween the object's scale.
        m_preconstructedTowerObj.transform.localScale = Vector3.one * 0.5f;
        m_preconstructedTowerObj.transform.DOScale(Vector3.one, 0.15f)
            .SetEase(Ease.InOutBack)
            .SetUpdate(true);

        // Update building inventory
        if (m_unlockedStructures.TryGetValue(m_preconstructedTowerData, out int quantity))
        {
            m_qty = quantity;
        }
        else
        {
            m_qty = -1;
        }

        OnGameObjectSelected?.Invoke(m_preconstructedTowerObj);

        m_canAfford = false;
        m_canBuild = false;
        m_canPlace = false;
    }

    private List<Cell> m_preconstructedTowerCells;
    private List<Cell> m_preconNeighborCells;
    private int m_preconBuildingWidth;
    private int m_preconBuildingHeight;
    private float m_preconxOffset;
    private float m_preconzOffset;

    private void HandlePreconMousePosition()
    {
        if (m_interactionState != InteractionState.PreconstructionTower) return;

        // Spawn the tower at the mouse position
        Vector2Int gridPos = new Vector2Int();
        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);

        if (ray.direction.y == 0) return; // Ensure the ray isn't parallel to the XZ plane

        float t = -ray.origin.y / ray.direction.y; // Solve for t where Y = 0
        Vector3 worldPos = ray.origin + t * ray.direction; // Get intersection point

        worldPos.x += m_preconxOffset;
        worldPos.z += m_preconzOffset;

        gridPos = Util.FindNearestValidCellPos(worldPos, m_preconstructedTowerData.m_buildingSize);

        if (gridPos == m_preconstructedTowerPos) return;

        m_preconstructedTowerPos = gridPos;

        List<Cell> newCells = Util.GetCellsFromPos(m_preconstructedTowerPos, m_preconBuildingWidth, m_preconBuildingHeight);

        if (newCells == null) return;

        m_preconstructedTowerCells = newCells;
        m_preconNeighborCells = GetNeighborCells(m_preconstructedTowerPos, m_preconBuildingWidth, m_preconBuildingHeight);
        OnPreconBuildingMoved?.Invoke(m_preconstructedTowerCells);
    }

    void CheckPreconRestrictions()
    {
        if (m_preconstructedTowerCells == null || m_preconNeighborCells == null) return;

        m_canPlace = IsPlacementRestricted(m_preconstructedTowerCells);
        m_canPath = IsPathingRestricted(m_preconNeighborCells);
        m_canAfford = IsCurrencyRestricted();
        m_canBuild = m_canAfford && m_canPlace && m_canPath;

        SetOutlineColor(m_canBuild);
        DrawPreconCellObjs();
    }

    private bool IsCurrencyRestricted()
    {
        //Check cost & banks
        int curStone = ResourceManager.Instance.GetStoneAmount();
        int curWood = ResourceManager.Instance.GetWoodAmount();
        ValueTuple<int, int> cost = m_preconstructedTower.GetTowercost();

        if (curStone < cost.Item1)
        {
            m_pathRestrictedReason = m_UIStringData.m_stoneRequirementNotMet;
            return false;
        }

        if (curWood < cost.Item2)
        {
            m_pathRestrictedReason = m_UIStringData.m_woodRequirmentNotMet;
            return false;
        }

        if (m_qty == 0)
        {
            m_pathRestrictedReason = m_UIStringData.m_buildRestrictedQuantityNotMet;
            return false;
        }

        return true;
    }

    private bool IsPlacementRestricted(List<Cell> cells)
    {
        if (cells == null || cells.Count != m_preconBuildingWidth * m_preconBuildingHeight)
        {
            //Debug.Log($"Invalid Position.");
            return false;
        }

        // Check the individual cells
        for (int i = 0; i < cells.Count; ++i)
        {
            Cell curCell = cells[i];

            if (curCell.HasActors())
            {
                //Debug.Log($"Cannot Place: There are actors on this cell.");
                m_pathRestrictedReason = m_UIStringData.m_buildRestrictedActorsOnCell;
                return false;
            }

            //If we're hovering on the exit cell
            if (m_preconstructedTowerPos == Util.GetVector2IntFrom3DPos(GameplayManager.Instance.m_enemyGoal.position))
            {
                //Debug.Log($"Cannot Place: This is the exit cell.");
                return false;
            }

            if (curCell.m_isOccupied)
            {
                if (curCell.m_occupant != null && curCell.m_occupant.GetComponent<TowerBlueprint>() == null) // If we DO have a tower blueprint, we're ok placing here.
                {
                    //Debug.Log($"Cannot Place: This cell is already occupied.");
                    m_pathRestrictedReason = m_UIStringData.m_buildRestrictedOccupied;
                    return false;
                }
            }

            if (curCell.m_isOutOfBounds)
            {
                m_pathRestrictedReason = m_UIStringData.m_buildRestrictedOutOfBounds;
                return false;
            }

            // If the current cell is build restricted (bridges, obelisk ground, pathways), not a valid spot.
            // Or if the current cell has a critter in it.
            if (curCell.IsBuildRestricted())
            {
                //Debug.Log($"Cannot Place: This cell is build restricted.");
                m_pathRestrictedReason = m_UIStringData.m_buildRestrictedRestricted;
                return false;
            }
        }

        return true;
    }

    bool IsPathingRestricted(List<Cell> cells)
    {
        if (cells == null) return false;

        // This is checking pathing from each neighbor of the precon building.
        for (int i = 0; i < cells.Count; ++i)
        {
            Cell cell = cells[i];
            if (cell.m_isOccupied) continue;

            List<Vector2Int> testPath = AStar.GetExitPath(cell.m_cellPos, m_goalPointPos);

            if (testPath != null)
            {
                ListPool<Vector2Int>.Release(testPath);
                continue;
            }

            List<Cell> islandCells = ListPool<Cell>.Get();
            islandCells.AddRange(AStar.FindIsland(cell)); // Assuming `FindIsland` returns a list.

            if (islandCells.Count == 0 && cell.m_actorCount > 0)
            {
                ListPool<Cell>.Release(islandCells);
                m_pathRestrictedReason = m_UIStringData.m_buildRestrictedActorsInIsland;
                return false;
            }

            foreach (Cell islandCell in islandCells)
            {
                if (islandCell.m_actorCount > 0)
                {
                    ListPool<Cell>.Release(islandCells);
                    m_pathRestrictedReason = m_UIStringData.m_buildRestrictedActorsInIsland;
                    return false;
                }
            }

            ListPool<Cell>.Release(islandCells);
        }

        // EXITS AND SPAWNERS
        // Check to see if any of our UnitPaths have no path.
        if (!GridManager.Instance.m_spawnPointsAccessible)
        {
            m_pathRestrictedReason = m_UIStringData.m_buildRestrictedBlocksPath;
            return false;
        }

        // Check that exits can path to one another.
        int exitsPathable = 0;
        for (int x = 0; x < m_castleController.m_castleEntrancePoints.Count; ++x)
        {
            bool startObjPathable = false;
            GameObject startObj = m_castleController.m_castleEntrancePoints[x];
            Vector2Int startPos = Util.GetVector2IntFrom3DPos(startObj.transform.position);

            // Define the end
            for (int z = 0; z < m_castleController.m_castleEntrancePoints.Count; ++z)
            {
                GameObject endObj = m_castleController.m_castleEntrancePoints[z];
                Vector2Int endPos = Util.GetVector2IntFrom3DPos(endObj.transform.position);

                // Skip if they are the same
                if (startObj == endObj)
                {
                    continue;
                }

                // Path from Start to End and exclude the Game's Goal cell.
                List<Vector2Int> testPath = AStar.FindExitPath(startPos, endPos, m_preconstructedTowerPos, m_goalPointPos);
                if (testPath != null)
                {
                    ++exitsPathable;
                    startObjPathable = true;
                    ListPool<Vector2Int>.Release(testPath);
                    break;
                }
            }

            // If we couldn't path to any other exit, check spawners
            if (!startObjPathable)
            {
                for (int y = 0; y < m_enemySpawners.Count; ++y)
                {
                    Vector2Int spawnerPos = Util.GetVector2IntFrom3DPos(m_enemySpawners[y].GetSpawnPointTransform().position);
                    List<Vector2Int> testPath = AStar.FindExitPath(spawnerPos, startPos, m_preconstructedTowerPos, m_goalPointPos);
                    if (testPath != null)
                    {
                        ++exitsPathable;
                        ListPool<Vector2Int>.Release(testPath);
                        break;
                    }
                }
            }
        }

        // If the number of pathable exits is less than the number of exits, return false
        if (exitsPathable < m_castleController.m_castleEntrancePoints.Count)
        {
            m_pathRestrictedReason = m_UIStringData.m_buildRestrictedBlocksPath;
            return false;
        }

        return true;
    }


    private void PositionPrecon()
    {
        if (m_interactionState != InteractionState.PreconstructionTower) return;

        //Define the new destination of the Precon Tower Obj. Offset the tower on Y.
        Vector3 moveToPosition = new Vector3(m_preconstructedTowerPos.x - m_preconxOffset, 0.7f, m_preconstructedTowerPos.y - m_preconzOffset);

        //Position the precon Tower at the cursor position.
        m_preconstructedTowerObj.transform.position = Vector3.Lerp(m_preconstructedTowerObj.transform.position,
            moveToPosition, 20f * Time.unscaledDeltaTime);
    }

    private float m_preconPeriodTimeElapsed;
    private float m_preconPeriod = 0.1f;

    void PreconPeriodicTimer()
    {
        if (m_interactionState != InteractionState.PreconstructionTower) return;

        m_preconPeriodTimeElapsed += Time.unscaledDeltaTime;

        if (m_preconPeriodTimeElapsed > m_preconPeriod)
        {
            CheckPreconRestrictions();
            m_preconPeriodTimeElapsed = 0;
        }
    }

    public List<Cell> GetNeighborCells(Vector2Int pos, int width, int height)
    {
        List<Cell> neighborCells = new List<Cell>();
        Vector2Int bottomLeftCellPos = pos;
        bottomLeftCellPos.x -= (width + 2) / 2;
        bottomLeftCellPos.y -= (height + 2) / 2;

        int index = 0;
        for (int x = 0; x < width + 2; ++x)
        {
            for (int z = 0; z < height + 2; ++z)
            {
                // Skip the middle area and corners
                if ((x > 0 && x < width + 1) && (z > 0 && z < height + 1)) continue; // Inside box
                if (x == 0 && z == 0) continue; // SW corner
                if (x == 0 && z == height + 1) continue; // NW corner
                if (x == width + 1 && z == 0) continue; // SE corner
                if (x == width + 1 && z == height + 1) continue; // NE corner

                // Calculate position for this neighbor
                Vector3 objPos = new Vector3(bottomLeftCellPos.x + x, 0, bottomLeftCellPos.y + z);
                Cell neighborCell = Util.GetCellFrom3DPos(objPos);
                if (neighborCell != null) neighborCells.Add(neighborCell);
            }
        }

        return neighborCells;
    }

    public TowerData GetPreconTowerData()
    {
        return m_preconstructedTowerData;
    }

    private Material m_drawCellMaterial;

    private void DrawPreconCellObjs()
    {
        if (m_preconstructedTowerCells == null) return;

        if (m_invalidCellObjs == null) m_invalidCellObjs = new List<InvalidCell>();

        int totalCells = Math.Max(m_preconstructedTowerCells.Count, m_invalidCellObjs.Count);

        for (int i = 0; i < totalCells; ++i)
        {
            if (i >= m_invalidCellObjs.Count)
            {
                // Spawn new object if needed
                Cell spawnCell = m_preconstructedTowerCells[i];
                Vector3 spawnPos = new Vector3(spawnCell.m_cellPos.x, 0, spawnCell.m_cellPos.y);
                GameObject newCellVisualObj = ObjectPoolManager.SpawnObject(m_invalidCellObj.gameObject, spawnPos, quaternion.identity, transform);
                InvalidCell invalidCell = newCellVisualObj.GetComponent<InvalidCell>();
                m_invalidCellObjs.Add(invalidCell);
            }

            if (i < m_preconstructedTowerCells.Count)
            {
                // Enable and update cell
                if (m_canAfford && m_canPath && m_canPlace)
                {
                    m_invalidCellObjs[i].CellIsBuildable = !m_preconstructedTowerCells[i].m_isOccupied && !m_preconstructedTowerCells[i].m_isBuildRestricted && m_preconstructedTowerCells[i].m_critterCount == 0;
                }
                else
                {
                    m_invalidCellObjs[i].CellIsBuildable = false;
                }


                Cell cell = m_preconstructedTowerCells[i];
                Vector3 pos = new Vector3(cell.m_cellPos.x, 0, cell.m_cellPos.y);
                m_invalidCellObjs[i].CurrentCellPosition = pos;

                if (!m_invalidCellObjs[i].gameObject.activeSelf)
                {
                    m_invalidCellObjs[i].transform.position = pos;
                    m_invalidCellObjs[i].gameObject.SetActive(true);
                }
            }
            else
            {
                // Disable extra objects
                if (m_invalidCellObjs[i].gameObject.activeSelf)
                    m_invalidCellObjs[i].gameObject.SetActive(false);
            }
        }
    }


    private void HidePreconCellObjs()
    {
        for (int i = 0; i < m_invalidCellObjs.Count; ++i)
        {
            m_invalidCellObjs[i].gameObject.SetActive(false);
        }
    }

    private string m_pathRestrictedReason;

    public void ClearPreconstructedTower()
    {
        if (m_preconstructedTowerObj)
        {
            OnGameObjectDeselected?.Invoke(m_preconstructedTowerObj);
            Destroy(m_preconstructedTowerObj);
        }

        HidePreconCellObjs();
        m_preconstructedTowerPos = new Vector2Int(999, 999);
        m_preconstructedTowerData = null;
        m_preconstructedTowerObj = null;
        m_preconstructedTower = null;
        OnPreconBuildingClear?.Invoke();
    }

    public void BuildTower()
    {
        // If there is a blueprint tower in this cell, remove it.
        for (var i = 0; i < m_blueprintList.Count; i++)
        {
            TowerBlueprint blueprint = m_blueprintList[i];
            Cell towerCell = Util.GetCellFrom3DPos(blueprint.transform.position);
            if (towerCell.m_cellPos == m_preconstructedTowerPos)
            {
                Debug.Log($"Build Tower: Removing Blueprint in Tower destination.");
                SellTower(blueprint, 0, 0);
            }
        }

        Vector3 gridPos = new Vector3(m_preconstructedTowerPos.x - m_preconxOffset, 0.0f, m_preconstructedTowerPos.y - m_preconzOffset);

        //We want to place towers to they look away from the Castle.
        // Calculate the direction vector from the target object to the current object.
        Vector3 direction = m_enemyGoal.position - m_preconstructedTowerObj.transform.position;

        // Calculate the rotation angle to make the new object face away from the target.
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 180f;

        Debug.Log($"Build Tower: Spawning Tower {m_preconstructedTowerData.m_prefab.name} at {gridPos}.");
        GameObject newTowerObj = ObjectPoolManager.SpawnObject(m_preconstructedTowerData.m_prefab, gridPos, Quaternion.identity, null, ObjectPoolManager.PoolType.Tower);
        Tower newTower = newTowerObj.GetComponent<Tower>();
        newTower.GetTurretTransform().rotation = Quaternion.Euler(0, angle, 0);
        newTower.SetupTower();
        newTower.SetRangeCircleColor(m_selectionColors.m_outlineBaseColor);

        //Update banks
        ValueTuple<int, int> cost = newTower.GetTowercost();
        if (cost.Item1 > 0 || cost.Item2 > 0)
        {
            if (cost.Item1 > 0)
            {
                ResourceManager.Instance.UpdateStoneAmount(-cost.Item1);
            }

            if (cost.Item2 > 0)
            {
                ResourceManager.Instance.UpdateWoodAmount(-cost.Item2);
            }

            IngameUIController.Instance.SpawnCurrencyAlert(cost.Item2, cost.Item1, false, newTowerObj.transform.position);
        }


        //Update Quantities
        if (m_qty != -1 && m_unlockedStructures.ContainsKey(m_preconstructedTowerData))
        {
            m_unlockedStructures[m_preconstructedTowerData] = Math.Max(0, m_unlockedStructures.GetValueOrDefault(m_preconstructedTowerData, 0) - 1);
            m_qty = m_unlockedStructures[m_preconstructedTowerData];
        }

        OnTowerBuild?.Invoke(m_preconstructedTowerData, newTowerObj);

        //If this was the last of our stock, leave precon. (Emulate a mouse2 click while in precon state)
        if (m_qty == 0)
        {
            SetOutlineColor(true);
            ClearPreconstructedTower();
            UpdateInteractionState(InteractionState.Idle);
        }
    }

    // Clear Blueprint Tower Models -- Called via CombatView button.
    public void ClearBlueprintTowerModels()
    {
        if (m_blueprintList.Count == 0) return;

        foreach (TowerBlueprint blueprint in m_blueprintList)
        {
            if (m_curSelectable && m_curSelectable.gameObject == blueprint.gameObject)
            {
                OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
                m_curSelectable = null;
            }

            //Configure Grid
            GridCellOccupantUtil.SetOccupant(blueprint.gameObject, false, 1, 1);

            blueprint.RemoveTower();
        }

        m_blueprintList = new List<TowerBlueprint>();
        OnBlueprintCountChanged?.Invoke(m_blueprintList.Count);

        // Removing this refresh, since the SetOccupant does it also.
        //GridManager.Instance.RefreshGrid();
    }

    // Clear/Apply Blueprint cell directions -- Called when entering/leaving pause mode.
    public void ToggleBlueprintPathDirections(bool value)
    {
        if (m_blueprintList.Count == 0) return;

        foreach (TowerBlueprint blueprint in m_blueprintList)
        {
            //Configure Grid
            GridCellOccupantUtil.SetOccupant(blueprint.gameObject, value, 1, 1);
        }

        // Removing this refresh, since the SetOccupant does it also.
        //GridManager.Instance.RefreshGrid();
    }

    public void AddTowerToList(Tower tower)
    {
        m_towerList.Add(tower);
    }

    public void RemoveTowerFromList(Tower tower)
    {
        /*
            for (int i = 0; i < m_towerList.Count; ++i)
            {
                if (m_towerList[i] == tower)
                {
                    m_towerList.RemoveAt(i);
                }
            }
            */
        m_towerList.Remove(tower);
    }

    public void RemoveTower(Tower tower)
    {
        //Configure Grid
        GridCellOccupantUtil.SetOccupant(tower.gameObject, false, 1, 1);

        //Clean up actor list
        RemoveTowerFromList(tower);

        //Remove the tower
        tower.RemoveTower();
    }

    public void AddBlueprintToList(TowerBlueprint blueprint)
    {
        if (m_blueprintList == null) m_blueprintList = new List<TowerBlueprint>();

        m_blueprintList.Add(blueprint);
        OnBlueprintCountChanged?.Invoke(m_blueprintList.Count);
        Debug.Log($"Build Blueprint: Blueprint Added to GameManager List.");
    }

    public void RemoveBlueprintFromList(TowerBlueprint blueprint)
    {
        Debug.Log($"Remove Blueprint: Blueprint Removed from GameManager List.");
        m_blueprintList.Remove(blueprint);
        OnBlueprintCountChanged?.Invoke(m_blueprintList.Count);
    }

    public void SellTower(Tower tower, int stoneValue, int woodValue)
    {
        //Configure Grid
        TowerData towerData = tower.GetTowerData();
        GridCellOccupantUtil.SetOccupant(tower.gameObject, false, towerData.m_buildingSize.x, towerData.m_buildingSize.y);

        //Clean up actor list
        if (tower is TowerBlueprint)
        {
            RemoveBlueprintFromList(tower as TowerBlueprint);
        }
        else
        {
            RemoveTowerFromList(tower);
        }

        //Handle Quantity
        if (m_unlockedStructures.ContainsKey(towerData))
        {
            OnStructureSold?.Invoke(towerData); // Tells UI To update quantity display.
            m_unlockedStructures[towerData] += 1;
        }

        //Handle currency
        ResourceManager.Instance.UpdateStoneAmount(stoneValue);
        ResourceManager.Instance.UpdateWoodAmount(woodValue);

        if (woodValue > 0 || stoneValue > 0)
        {
            IngameUIController.Instance.SpawnCurrencyAlert(woodValue, stoneValue, true, tower.transform.position);
        }


        //Remove the tower
        tower.RemoveTower();

        //Set Gameplay Manager's state
        if (m_interactionState == InteractionState.SelectedTower)
        {
            m_curSelectable = null;
            UpdateInteractionState(InteractionState.Idle);
        }
    }

    public void UpgradeTower(Tower oldTower, TowerData newTowerData, int stoneValue, int woodValue)
    {
        //Configure Grid
        Vector3 pos = oldTower.gameObject.transform.position;
        GridCellOccupantUtil.SetOccupant(oldTower.gameObject, false, 1, 1);

        //Clean up actor list
        RemoveTowerFromList(oldTower);

        //Handle currency
        ResourceManager.Instance.UpdateStoneAmount(-stoneValue);
        ResourceManager.Instance.UpdateWoodAmount(-woodValue);
        IngameUIController.Instance.SpawnCurrencyAlert(woodValue, stoneValue, false, oldTower.transform.position);

        //Cache current tower data to apply to new tower.
        TowerUpgradeData towerUpgradeData = oldTower.GetUpgradeData();

        //Remove the tower
        oldTower.RemoveTower();

        //Place new tower
        GameObject newTowerObj = ObjectPoolManager.SpawnObject(newTowerData.m_prefab, pos, Quaternion.identity, null, ObjectPoolManager.PoolType.Tower);
        Tower newTower = newTowerObj.GetComponent<Tower>();
        newTower.SetUpgradeData(towerUpgradeData);
        newTower.SetupTower();

        //Test - If we're a void tower, copy over the stacks.
        newTower.SetUpgradeData(oldTower.GetUpgradeData());

        //Set Gameplay Manager's state
        m_curSelectable = null;
        UpdateInteractionState(InteractionState.Idle);

        //Let rest of game know of new tower.
        OnTowerUpgrade?.Invoke();
        //Debug.Log("Tower upgraded.");
    }

    public void AddEnemyToList(EnemyController enemy)
    {
        ++EnemiesCreatedThisWave;
        m_enemyList.Add(enemy);
    }

    public void RemoveEnemyFromList(EnemyController enemy)
    {
        for (int i = 0; i < m_enemyList.Count; ++i)
        {
            if (m_enemyList[i] == enemy)
            {
                m_enemyList.RemoveAt(i);
                break;
            }
        }

        ++EnemiesKilledThisWave;

        // Dont check for win in Survival.
        if (m_gameplayData.m_gameMode == MissionGameplayData.GameMode.Survival) return;

        // Are there enemies remaining?
        if (m_enemyList.Count > 0) return;

        // Are there active spawners?
        foreach (EnemySpawner spawner in m_enemySpawners)
        {
            if (spawner.SpawnerActive)
            {
                //Debug.Log($"Spawner Still Active.");
                return;
            }
        }

        foreach (EnemySpawner spawner in m_enemyTrojanSpawners)
        {
            if (spawner.SpawnerActive)
            {
                //Debug.Log($"Spawner Still Active.");
                return;
            }
        }

        if (m_gameplayState == GameplayState.Defeat) return;

        if (m_gameplayState == GameplayState.Victory) return;

        if (m_gameplayState == GameplayState.BossWave)
        {
            // If it's a boss wave, we only care about RemoveBossFromList as tracking wave completion.
            //UpdateCompletedWaves(); 
        }
        else
        {
            UpdateCompletedWaves();
        }

        RequestPlayAudio(m_gameplayAudioData.m_audioWaveEndClips);

        UpdateGameplayState(GameplayState.Build);
    }

    private void UpdateCompletedWaves()
    {
        // Calculate if this was a perfected wave.
        float wavePercent = 0;
        if (IsEndlessModeActive())
        {
            wavePercent = (float)EnemiesKilledThisWave / EnemiesCreatedThisWave;
        }
        else
        {
            wavePercent = (float)CoresClaimedThisWave / EnemiesCreatedThisWave;
        }

        if (wavePercent == 1) ++m_perfectWavesCompleted;

        CompletedWave completedWave = new CompletedWave(EnemiesCreatedThisWave, EnemiesKilledThisWave, CoresClaimedThisWave, DamageTakenThisWave, wavePercent);

        if (m_wavesCompleted == null) m_wavesCompleted = new List<CompletedWave>();
        m_wavesCompleted.Add(completedWave);

        Debug.Log($"Wave {m_wavesCompleted.Count} completed.");
        Debug.Log($"Wave {m_wavesCompleted.Count} data: " +
                  $"Enemies Created: {EnemiesCreatedThisWave}, " +
                  $"Enemies Killed: {EnemiesKilledThisWave}, " +
                  $"Cores Claimed: {CoresClaimedThisWave}, " +
                  $"Damage Taken: {DamageTakenThisWave}, " +
                  $"Completion: {wavePercent}%");


        OnWaveCompleted?.Invoke(completedWave);

        ResetWaveCompleteValues();
    }

    private void UpdateCompletedBossWave()
    {
        // Calculate if this was a perfected wave.
        // Calculate Damage Taken / Castle Starting Health.
        float wavePercent = 0;

        int currentMaxHealth = m_castleController.GetCurrentMaxHealth();
        wavePercent = (float)currentMaxHealth / (currentMaxHealth + DamageTakenThisWave);

        if (wavePercent == 1) ++m_perfectWavesCompleted;

        CompletedWave completedWave = new CompletedWave(EnemiesCreatedThisWave, EnemiesKilledThisWave, CoresClaimedThisWave, DamageTakenThisWave, wavePercent);

        if (m_wavesCompleted == null) m_wavesCompleted = new List<CompletedWave>();

        m_wavesCompleted.Add(completedWave);

        Debug.Log($"Boss Wave {m_wavesCompleted.Count} completed.");
        Debug.Log($"Wave {m_wavesCompleted.Count} data: " +
                  $"Enemies Created: {EnemiesCreatedThisWave}, " +
                  $"Enemies Killed: {EnemiesKilledThisWave}, " +
                  $"Cores Claimed: {CoresClaimedThisWave}, " +
                  $"Damage Taken: {DamageTakenThisWave}, " +
                  $"Completion: {wavePercent}%");


        OnWaveCompleted?.Invoke(completedWave);

        ResetWaveCompleteValues();
    }

    private void AddVictoryWave()
    {
        float wavePercent = 0;
        if (DamageTakenThisWave == 0)
        {
            wavePercent = 1;
        }
        else
        {
            // What percent of enemies created escaped?
            wavePercent = 1 - (DamageTakenThisWave / EnemiesCreatedThisWave);
        }

        if (wavePercent == 1) ++m_perfectWavesCompleted;

        CompletedWave completedWave = new CompletedWave(EnemiesCreatedThisWave, EnemiesKilledThisWave, CoresClaimedThisWave, DamageTakenThisWave, wavePercent);

        if (m_wavesCompleted == null) m_wavesCompleted = new List<CompletedWave>();
        m_wavesCompleted.Add(completedWave);

        Debug.Log($"Wave {m_wavesCompleted.Count} completed.");
        Debug.Log($"Wave {m_wavesCompleted.Count} data: " +
                  $"Enemies Created: {EnemiesCreatedThisWave}, " +
                  $"Enemies Killed: {EnemiesKilledThisWave}, " +
                  $"Cores Claimed: {CoresClaimedThisWave}, " +
                  $"Damage Taken: {DamageTakenThisWave}, " +
                  $"Completion: {wavePercent}%");


        OnWaveCompleted?.Invoke(completedWave);

        ResetWaveCompleteValues();
    }

    public void AddBossToList(EnemyController enemy)
    {
        ++EnemiesCreatedThisWave;
        m_enemyBossList.Add(enemy);
    }

    public void RemoveBossFromList(EnemyController enemy)
    {
        for (int i = 0; i < m_enemyBossList.Count; ++i)
        {
            if (m_enemyBossList[i] == enemy)
            {
                m_enemyBossList.RemoveAt(i);
                break;
            }
        }

        ++EnemiesKilledThisWave; // This is not garaunteed by bosses, as they may also ESCAPE and not be killed. But i don't think this can occur yet.

        if (m_enemyBossList.Count > 0) return;

        UpdateCompletedBossWave();
    }

    public void AddSpawnerToList(EnemySpawner spawner)
    {
        m_enemySpawners.Add(spawner);
    }

    public void RemoveSpawnerFromList(EnemySpawner spawner)
    {
        for (int i = 0; i < m_enemySpawners.Count; ++i)
        {
            if (m_enemySpawners[i] == spawner)
            {
                m_enemySpawners.RemoveAt(i);
                break;
            }
        }
    }

    public void AddTrojanSpawnerToList(EnemySpawner spawner)
    {
        m_enemyTrojanSpawners.Add(spawner);
    }

    public void RemoveTrojanSpawnerFromList(EnemySpawner spawner)
    {
        for (int i = 0; i < m_enemyTrojanSpawners.Count; ++i)
        {
            if (m_enemyTrojanSpawners[i] == spawner)
            {
                m_enemyTrojanSpawners.RemoveAt(i);
                break;
            }
        }
    }

    [HideInInspector]
    public int m_obeliskCount;
    private int m_obelisksChargedCount;
    private GameplayState m_preVictoryState;

    public void AddObeliskToList(Obelisk obelisk)
    {
        if (m_obelisksInMission == null) m_obelisksInMission = new List<Obelisk>();
        m_obelisksInMission.Add(obelisk);
        ++m_obeliskCount;
        OnObeliskAdded?.Invoke(m_obelisksChargedCount, m_obeliskCount);
    }

    public void CheckObeliskStatus()
    {
        m_obelisksChargedCount = 0;

        //Identify if there are any obelisks still charging.
        foreach (Obelisk obelisk in m_obelisksInMission)
        {
            if (obelisk.m_obeliskState == Obelisk.ObeliskState.Charged)
            {
                ++m_obelisksChargedCount;
            }
        }

        if (m_obeliskCount > 0 && m_obelisksChargedCount == m_obeliskCount)
        {
            AddVictoryWave();
            UpdateGameplayState(GameplayState.Victory);
            return;
        }

        OnObelisksCharged?.Invoke(m_obelisksChargedCount, m_obeliskCount);
    }

    private bool m_endlessModeActive;

    public void StartEndlessMode()
    {
        // Re-enable the CombatHUD
        CombatHUD.SetCanvasInteractive(true);

        // Disable the Gossamer Full Screen effect.
        OnGossamerHealed?.Invoke(false);

        // Enable Spire Damage.
        m_castleController.SetCastleInvulnerable(false);

        // Disable Obelisk Beams
        foreach (Obelisk obelisk in m_obelisksInMission)
        {
            Debug.Log($"{obelisk.gameObject.name} beam activated.");
            obelisk.HandleSpireBeamVFX(false);
        }

        // Disable Spire Beam.
        m_castleController.HandleSpireBeamVFX(false);

        foreach (GathererController gatherer in m_woodGathererList)
        {
            gatherer.ResumeGatherer();
        }

        // Audio
        RequestPlayAudio(m_gameplayAudioData.m_endlessModeStartedClip);

        // Enable Endless Mode bool (used for next victory/defeat display)
        m_endlessModeActive = true;

        // Resume previous Game State.
        //m_gameplayState = m_preVictoryState; // Resume the previous state from where victory / endless came from
        switch (m_gameplayData.m_gameMode)
        {
            case MissionGameplayData.GameMode.Standard: // in standard, we go to the next wave since we killed all the enemies.
                UpdateGameplayState(GameplayState.Build);
                break;
            case MissionGameplayData.GameMode.Survival: // in survival, we want to resume right from where we left off.
                m_gameplayState = GameplayState.SpawnEnemies;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        UpdateGameplayState(GameplayState.Build);
        UpdateInteractionState(InteractionState.Idle);
        ResetWaveCompleteValues();

        // Disable FFW
        if (m_playbackSpeed == 2) UpdateGameSpeed();
    }

    public bool IsEndlessModeActive()
    {
        return m_endlessModeActive;
    }

    public bool IsWatchingCutscene()
    {
        return m_watchingCutScene;
    }

    public void RequestSelectGatherer(GameObject obj)
    {
        if (m_interactionState == InteractionState.PreconstructionTower)
        {
            ClearPreconstructedTower();
        }

        SetOutlineColor(true);

        OnGameObjectSelected?.Invoke(obj);
    }

    public void RequestSelectGatherer(int i)
    {
        if (i >= m_woodGathererList.Count) return;

        GameObject obj = m_woodGathererList[i].gameObject;

        if (m_interactionState == InteractionState.PreconstructionTower)
        {
            ClearPreconstructedTower();
        }

        SetOutlineColor(true);

        OnGameObjectSelected?.Invoke(obj);
    }

    public void KillAllEnemies()
    {
        if (m_enemyList.Count > 0)
        {
            List<EnemyController> livingEnemies = new List<EnemyController>(m_enemyList);
            foreach (EnemyController enemy in livingEnemies)
            {
                enemy.OnTakeDamage(999999);
            }
        }

        if (m_enemyBossList.Count > 0)
        {
            List<EnemyController> livingBossEnemies = new List<EnemyController>(m_enemyBossList);
            foreach (EnemyController enemy in livingBossEnemies)
            {
                enemy.OnTakeDamage(999999);
            }
        }
    }

    public Selectable GetCurSelectedObj()
    {
        return m_curSelectable;
    }

    private Light[] m_lights;

    public void WatchingCutScene()
    {
        Debug.Log($"Cut Scene: Gameplay Manager is now Watching Cut Scene.");
        m_watchingCutScene = true;
        Debug.Log($"Cut Scene: Set Watching Cut Scene to: {m_watchingCutScene}.");

        //Clear precon if we're precon
        Debug.Log($"Cut Scene: Clear Precon Tower.");
        if (m_preconstructedTowerObj) ClearPreconstructedTower();

        //Clear selected
        if (m_curSelectable)
        {
            Debug.Log($"Cut Scene: Deselecting Current Selectable.");
            OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
            m_curSelectable = null;
        }

        //Set interaction to disabled
        UpdateInteractionState(InteractionState.Disabled);

        //Set speed to paused
        UpdateGamePlayback(GameSpeed.Paused);

        m_combatHUD.SetCanvasInteractive(false, 0.1f);

        /*// Disable Lights
        m_lights = FindObjectsOfType<Light>();

        // Loop through each light and disable it
        foreach (Light light in m_lights)
        {
            light.enabled = false;
        }*/
    }

    public void DoneWatchingLeaveCutScene()
    {
        Debug.Log($"Cut Scene: Done watching Cut Scene.");

        Debug.Log($"Cut Scene: Set Combat HUD Interativity to True.");
        m_combatHUD.SetCanvasInteractive(true);

        //Set interaction to disabled
        UpdateInteractionState(InteractionState.Idle);

        //Set speed to paused
        UpdateGamePlayback(GameSpeed.Normal);

        m_watchingCutScene = false;
        Debug.Log($"Cut Scene: Set Watching Cut Scene to: {m_watchingCutScene}.");


        /*// Loop through each light and re-enable it
        foreach (Light light in m_lights)
        {
            light.enabled = true;
        }*/

        Debug.Log($"Cut Scene: OnCutSceneEnd Invoked.");
        OnCutSceneEnd?.Invoke();
    }

    public void CastleControllerDestroyed()
    {
        //To replace this with playing a cutscene then triggering defeat, or they're the same?
        if (m_endlessModeActive)
        {
            UpdateGameplayState(GameplayState.Victory);
        }
        else
        {
            UpdateGameplayState(GameplayState.Defeat);
        }
    }

    private void HandleVictorySequence()
    {
        m_victorySequence = StartCoroutine(VictorySequence());
    }

    private void HandleDefeatSequence()
    {
        m_defeatSequence = StartCoroutine(DefeatSequence());
    }

    private void StopVictorySequence()
    {
        if (m_victorySequence == null) return;

        StopCoroutine(m_victorySequence);
        m_victorySequence = null;
    }

    private Coroutine m_victorySequence;
    private float m_obeliskToSpireDelay = 2f;
    private float m_killAllEnemiesDelay = 0.8f;
    private float m_displayVictoryUIDelay = 0f;
    private float m_displayDefeatUIDelay = 2.5f;

    IEnumerator VictorySequence()
    {
        UpdateInteractionState(InteractionState.Disabled);

        // Disable Spire Damage.
        m_castleController.SetCastleInvulnerable(true);

        // Disable Tooltips.
        UITooltipController.Instance.HideAndSuppressToolTips();

        // Hide the CombatHUD.
        CombatHUD.SetCanvasInteractive(false);

        // Idle the gatherers
        foreach (GathererController gatherer in m_woodGathererList)
        {
            gatherer.PauseGatherer();
        }

        // Deactivate Spawners
        foreach (EnemySpawner spawner in m_enemySpawners)
        {
            spawner.DeactivateSpawner();
        }

        // Bringing the camera to the Spire.
        CameraController.Instance.RequestOnRailsMove(m_castleController.transform.position + (Vector3.forward * 2), 2f);
        float zoomDestination = CameraController.Instance.m_startZoom + CameraController.Instance.m_maxZoomOut;
        CameraController.Instance.RequestOnRailsZoom(zoomDestination, 2f);

        // Begin to slow gameplay.
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, .2f, m_obeliskToSpireDelay).SetUpdate(true).OnComplete(() => Debug.Log($"Time Scale: 0.2"));

        // Enable the beams that connect the Obelisks to Spire.
        foreach (Obelisk obelisk in m_obelisksInMission)
        {
            obelisk.HandleSpireBeamVFX(true);
        }

        // Wait for the Obelisk beams to connect to the spire.
        yield return new WaitForSecondsRealtime(m_obeliskToSpireDelay);

        // Enable the Spire's Beam
        m_castleController.HandleSpireBeamVFX(true);

        // Enable the Endless spire vfx (persists if we go into endless mode)
        m_castleController.HandleSpireEndlessVFX(true);

        // Remove all enemies from the map.
        yield return new WaitForSecondsRealtime(m_killAllEnemiesDelay);
        Debug.Log($"Time Scale: 1");
        Time.timeScale = 1;
        KillAllEnemies();

        // Enable the Gossamer Fullscreen effect
        OnGossamerHealed?.Invoke(true);

        // Wait a beat after killing enemies to display the victory UI.
        yield return new WaitForSecondsRealtime(m_displayVictoryUIDelay);

        // Display the Victory UI.
        UIPopupManager.Instance.ShowPopup<UIMissionCompletePopup>("MissionComplete");

        // Play Victory Stinger
        RequestPlayAudio(m_gameplayAudioData.m_victoryClip);

        // Restore Tooltips
        UITooltipController.Instance.UnsuppressToolTips();
    }

    private Coroutine m_defeatSequence;
    private float m_cameraMoveDuration = 2f;

    IEnumerator DefeatSequence()
    {
        UpdateInteractionState(InteractionState.Disabled);

        // Disable Spire Damage.
        m_castleController.SetCastleInvulnerable(true);

        // Disable Tooltips.
        UITooltipController.Instance.HideAndSuppressToolTips();

        // Hide the CombatHUD.
        CombatHUD.SetCanvasInteractive(false);

        // Assure we're at Time Scale 1.
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 1f, 3).SetUpdate(true);

        // Idle the gatherers
        foreach (GathererController gatherer in m_woodGathererList)
        {
            gatherer.PauseGatherer();
        }

        // Disable Living enemies.
        foreach (EnemyController enemyController in m_enemyList)
        {
            enemyController.SetEnemyActive(false);
        }

        foreach (EnemyController enemyController in m_enemyBossList)
        {
            enemyController.SetEnemyActive(false);
        }

        // Deactivate Spawners
        foreach (EnemySpawner spawner in m_enemySpawners)
        {
            spawner.DeactivateSpawner();
        }

        // Bringing the camera to the Spire.
        CameraController.Instance.RequestOnRailsMove(m_castleController.transform.position + (Vector3.forward * 2), m_cameraMoveDuration);
        float zoomDestination = CameraController.Instance.m_startZoom + CameraController.Instance.m_maxZoomIn;
        CameraController.Instance.RequestOnRailsZoom(zoomDestination, m_cameraMoveDuration);

        // Wait for camera position
        yield return new WaitForSecondsRealtime(m_cameraMoveDuration * .5f);

        // Blow up the Spire
        m_castleController.HandleSpireDestroyedVFX();

        // Enable the Gossamer Fullscreen effect
        OnSpireDestroyed?.Invoke(true);

        // Wait a beat after killing enemies to display the victory UI.
        yield return new WaitForSecondsRealtime(m_displayDefeatUIDelay);

        // Play Defeat Stinger
        RequestPlayAudio(m_gameplayAudioData.m_defeatClip);

        // Display the Victory UI.
        UIPopupManager.Instance.ShowPopup<UIMissionCompletePopup>("MissionComplete");

        // Restore Tooltips
        UITooltipController.Instance.UnsuppressToolTips();
    }

    public void BuildTowerQuestCompleted()
    {
        m_delayForQuest = false;
    }

    public void SetActiveBossController(BossSequenceController bossSequenceController)
    {
        Debug.Log($"Setting Active Boss Sequence Controller to: {bossSequenceController}");
        m_activeBossSequenceController = bossSequenceController;
    }

    public BossSequenceController GetActiveBossController()
    {
        return m_activeBossSequenceController;
    }

    public void RequestPlayAudio(AudioClip clip)
    {
        m_audioSource.PlayOneShot(clip);
    }

    public void RequestPlayAudio(List<AudioClip> clips)
    {
        int i = Random.Range(0, clips.Count);
        m_audioSource.PlayOneShot(clips[i]);
    }

    void HandleMissionProgression()
    {
        if (GameManager.Instance == null) return;

        // Each mission houses the keys it needs to flip if obelisks fill, and if endless wave passed.
        // GameManager has a reference to the Mission Data. The keys will be held in there.

        // For each reward in our MissionData, check if our completion wave is greater than the required wave. If so, flip the key.
        foreach (ProgressionUnlockableData unlockable in GameManager.Instance.m_curMission.m_unlockableRewards)
        {
            Debug.Log($"HandleMissionProgression: {unlockable}.");
            if (Wave < unlockable.GetWaveRequirement())
            {
                Debug.Log($"Wave requirement not met for {unlockable}. Current Wave {Wave} / Required Wave {unlockable.GetWaveRequirement()}");
                continue; // We have not met the requirement, go next.
            }

            // We have met the requirement. Get the Key(s) and update them.
            foreach (ProgressionKeyData key in unlockable.GetKeyData())
            {
                Debug.Log($"HandleMissionProgression: {unlockable}'s {key} unlocked.");
                PlayerDataManager.Instance.RequestUnlockKey(key);
            }
        }
    }
}

public class OozeManager
{
    public List<Cell> m_currentOozedCells = new List<Cell>();

    public OozeManager()
    {
        GameplayManager.OnGameplayStateChanged += GameplayStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayStateChanged;
    }

    private void GameplayStateChanged(GameplayManager.GameplayState newState)
    {
        if (m_currentOozedCells.Count <= 0) return;

        if (newState == GameplayManager.GameplayState.Build)
        {
            m_currentOozedCells.Clear();
            Debug.Log($"List of Ooze cleared.");
        }
    }

    public bool IsCellOozed(Cell cell)
    {
        return m_currentOozedCells.Contains(cell);
    }

    public void AddCell(Cell cell)
    {
        m_currentOozedCells.Add(cell);
    }

    public void RemoveCell(Cell cell)
    {
        if (m_currentOozedCells.Contains(cell))
        {
            m_currentOozedCells.Remove(cell);
        }
    }
}

public class CompletedWave
{
    public int m_enemiesCreatedThisWave;
    public int m_enemiesKilledThisWave;
    public int m_coresClaimedThisWave;
    public int m_damageTakenThisWave;
    public float m_wavePercent;

    public CompletedWave(int enemiesCreatedThisWave, int enemiesKilledThisWave, int coresClaimedThisWave, int damageTakenThisWave, float wavePercent)
    {
        m_enemiesCreatedThisWave = enemiesCreatedThisWave;
        m_enemiesKilledThisWave = enemiesKilledThisWave;
        m_coresClaimedThisWave = coresClaimedThisWave;
        m_damageTakenThisWave = damageTakenThisWave;
        m_wavePercent = wavePercent;
    }
}