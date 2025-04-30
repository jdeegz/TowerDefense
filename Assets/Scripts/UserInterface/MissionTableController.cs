using System;
using System.Collections.Generic;
using DG.Tweening;
using GameUtil;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class MissionTableController : MonoBehaviour
{
    public static MissionTableController Instance;

    [Header("Prefabs")]
    [SerializeField] private GameObject m_hoveredMissionIndicatorFab;
    public GameObject HoveredMissionIndicatorFab => m_hoveredMissionIndicatorFab;

    [SerializeField] private GameObject m_selectedMissionIndicatorFab;
    public GameObject SelectedMissionIndicatorFab => m_selectedMissionIndicatorFab;


    [Header("Table Rotation")]
    [SerializeField] private Transform m_rotationRoot;
    [SerializeField] private float m_mouseWheelRotationSpeed = 10f; // Adjust sensitivity

    [Header("Mission Buttons")]
    [SerializeField] private List<MissionButtonInteractable> m_missionButtonList;
    public List<MissionButtonInteractable> MissionButtonList => m_missionButtonList;

    [Header("Tears")]
    public MissionTableTearController m_tearController;

    public static event Action<MissionButtonInteractable> OnMissionSelected;

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
    private GameObject m_currentSelectedIndicator;

    private List<float> m_missionButtonRotations = new List<float>();

    private bool m_menuMode = true;
    private Tween m_menuModeRotationTween;
    private Sequence m_curTransitionSequence;
    private Sequence m_transitionToMenuSequence;
    private Sequence m_transitionToMissionTableSequence;

    [SerializeField] private GameObject m_cameraController;
    [SerializeField] private Camera m_camera;
    [SerializeField] private Volume m_globalVolume;

    [SerializeField] private Transform m_menuControllerTransform;
    [SerializeField] private Transform m_menuCameraTransform;
    [SerializeField] private Transform m_tableControllerTransform;
    [SerializeField] private Transform m_tableCameraTransform;
    [SerializeField] private float m_transitionToTableDuration;
    [SerializeField] private float m_transitionToMenuDuration;

    private float m_menuDOFStart = 25f;
    private float m_menuDOFEnd = 30f;
    private float m_tableDOFStart = 60f;
    private float m_tableDOFEnd = 90f;

    private float m_menuCameraClippingNear = 5f;
    private float m_menuCameraClippingFar = 90f;
    private float m_tableCameraClippingNear = 40f;
    private float m_tableCameraClippingFar = 140f;

    private DepthOfField m_dofSettings;

    [SerializeField] private float m_maxIdleTableRotationSpeed = 3;
    [SerializeField] private float m_idleTableAccelerationDuration = 3;
    private float m_curIdleTableRotationSpeed = 0;
    private float m_lastTableInteractionTime;
    private Vector3 m_tableRotation;

    void Awake()
    {
        Instance = this;
        m_isActive = false;
        m_globalVolume.profile.TryGet(out m_dofSettings);

        CalculateMissionButtonRotationValues();

        SetCameraStartingPosition();

        m_tableRotation = m_rotationRoot.transform.eulerAngles;
    }

    private void SetCameraStartingPosition()
    {
        //Set to Menu starting, add logic for maybe skipping later.
        m_menuMode = true;
        m_cameraController.transform.position = m_menuControllerTransform.transform.position;
        m_cameraController.transform.rotation = m_menuControllerTransform.transform.rotation;
        m_camera.transform.position = m_menuCameraTransform.transform.position;
        m_camera.transform.rotation = m_menuCameraTransform.transform.rotation;
        m_dofSettings.gaussianStart.value = m_menuDOFStart;
        m_dofSettings.gaussianEnd.value = m_menuDOFEnd;
        m_camera.nearClipPlane = m_menuCameraClippingNear;
        m_camera.farClipPlane = m_menuCameraClippingFar;
    }


    void Update()
    {
        if (ShouldRotateTable())
        {
            HandleTableIdleRotation();
        }
        else
        {
            RotateToTargetRotation();
        }

        if (m_menuMode) return;


        if (EventSystem.current.IsPointerOverGameObject()) return;

        HandleMouseHover();

        HandleMouseScroll();

        HandleDrag();

        HandleHotkeys();
    }


    private bool ShouldRotateTable()
    {
        if (m_menuMode) return true; // Always spin in menu

        if (UIPopupManager.Instance.IsPopupOpen<UIMissionInfo>()) return false;
        
        if (Time.unscaledTime - m_lastTableInteractionTime > 8f)
        {
            m_tableRotation = m_rotationRoot.transform.eulerAngles;
            return true;
        }
        return false;
    }

    public void OnTableInteracted()
    {
        m_curIdleTableRotationSpeed = 0;
        m_lastTableInteractionTime = Time.unscaledTime;
    }

    private void HandleTableIdleRotation()
    {
        float accelerationRate = m_maxIdleTableRotationSpeed / m_idleTableAccelerationDuration;
        m_curIdleTableRotationSpeed = Mathf.MoveTowards(
            m_curIdleTableRotationSpeed,
            m_maxIdleTableRotationSpeed,
            accelerationRate * Time.unscaledDeltaTime
        );

        m_tableRotation.y = Mathf.Repeat(m_tableRotation.y + m_curIdleTableRotationSpeed * Time.unscaledDeltaTime, 360f);
        m_rotationRoot.eulerAngles = m_tableRotation;
    }

    public void TriggerSequence()
    {
        if (m_curTransitionSequence != null && m_curTransitionSequence.IsActive())
            m_curTransitionSequence.Kill();

        if (m_menuMode)
        {
            OnTableInteracted();
            m_curRotateToTargetSpeed = m_rotateToTargetFromMenuSpeed;
            SelectedMissionIndex = m_missionButtonList.IndexOf(GetFurthestUnlockedMission());
            m_curTransitionSequence = BuildTransitionSequence(
                m_tableCameraTransform,
                m_tableControllerTransform,
                m_tableDOFStart,
                m_tableDOFEnd,
                m_tableCameraClippingNear,
                m_tableCameraClippingFar,
                m_transitionToTableDuration);
            
            m_curTransitionSequence.OnComplete( () => m_curRotateToTargetSpeed = m_rotateToTargetSpeed);
        }
        else
        {
            m_curTransitionSequence = BuildTransitionSequence(
                m_menuCameraTransform,
                m_menuControllerTransform,
                m_menuDOFStart,
                m_menuDOFEnd,
                m_menuCameraClippingNear,
                m_menuCameraClippingFar,
                m_transitionToMenuDuration);
        }

        m_menuMode = !m_menuMode;
        m_curTransitionSequence.Play();
    }

    private Sequence BuildTransitionSequence(
        Transform cameraTarget,
        Transform controllerTarget,
        float dofStart,
        float dofEnd,
        float nearClip,
        float farClip,
        float duration)
    {
        var seq = DOTween.Sequence();

        // Camera
        seq.Append(m_camera.transform.DOMove(cameraTarget.position, duration));
        seq.Join(m_camera.transform.DORotateQuaternion(cameraTarget.rotation, duration));

        // Controller
        seq.Join(m_cameraController.transform.DOMove(controllerTarget.position, duration));
        seq.Join(m_cameraController.transform.DORotateQuaternion(controllerTarget.rotation, duration));

        // DOF
        seq.Join(DOTween.To(() => m_dofSettings.gaussianStart.value, x => m_dofSettings.gaussianStart.value = x, dofStart, duration));
        seq.Join(DOTween.To(() => m_dofSettings.gaussianEnd.value, x => m_dofSettings.gaussianEnd.value = x, dofEnd, duration));

        // Clipping Planes
        seq.Join(DOTween.To(() => m_camera.nearClipPlane, x => m_camera.nearClipPlane = x, nearClip, duration));
        seq.Join(DOTween.To(() => m_camera.farClipPlane, x => m_camera.farClipPlane = x, farClip, duration));

        return seq;
    }

    private float m_rotateToTargetSpeed = 15f;
    private float m_rotateToTargetFromMenuSpeed = 3f;
    private float m_curRotateToTargetSpeed;
    
    void RotateToTargetRotation()
    {
        // Ensure shortest rotation path by normalizing the difference
        float deltaAngle = Mathf.DeltaAngle(m_currentYRotation, m_targetYRotation);
        m_currentYRotation = Mathf.Lerp(m_currentYRotation, m_currentYRotation + deltaAngle, Time.deltaTime * m_curRotateToTargetSpeed);
        m_rotationRoot.rotation = Quaternion.Euler(0f, m_currentYRotation, 0f);

        Debug.Log($"Current Rotation: {m_currentYRotation}");
    }

    public void SetTargetRotation(Transform targetObject)
    {
        m_currentYRotation = m_rotationRoot.eulerAngles.y;
        Vector3 direction = m_rotationRoot.position - targetObject.position;
        float targetYRotation = -Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        m_targetYRotation = m_rotationRoot.eulerAngles.y + targetYRotation;

        Debug.Log($"SetTargetRotation: Starting Rotation: {m_rotationRoot.eulerAngles.y}, Rotation to Obj: {targetYRotation}. Target Rotation: {m_targetYRotation}.");
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
                
                //Restart the Idle Timer.
                OnTableInteracted();
                return;
            }
        }

        OnMissionSelected?.Invoke(null);
        m_curSelectedMission = null;
    }

    void HandleMouseScroll()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scrollDelta) > 0.01f) // Prevent minor jittering
        {
            //Debug.Log($"Scroll Delta {scrollDelta}.");
            m_targetYRotation += scrollDelta * m_mouseWheelRotationSpeed; // Update target rotation
            m_targetYRotation = Mathf.Repeat(m_targetYRotation, 360f);
            UIPopupManager.Instance.ClosePopup<UIMissionInfo>();

            CalculateAdjacentIndexes();
            
            //Restart the Idle Timer.
            OnTableInteracted();
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

            Vector3 pos = m_rotationRoot.position;
            pos.y += 4f;
            Plane plane = new Plane(Vector3.up, pos);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float entry))
            {
                m_dragStartPosition = ray.GetPoint(entry);
                m_dragStartDirection = (pos - m_dragStartPosition).normalized;
                m_initialYRotation = m_targetYRotation;
                m_isDragging = true;
                m_draggedDistance = 0f;
                //Debug.Log($"Drag Start.");
            }
        }

        if (Input.GetMouseButton(0) && !m_startedOnUI)
        {
            Vector3 pos = m_rotationRoot.position;
            pos.y += 4f;
            Plane plane = new Plane(Vector3.up, pos);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float entry))
            {
                Vector3 dragCurrentPosition = ray.GetPoint(entry);
                Vector3 currentDirection = (pos - dragCurrentPosition).normalized;
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
            
            //Restart the Idle Timer.
            OnTableInteracted();
        }
    }

    void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            //Go backwards.
            NextMissionIndex(false);
            
            //Restart the Idle Timer.
            OnTableInteracted();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            //Go backwards.
            NextMissionIndex(true);
            
            //Restart the Idle Timer.
            OnTableInteracted();
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
            m_curSelectedMissionIndex = (value + m_missionButtonList.Count) % m_missionButtonList.Count;
            m_prevSelectedMissionIndex = m_curSelectedMissionIndex - 1;
            m_nextSelectedMissionIndex = m_curSelectedMissionIndex + 1;

            SetTargetRotation(m_missionButtonList[m_curSelectedMissionIndex].transform);
            OnMissionSelected?.Invoke(m_missionButtonList[m_curSelectedMissionIndex]);
            //Debug.Log($"Selected Mission Index: {m_curSelectedMissionIndex}.");
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
        for (int i = 0; i < m_missionButtonList.Count; ++i) // Kind of stupid to use the buttons display state to determine a missions status...
        {
            // Is the mission unlocked?
            if (m_missionButtonList[i].ButtonDisplayState == MissionButtonInteractable.DisplayState.Unlocked)
            {
                return m_missionButtonList[i];
            }

            // Has the mission been defeated?
            if (m_missionButtonList[i].ButtonDisplayState == MissionButtonInteractable.DisplayState.Defeated)
            {
                m_furthestDefeatedIndex = i;
            }
        }

        Debug.Log($"Furthest Fallback Mission Found: {m_missionButtonList[m_furthestDefeatedIndex].MissionSaveData.m_sceneName} with competion Rank 2.");
        return m_missionButtonList[m_furthestDefeatedIndex];
    }

    public void RequestTableReset()
    {
        foreach (var button in m_missionButtonList)
        {
            button.UpdateDisplayState();
        }

        m_tearController.SetValidMissions();
    }
}