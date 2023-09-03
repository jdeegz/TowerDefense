using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    public float m_scrollSpeed = 5f;
    public float m_scrollZone = 50f;

    public float m_xBounds;
    public float m_zBounds;

    private Vector3 m_dragStartPosition;
    private Vector3 m_dragCurrentPosition;
    private bool m_isDragging;
    private float m_minXBounds;
    private float m_maxXBounds;
    private float m_minZBounds;
    private float m_maxZBounds;

    void Start()
    {
        float x = transform.position.x;
        float z = transform.position.z;

        m_minXBounds = x - m_xBounds;
        m_maxXBounds = x + m_xBounds;

        m_minZBounds = z - m_zBounds;
        m_maxZBounds = z + m_zBounds;
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