using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    public float m_scrollSpeed = 5f;
    public float m_scrollZone = 50f;

    public float m_xPadding;
    public float m_zPaddingTop = 1;
    public float m_zPaddingBot = 1;

    public float m_zoomSpeed = 2f;
    public float m_maxZoomIn = 5f;
    public float m_startZoom = 0f;
    public float m_maxZoomOut = -10f;

    private Vector3 m_dragStartPosition;
    private Vector3 m_dragCurrentPosition;
    private bool m_isDragging;
    
    private float m_minXBounds;
    private float m_maxXBounds;
    private float m_minZBounds;
    private float m_maxZBounds;

    private Vector3 m_cameraNorthEast;
    private Vector3 m_cameraNorthWest;
    private Vector3 m_cameraSouthEast;
    private Vector3 m_cameraSouthWest;
    
    private Camera m_camera;
    private bool m_cameraIsLive = false;
    private float m_forceCameraClampPadding = 0.5f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Get screen-to-world corners
        Vector3 cameraNorthEast = GetScreenToWorldPoint(Screen.width, Screen.height);
        Vector3 cameraNorthWest = GetScreenToWorldPoint(0, Screen.height);
        Vector3 cameraSouthWest = GetScreenToWorldPoint(0, 0);

        // Adjust bounds
        m_minXBounds = transform.position.x - cameraNorthWest.x - m_xPadding;
        m_maxXBounds = transform.position.x + (GridManager.Instance.m_gridWidth - cameraNorthEast.x) + m_xPadding;
    
        m_minZBounds = transform.position.z - cameraSouthWest.z - m_zPaddingBot;
        m_maxZBounds = transform.position.z + (GridManager.Instance.m_gridHeight - cameraNorthEast.z) + m_zPaddingTop;

        m_camera = Camera.main;
        m_startZoom = m_camera.gameObject.transform.localPosition.z;

        // Center the camera in the grid
        if (GameplayManager.Instance)
        {
            Vector3 pos = GameplayManager.Instance.m_castleController.transform.position;
            pos.z += 1f;
            transform.position = GetPositionInBounds(pos);
            m_cameraIsLive = true;
        }
    }
    
    Vector3 GetScreenToWorldPoint(float screenX, float screenY)
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(screenX, screenY, 100));
        if (ray.direction.y != 0)
        {
            float t = -ray.origin.y / ray.direction.y;
            return ray.origin + t * ray.direction;
        }
        return Vector3.zero;
    }

    void Update()
    {
        if (!m_cameraIsLive) return;
        
        //On rails movement will focus the camera on a destination. (Example: Selecting a gatherer from the UI)
        if (OnRails)
        {
            if (m_onMoveRails) HandleOnRailsMovement();
            if (m_onZoomRails) HandleOnRailsZoom();
            return;
        }

        HandleScreenEdgePan();

        HandleKeyInput();

        if (GameplayManager.Instance != null && GameplayManager.Instance.m_interactionState != GameplayManager.InteractionState.PreconstructionTower)
        {
            HandleMouseInput();
        }

        // Detect mouse wheel scrolling
        float scrollDelta = Input.mouseScrollDelta.y;

        // Adjust camera position based on scrolling direction
        if (scrollDelta != 0f)
        {
            // Calculate the new zoom level
            float newZoom = Mathf.Clamp(m_camera.gameObject.transform.localPosition.z + scrollDelta * m_zoomSpeed, m_startZoom + m_maxZoomOut, m_startZoom + m_maxZoomIn);

            // Set the new camera position
            SetCameraZoomLevel(newZoom);
        }
    }

    void SetCameraControllerPosition(Vector3 pos)
    {
        if (pos.x <= m_minXBounds - m_forceCameraClampPadding)
        {
            Debug.Log($"Camera's Current Position exceeds min X Bounds.");
            pos.x = m_minXBounds;
        }

        if (pos.x >= m_maxXBounds + m_forceCameraClampPadding)
        {
            Debug.Log($"Camera's Current Position exceeds max X Bounds.");
            pos.x = m_maxXBounds;
        }

        if (pos.z <= m_minZBounds - m_forceCameraClampPadding)
        {
            Debug.Log($"Camera's Current Position exceeds min Z Bounds.");
            pos.z = m_minZBounds;
        }

        if (pos.z >= m_maxZBounds + m_forceCameraClampPadding)
        {
            Debug.Log($"Camera's Current Position exceeds max Z Bounds.");
            pos.z = m_maxZBounds;
        }
        
        transform.position = pos;
    }

    void SetCameraZoomLevel(float zoomLevel)
    {
        // Set the camera position along the z-axis
        m_camera.gameObject.transform.localPosition = new Vector3(m_camera.gameObject.transform.localPosition.x, m_camera.gameObject.transform.localPosition.y, zoomLevel);
    }

    void HandleKeyInput()
    {
        if (OnRails) return;

        // Define the vector for the new camera position.
        Vector3 newPosition = transform.position;

        // Adjust the position based on input, ensuring it stays within bounds.
        if ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) && newPosition.z < m_maxZBounds)
        {
            newPosition.z += m_scrollSpeed * Time.unscaledDeltaTime;
        }

        if ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) && newPosition.x > m_minXBounds)
        {
            newPosition.x -= m_scrollSpeed * Time.unscaledDeltaTime;
        }

        if ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && newPosition.z > m_minZBounds)
        {
            newPosition.z -= m_scrollSpeed * Time.unscaledDeltaTime;
        }

        if ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) && newPosition.x < m_maxXBounds)
        {
            newPosition.x += m_scrollSpeed * Time.unscaledDeltaTime;
        }

        // Apply the new position using the dedicated setter function.
        if(newPosition != transform.position) SetCameraControllerPosition(newPosition);
    }

    private bool m_startedOnUI = false;

    void HandleMouseInput()
    {
        // Track if the drag started on a UI element


        if (Input.GetMouseButtonDown(0))
        {
            // Check if the pointer is over a UI element
            if (EventSystem.current.IsPointerOverGameObject())
            {
                m_startedOnUI = true; // Set the flag if we started dragging over UI
                return; // Exit the method if over UI
            }
            else
            {
                m_startedOnUI = false;
            }

            Plane plane = new Plane(Vector3.up, Vector3.zero);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float entry;

            if (plane.Raycast(ray, out entry))
            {
                m_dragStartPosition = ray.GetPoint(entry);

                CancelOnRailsMove();
            }
        }

        if (Input.GetMouseButton(0))
        {
            // Prevent dragging if it started over UI
            if (m_startedOnUI)
            {
                return; // Exit the method if drag started over UI
            }

            Plane plane = new Plane(Vector3.up, Vector3.zero);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float entry;

            if (plane.Raycast(ray, out entry))
            {
                m_isDragging = true;
                m_dragCurrentPosition = ray.GetPoint(entry);
                Vector3 delta = m_dragStartPosition - m_dragCurrentPosition;
        
                // Compute the new potential position
                Vector3 newPosition = transform.position + delta;

                // Clamp the new position to stay within bounds
                newPosition.x = Mathf.Clamp(newPosition.x, m_minXBounds, m_maxXBounds);
                newPosition.z = Mathf.Clamp(newPosition.z, m_minZBounds, m_maxZBounds);

                SetCameraControllerPosition(newPosition);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            m_isDragging = false;
            m_startedOnUI = false;
        }
    }

    void HandleScreenEdgePan()
    {
        // Skip movement if interacting with UI
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // Ensure we're not in preconstruction mode
        if (GameplayManager.Instance?.m_interactionState == GameplayManager.InteractionState.PreconstructionTower)
        {
            m_isDragging = false;
        }

        // Stop movement if dragging or cursor is outside the game window
        if (m_isDragging || !IsCursorInGameWindow()) return;

        // Cache screen dimensions
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        Vector3 mousePos = Input.mousePosition;

        // Determine if the cursor is near the screen edges
        bool atLeftEdge = mousePos.x < m_scrollZone;
        bool atRightEdge = mousePos.x > screenWidth - m_scrollZone;
        bool atTopEdge = mousePos.y > screenHeight - m_scrollZone;
        bool atBotEdge = mousePos.y < m_scrollZone;

        // Calculate new position within bounds
        Vector3 newPosition = transform.position;
        if (atLeftEdge && newPosition.x > m_minXBounds) newPosition.x -= m_scrollSpeed * Time.unscaledDeltaTime;
        if (atRightEdge && newPosition.x < m_maxXBounds) newPosition.x += m_scrollSpeed * Time.unscaledDeltaTime;
        if (atTopEdge && newPosition.z < m_maxZBounds) newPosition.z += m_scrollSpeed * Time.unscaledDeltaTime;
        if (atBotEdge && newPosition.z > m_minZBounds) newPosition.z -= m_scrollSpeed * Time.unscaledDeltaTime;

        // Apply the new position
        SetCameraControllerPosition(newPosition);
    }

    private bool IsCursorInGameWindow()
    {
        Vector2 mousePosition = Input.mousePosition;
        Rect gameWindowRect = new Rect(0, 0, Screen.width, Screen.height);
        return gameWindowRect.Contains(mousePosition);
    }

    // MOVING FUNCTIONS
    float m_stoppingDistance = 0.1f;

    private bool OnRails
    {
        get { return m_onMoveRails || m_onZoomRails; }
    }

    private bool m_onMoveRails;
    private bool m_onZoomRails;
    private Vector3 m_railsMoveDestination;
    private Vector3 m_railsMoveStartPosition;
    private float m_railsMoveDuration;
    private float m_railsMoveElapsedTIme;
    public AnimationCurve m_railsMoveCurve;
    private float m_railsMoveSpeedMultiplier = 1;

    public void RequestOnRailsMove(Vector3 pos, float duration)
    {
        m_onMoveRails = true;
        m_railsMoveDestination = pos;
        m_railsMoveDuration = duration;
        m_railsMoveElapsedTIme = 0;
        m_railsMoveStartPosition = transform.position;
        m_railsMoveSpeedMultiplier = 1;
    }

    void HandleOnRailsMovement()
    {
        if (Vector3.Distance(transform.position, m_railsMoveDestination) <= m_stoppingDistance)
        {
            m_onMoveRails = false;
        }

        m_railsMoveElapsedTIme += Time.unscaledDeltaTime * m_railsMoveSpeedMultiplier;
        float t = Mathf.Clamp01(m_railsMoveElapsedTIme / m_railsMoveDuration);

        // Lerp towards the destination
        float curveT = m_railsMoveCurve.Evaluate(t);
        Vector3 newPos = Vector3.Lerp(m_railsMoveStartPosition, m_railsMoveDestination, curveT);

        Mathf.Clamp(newPos.x, m_minXBounds, m_maxXBounds);
        Mathf.Clamp(newPos.z, m_minZBounds, m_maxZBounds);

        transform.position = newPos;
    }

    void CancelOnRailsMove()
    {
        m_onMoveRails = false;
        m_railsMoveDestination = Vector3.zero;
        m_railsMoveDuration = 0;
    }


    // ZOOMING FUNCTIONS
    private float m_railsZoomDestination;
    private float m_railsZoomDuration;
    private float m_railsZoomStartLevel;
    private float m_railsZoomElapsedTIme;
    private float m_railsZoomSpeedMultiplier = 1;
    public AnimationCurve m_railsZoomCurve;

    public void RequestOnRailsZoom(float zoomLevel, float duration)
    {
        m_onZoomRails = true;
        m_railsZoomDestination = zoomLevel;
        m_railsZoomDuration = duration;
        m_railsZoomStartLevel = m_camera.gameObject.transform.localPosition.z;
        m_railsZoomElapsedTIme = 0;
        m_railsZoomSpeedMultiplier = 1;
    }

    void HandleOnRailsZoom()
    {
        // Time-based interpolation factor
        m_railsZoomElapsedTIme += Time.unscaledDeltaTime * m_railsZoomSpeedMultiplier;
        float t = Mathf.Clamp01(m_railsZoomElapsedTIme / m_railsZoomDuration);

        // Lerp towards the destination
        float curveT = m_railsZoomCurve.Evaluate(t);
        float newZoomLevel = Mathf.Lerp(m_railsZoomStartLevel, m_railsZoomDestination, curveT);
        SetCameraZoomLevel(newZoomLevel);

        // Are we there yet?
        float currentZoomLevel = m_camera.gameObject.transform.localPosition.z;
        if (Mathf.Abs(currentZoomLevel - m_railsZoomDestination) <= m_stoppingDistance)
        {
            m_onZoomRails = false;
        }
    }

    void CancelOnRailsZoom()
    {
        m_onZoomRails = false;
        m_railsZoomDestination = 0;
        m_railsZoomDuration = 0;
    }

    Vector3 GetPositionInBounds(Vector3 positionRequested)
    {
        float clampedX = Mathf.Clamp(positionRequested.x, m_minXBounds, m_maxXBounds);
        float clampedZ = Mathf.Clamp(positionRequested.z, m_minZBounds, m_maxZBounds);

        return new Vector3(clampedX, positionRequested.y, clampedZ);
    }
}