using System;
using System.Collections;
using System.Collections.Generic;
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
    public int m_wave;

    public static event Action<GameplayState> OnGameplayStateChanged;
    public static event Action<GameObject> OnGameObjectSelected;
    public static event Action<GameObject> OnGameObjectDeselected;
    public static event Action<GameObject> OnCommandRequested;
    public static event Action<GameObject, bool> OnObjRestricted;
    public static event Action<String> OnAlertDisplayed;

    [Header("Castle")] public CastleController m_castleController;
    public Transform[] m_enemyGoals;
    [Header("Equipped Towers")] public ScriptableTowerDataObject[] m_equippedTowers;
    [Header("Unit Spawners")] public List<UnitSpawner> m_unitSpawners;
    [Header("Active Enemies")] public List<UnitEnemy> m_enemyList;
    public Transform m_enemiesObjRoot;

    [Header("Player Constructed")] public Transform m_gathererObjRoot;
    public List<GathererController> m_woodGathererList;
    public List<GathererController> m_stoneGathererList;
    public Transform m_towerObjRoot;
    public List<TowerController> m_towerList;


    [Header("Selected Object Info")] private Selectable m_curSelectable;
    private Selectable m_hoveredSelectable;
    private bool m_placementOpen;
    private bool m_costAffordable;
    private bool m_placementPathsValid;
    private bool m_canBuild = true;

    [Header("Preconstructed Tower Info")] public GameObject m_preconstructedTowerObj;
    public TowerController m_preconstructedTower;
    [SerializeField] private LayerMask m_buildSurface;
    [SerializeField] private LayerMask m_pathObstructableLayer;
    private int m_preconstructedTowerIndex;
    private Vector3 m_preconstructedTowerPos;

    [SerializeField] private ScriptableUIStrings m_uiStrings;

    private Camera m_mainCamera;
    public float m_buildDuration;
    private float m_timeToNextWave;


    public enum GameplayState
    {
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
        Idle,
        SelectedGatherer,
        SelectedTower,
        PreconstructionTower,
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
                    case InteractionState.Idle:
                        break;
                    case InteractionState.SelectedGatherer:
                        break;
                    case InteractionState.SelectedTower:
                        break;
                    case InteractionState.PreconstructionTower:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            //Mouse 1 Clicking
            if (Input.GetMouseButtonUp(0))
            {
                //Based on the interaction state we're in, when mouse 1 is pressed, do X.
                //If the object we're hovering is not currently the selected object.
                if (m_interactionState == InteractionState.PreconstructionTower)
                {
                    if (!m_costAffordable)
                    {
                        //Debug.Log("Not Enough Resources.");
                        OnAlertDisplayed?.Invoke(m_uiStrings.m_cannotAfford);
                        return;
                    }

                    if (!m_placementOpen || !m_placementPathsValid)
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
            if (Input.GetMouseButtonDown(1))
            {
                //If something is selected.
                if (m_hoveredSelectable != null || m_preconstructedTowerObj != null)
                {
                    switch (m_interactionState)
                    {
                        case InteractionState.Idle:
                            break;
                        case InteractionState.SelectedGatherer:
                            if (m_hoveredSelectable.m_selectedObjectType == Selectable.SelectedObjectType.ResourceWood)
                            {
                                OnCommandRequested?.Invoke(m_hoveredSelectable.gameObject);
                            }

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
        OnGameplayStateChanged += GameplayManagerStateChanged;
        OnGameObjectSelected += GameObjectSelected;
        OnGameObjectDeselected += GameObjectDeselected;
    }

    void OnDestroy()
    {
        OnGameplayStateChanged -= GameplayManagerStateChanged;
        OnGameObjectSelected -= GameObjectSelected;
        OnGameObjectDeselected -= GameObjectDeselected;
    }

    private void GameplayManagerStateChanged(GameplayState state)
    {
        //
    }

    void Start()
    {
        UpdateGameplayState(GameplayState.Setup);
    }

    public void UpdateGameplayState(GameplayState newState)
    {
        m_gameplayState = newState;

        switch (m_gameplayState)
        {
            case GameplayState.Setup:
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
                break;
            case GameplayState.Defeat:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        OnGameplayStateChanged?.Invoke(newState);
    }

    public void UpdateInteractionState(InteractionState newState)
    {
        m_interactionState = newState;

        switch (m_interactionState)
        {
            case InteractionState.Idle:
                break;
            case InteractionState.SelectedGatherer:
                break;
            case InteractionState.SelectedTower:
                break;
            case InteractionState.PreconstructionTower:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void CheckObjRestriction()
    {
        bool canBuild;
        
        //Check cost & banks
        int curStone = ResourceManager.Instance.GetStoneAmount();
        int curWood = ResourceManager.Instance.GetWoodAmount();
        ValueTuple<int, int> cost = m_preconstructedTower.GetTowercost();
        bool newCostAffordable = curStone >= cost.Item1 && curWood >= cost.Item2;
        if (newCostAffordable != m_costAffordable)
        {
            Debug.Log(newCostAffordable ? "Cost is Affordable." : "Cost is not Affordable.");
            m_costAffordable = newCostAffordable;
        }

        //Check collision
        bool newPlacementOpen = !m_hoveredSelectable;
        if (newPlacementOpen != m_placementOpen)
        {
            Debug.Log(newPlacementOpen ? "Placement is Open" : "Placement is not Open");
            m_placementOpen = newPlacementOpen;
        }

        //Check pathing
        Vector3 newPlacementPosition = m_preconstructedTowerObj.transform.position;
        newPlacementPosition = Util.RoundVectorToInt(newPlacementPosition);
        newPlacementPosition.y = 0f;
        if (m_placementOpen && m_costAffordable && newPlacementPosition != m_preconstructedTowerPos)
        {
            m_preconstructedTowerPos = newPlacementPosition;
            m_placementPathsValid = CheckPathWithObstacle(m_preconstructedTowerPos);
            Debug.Log(m_placementPathsValid ? "Placement paths are valid" : "Placement paths are not valid");
        }

        //Can we build
        canBuild = m_costAffordable && m_placementOpen && m_placementPathsValid;
        Debug.Log(canBuild);
        if (canBuild != m_canBuild)
        {
            m_canBuild = canBuild;
            OnObjRestricted?.Invoke(m_curSelectable.gameObject, m_canBuild);
            Debug.Log("Can build : " + m_canBuild);
        }
    }

    private bool CheckPathWithObstacle(Vector3 testPos)
    {
        /*//Can the spawners path to the the testPos, and from the testPos to the castle?
        for (int i = 0; i < m_unitSpawners.Count;)
        {
            Vector3 spawnerPos = m_unitSpawners[i].m_spawnPoint.position;

            // Check if the path would be valid with the obstacle placed
            bool pathToObstacle = IsPathValid(testPos, spawnerPos);
            if (!pathToObstacle)
            {
                return false;
            }
            
            //Check if the path is valid from the obstacle to the castle.
            for (int x = 0; x < m_enemyGoals.Length; ++x)
            {
                bool pathToCastle = IsPathValid(testPos, m_enemyGoals[i].position);
                if (!pathToCastle)
                {
                    return false;
                }
            }
        }*/

        return false;
    }

    private bool IsPathValid(Vector3 from, Vector3 to)
    {
        /*NavMeshPath path = new NavMeshPath();
        bool validPath = NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);

        // Check if the path is complete and valid
        if (validPath && path.status == NavMeshPathStatus.PathComplete)
        {
            return true;
        }
        else
        {
            return false;
        }*/
        return false;
    }

    private void GameObjectSelected(GameObject obj)
    {
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
                    m_interactionState = InteractionState.PreconstructionTower;
                }
                else
                {
                    m_interactionState = InteractionState.SelectedTower;
                }

                break;
            case Selectable.SelectedObjectType.Gatherer:
                m_interactionState = InteractionState.SelectedGatherer;
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
                break;
            case ResourceManager.ResourceType.Stone:
                m_stoneGathererList.Add(gatherer);
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
        m_costAffordable = true;
        m_placementOpen = true;
    }

    private void DrawPreconstructedTower()
    {
        CheckObjRestriction();

        //Position the objects
        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, m_buildSurface))
        {
            Vector3 gridPos = raycastHit.point;
            gridPos = Util.RoundVectorToInt(gridPos);
            gridPos.y = 0.7f;
            m_preconstructedTowerObj.transform.position = Vector3.Lerp(m_preconstructedTowerObj.transform.position, gridPos, 20f * Time.deltaTime);
        }
    }

    public void ClearPreconstructedTower()
    {
        if (m_preconstructedTowerObj)
        {
            Destroy(m_preconstructedTowerObj);
        }

        m_preconstructedTowerObj = null;
        m_preconstructedTower = null;
    }

    public void BuildTower()
    {
        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, m_buildSurface))
        {
            Vector3 gridPos = raycastHit.point;
            gridPos = Util.RoundVectorToInt(gridPos);
            GameObject newTowerObj = Instantiate(m_equippedTowers[m_preconstructedTowerIndex].m_prefab, gridPos, Quaternion.identity, m_towerObjRoot.transform);
            TowerController newTower = newTowerObj.GetComponent<TowerController>();
            newTower.SetupTower();

            //Update banks
            ValueTuple<int, int> cost = newTower.GetTowercost();
            ResourceManager.Instance.UpdateStoneAmount(-cost.Item1);
            ResourceManager.Instance.UpdateWoodAmount(-cost.Item2);
        }
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
        //Debug.Log("Added creep spawner: " + m_unitSpawners.Count);
    }

    public void DisableSpawner()
    {
        bool spawning = false;
        foreach (UnitSpawner unitSpawner in m_unitSpawners)
        {
            spawning = unitSpawner.IsSpawning();
        }

        if (!spawning)
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