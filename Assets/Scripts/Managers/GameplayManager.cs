using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance;
    public GameplayState m_gameplayState;
    public GameSpeed m_gameSpeed;
    public int m_totalWaves = 10;
    public int m_wave;
    public int m_bossWaveFactor = 20; //Spawn a boss every N waves.

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
    public static event Action<int, int> OnObelisksCharged;
    public static event Action<GathererController> OnGathererAdded;
    public static event Action<GathererController> OnGathererRemoved;


    [Header("Castle")] public CastleController m_castleController;
    public Transform m_enemyGoal;
    [Header("Equipped Towers")] public TowerData[] m_equippedTowers;
    [Header("Unit Spawners")] public List<UnitSpawner> m_unitSpawners;
    private int m_activeSpawners;
    [Header("Active Enemies")] public List<EnemyController> m_enemyList;
    public Transform m_enemiesObjRoot;

    private Vector2Int m_curCellPos;
    private Vector2Int m_goalPointPos;

    [Header("Player Constructed")] public List<GathererController> m_woodGathererList;
    public List<GathererController> m_stoneGathererList;
    public Transform m_towerObjRoot;
    public List<Tower> m_towerList;

    [Header("Selected Object Info")] private Selectable m_curSelectable;
    public Selectable m_hoveredSelectable;
    public bool m_canAfford;
    public bool m_canPlace;
    public bool m_canBuild;

    [Header("Preconstructed Tower Info")] public GameObject m_preconstructedTowerObj;
    public Tower m_preconstructedTower;
    public Vector2Int m_preconstructedTowerPos;
    public LayerMask m_buildSurface;
    private int m_preconstructedTowerIndex;

    [SerializeField] private UIStringData m_UIStringData;

    private Camera m_mainCamera;
    public float m_buildDuration = 6;
    public float m_firstBuildDuraction = 15;
    [HideInInspector] public float m_timeToNextWave;

    public List<Obelisk> m_activeObelisks;

    public enum GameplayState
    {
        BuildGrid,
        PlaceObstacles,
        FloodFillGrid,
        CreatePaths,
        Setup,
        SpawnEnemies,
        SpawnBoss,
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
        SelecteObelisk,
    }

    void Update()
    {
        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObj = hit.collider.gameObject;

            //If we hit a UI Game Object.
            if (EventSystem.current.IsPointerOverGameObject())
            {
                HandleUIInteraction(hitObj);
            }
            else //We hit a World Space Object.
            {
                HandleWorldSpaceInteraction(hitObj);
            }
        }

        if (m_interactionState == InteractionState.PreconstructionTower)
        {
            DrawPreconstructedTower();
        }

        m_timeToNextWave -= Time.deltaTime;
        if (m_timeToNextWave <= 0 && m_gameplayState == GameplayState.Build)
        {
            if (m_wave % m_bossWaveFactor == 0)
            {
                UpdateGameplayState(GameplayState.SpawnBoss);
            }
            else
            {
                UpdateGameplayState(GameplayState.SpawnEnemies);
            }
        }
    }

    void HandleUIInteraction(GameObject obj)
    {
        m_hoveredSelectable = null;
    }

    void HandleWorldSpaceInteraction(GameObject obj)
    {
        //Is the object selectable?
        Selectable hitSelectable = obj.GetComponent<Selectable>();
        if (m_hoveredSelectable == null || m_hoveredSelectable != hitSelectable)
        {
            m_hoveredSelectable = hitSelectable;
        }

        //
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
                case InteractionState.SelecteObelisk:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //
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
                    OnAlertDisplayed?.Invoke(m_UIStringData.m_cannotAfford);
                    return;
                }

                if (!m_canPlace)
                {
                    //Debug.Log("Cannot build here.");
                    OnAlertDisplayed?.Invoke(m_UIStringData.m_cannotPlace);
                    return;
                }

                BuildTower();
                return;
            }

            if (m_hoveredSelectable != null && m_curSelectable != m_hoveredSelectable)
            {
                //Debug.Log(m_hoveredSelectable + " : selected.");
                OnGameObjectSelected?.Invoke(m_hoveredSelectable.gameObject);
                //Clear Hoverable because we've selected.
                m_hoveredSelectable = null;
            }
            else if (m_curSelectable && m_hoveredSelectable == null)
            {
                OnGameObjectDeselected?.Invoke(m_curSelectable.gameObject);
            }
        }

        //
        //Mouse 2 Clicking
        if (Input.GetMouseButtonDown(1) && m_interactionState != InteractionState.Disabled)
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
                    case InteractionState.SelecteObelisk:
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

    private void Awake()
    {
        Instance = this;
        m_mainCamera = Camera.main;
        if (m_enemyGoal != null)
        {
            m_goalPointPos = new Vector2Int((int)m_enemyGoal.position.x, (int)m_enemyGoal.position.z);
        }

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
            case GameplayState.FloodFillGrid:
                break;
            case GameplayState.CreatePaths:
                break;
            case GameplayState.Setup:
                UpdateGameSpeed(GameSpeed.Normal);
                break;
            case GameplayState.SpawnEnemies:
                m_wave++;
                break;
            case GameplayState.SpawnBoss:
                m_wave++;
                break;
            case GameplayState.Combat:
                break;
            case GameplayState.Build:
                //If this is the first wave, give a bit longer to build.
                if (m_wave < 0)
                {
                    m_timeToNextWave = m_firstBuildDuraction;
                }
                else
                {
                    m_timeToNextWave = m_buildDuration;
                }
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
            case InteractionState.SelecteObelisk:
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
            case Selectable.SelectedObjectType.Obelisk:
                UpdateInteractionState(InteractionState.SelecteObelisk);
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

    public void PreconstructTower(int i)
    {
        if (i >= m_equippedTowers.Length) return;

        ClearPreconstructedTower();

        //Set up the objects
        m_preconstructedTowerObj = Instantiate(m_equippedTowers[i].m_prefab, Vector3.zero, Quaternion.identity);
        m_preconstructedTower = m_preconstructedTowerObj.GetComponent<Tower>();
        m_preconstructedTowerIndex = i;
        OnGameObjectSelected?.Invoke(m_preconstructedTowerObj);

        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, m_buildSurface))
        {
            //Raycast has hit the ground, round that point to the nearest int.
            Vector3 gridPos = raycastHit.point;

            //Convert hit point to 2d grid position
            m_preconstructedTowerPos = Util.GetVector2IntFrom3DPos(gridPos);

            OnPreconTowerMoved?.Invoke(m_preconstructedTowerPos);
        }

        //Set the bools to negative and let DrawPreconstructedTower flip them.
        m_canAfford = false;
        m_canBuild = false;
        m_canPlace = false;
        OnObjRestricted?.Invoke(m_curSelectable.gameObject, m_canBuild);
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
            Vector2Int newPos = Util.GetVector2IntFrom3DPos(gridPos);

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

        //We want to rotate towers to they look away from the Castle.
        // Calculate the direction vector from the target object to the current object.
        Vector3 direction = m_enemyGoal.position - m_preconstructedTowerObj.transform.position;

        // Calculate the rotation angle to make the new object face away from the target.
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 180f;
        m_preconstructedTower.GetTurretTransform().rotation = Quaternion.Euler(0, angle, 0);


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

        //If the current cell is not apart of the grid, not a valid spot.
        if (curCell == null)
        {
            Debug.Log($"CannotPlace: Current Cell is Null");
            return false;
        }

        //If we have actors on the cell.
        if (curCell.m_actorCount > 0)
        {
            Debug.Log($"Cannot Place: There are actors on this cell.");
            return false;
        }

        //If we're hovering on the exit cell
        if (m_preconstructedTowerPos == Util.GetVector2IntFrom3DPos(m_enemyGoal.position))
        {
            Debug.Log($"Cannot Place: This is the exit cell.");
            return false;
        }

        //If the currenct cell is occupied (by a structure), not a valid spot.
        if (curCell.m_isOccupied)
        {
            Debug.Log($"Cannot Place: This cell is already occupied.");
            return false;
        }

        //Check to see if any of our UnitPaths have no path.
        if (!GridManager.Instance.m_spawnPointsAccessible)
        {
            Debug.Log($"Cannot Place: This would block one or more spawners from reaching the exit.");
            return false;
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
                List<Vector2Int> testPath = AStar.FindExitPath(neighbors[i], m_goalPointPos, m_preconstructedTowerPos, new Vector2Int(-1, -1));

                //If we found a path, this neighbor is OK. If not, we need to check if it creates an island.
                if (testPath == null)
                {
                    Debug.Log($"No path found from neighbor {i}, checking for inhabited islands.");
                    //If we did not find a path, check islands for actors. If there are, we cannot build here.
                    List<Vector2Int> islandCells = new List<Vector2Int>(AStar.FindIsland(neighbors[i], m_preconstructedTowerPos));
                    foreach (Vector2Int cellPos in islandCells)
                    {
                        Cell islandCell = Util.GetCellFromPos(cellPos);
                        if (islandCell.m_actorCount > 0)
                        {
                            Debug.Log($"Cannot Place: {islandCells.Count} Island created, and Cell:{islandCell.m_cellIndex} contains actors");
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

        //We want to place towers to they look away from the Castle.
        // Calculate the direction vector from the target object to the current object.
        Vector3 direction = m_enemyGoal.position - m_preconstructedTowerObj.transform.position;

        // Calculate the rotation angle to make the new object face away from the target.
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 180f;

        GameObject newTowerObj = Instantiate(m_equippedTowers[m_preconstructedTowerIndex].m_prefab, gridPos, Quaternion.identity, m_towerObjRoot.transform);
        Tower newTower = newTowerObj.GetComponent<Tower>();
        newTower.GetTurretTransform().rotation = Quaternion.Euler(0, angle, 0);
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

    public void AddTowerToList(Tower tower)
    {
        m_towerList.Add(tower);
    }

    public void RemoveTowerFromList(Tower tower)
    {
        for (int i = 0; i < m_towerList.Count; ++i)
        {
            if (m_towerList[i] == tower)
            {
                m_towerList.RemoveAt(i);
            }
        }
    }

    public void SellTower(Tower tower, int stoneValue, int woodValue)
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

    public void UpgradeTower(Tower oldTower, TowerData newTowerData, int stoneValue, int woodValue)
    {
        Vector3 pos = oldTower.gameObject.transform.position;
        GridCellOccupantUtil.SetOccupant(oldTower.gameObject, false, 1, 1);
        RemoveTowerFromList(oldTower);
        ResourceManager.Instance.UpdateStoneAmount(-stoneValue);
        ResourceManager.Instance.UpdateWoodAmount(-woodValue);
        IngameUIController.Instance.SpawnCurrencyAlert(woodValue, stoneValue, false, oldTower.transform.position);
        Quaternion curRotation = oldTower.GetTurretRotation();
        Destroy(oldTower.gameObject);

        GameObject newTowerObj = Instantiate(newTowerData.m_prefab, pos, Quaternion.identity, m_towerObjRoot.transform);
        Tower newTower = newTowerObj.GetComponent<Tower>();
        newTower.SetTurretRotation(curRotation);
        newTower.SetupTower();

        m_curSelectable = null;
        UpdateInteractionState(InteractionState.Idle);
        Debug.Log("Tower upgraded.");
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
            }
        }

        if (m_activeSpawners == 0 && m_enemyList.Count == 0 && m_gameplayState != GameplayState.Defeat)
        {
            CheckForWin();
        }
    }

    public void AddSpawnerToList(UnitSpawner unitSpawner)
    {
        m_unitSpawners.Add(unitSpawner);
        Debug.Log($"Added creep spawner: {m_unitSpawners.Count}");
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

    [HideInInspector] public int m_obeliskCount;
    private int m_obelisksChargedCount;

    public void AddObeliskToList(Obelisk obelisk)
    {
        if (m_activeObelisks == null) m_activeObelisks = new List<Obelisk>();
        m_activeObelisks.Add(obelisk);
        ++m_obeliskCount;
        OnObelisksCharged?.Invoke(m_obelisksChargedCount, m_obeliskCount);
    }

    public void CheckObeliskStatus()
    {
        m_obelisksChargedCount = 0;
        bool charging = false;

        //Identify if there are any obelisks still charging.
        foreach (Obelisk obelisk in m_activeObelisks)
        {
            if (obelisk.m_obeliskState == Obelisk.ObeliskState.Charged)
            {
                ++m_obelisksChargedCount;
            }
        }

        OnObelisksCharged?.Invoke(m_obelisksChargedCount, m_obeliskCount);
    }

    public void CheckForWin()
    {
        //Obelisk Win Condition.
        if (m_obeliskCount > 0 && m_obelisksChargedCount == m_obeliskCount)
        {
            UpdateGameplayState(GameplayState.Victory);
            return;
        }

        //Total Waves Win Condition.
        if (m_wave >= m_totalWaves - 1)
        {
            UpdateGameplayState(GameplayState.Victory);
            return;
        }

        UpdateGameplayState(GameplayState.Build);
    }

    public void RequestSelectGatherer(GameObject obj)
    {
        if (m_interactionState == InteractionState.PreconstructionTower)
        {
            ClearPreconstructedTower();
        }

        OnGameObjectSelected?.Invoke(obj);
    }
}