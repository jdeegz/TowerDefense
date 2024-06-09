using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Trail Data", menuName = "ScriptableObjects/TrailData")]
public class BulletTrailData : ScriptableObject
{
    public AnimationCurve m_widthCurve;
    public float m_time = 0.5f;
    public float m_minVertexDistance = 0.1f;
    public Gradient m_colorGradient;
    public Material m_material;
    public int m_cornerVertices;
    public int m_endVertices;

    public void SetupTrail(TrailRenderer trailRenderer)
    {
        trailRenderer.widthCurve = m_widthCurve;
        trailRenderer.time = m_time;
        trailRenderer.minVertexDistance = m_minVertexDistance;
        trailRenderer.colorGradient = m_colorGradient;
        trailRenderer.sharedMaterial = m_material;
        trailRenderer.numCornerVertices = m_cornerVertices;
        trailRenderer.numCapVertices = m_endVertices;
        //Debug.Log($"Trail Setup completed.");
    }
}
