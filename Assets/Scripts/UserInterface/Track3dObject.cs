using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Track3dObject : MonoBehaviour
{

    private GameObject m_targetObj;
    private RectTransform m_rectTransform;
    private Canvas m_canvas;
    private Vector3 m_screenPos;
    private Vector2 m_viewportPos;
    private Camera m_camera;
    private float m_screenWidth;
    private float m_screenHeight;
    private float m_yOffset;
    private bool m_trackObj;

    void Start()
    {
        m_camera = Camera.main;
        m_canvas = GetComponentInParent<Canvas>();
        m_screenWidth = Screen.width / m_canvas.scaleFactor;
        m_screenHeight = Screen.height / m_canvas.scaleFactor;
    }
    
    public void SetupTracking(GameObject target, RectTransform rect, float yOffset)
    {
        m_targetObj = target;
        m_rectTransform = rect;
        m_trackObj = true;
        m_yOffset = yOffset;
    }

    public void StopTracking()
    {
        m_trackObj = false;
    }

    void LateUpdate()
    {
        if (!m_trackObj) return;
        
        m_viewportPos = m_camera.WorldToViewportPoint(m_targetObj.transform.position);
        m_viewportPos.x = m_viewportPos.x * m_screenWidth - m_screenWidth * 0.5f;
        m_viewportPos.y = (m_viewportPos.y * m_screenHeight - m_screenHeight * 0.5f) + m_yOffset;
        m_rectTransform.anchoredPosition = m_viewportPos;
    }
   
}
