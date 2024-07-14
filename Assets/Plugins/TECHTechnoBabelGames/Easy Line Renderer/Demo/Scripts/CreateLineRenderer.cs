using System.Collections;
using System.Collections.Generic;
using TechnoBabelGames;
using TMPro;
using UnityEngine;
using UnityEngine.Diagnostics;

public class CreateLineRenderer : MonoBehaviour
{
    public List<Vector2Int> m_linePoints;

    public GameObject m_lineObj;

    public TBLineRendererComponent m_lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        m_lineObj = new GameObject("Line");
        m_lineRenderer = m_lineObj.AddComponent<TBLineRendererComponent>();
        
        //Define desired properties of the line.
        m_lineRenderer.lineRendererProperties = new TBLineRenderer();
        m_lineRenderer.lineRendererProperties.linePoints = m_linePoints.Count;
        m_lineRenderer.lineRendererProperties.lineWidth = 0.5f;
        m_lineRenderer.lineRendererProperties.startColor = Color.red;
        m_lineRenderer.lineRendererProperties.endColor = Color.yellow;
        m_lineRenderer.lineRendererProperties.axis = TBLineRenderer.Axis.Y;
        
        //Assign the properties.
        m_lineRenderer.SetLineRendererProperties();
        
        //Create the points.
        for (int i = 0; i < m_linePoints.Count; ++i)
        {
            GameObject point = new GameObject("Point: " + i);
            point.transform.SetParent(m_lineObj.transform);
            point.transform.position = new Vector3(m_linePoints[i].x, 0.2f, m_linePoints[i].y);
        }

        //Assign the child objects to the line renderer as points.
        //m_lineRenderer.SetPoints();
    }

    // Update is called once per frame
    void Update()
    {
    }
}