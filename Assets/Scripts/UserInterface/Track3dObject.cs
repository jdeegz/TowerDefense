using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Track3dObject : MonoBehaviour
{

    public GameObject m_targetObj;

    private RectTransform m_rectTransform;
    private Vector3 m_screenPos;
    private Camera m_camera;
    private float m_screenWidth;
    private float m_screenHeight;
    
    // Start is called before the first frame update
    void Start()
    {
        
        m_camera = Camera.main;
        m_rectTransform = GetComponent<RectTransform>();
        m_screenWidth = Screen.width;
        m_screenHeight = Screen.height;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        
        m_screenPos = m_camera.WorldToScreenPoint(m_targetObj.transform.position);
        m_screenPos.x -= m_screenWidth / 2;
        m_screenPos.y -= m_screenHeight / 2;
        m_rectTransform.anchoredPosition = new Vector2(m_screenPos.x, m_screenPos.y);
    }
}
