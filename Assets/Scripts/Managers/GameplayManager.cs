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

    public Transform[] m_enemyGoals;
    public List<GathererController> m_woodGathererList;
    public List<GathererController> m_stoneGathererList;

    [Header("Cross Hair Info")] public GameObject m_crossHair;
    public LayerMask m_crossHairLayerMask;
    private bool m_drawCrosshair;
    public static event Action<GameplayState> OnGameplayStateChanged;
    public static event Action<GameObject> OnGameObjectSelected;
    public static event Action<GameObject> OnCommandRequested;

    [Header("Selected Object Info")] public GameObject m_selectedRing;
    public GameObject m_selectedObj;
    public LayerMask m_objLayerMask;

    [Header("Equipped Towers")] public ScriptableTowerDataObject[] m_equippedTowers;


    public enum GameplayState
    {
        Setup,
        Combat,
        Paused,
        Victory,
        Defeat,
    }

    void Update()
    {
        if (m_drawCrosshair)
        {
            m_crossHair.SetActive(true);
            DrawCrosshair();
        }
        else
        {
            m_crossHair.SetActive(false);
        }

        if (m_selectedObj)
        {
            Vector3 pos = m_selectedObj.transform.position;
            pos.y = 0.02f;
            m_selectedRing.transform.position = pos;
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
            Debug.Log(raycastHit.collider.name + " command issued.");
            OnCommandRequested?.Invoke(raycastHit.collider.gameObject);
        }
        else
        {
            Debug.Log("No valid raycast hit.");
        }
    }

    private void DrawCrosshair()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, m_crossHairLayerMask))
        {
            Vector3 gridPos = raycastHit.point;
            gridPos = Util.RoundVectorToInt(gridPos);
            gridPos.y = .02f;
            m_crossHair.transform.position = gridPos;
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
        m_drawCrosshair = m_gameplayState == GameplayState.Combat;
    }


    void Start()
    {
        m_crossHair = Instantiate(m_crossHair, Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
        m_selectedRing = Instantiate(m_selectedRing, Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
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
        Debug.Log("Gameplay State:" + newState);
    }

    private void OnOnGameObjectSelected(GameObject obj)
    {
        m_selectedRing.SetActive(false);
        m_selectedObj = obj;
        //This quick toggle re-starts the Animator so we get a little 'bounce'. Optimize later!
        m_selectedRing.SetActive(true);
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
}