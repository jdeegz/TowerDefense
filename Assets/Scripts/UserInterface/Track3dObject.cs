using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Track3dObject : MonoBehaviour
{

    private GameObject m_targetObj;
    private RectTransform m_rectTransform;
    private Vector3 m_screenPos;
    private Camera m_camera;
    private float m_screenWidth;
    private float m_screenHeight;
    private bool m_trackObj = false;
    
    public void SetupTracking(GameObject target, RectTransform rect)
    { 
        m_camera = Camera.main;
        m_targetObj = target;
        m_rectTransform = rect;
        m_screenWidth = Screen.width;
        m_screenHeight = Screen.height;
        m_trackObj = true;
    }

    void LateUpdate()
    {
        if (!m_trackObj) return;
        
        m_screenPos = m_camera.WorldToScreenPoint(m_targetObj.transform.position);
        m_screenPos.x -= m_screenWidth / 2;
        m_screenPos.y -= m_screenHeight / 2 - 35f;
        m_rectTransform.anchoredPosition = new Vector2(m_screenPos.x, m_screenPos.y);
    }
    
}
