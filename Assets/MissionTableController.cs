using System.Collections.Generic;
using GameUtil;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class MissionTableController : MonoBehaviour
{
    public static MissionTableController Instance;

    [Header("Table Rotation")]
    [SerializeField] private Transform m_rotationRoot;
    [SerializeField] private float m_rotationSpeed = 10f; // Adjust sensitivity
    
    [Header("Mission Buttons")]
    [SerializeField] private List<MissionButtonInteractable> m_missionButtonList;
    
    
    private float m_currentYRotation = 0f;
    private float m_targetYRotation = 0f;
    private float m_initialYRotation;
    private float m_draggedDistance;
    
    private bool m_isActive;
    private bool m_startedOnUI;
    private bool m_isDragging;
    
    private Vector3 m_dragStartPosition;
    private Vector3 m_dragStartDirection;
    private Vector3 m_dragCurrentPosition;
    
    private int m_prevSelectedMissionIndex = -1;
    private int m_nextSelectedMissionIndex = 1;
    private int m_curSelectedMissionIndex = 0;
    private int m_furthestDefeatedIndex;
    
    private MissionButtonInteractable m_curSelectedMission;
    private Interactable m_previousInteractable;
    
    private List<float> m_missionButtonRotations = new List<float>();

    void Awake()
    {
        Instance = this;
        m_isActive = false;
        CalculateMissionButtonRotationValues();
    }

    void Start()
    {
        SelectedMissionIndex = m_missionButtonList.IndexOf(GetFurthestUnlockedMission());
        SetRotation(m_missionButtonList[SelectedMissionIndex].transform);
    }

    void Update()
    {
        if (!m_isActive) return;

        HandleMouseScroll();

        HandleDrag();

        HandleMouseHover();

        HandleHotkeys();

        RotateToTargetRotation();
    }

    void RotateToTargetRotation()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scrollDelta) > 0.01f) // Prevent minor jittering
        {
            m_targetYRotation += scrollDelta * m_rotationSpeed;
        }

        // Ensure shortest rotation path by normalizing the difference
        float deltaAngle = Mathf.DeltaAngle(m_currentYRotation, m_targetYRotation);
        m_currentYRotation = Mathf.Lerp(m_currentYRotation, m_currentYRotation + deltaAngle, Time.deltaTime * 10f);
        m_rotationRoot.rotation = Quaternion.Euler(0f, m_currentYRotation, 0f);

        //Debug.Log($"Current Rotation: {m_currentYRotation}");
    }

    public void SetTargetRotation(Transform targetObject)
    {
        Vector3 direction = m_rotationRoot.position - targetObject.position;
        float targetYRotation = -Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        m_targetYRotation = m_rotationRoot.eulerAngles.y + targetYRotation;

        //Debug.Log($"SetTargetRotation: Starting Rotation: {m_rotationRoot.eulerAngles.y}, Rotation to Obj: {targetYRotation}. Target Rotation: {m_targetYRotation}.");
    }

    public void SetSelectedMission(MissionButtonInteractable missionButton)
    {
        for (var i = 0; i < m_missionButtonList.Count; ++i)
        {
            MissionButtonInteractable button = m_missionButtonList[i];
            if (missionButton == button)
            {
                m_curSelectedMission = button;
                SelectedMissionIndex = i;
                return;
            }
        }

        m_curSelectedMission = null;
    }

    public void SetRotation(Transform targetObject)
    {
        Vector3 direction = m_rotationRoot.position - targetObject.position;
        float targetYRotation = -Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        m_targetYRotation = targetYRotation;
        m_currentYRotation = m_targetYRotation;
        m_rotationRoot.rotation = Quaternion.Euler(0f, targetYRotation, 0f);
        m_isActive = true;
    }


    void HandleMouseScroll()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scrollDelta) > 0.01f) // Prevent minor jittering
        {
            //Debug.Log($"Scroll Delta {scrollDelta}.");
            m_targetYRotation += scrollDelta * m_rotationSpeed; // Update target rotation
            m_targetYRotation = Mathf.Repeat(m_targetYRotation, 360f);
            UIPopupManager.Instance.ClosePopup<UIMissionInfo>();

            CalculateAdjacentIndexes();
        }
    }


    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                m_startedOnUI = true;
                return;
            }
            else
            {
                m_startedOnUI = false;
            }

            Plane plane = new Plane(Vector3.up, m_rotationRoot.position);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float entry))
            {
                m_dragStartPosition = ray.GetPoint(entry);
                m_dragStartDirection = (m_rotationRoot.position - m_dragStartPosition).normalized;
                m_initialYRotation = m_targetYRotation;
                m_isDragging = true;
                m_draggedDistance = 0f;
                //Debug.Log($"Drag Start.");
            }
        }

        if (Input.GetMouseButton(0) && !m_startedOnUI)
        {
            Plane plane = new Plane(Vector3.up, m_rotationRoot.position);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float entry))
            {
                Vector3 dragCurrentPosition = ray.GetPoint(entry);
                Vector3 currentDirection = (m_rotationRoot.position - dragCurrentPosition).normalized;
                m_draggedDistance += Vector3.Distance(m_dragStartPosition, dragCurrentPosition);

                float angleOffset = Vector3.SignedAngle(m_dragStartDirection, currentDirection, Vector3.up);

                m_targetYRotation = m_initialYRotation + angleOffset;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            m_isDragging = false;
            m_startedOnUI = false;

            //Determine if we've dragged enough to qualify for a drag or click.
            if (m_draggedDistance < 0.5f)
            {
                TryTriggerClick();
                return;
            }

            //Debug.Log($"Drag Stop. Dragged distance: {m_draggedDistance}");

            //Try and calculate the new selected index.
            CalculateAdjacentIndexes();
        }
    }

    void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            //Go backwards.
            NextMissionIndex(false);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            //Go backwards.
            NextMissionIndex(true);
        }
    }

    void TryTriggerClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != null) interactable.OnClick();
        }
    }

    void HandleMouseHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // Build the ray.
        if (Physics.Raycast(ray, out RaycastHit hit)) // Shoot the ray and return the hit.
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();

            if (interactable != null) // We hit an interactable
            {
                if (m_previousInteractable != null && interactable != m_previousInteractable) // Its a new interactable
                {
                    m_previousInteractable.OnHoverExit();
                }

                interactable.OnHover();
                m_previousInteractable = interactable;
            }
        }
        else
        {
            if (m_previousInteractable != null) // We did not hit an interactable, so exit then null.
            {
                m_previousInteractable.OnHoverExit();
                m_previousInteractable = null;
            }
        }
    }


    private void CalculateMissionButtonRotationValues()
    {
        for (int i = 0; i < m_missionButtonList.Count; ++i)
        {
            Vector3 direction = m_missionButtonList[i].transform.position - m_rotationRoot.position;
            float buttonRotationValue = -Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            buttonRotationValue = Mathf.Repeat(buttonRotationValue - 180, 360f);

            //Debug.Log($"Mission {i}; Rotation Value {buttonRotationValue}.");
            m_missionButtonRotations.Add(buttonRotationValue);
        }
    }

    private void CalculateAdjacentIndexes()
    {
        // for each item in the mission list, get its relative angle and compare it to our current angle. Get the item that preceeds the item that has a larger angle.
        float currentRotationValue = Mathf.Repeat(m_rotationRoot.eulerAngles.y, 360f);
        int passedIndex = 0;
        for (int i = 0; i < m_missionButtonList.Count; ++i)
        {
            //Debug.Log($"Mission {i}; Current Rot Value {currentRotationValue} is greater than mission's value {m_missionButtonRotations[i]} : {currentRotationValue > m_missionButtonRotations[i]}");
            if (currentRotationValue > m_missionButtonRotations[i])
            {
                passedIndex = i;
            }

            if (currentRotationValue < m_missionButtonRotations[i])
            {
                m_nextSelectedMissionIndex = i;
                break;
            }
        }

        m_prevSelectedMissionIndex = passedIndex;
    }

    public int SelectedMissionIndex
    {
        get { return m_curSelectedMissionIndex; }
        set
        {
            if (value != m_curSelectedMissionIndex)
            {
                m_curSelectedMissionIndex = (value + m_missionButtonList.Count) % m_missionButtonList.Count;
                m_prevSelectedMissionIndex = m_curSelectedMissionIndex - 1;
                m_nextSelectedMissionIndex = m_curSelectedMissionIndex + 1;

                SetTargetRotation(m_missionButtonList[m_curSelectedMissionIndex].transform);
                //Debug.Log($"Selected Mission Index: {m_curSelectedMissionIndex}.");
            }
        }
    }

    private void NextMissionIndex(bool value)
    {
        SelectedMissionIndex = value ? m_nextSelectedMissionIndex : m_prevSelectedMissionIndex;

        if (m_curSelectedMission != null)
        {
            m_curSelectedMission = m_missionButtonList[SelectedMissionIndex];
            m_curSelectedMission.RequestMissionInfoPopup();
        }
    }

    private MissionButtonInteractable GetFurthestUnlockedMission()
    {
        for (int i = 0; i < m_missionButtonList.Count; ++i)
        {
            if (m_missionButtonList[i].MissionSaveData.m_missionCompletionRank == 1)
            {
                //Debug.Log($"Furthest Mission Found: {m_missionButtonList[i].MissionSaveData.m_sceneName} with competion Rank 1.");
                return m_missionButtonList[i];
            }

            if (m_missionButtonList[i].MissionSaveData.m_missionCompletionRank >= 2)
            {
                m_furthestDefeatedIndex = i;
            }
        }

        //Debug.Log($"Furthest Fallback Mission Found: {m_missionButtonList[m_furthestDefeatedIndex].MissionSaveData.m_sceneName} with competion Rank 2.");
        return m_missionButtonList[m_furthestDefeatedIndex];
    }

    public void RequestTableReset()
    {
        foreach (var button in m_missionButtonList)
        {
            button.UpdateDisplayState();
        }
    }
}