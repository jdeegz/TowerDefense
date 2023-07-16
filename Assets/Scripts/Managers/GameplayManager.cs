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

    [Header("Castle Points")] public Transform[] m_enemyGoals;
    [Header("Equipped Towers")] public ScriptableTowerDataObject[] m_equippedTowers;

    [Header("Player Constructed")] public Transform m_gathererObjRoot;
    public List<GathererController> m_woodGathererList;
    public List<GathererController> m_stoneGathererList;
    public Transform m_towerObjRoot;
    public List<TowerController> m_towerList;

    [Header("Cross Hair Info")] public LayerMask m_crossHairLayerMask;
    public static event Action<GameplayState> OnGameplayStateChanged;
    public static event Action<GameObject> OnGameObjectSelected;
    public static event Action<GameObject> OnCommandRequested;
    public static event EventHandler OnObjRestricted;

    [Header("Selected Object Info")] public GameObject m_selectedRing;
    public GameObject m_selectedObj;
    public bool m_selectedObjIsRestricted;
    public LayerMask m_objLayerMask;
    public int m_preConstructedTowerIndex;
    public Color m_outlineBaseColor;
    public Color m_outlineRestrictedColor;
    public float m_outlineWidth;

    [Header("Preconstructed Tower Info")] public GameObject m_preconstructedTowerObj;
    public TowerController m_preconstructedTower;


    private GameObject m_hoveredObj;
    
    public enum GameplayState
    {
        Setup,
        Combat,
        Paused,
        Victory,
        Defeat,
    }
    
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

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject != m_hoveredObj)
            {
                //Debug.Log("Mouse hovering over: " + hit.collider.gameObject.name);
                m_hoveredObj = hit.collider.gameObject;
            }
        }
        
        //Tommorow: Change all of this update code store what im hovering and act upon it based on what it is, rather than having a lot of splintered code.
        //May need states; like No Selection, Selected Object, Preconstruction to help clean up calls based on input and context.
        if (m_preconstructedTowerObj)
        {
            DrawPreconstructedTower();
        }

        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnMouseLeftDown();
            }

            if (Input.GetMouseButtonDown(1))
            {
                OnMouseRightDown();
                OnObjRestricted?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    void OnMouseLeftDown()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, m_objLayerMask))
        {
            //Debug.Log(raycastHit.collider.name + " was clicked on.");
            OnGameObjectSelected?.Invoke(raycastHit.collider.gameObject);
        }
        else
        {
            Debug.Log("No valid raycast hit.");
        }
    }

    void OnMouseRightDown()
    {
        //Move Unit
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, m_objLayerMask))
        {
            OnCommandRequested?.Invoke(raycastHit.collider.gameObject);
        }
        else
        {
            Debug.Log("No valid raycast hit.");
        }
    }

    private void Awake()
    {
        Instance = this;
        OnGameplayStateChanged += GameplayManagerStateChanged;
        OnGameObjectSelected += OnOnGameObjectSelected;
    }

    void OnDestroy()
    {
        OnGameplayStateChanged -= GameplayManagerStateChanged;
        OnGameObjectSelected -= OnOnGameObjectSelected;
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

    private void OnOnGameObjectSelected(GameObject obj)
    {
        m_selectedObj = obj;
        Debug.Log("Selected : " + obj.name);
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
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, m_crossHairLayerMask))
        {
            Vector3 gridPos = raycastHit.point;
            gridPos = Util.RoundVectorToInt(gridPos);
            gridPos.y = .02f;
            m_preconstructedTowerObj.transform.position = gridPos;
        }
        //Check currency

        //Set visibility
    }

    public void ClearPreconstructedTower()
    {
        Destroy(m_preconstructedTowerObj);
        m_preconstructedTowerObj = null;
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