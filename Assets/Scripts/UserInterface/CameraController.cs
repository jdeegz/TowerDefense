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
    public float m_zPadding;

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

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        float x = GridManager.Instance.m_gridWidth / 2;
        float z = GridManager.Instance.m_gridHeight / 2;

        //Get Screen to Plane corners.
        //North East
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width, Screen.height, 100));
        if (ray.direction.y != 0)
        {
            float t = -ray.origin.y / ray.direction.y;
            m_cameraNorthEast = ray.origin + t * ray.direction;
            //Instantiate(m_testDummy, m_cameraNorthEast, quaternion.identity);
        }

        //North West
        ray = Camera.main.ScreenPointToRay(new Vector3(0, Screen.height, 100));
        if (ray.direction.y != 0)
        {
            float t = -ray.origin.y / ray.direction.y;
            m_cameraNorthWest = ray.origin + t * ray.direction;
            //Instantiate(m_testDummy, m_cameraNorthWest, quaternion.identity);
        }

        //South East -- Not needed for any calculations.
        /*ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width, 0, 100));
        if (ray.direction.y != 0)
        {
            float t = -ray.origin.y / ray.direction.y;
            m_cameraSouthEast = ray.origin + t * ray.direction;
            //Instantiate(m_testDummy, m_cameraSouthEast, quaternion.identity);
        }*/

        //South West
        ray = Camera.main.ScreenPointToRay(new Vector3(0, 0, 100));
        if (ray.direction.y != 0)
        {
            float t = -ray.origin.y / ray.direction.y;
            m_cameraSouthWest = ray.origin + t * ray.direction;
            //Instantiate(m_testDummy, m_cameraSouthWest, quaternion.identity);
        }

        m_minXBounds = transform.position.x - m_cameraNorthWest.x - m_xPadding;
        m_maxXBounds = transform.position.x + (GridManager.Instance.m_gridWidth - m_cameraNorthEast.x) + m_xPadding;

        m_minZBounds = transform.position.z - m_cameraSouthWest.z - m_zPadding;
        m_maxZBounds = transform.position.z + (GridManager.Instance.m_gridHeight - m_cameraNorthEast.z) + m_zPadding;

        m_camera = Camera.main;
        m_startZoom = m_camera.gameObject.transform.localPosition.z;

        //Center the camera
        if (GameplayManager.Instance)
        {
            Vector3 pos = (GameplayManager.Instance.m_castleController.transform.position);
            pos.z += 1f;
            transform.position = GetPositionInBounds(pos);
        }
    }

    void Update()
    {
        //Do nothing if we are paused.
        //if (GameplayManager.Instance.m_gameSpeed == GameplayManager.GameSpeed.CutScene) return;

        //On rails movement will focus the camera on a destination. (Example: Selecting a gatherer from the UI)
        if (m_onRails)
        {
            HandleOnRailsMovement();
        }
        else
        {
            HandleScreenEdgePan();

            HandleKeyInput();
        }

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
            SetCameraPosition(newZoom);
        }
    }

    void SetCameraPosition(float zoomLevel)
    {
        // Set the camera position along the z-axis
        m_camera.gameObject.transform.localPosition = new Vector3(m_camera.gameObject.transform.localPosition.x, m_camera.gameObject.transform.localPosition.y, zoomLevel);
    }

    void HandleKeyInput()
    {
        //If we're on rails at the moment, ignore inputs.
        if (m_onRails) return;

        //Define the vector we wish to move. Make sure we're not moving past the Camera Bounds.
        Vector3 cameraMovement = Vector3.zero;

        //GetKey is used for doing a function while holding.
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) && transform.position.z < m_maxZBounds)
        {
            cameraMovement.z += m_scrollSpeed;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) && transform.position.x > m_minXBounds)
        {
            cameraMovement.x -= m_scrollSpeed;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) && transform.position.z > m_minZBounds)
        {
            cameraMovement.z -= m_scrollSpeed;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) && transform.position.x < m_maxXBounds)
        {
            cameraMovement.x += m_scrollSpeed;
        }

        transform.Translate(cameraMovement * Time.unscaledDeltaTime, Space.World);
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
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
            Plane plane = new Plane(Vector3.up, Vector3.zero);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float entry;

            if (plane.Raycast(ray, out entry))
            {
                m_isDragging = true;
                m_dragCurrentPosition = ray.GetPoint(entry);
                Vector3 delta = m_dragStartPosition - m_dragCurrentPosition;
                if (transform.position.x <= m_minXBounds && delta.x < 0)
                {
                    delta.x = 0;
                }

                if (transform.position.x >= m_maxXBounds && delta.x > 0)
                {
                    delta.x = 0;
                }

                if (transform.position.z <= m_minZBounds && delta.z < 0)
                {
                    delta.z = 0;
                }

                if (transform.position.z >= m_maxZBounds && delta.z > 0)
                {
                    delta.z = 0;
                }

                transform.position += delta;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            m_isDragging = false;
        }
    }

    void HandleScreenEdgePan()
    {
        Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            //If we hit a UI Game Object.
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
        }

        if (GameplayManager.Instance != null && GameplayManager.Instance.m_interactionState == GameplayManager.InteractionState.PreconstructionTower)
        {
            m_isDragging = false;
        }


        if (m_isDragging || !IsCursorInGameWindow()) return;

        Vector3 mousePos = Input.mousePosition;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        bool atLeftEdge = mousePos.x < m_scrollZone;
        bool atRightEdge = mousePos.x > screenWidth - m_scrollZone;

        bool atTopEdge = mousePos.y > screenHeight - m_scrollZone;
        bool atBotEdge = mousePos.y < m_scrollZone;

        Vector3 cameraMovement = Vector3.zero;
        if (atLeftEdge && transform.position.x > m_minXBounds) cameraMovement.x -= m_scrollSpeed;
        if (atRightEdge && transform.position.x < m_maxXBounds) cameraMovement.x += m_scrollSpeed;
        if (atTopEdge && transform.position.z < m_maxZBounds) cameraMovement.z += m_scrollSpeed;
        if (atBotEdge && transform.position.z > m_minZBounds) cameraMovement.z -= m_scrollSpeed;

        if (cameraMovement != Vector3.zero) CancelOnRailsMove();

        transform.Translate(cameraMovement * Time.unscaledDeltaTime, Space.World);
    }

    private bool IsCursorInGameWindow()
    {
        Vector2 mousePosition = Input.mousePosition;
        Rect gameWindowRect = new Rect(0, 0, Screen.width, Screen.height);
        return gameWindowRect.Contains(mousePosition);
    }

    private bool m_onRails;
    private Vector3 m_railsDestination;

    public void RequestOnRailsMove(Vector3 pos)
    {
        //Debug.Log($"CameraController: Request On Rails Move to: {pos}");
        m_onRails = true;
        m_railsDestination = pos;
    }

    void CancelOnRailsMove()
    {
        m_onRails = false;
        m_railsDestination = Vector3.zero;
        //Debug.Log($"CameraController: On Rails movement cancelled.");
    }

    void HandleOnRailsMovement()
    {
        //Debug.Log($"CameraController: Handling On Rails Movement to: {m_railsDestination}");
        float stoppingDistance = 0.1f;

        //Check to see if we're close enough.
        if (Vector3.Distance(transform.position, m_railsDestination) <= stoppingDistance)
        {
            //Debug.Log($"CameraController: Reached stopping distance for On Rails Movement.");
            m_onRails = false;
            return;
        }

        bool xAxisComplete = false;
        bool zAxisComplete = false;
        Vector3 newPos = transform.position;

        newPos.x = Mathf.Lerp(transform.position.x, m_railsDestination.x, m_scrollSpeed * Time.unscaledDeltaTime);
        newPos.z = Mathf.Lerp(transform.position.z, m_railsDestination.z, m_scrollSpeed * Time.unscaledDeltaTime);

        Mathf.Clamp(newPos.x, m_minXBounds, m_maxXBounds);
        Mathf.Clamp(newPos.z, m_minZBounds, m_maxZBounds);

        if (transform.position.x == newPos.x)
        {
            xAxisComplete = true;
        }

        if (transform.position.z == newPos.x)
        {
            zAxisComplete = true;
        }

        //Do the MOVE unless we're done on each axis.
        if (xAxisComplete && zAxisComplete)
        {
            m_onRails = false;
        }
        else
        {
            transform.position = newPos;
        }
    }

    Vector3 GetPositionInBounds(Vector3 positionRequested)
    {
        float clampedX = Mathf.Clamp(positionRequested.x, m_minXBounds, m_maxXBounds);
        float clampedZ = Mathf.Clamp(positionRequested.z, m_minZBounds, m_maxZBounds);

        return new Vector3(clampedX, positionRequested.y, clampedZ);
    }
}