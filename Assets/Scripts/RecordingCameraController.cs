#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Splines;

[ExecuteInEditMode]
public class RecordingCameraController : MonoBehaviour
{
    public AnimationCurve m_positionCurve;
    public SplineContainer m_splineContainer;
    public GameObject m_cameraRoot;
    public Camera m_camera;
    public float m_duration = 1f;
    public GameObject m_trackingTarget;

    public bool m_isMoving;
    private Spline m_motionSpline;
    private float m_elapsedTime;

    void Update()
    {
        if (Application.isPlaying)
        {
            if (m_trackingTarget != null)
            {
                UpdateRotation();
            }
            
            if (m_isMoving)
            {
                UpdateMovement(Time.deltaTime);
            }
        }
    }

    public void StartMoving()
    {
        
        m_motionSpline = m_splineContainer?.Spline;
        
        m_isMoving = true;
        m_elapsedTime = 0f;

        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            EditorApplication.update += EditorUpdate; // Subscribe to editor update
#endif
        }
    }

    public void StopMoving()
    {
        m_isMoving = false;
        m_elapsedTime = 0f;

        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate; // Subscribe to editor update
#endif
        }
    }

#if UNITY_EDITOR
    private void EditorUpdate()
    {
        if (!m_isMoving)
        {
            EditorApplication.update -= EditorUpdate; // Unsubscribe when done
            return;
        }

        float deltaTime = 0.02f; // Simulate a frame duration
        UpdateMovement(deltaTime);

        if (m_trackingTarget != null)
        {
            UpdateRotation();
        }

        if (m_elapsedTime >= m_duration)
        {
            m_isMoving = false;
            EditorApplication.update -= EditorUpdate; // Unsubscribe when movement ends
        }

        SceneView.RepaintAll(); // Refresh Scene View
    }
#endif

    private void UpdateMovement(float deltaTime)
    {
        m_elapsedTime += deltaTime;

        if (m_motionSpline != null && m_cameraRoot != null && m_camera != null && m_positionCurve != null)
        {
            float index = m_positionCurve.Evaluate(m_elapsedTime / m_duration);
            Vector3 position = m_motionSpline.EvaluatePosition(index);
            m_cameraRoot.transform.position = position + m_splineContainer.transform.position;
        }
    }

    private void UpdateRotation()
    {
        Vector3 lookDirection = m_trackingTarget.transform.position - m_camera.transform.position;
        if (lookDirection != Vector3.zero)
        {
            m_camera.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }
}