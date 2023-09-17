using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance;
    public GameplayState m_gameplayState;
    public GameSpeed m_gameSpeed;
    public int m_wave;

    public static event Action<GameplayState> OnGameplayStateChanged;
    public static event Action<GameSpeed> OnGameSpeedChanged;
    public static event Action<GameObject> OnGameObjectSelected;
    public static event Action<GameObject> OnGameObjectDeselected;
    public static event Action<GameObject, Selectable.SelectedObjectType> OnCommandRequested;
    public static event Action<GameObject, bool> OnObjRestricted;
    public static event Action<String> OnAlertDisplayed;
    public static event Action<Vector2Int> OnPreconTowerMoved;
    public static event Action OnPreconstructedTowerClear;
    public static event Action OnTowerBuild;
    public static event Action OnTowerSell;


    [Header("Castle")] public CastleController m_castleController;
    [FormerlySerializedAs("m_enemyGoals")] public Transform m_enemyGoal;
    [Header("Equipped Towers")] public ScriptableTowerDataObject[] m_equippedTowers;
    [Header("Unit Spawners")] public List<UnitSpawner> m_unitSpawners;
    [Header("Active Enemies")] public List<UnitEnemy> m_enemyList;
    public Transform m_enemiesObjRoot;

    private Vector2Int m_curCellPos;
    private Vector2Int m_goalPointPos;

    [Header("Player Constructed")] public Transform m_gathererObjRoot;
    public List<GathererController> m_woodGathererList;
    public List<GathererController> m_stoneGathererList;
    public Transform m_towerObjRoot;
    public List<TowerController> m_towerList;


    [Header("Selected Object Info")] private Selectable m_curSelectable;
    public Selectable m_hoveredSelectable;
    private bool m_placementOpen;
    public bool m_canAfford;
    public bool m_canPlace;
    public bool m_canBuild = true;

    [Header("Preconstructed Tower Info")] public GameObject m_preconstructedTowerObj;
    public TowerController m_preconstructedTower;
    public Vector2Int m_preconstructedTowerPos;
    [SerializeField] private LayerMask m_buildSurface;
    [SerializeField] private LayerMask m_pathObstructableLayer;
    private int m_preconstructedTowerIndex;

    [SerializeField] private ScriptableUIStrings m_uiStrings;

    private Camera m_mainCamera;
    public float m_buildDuration;
    private float m_timeToNextWave;


    public enum GameplayState
    {
        BuildGrid,
        PlaceObstacles,
        CreatePaths,
        Setup,
        SpawnEnemies,
        Combat,
        Build,
        Paused,
        Victory,
        Defeat,
    }

    public InteractionState m_interactionState;


    public enum InteractionState
    {
        Disabled,
        Idle,
        SelectedGatherer,
        SelectedTower,
        PreconstructionTower,
        SelectedCastle,
    }

    void Update()
    {
        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && !EventSystem.current.IsPointerOverGameObject())
        {
            //Is the object selectable?
            Selectable hitSelectable = hit.collider.gameObject.GetComponent<Selectable>();
            if (m_hoveredSelectable == null || m_hoveredSelectable != hitSelectable)
            {
                m_hoveredSelectable = hitSelectable;
            }

            //Hovering
            if (m_hoveredSelectable)
            {
                switch (m_interactionState)
                {
                    case InteractionState.Disabled:
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            //Mouse 1 Clicking
            if (Input.GetMouseButtonUp(0) && m_interactionState != InteractionState.Disabled)
            {
                //Based on the interaction state we're in, when mouse 1 is pressed, do X.
                //If the object we're hovering is not currently the selected object.
                if (m_interactionState == InteractionState.PreconstructionTower)
                {
                    if (!m_canAfford)
                    {
                        //Debug.Log("Not Enough Resources.");
                        OnAlertDisplayed?.Invoke(m_uiStrings.m_cannotAfford);
                        return;
                    }

                    if (!m_placementOpen || !m_canPlace)
                    {
                        //Debug.Log("Cannot build here.");
                        OnAlertDisplayed?.Invoke(m_uiStrings.m_cannotPlace);
                        return;
                    }

                    BuildTower();
                    return;
                }

                if (m_hoveredSelectable != null && m_curSelectable != m_hoveredSelectable)
                {
                    //Debug.Log(m_hoveredSelectable + " : selected.");
                    OnGameObjectSelected?.Invoke(m_hoveredSelectable.gameObject);
                }
                else
                {
                    //Debug.Log("I clicked on nothing.");
                }
            }

            //Mouse 2 Clicking
            if (Input.GetMouseButtonDown(1) && m_interactionState != InteractionState.Disabled)
            {
                //If something is selected.
                if (m_hoveredSelectable != null || m_preconstructedTowerObj != null)
                {
                    switch (m_interactionState)
                    {
                        case InteractionState.Idle:
                            break;
                        case InteractionState.SelectedGatherer:
                            Debug.Log("Command Requested on Gatherer.");
                            OnCommandRequested?.Invoke(m_hoveredSelectable.gameObject, m_hoveredSelectable.m_selectedObjectType);
                            break;
                        case InteractionState.SelectedTower:
                            OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
                            break;
                        case InteractionState.PreconstructionTower:
                            //Cancel tower preconstruction
                            OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
                            ClearPreconstructedTower();
                            m_interactionState = InteractionState.Idle;
                            break;
                        case InteractionState.SelectedCastle:
                            OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (m_curSelectable)
                {
                    OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
                }
            }
        }

        if (m_interactionState == InteractionState.PreconstructionTower)
        {
            DrawPreconstructedTower();
        }

        m_timeToNextWave -= Time.deltaTime;
        if (m_timeToNextWave <= 0 && m_gameplayState == GameplayState.Build)
        {
            UpdateGameplayState(GameplayState.SpawnEnemies);
        }
    }

    private void Awake()
    {
        Instance = this;
        m_mainCamera = Camera.main;
        m_goalPointPos = new Vector2Int((int)m_enemyGoal.position.x, (int)m_enemyGoal.position.z);
        OnGameplayStateChanged += GameplayManagerStateChanged;
        OnGameSpeedChanged += GameplaySpeedChanged;
        OnGameObjectSelected += GameObjectSelected;
        OnGameObjectDeselected += GameObjectDeselected;
    }

    void OnDestroy()
    {
        OnGameplayStateChanged -= GameplayManagerStateChanged;
        OnGameSpeedChanged -= GameplaySpeedChanged;
        OnGameObjectSelected -= GameObjectSelected;
        OnGameObjectDeselected -= GameObjectDeselected;
    }

    private void GameplaySpeedChanged(GameSpeed state)
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
    }

    public enum GameSpeed
    {
        Paused,
        Normal,
        Fast,
    }

    public void UpdateGameSpeed(GameSpeed newSpeed)
    {
        switch (newSpeed)
        {
            case GameSpeed.Paused:
                Time.timeScale = 0;
                break;
            case GameSpeed.Normal:
                Time.timeScale = 1;
                break;
            case GameSpeed.Fast:
                Time.timeScale = 1.5f;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newSpeed), newSpeed, null);
        }

        OnGameSpeedChanged?.Invoke(newSpeed);
    }

    public void UpdateGameplayState(GameplayState newState)
    {
        m_gameplayState = newState;
        Debug.Log($"Game state is now: {m_gameplayState}");

        switch (m_gameplayState)
        {
            case GameplayState.BuildGrid:
                m_interactionState = InteractionState.Disabled;
                break;
            case GameplayState.PlaceObstacles:
                break;
            case GameplayState.CreatePaths:
                break;
            case GameplayState.Setup:
                UpdateGameSpeed(GameSpeed.Normal);
                break;
            case GameplayState.SpawnEnemies:
                m_wave++;
                break;
            case GameplayState.Combat:
                break;
            case GameplayState.Build:
                m_timeToNextWave = m_buildDuration;
                break;
            case GameplayState.Paused:
                break;
            case GameplayState.Victory:
                m_interactionState = InteractionState.Disabled;
                break;
            case GameplayState.Defeat:
                m_interactionState = InteractionState.Disabled;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        OnGameplayStateChanged?.Invoke(newState);
    }

    public void UpdateInteractionState(InteractionState newState)
    {
        m_interactionState = newState;
        //Debug.Log($"Interaction state is now: {m_interactionState}");

        switch (m_interactionState)
        {
            case InteractionState.Disabled:
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
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void GameObjectSelected(GameObject obj)
    {
        //Deselect current selection.
        if (m_curSelectable)
        {
            OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
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
            default:
                throw new ArgumentOutOfRangeException();
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
    }

    public void PreconstructTower(int i)
    {
        ClearPreconstructedTower();

        //Set up the objects
        m_preconstructedTowerObj = Instantiate(m_equippedTowers[i].m_prefab, Vector3.zero, Quaternion.identity);
        m_preconstructedTower = m_preconstructedTowerObj.GetComponent<TowerController>();
        m_preconstructedTowerIndex = i;
        OnGameObjectSelected?.Invoke(m_preconstructedTowerObj);
        m_canAfford = true;
        m_placementOpen = true;
    }

    private void DrawPreconstructedTower()
    {
        //Position the objects
        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, m_buildSurface))
        {
            //Raycast has hit the ground, round that point to the nearest int.
            Vector3 gridPos = raycastHit.point;

            //Convert hit point to 2d grid position
            Vector2Int newPos =
                new Vector2Int(Mathf.FloorToInt(gridPos.x + 0.5f), Mathf.FloorToInt(gridPos.z + 0.5f));

            if (newPos != m_preconstructedTowerPos)
            {
                m_preconstructedTowerPos = newPos;

                OnPreconTowerMoved?.Invoke(m_preconstructedTowerPos);
            }

            //Define the new destination of the Precon Tower Obj. Offset the tower on Y.
            Vector3 moveToPosition = new Vector3(m_preconstructedTowerPos.x, 0.7f, m_preconstructedTowerPos.y);

            //Position the precon Tower at the cursor position.
            m_preconstructedTowerObj.transform.position = Vector3.Lerp(m_preconstructedTowerObj.transform.position,
                moveToPosition, 20f * Time.deltaTime);
        }

        //Check Affordability & Pathing every frame. (Because you may be sitting waiting for units to move out of an island)
        bool canAfford = CheckAffordability();
        bool canPlace = CheckPathRestriction();

        //If either values have changed, updated the canBuild and invoke ObjRestricted.
        if (canAfford != m_canAfford || canPlace != m_canPlace)
        {
            m_canAfford = canAfford;
            m_canPlace = canPlace;
            m_canBuild = canAfford && canPlace;
            OnObjRestricted?.Invoke(m_curSelectable.gameObject, m_canBuild);
            Debug.Log("Can Afford : " + m_canAfford + " Can Place: " + m_canPlace);
        }
    }

    bool CheckAffordability()
    {
        //Check cost & banks
        int curStone = ResourceManager.Instance.GetStoneAmount();
        int curWood = ResourceManager.Instance.GetWoodAmount();
        ValueTuple<int, int> cost = m_preconstructedTower.GetTowercost();
        bool canAfford = curStone >= cost.Item1 && curWood >= cost.Item2;
        return canAfford;
    }

    bool CheckPathRestriction()
    {
        Cell curCell = Util.GetCellFromPos(m_preconstructedTowerPos);
        //Debug.Log("Checking path from: " + m_preconstructedTowerPos);

        //If the current cell is not apart of the grid, not a valid spot.
        if (curCell == null)
        {
            //Debug.Log("CheckPathRestriction: No cell here.");
            return false;
        }

        if (curCell.m_actorCount > 0)
        {
            return false;
        }

        //If the currenct cell is occupied (by a structure), not a valid spot.
        if (curCell.m_isOccupied)
        {
            //Debug.Log("CheckPathRestriction: Cell is occupied");
            return false;
        }

        //Check to see if any of our UnitPaths have no path.
        for (var i = 0; i < GridManager.Instance.m_unitPaths.Count; i++)
        {
            var unitPath = GridManager.Instance.m_unitPaths[i];
            if (!unitPath.m_hasPath)
            {
                Debug.Log($"{unitPath.m_sourceObj.name} has no path.");
                return false;
            }
        }

        //Get neighbor cells.
        Vector2Int[] neighbors =
        {
            new Vector2Int(m_preconstructedTowerPos.x, m_preconstructedTowerPos.y + 1), //North
            new Vector2Int(m_preconstructedTowerPos.x + 1, m_preconstructedTowerPos.y), //East
            new Vector2Int(m_preconstructedTowerPos.x, m_preconstructedTowerPos.y - 1), //South
            new Vector2Int(m_preconstructedTowerPos.x - 1, m_preconstructedTowerPos.y) //West
        };

        //Check the path from each neighbor to the goal.
        for (int i = 0; i < neighbors.Length; ++i)
        {
            //Debug.Log("Pathing from Neighbor:" + i + " of: " + neighbors.Length);
            Cell cell = Util.GetCellFromPos(neighbors[i]);

            //Assure we're not past the bound of the grid.
            if (cell == null)
            {
                Debug.Log("Neighbor not on Grid:" + neighbors[i]);
                continue;
            }

            //Assure the neighbor cell is not occupied.
            if (!cell.m_isOccupied)
            {
                List<Vector2Int> testPath = AStar.FindPath(neighbors[i], m_goalPointPos);

                //If we found a path, this neighbor is OK. If not, we need to check if it creates an island.
                if (testPath == null)
                {
                    //If we did not find a path, check islands for actors. If there are, we cannot build here.
                    List<Vector2Int> islandCells = new List<Vector2Int>(AStar.FindIsland(neighbors[i]));
                    foreach (Vector2Int cellPos in islandCells)
                    {
                        Cell islandCell = Util.GetCellFromPos(cellPos);
                        if (islandCell.m_actorCount > 0)
                        {
                            Debug.Log("Island created, and contains actors");
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    public void ClearPreconstructedTower()
    {
        if (m_preconstructedTowerObj)
        {
            Destroy(m_preconstructedTowerObj);
        }

        m_preconstructedTowerObj = null;
        m_preconstructedTower = null;
        OnPreconstructedTowerClear?.Invoke();
    }

    public void BuildTower()
    {
        Vector3 gridPos = new Vector3(m_preconstructedTowerPos.x, 0, m_preconstructedTowerPos.y);
        GameObject newTowerObj = Instantiate(m_equippedTowers[m_preconstructedTowerIndex].m_prefab, gridPos, Quaternion.identity, m_towerObjRoot.transform);
        TowerController newTower = newTowerObj.GetComponent<TowerController>();
        newTower.SetupTower();

        //Update banks
        ValueTuple<int, int> cost = newTower.GetTowercost();
        if (cost.Item1 > 0)
        {
            ResourceManager.Instance.UpdateStoneAmount(-cost.Item1);
        }

        if (cost.Item2 > 0)
        {
            ResourceManager.Instance.UpdateWoodAmount(-cost.Item2);
        }

        IngameUIController.Instance.SpawnCurrencyAlert(cost.Item2, cost.Item1, false, newTowerObj.transform.position);
        OnTowerBuild?.Invoke();
    }

    public void AddTowerToList(TowerController tower)
    {
        m_towerList.Add(tower);
    }

    public void RemoveTowerFromList(TowerController tower)
    {
        for (int i = 0; i < m_towerList.Count; ++i)
        {
            if (m_towerList[i] == tower)
            {
                m_towerList.RemoveAt(i);
            }
        }
    }

    public void SellTower(TowerController tower, int stoneValue, int woodValue)
    {
        GridCellOccupantUtil.SetOccupant(tower.gameObject, false, 1, 1);
        RemoveTowerFromList(tower);
        ResourceManager.Instance.UpdateStoneAmount(stoneValue);
        ResourceManager.Instance.UpdateWoodAmount(woodValue);
        IngameUIController.Instance.SpawnCurrencyAlert(woodValue, stoneValue, true, tower.transform.position);
        Destroy(tower.gameObject);
        m_curSelectable = null;
        UpdateInteractionState(InteractionState.Idle);
        Debug.Log("Tower sold.");
        OnTowerSell?.Invoke();
    }

    public void AddEnemyToList(UnitEnemy enemy)
    {
        m_enemyList.Add(enemy);
    }

    public void RemoveEnemyFromList(UnitEnemy enemy)
    {
        for (int i = 0; i < m_enemyList.Count; ++i)
        {
            if (m_enemyList[i] == enemy)
            {
                m_enemyList.RemoveAt(i);
            }
        }

        if (m_enemyList.Count <= 0)
        {
            UpdateGameplayState(GameplayState.Build);
        }
    }

    public void AddSpawnerToList(UnitSpawner unitSpawner)
    {
        m_unitSpawners.Add(unitSpawner);
        Debug.Log($"Added creep spawner: {m_unitSpawners.Count}");
    }

    public void DisableSpawner()
    {
        int activeSpawners = m_unitSpawners.Count;
        foreach (UnitSpawner unitSpawner in m_unitSpawners)
        {
            if (!unitSpawner.IsSpawning())
            {
                activeSpawners--;
            }
        }

        if (activeSpawners == 0)
        {
            //Debug.Log("Spawning Completed.");
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
}