using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    public float m_scrollSpeed = 5f;
    public float m_scrollZone = 50f;

    public float m_xPadding;
    public float m_zPadding;

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

    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        float x = GridManager.Instance.m_gridWidth / 2;
        float z = GridManager.Instance.m_gridHeight / 2;

        //Center the camera
        transform.position = GameplayManager.Instance.m_castleController.transform.position;

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
        
    }

    void Update()
    {
        HandleScreenEdgePan();

        if (GameplayManager.Instance.m_interactionState != GameplayManager.InteractionState.PreconstructionTower)
        {
            HandleMouseInput();
        }
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
        if (GameplayManager.Instance.m_interactionState == GameplayManager.InteractionState.PreconstructionTower)
        {
            m_isDragging = false;
        }

        if (m_isDragging) return;

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

        transform.Translate(cameraMovement * Time.deltaTime, Space.World);
    }
}