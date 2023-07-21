using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance;
    public GameplayState m_gameplayState;

    public static event Action<GameplayState> OnGameplayStateChanged;
    public static event Action<GameObject> OnGameObjectSelected;
    public static event Action<GameObject> OnGameObjectDeselected;
    public static event Action<GameObject> OnCommandRequested;
    public static event EventHandler OnObjRestricted;

    [Header("Castle Points")] public Transform[] m_enemyGoals;
    [Header("Equipped Towers")] public ScriptableTowerDataObject[] m_equippedTowers;

    [Header("Player Constructed")] public Transform m_gathererObjRoot;
    public List<GathererController> m_woodGathererList;
    public List<GathererController> m_stoneGathererList;
    public Transform m_towerObjRoot;
    public List<TowerController> m_towerList;


    [Header("Selected Object Info")] private Selectable m_curSelectable;
    private Selectable m_hoveredSelectable;
    public bool m_selectedObjIsRestricted;

    [Header("Preconstructed Tower Info")] public GameObject m_preconstructedTowerObj;
    public TowerController m_preconstructedTower;
    [SerializeField] private LayerMask m_buildSurface;


    public enum GameplayState
    {
        Setup,
        Combat,
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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // && !EventSystem.current.IsPointerOverGameObject()
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
                        //Check for restrictions
                        //OnObjRestricted?.Invoke(this, EventArgs.Empty);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            //Mouse 1 Clicking
            if (Input.GetMouseButtonDown(0))
            {
                //Based on the interaction state we're in, when mouse 1 is pressed, do X.
                //If the object we're hovering is not currently the selected object.
                if (m_hoveredSelectable != null && m_curSelectable != m_hoveredSelectable)
                {
                    Debug.Log(m_hoveredSelectable + " : selected.");
                    switch (m_interactionState)
                    {
                        case InteractionState.Idle:
                            OnGameObjectSelected?.Invoke(m_hoveredSelectable.gameObject);
                            break;
                        case InteractionState.SelectedGatherer:
                            OnGameObjectSelected?.Invoke(m_hoveredSelectable.gameObject);
                            break;
                        case InteractionState.SelectedTower:
                            OnGameObjectSelected?.Invoke(m_hoveredSelectable.gameObject);
                            break;
                        case InteractionState.PreconstructionTower:
                            //Try to build the tower
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
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
    }

    private void Awake()
    {
        Instance = this;
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
            case GameplayState.Combat:
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
        OnGameObjectSelected?.Invoke(m_preconstructedTowerObj);
    }

    private void DrawPreconstructedTower()
    {
        //Position the objects
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, m_buildSurface))
        {
            Vector3 gridPos = raycastHit.point;
            gridPos = Util.RoundVectorToInt(gridPos);
            gridPos.y = 0.7f;
            m_preconstructedTowerObj.transform.position = Vector3.Lerp(m_preconstructedTowerObj.transform.position, gridPos, 20f * Time.deltaTime);
        }
        //Check currency

        //Set visibility
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
}