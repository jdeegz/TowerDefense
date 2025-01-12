using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Mathematics;
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
    public static event Action<GameObject> OnGameObjectSelected;
    public static event Action<GameObject> OnGameObjectDeselected;
    public static event Action<GameObject> OnGameObjectHoveredEnter;
    public static event Action<GameObject> OnGameObjectHoveredExit;
    public static event Action<GameObject, Selectable.SelectedObjectType> OnCommandRequested;
    public static event Action<GameObject, bool> OnObjRestricted;
    public static event Action<String> OnAlertDisplayed;
    public static event Action<String> OnWaveCompleted;
    public static event Action<Vector2Int> OnPreconTowerMoved;
    public static event Action OnPreconstructedTowerClear;
    public static event Action<List<Cell>> OnPreconBuildingMoved;
    public static event Action OnPreconBuildingClear;
    public static event Action<TowerData> OnTowerBuild;
    public static event Action OnTowerSell;
    public static event Action OnTowerUpgrade;
    public static event Action<int, int> OnObelisksCharged;
    public static event Action<int, int> OnObeliskAdded;
    public static event Action<GathererController> OnGathererAdded;
    public static event Action<GathererController> OnGathererRemoved;
    public static event Action OnCutSceneEnd;
    public static event Action<bool> OnDelayForQuestChanged;
    public static event Action<int> OnBlueprintCountChanged;
    public static event Action<TowerData, int> OnUnlockedStucturesUpdated;
    public static event Action<TowerData, bool> OnUnlockedTowersUpdated;


    [Header("Progression")]
    [SerializeField] private ProgressionTable m_progressionTable;
    private int m_qty;
    public Dictionary<TowerData, int> m_unlockedStructures;
    public List<TowerData> m_unlockedTowers;

    [Header("Wave Settings")]
    public MissionGameplayData m_gameplayData;
    public GameplayAudioData m_gameplayAudioData;
    public AudioSource m_audioSource;

    public int m_wave;
    [HideInInspector] public float m_timeToNextWave;
    [HideInInspector] public bool delayForQuest;

    [Header("Castle")]
    public CastleController m_castleController;
    public Transform m_enemyGoal;

    [HideInInspector]
    public Vector2Int m_goalPointPos;

    [Header("Obelisks")]
    [FormerlySerializedAs("m_activeObelisks")]
    public List<Obelisk> m_obelisksInMission;

    [Header("Unit Spawners")]
    public List<UnitSpawner> m_unitSpawners;
    private int m_activeSpawners;

    [Header("Active Enemies")]
    public List<EnemyController> m_enemyList;
    public List<EnemyController> m_enemyBossList;

    [Header("Player Constructed")]
    public List<GathererController> m_woodGathererList;
    public List<GathererController> m_stoneGathererList;
    public List<Tower> m_towerList;
    private List<TowerBlueprint> m_blueprintList;

    [Header("Selected Object Info")]
    [SerializeField]
    private SelectionColors m_selectionColors;
    private Material m_selectedOutlineMaterial; //Assigned on awake
    private Selectable m_curSelectable;
    public Selectable m_hoveredSelectable;

// Precon Tower Info
    public GameObject m_invalidCellObj;
    public List<GameObject> m_invalidCellObjs;
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
        //TEMPORARY TEST STUFF
        if (Input.GetKeyUp(KeyCode.G))
        {
            Debug.Log($"P button pressed.");
            PlayerDataManager.Instance.ResetProgressionTable();
        }

        //We do not want to update _anything_ while we're in a cutscene!
        if (m_watchingCutScene) return;

        HandleHotkeys();

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

        if (m_interactionState == InteractionState.PreconstructionTower)
        {
            PreconPeriodicTimer();
            PositionPrecon();
            HandlePreconMousePosition();
        }

        if (m_delayForQuest == false && m_gameplayState == GameplayState.Build)
        {
            m_timeToNextWave -= Time.deltaTime;
        }

        if (m_timeToNextWave <= 0 && m_gameplayState == GameplayState.Build)
        {
            ++m_wave;
            if (m_bossWaves.Contains(m_wave) && m_bossSequenceController)
            {
                UpdateGameplayState(GameplayState.BossWave);
            }
            else
            {
                //Does a spawner have a cutscene request?
                string cutsceneName = null;
                foreach (UnitSpawner spawner in m_unitSpawners)
                {
                    //Check for Unit Cutscene.
                    if (GameManager.Instance && spawner.GetActiveWave() is NewTypeCreepWave newTypeWave && !string.IsNullOrEmpty(newTypeWave.m_waveCutscene))
                    {
                        cutsceneName = newTypeWave.m_waveCutscene;
                    }
                }

                if (!string.IsNullOrEmpty(cutsceneName))
                {
                    Debug.Log($"Cutscene named: {cutsceneName} found for this wave.");

                    UpdateGameplayState(GameplayState.CutScene);
                    OnCutSceneEnd += ResumeSpawnWave;
                    GameManager.Instance.RequestAdditiveSceneLoad(cutsceneName);
                }
                else
                {
                    UpdateGameplayState(GameplayState.SpawnEnemies);
                }
            }
        }
    }

    void ResumeSpawnWave()
    {
        OnCutSceneEnd -= ResumeSpawnWave;
        UpdateGameplayState(GameplayState.SpawnEnemies);
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
                    //Debug.Log("Not Enough Resources.");
                    OnAlertDisplayed?.Invoke(m_pathRestrictedReason);
                    RequestPlayAudio(m_gameplayAudioData.m_cannotPlaceClip);
                    return;
                }

                if (!m_canPlace)
                {
                    //Debug.Log("Cannot build here.");
                    OnAlertDisplayed?.Invoke(m_pathRestrictedReason);
                    RequestPlayAudio(m_gameplayAudioData.m_cannotPlaceClip);
                    return;
                }
                
                if (!m_canPath)
                {
                    //Debug.Log("Cannot build here.");
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
                        Debug.Log("Command Requested on Gatherer.");
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
                        RemoveBlueprintTower(blueprint);
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
        Color color = canBuild ? m_selectionColors.m_outlineBaseColor: m_selectionColors.m_outlineRestrictedColor;
        m_selectedOutlineMaterial.SetColor("_Outline_Color", color);
    }

    private void Awake()
    {
        m_unlockedStructures = new Dictionary<TowerData, int>();
        PlayerDataManager.Instance.SetProgressionTable(m_progressionTable);
        PlayerDataManager.OnUnlockableUnlocked += UnlockableUnlocked;
        PlayerDataManager.OnUnlockableLocked += UnlockableLocked;

        // DELETE THIS
        //PlayerDataManager.Instance.ResetProgressionTable();

        SortedAndUnlocked sortedAndUnlocked = PlayerDataManager.Instance.GetSortedUnlocked();
        m_unlockedStructures = sortedAndUnlocked.m_unlockedStructures;
        m_unlockedTowers = sortedAndUnlocked.m_unlockedTowers;
        m_selectedOutlineMaterial = Resources.Load<Material>("Materials/Mat_OutlineSelected");
        Instance = this;
        m_mainCamera = Camera.main;
        if (m_enemyGoal != null)
        {
            m_goalPointPos = new Vector2Int((int)m_enemyGoal.position.x, (int)m_enemyGoal.position.z);
        }

        OnGameplayStateChanged += GameplayManagerStateChanged;
        OnGamePlaybackChanged += GameplayPlaybackChanged;
        OnGameObjectSelected += GameObjectSelected;
        OnGameObjectDeselected += GameObjectDeselected;

        m_delayForQuest = m_gameplayData.m_delayForQuest;
        m_blueprintList = new List<TowerBlueprint>();
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
        m_timeToNextWave = m_gameplayData.m_firstBuildDuraction;
        
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
        Debug.Log($"Game state is now: {m_gameplayState}");

        switch (m_gameplayState)
        {
            case GameplayState.BuildGrid:
                break;
            case GameplayState.PlaceObstacles:
                break;
            case GameplayState.FloodFillGrid:
                break;
            case GameplayState.CreatePaths:
                //Test to open player save data and read from it.
                break;
            case GameplayState.Setup:
                UpdateInteractionState(InteractionState.Idle);
                UpdateGamePlayback(GameSpeed.Normal);
                break;
            case GameplayState.SpawnEnemies:
                RequestPlayAudio(m_gameplayAudioData.m_waveStartClip);
                m_timeToNextWave = m_gameplayData.m_buildDuration;
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
                PlayerDataManager.Instance.UpdateMissionSaveData(gameObject.scene.name, 2, m_wave);
                UpdateGamePlayback(GameSpeed.Paused);
                UpdateInteractionState(InteractionState.Disabled);
                break;
            case GameplayState.Defeat:
                RequestPlayAudio(m_gameplayAudioData.m_defeatClip);

                int wave = 0;
                if (m_endlessModeActive) wave = m_wave - 1;
                PlayerDataManager.Instance.UpdateMissionSaveData(gameObject.scene.name, 1, wave);

                UpdateGamePlayback(GameSpeed.Paused);
                UpdateInteractionState(InteractionState.Disabled);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        OnGameplayStateChanged?.Invoke(newState);
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
        //Debug.Log($"Interaction state is now: {m_interactionState}");
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
        m_preconstructedTowerObj = Instantiate(towerData.m_prefab, m_castleController.transform.position + Vector3.up, Quaternion.identity);
        m_preconstructedTower = m_preconstructedTowerObj.GetComponent<Tower>();
        m_preconBuildingWidth = towerData.m_buildingSize.x;
        m_preconBuildingHeight = towerData.m_buildingSize.y;
        m_preconxOffset = m_preconBuildingWidth % 2 == 0 ? 0.5f : 0;
        m_preconzOffset = m_preconBuildingHeight % 2 == 0 ? 0.5f : 0;

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

    public void TriggerPreconBuildingMoved(List<Cell> cells)
    {
        //THIS SHOULD BE REMOVED OMG
        OnPreconBuildingMoved?.Invoke(cells);
    }

    private List<Cell> m_preconstructedTowerCells;
    private List<Cell> m_preconNeighborCells;
    private int m_preconBuildingWidth;
    private int m_preconBuildingHeight;
    private float m_preconxOffset;
    private float m_preconzOffset;

    private void HandlePreconMousePosition()
    {
        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, m_buildSurface))
        {
            //Raycast has hit the ground, round that point to the nearest int.
            Vector3 gridPos = raycastHit.point;

            //Convert hit point to 2d grid position
            gridPos.x += m_preconxOffset;
            gridPos.z += m_preconzOffset;

            Vector2Int newPos = Util.GetVector2IntFrom3DPos(gridPos);

            if (newPos == m_preconstructedTowerPos) return;

            m_preconstructedTowerPos = newPos;

            List<Cell> newCells = Util.GetCellsFromPos(m_preconstructedTowerPos, m_preconBuildingWidth, m_preconBuildingHeight);
            if (newCells != null)
            {
                m_preconstructedTowerCells = newCells;
                m_preconNeighborCells = GetNeighborCells(m_preconstructedTowerPos, m_preconBuildingWidth, m_preconBuildingHeight);
                OnPreconBuildingMoved?.Invoke(m_preconstructedTowerCells);
                CheckPreconRestrictions();
            }
        }
    }

    void CheckPreconRestrictions()
    {
        if (m_preconstructedTowerCells == null || m_preconNeighborCells == null) return;

        m_canPlace = IsPlacementRestricted(m_preconstructedTowerCells);
        m_canPath = IsPathingRestricted(m_preconNeighborCells);
        m_canAfford = IsCurrencyRestricted();
        m_canBuild = m_canAfford && m_canPlace && m_canPath;

        SetOutlineColor(m_canBuild);
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

            if (curCell.m_actorCount > 0)
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

            //If the currenct cell is build restricted (bridges, obelisk ground, pathways), not a valid spot.
            if (curCell.m_isBuildRestricted)
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

        for (int i = 0; i < cells.Count; ++i)
        {
            Cell cell = cells[i];
            if (cell.m_isOccupied) continue;

            //Debug.Log($"Neighbor cells {cell.m_cellPos} is unoccupied. Checking for path.");
            List<Vector2Int> testPath = AStar.GetExitPath(cell.m_cellPos, m_goalPointPos);

            if (testPath != null) continue;

            //Debug.Log($"No path found from Neighbor: {cell.m_cellPos} to exit, checking for inhabited islands.");
            List<Cell> islandCells = new List<Cell>(AStar.FindIsland(cell, cells));

            //Debug.Log($"Returning FALSE because an actor was found on a single-cell island.");
            if (islandCells.Count == 0 && cell.m_actorCount > 0)
            {
                m_pathRestrictedReason = m_UIStringData.m_buildRestrictedActorsInIsland;
                return false;
            }

            foreach (Cell islandCell in islandCells)
            {
                //Debug.Log($"Island Cell found: {islandCell.m_cellPos} and has actors: {islandCell.m_actorCount}.");
                if (islandCell.m_actorCount > 0)
                {
                    //Debug.Log($"Cannot Place: {islandCells.Count} Island created, and Cell: {islandCell.m_cellPos} contains actors");
                    m_pathRestrictedReason = m_UIStringData.m_buildRestrictedActorsInIsland;
                    return false;
                }
            }
        }

        // EXITS AND SPAWNERS
        //Check to see if any of our UnitPaths have no path.
        if (!GridManager.Instance.m_spawnPointsAccessible)
        {
            //Debug.Log($"Cannot Place: This would block one or more spawners from reaching the exit.");
            m_pathRestrictedReason = m_UIStringData.m_buildRestrictedBlocksPath;
            return false;
        }

        // Check that the Grid to make sure exits can path to one another.
        int exitsPathable = 0;
        for (int x = 0; x < m_castleController.m_castleEntrancePoints.Count; ++x)
        {
            bool startObjPathable = false;
            GameObject startObj = m_castleController.m_castleEntrancePoints[x];
            Vector2Int startPos = Util.GetVector2IntFrom3DPos(startObj.transform.position);

            //Define the end
            for (int z = 0; z < m_castleController.m_castleEntrancePoints.Count; ++z)
            {
                GameObject endObj = m_castleController.m_castleEntrancePoints[z];
                Vector2Int endPos = Util.GetVector2IntFrom3DPos(endObj.transform.position);

                //Go next if they're the same.
                if (startObj == endObj)
                {
                    continue;
                }

                //Path from Start to End and exclude the Game's Goal cell.
                List<Vector2Int> testPath = AStar.FindExitPath(startPos, endPos, m_preconstructedTowerPos, m_goalPointPos);
                if (testPath != null)
                {
                    //If we do get a path, this exit is good. Increment and break out of this for loop to check other exits.
                    ++exitsPathable;
                    startObjPathable = true;
                    break;
                }
            }

            // We could not path to any other exit. So check to see if we can path to atleast one spawner.
            if (!startObjPathable)
            {
                for (int y = 0; y < m_unitSpawners.Count; ++y)
                {
                    Vector2Int spawnerPos = Util.GetVector2IntFrom3DPos(m_unitSpawners[y].GetSpawnPointTransform().position);
                    List<Vector2Int> testPath = AStar.FindExitPath(spawnerPos, startPos, m_preconstructedTowerPos, m_goalPointPos);
                    if (testPath != null)
                    {
                        // If we do get a path, this exit is good. Increment and break out of this for loop to check other exits.
                        //Debug.Log($"Castle Exit: {startPos} was able to path to Spawner: {spawnerPos}.");
                        ++exitsPathable;
                        break;
                    }
                }
            }
        }

        //If the number of pathable exits is less than the number of exits, return false. One cannot path to another exit.
        if (exitsPathable < m_castleController.m_castleEntrancePoints.Count)
        {
            //Debug.Log($"An exit cannot path to another exit or spawner.");
            m_pathRestrictedReason = m_UIStringData.m_buildRestrictedBlocksPath;
            return false;
        }

        return true;
    }

    private void PositionPrecon()
    {
        //Define the new destination of the Precon Tower Obj. Offset the tower on Y.
        Vector3 moveToPosition = new Vector3(m_preconstructedTowerPos.x - m_preconxOffset, 0.7f, m_preconstructedTowerPos.y - m_preconzOffset);

        //Position the precon Tower at the cursor position.
        m_preconstructedTowerObj.transform.position = Vector3.Lerp(m_preconstructedTowerObj.transform.position,
            moveToPosition, 20f * Time.unscaledDeltaTime);
        
        DrawPreconCellObjs();
    }

    private float m_preconPeriodTimeElapsed;
    private float m_preconPeriod = 0.1f;

    void PreconPeriodicTimer()
    {
        m_preconPeriodTimeElapsed += Time.deltaTime;

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
        
        // Make sure we have enough objs to display.
        if (m_invalidCellObjs == null) m_invalidCellObjs = new List<GameObject>();

        if (m_invalidCellObjs.Count < m_preconstructedTowerCells.Count)
        {
            m_invalidCellObjs.Add(ObjectPoolManager.SpawnObject(m_invalidCellObj, Vector3.zero, quaternion.identity, transform));
        }

        for (int i = 0; i < m_invalidCellObjs.Count; ++i)
        {
            if (i < m_preconstructedTowerCells.Count)
            {
                //Show the cell if we cannot path or afford.
                if (!m_canAfford || !m_canPath)
                {
                    m_invalidCellObjs[i].SetActive(true);
                }
                else
                {
                    //Else show the cell if cell is occupied.
                    m_invalidCellObjs[i].SetActive(m_preconstructedTowerCells[i].m_isOccupied || m_preconstructedTowerCells[i].m_isBuildRestricted);
                }

                Cell cell = m_preconstructedTowerCells[i];

                Vector3 pos = new Vector3(cell.m_cellPos.x, 0, cell.m_cellPos.y);
                m_invalidCellObjs[i].transform.position = pos;
                continue;
            }
            
            m_invalidCellObjs[i].SetActive(false);
        }
    }

    private void HidePreconCellObjs()
    {
        for (int i = 0; i < m_invalidCellObjs.Count; ++i)
        {
            m_invalidCellObjs[i].SetActive(false);
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
        m_invalidCellObj.SetActive(false);
        m_preconstructedTowerData = null;
        m_preconstructedTowerObj = null;
        m_preconstructedTower = null;
        OnPreconstructedTowerClear?.Invoke();
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
                RemoveBlueprintTower(blueprint);
            }
        }

        Vector3 gridPos = new Vector3(m_preconstructedTowerPos.x - m_preconxOffset, 0.0f, m_preconstructedTowerPos.y - m_preconzOffset);

        //We want to place towers to they look away from the Castle.
        // Calculate the direction vector from the target object to the current object.
        Vector3 direction = m_enemyGoal.position - m_preconstructedTowerObj.transform.position;

        // Calculate the rotation angle to make the new object face away from the target.
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 180f;

        GameObject newTowerObj = ObjectPoolManager.SpawnObject(m_preconstructedTowerData.m_prefab, gridPos, Quaternion.identity, null, ObjectPoolManager.PoolType.Tower);
        Tower newTower = newTowerObj.GetComponent<Tower>();
        newTower.GetTurretTransform().rotation = Quaternion.Euler(0, angle, 0);
        newTower.SetupTower();

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

        OnTowerBuild?.Invoke(m_preconstructedTowerData);
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
    }

    public void RemoveBlueprintFromList(TowerBlueprint blueprint)
    {
        m_blueprintList.Remove(blueprint);
        OnBlueprintCountChanged?.Invoke(m_blueprintList.Count);
    }

    public void RemoveBlueprintTower(TowerBlueprint blueprint)
    {
        //Configure Grid
        GridCellOccupantUtil.SetOccupant(blueprint.gameObject, false, 1, 1);

        //Clean up actor list
        RemoveBlueprintFromList(blueprint);

        //Remove the tower
        blueprint.RemoveTower();
    }

    public void SellTower(Tower tower, int stoneValue, int woodValue)
    {
        //Configure Grid
        GridCellOccupantUtil.SetOccupant(tower.gameObject, false, 1, 1);

        //Clean up actor list
        if (tower is TowerBlueprint)
        {
            RemoveBlueprintTower(tower as TowerBlueprint);
        }
        else
        {
            RemoveTowerFromList(tower);
        }

        //Handle Quantity
        TowerData towerData = tower.GetTowerData();
        if (m_unlockedStructures.ContainsKey(towerData))
        {
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
        m_curSelectable = null;
        UpdateInteractionState(InteractionState.Idle);

        //Let rest of game know of new tower.
        OnTowerSell?.Invoke();
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

        if (m_activeSpawners == 0 && m_enemyList.Count == 0 && m_gameplayState != GameplayState.Defeat)
        {
            Debug.Log($"Check for win.");
            CheckForWin();
        }
    }

    public void AddBossToList(EnemyController enemy)
    {
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
    }

    public void AddSpawnerToList(UnitSpawner unitSpawner)
    {
        m_unitSpawners.Add(unitSpawner);
    }

    public void ActivateSpawner()
    {
        ++m_activeSpawners;
    }

    public void DisableSpawner()
    {
        --m_activeSpawners;

        if (m_activeSpawners == 0)
        {
            UpdateGameplayState(GameplayState.Combat);
        }
    }

    public void RemoveSpawnerFromList(UnitSpawner unitSpawner)
    {
        for (int i = 0; i < m_unitSpawners.Count; ++i)
        {
            if (m_unitSpawners[i] == unitSpawner)
            {
                m_unitSpawners.RemoveAt(i);
            }
        }
    }

    [HideInInspector]
    public int m_obeliskCount;

    private int m_obelisksChargedCount;

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
        bool charging = false;

        //Identify if there are any obelisks still charging.
        foreach (Obelisk obelisk in m_obelisksInMission)
        {
            if (obelisk.m_obeliskState == Obelisk.ObeliskState.Charged)
            {
                ++m_obelisksChargedCount;
            }
        }

        OnObelisksCharged?.Invoke(m_obelisksChargedCount, m_obeliskCount);
    }

    private bool m_endlessModeActive;

    public void StartEndlessMode()
    {
        RequestPlayAudio(m_gameplayAudioData.m_endlessModeStartedClip);
        m_endlessModeActive = true;
        UpdateGameplayState(GameplayState.Build);
        UpdateGamePlayback(GameSpeed.Normal);
        UpdateInteractionState(InteractionState.Idle);
    }

    public bool IsEndlessModeActive()
    {
        return m_endlessModeActive;
    }

    public void CheckForWin()
    {
        //Obelisk Win Condition.
        if (!m_endlessModeActive)
        {
            if (m_obeliskCount > 0 && m_obelisksChargedCount == m_obeliskCount)
            {
                RequestPlayAudio(m_gameplayAudioData.m_victoryClip);
                UpdateGameplayState(GameplayState.Victory);
                return;
            }
        }

        //Total Waves Win Condition. OLD, removed the total Waves field 10/7/2024
        /*if (m_wave >= m_totalWaves - 1)
        {
            UpdateGameplayState(GameplayState.Victory);
            return;
        }*/

        if (m_gameplayState == GameplayState.BossWave)
        {
            OnWaveCompleted?.Invoke(m_UIStringData.m_bossWaveComplete);
        }
        else
        {
            OnWaveCompleted?.Invoke(m_UIStringData.m_waveComplete);
        }

        RequestPlayAudio(m_gameplayAudioData.m_audioWaveEndClips);

        UpdateGameplayState(GameplayState.Build);
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

        OnGameObjectSelected?.Invoke(obj);
    }

    public void KillAllEnemies()
    {
        if (m_enemyList.Count <= 0) return;

        List<EnemyController> livingEnemies = new List<EnemyController>(m_enemyList);
        foreach (EnemyController enemy in livingEnemies)
        {
            enemy.OnTakeDamage(999999);
        }
    }

    public Selectable GetCurSelectedObj()
    {
        return m_curSelectable;
    }

    public void WatchingCutScene()
    {
        m_watchingCutScene = true;

        //Clear precon if we're precon
        if (m_preconstructedTowerObj) ClearPreconstructedTower();

        //Clear selected
        if (m_curSelectable)
        {
            OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
            m_curSelectable = null;
        }

        //Set interaction to disabled
        UpdateInteractionState(InteractionState.Disabled);

        //Set speed to paused
        UpdateGamePlayback(GameSpeed.Paused);
    }

    public void DoneWatchingLeaveCutScene()
    {
        //Set interaction to disabled
        UpdateInteractionState(InteractionState.Idle);

        //Set speed to paused
        UpdateGamePlayback(GameSpeed.Normal);

        m_watchingCutScene = false;

        OnCutSceneEnd?.Invoke();
    }

    public void CastleControllerDestroyed()
    {
        //To replace this with playing a cutscene then triggering defeat, or they're the same?
        UpdateGameplayState(GameplayState.Defeat);
    }

    public void BuildTowerQuestCompleted()
    {
        m_delayForQuest = false;
    }

    public void SetActiveBossController(BossSequenceController bossSequenceController)
    {
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
}

public class OozeManager
{
    public List<Cell> m_currentOozedCells = new List<Cell>();

    public OozeManager()
    {
        GameplayManager.OnGameplayStateChanged += GameplayStateChanged;
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