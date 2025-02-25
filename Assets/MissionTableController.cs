using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class MissionTableController : MonoBehaviour
{
    [Header("Spires")]
    [SerializeField] private GameObject m_spireObj;
    [SerializeField] private GameObject m_spiresRoot;
    [SerializeField] private float m_radius;
    [SerializeField] private int m_quantity;

    void Start()
    {
        PlaceSpires();
    }

    void PlaceSpires()
    {
        for (int i = 0; i < m_quantity; i++)
        {
            float angle = i * (360f / m_quantity); // Divide the circle evenly
            float radians = angle * Mathf.Deg2Rad;

            // Calculate position using sin and cos
            Vector3 position = new Vector3(
                m_spiresRoot.transform.position.x + m_radius * Mathf.Cos(radians),
                m_spiresRoot.transform.position.y,
                m_spiresRoot.transform.position.z + m_radius * Mathf.Sin(radians)
            );

            // Instantiate the spire at the calculated position
            Instantiate(m_spireObj, position, Quaternion.identity, m_spiresRoot.transform);
        }
    }

    [Header("Table Rotation")]
    [SerializeField] private Transform m_rotationRoot;
    [SerializeField] private float m_rotationSpeed = 10f; // Adjust sensitivity
    private float m_currentYRotation = 0f;

    void Update()
    {
        HandleMouseScroll();

        HandleDrag();


        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scrollDelta) > 0.01f) // Prevent minor jittering
        {
            m_targetYRotation += scrollDelta * m_rotationSpeed;
        }

        // Ensure shortest rotation path by normalizing the difference
        float deltaAngle = Mathf.DeltaAngle(m_currentYRotation, m_targetYRotation);

        // Smooth interpolation using Lerp
        m_currentYRotation = Mathf.Lerp(m_currentYRotation, m_currentYRotation + deltaAngle, Time.deltaTime * 10f);

        // Apply the rotation
        m_rotationRoot.rotation = Quaternion.Euler(0f, m_currentYRotation, 0f);
    }

    private float m_targetYRotation = 0f;
    private float m_rotationVelocity = 0f;

    void HandleMouseScroll()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scrollDelta) > 0.01f) // Prevent minor jittering
        {
            Debug.Log($"Scroll Delta {scrollDelta}.");
            m_targetYRotation += scrollDelta * m_rotationSpeed; // Update target rotation
            m_targetYRotation = Mathf.Repeat(m_targetYRotation, 360f);
            Debug.Log($"Scrolled.");
        }
    }

    private bool m_startedOnUI;
    private Vector3 m_dragStartPosition;
    private Vector3 m_dragCurrentPosition;
    private bool m_isDragging;

    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                m_startedOnUI = true;
                return;
            }
            else
            {
                m_startedOnUI = false;
            }

            Plane plane = new Plane(Vector3.up, m_rotationRoot.position);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float entry))
            {
                m_dragStartPosition = ray.GetPoint(entry);
                m_isDragging = true;
                Debug.Log($"Drag Start.");
            }
        }

        if (Input.GetMouseButton(0) && !m_startedOnUI)
        {
            Plane plane = new Plane(Vector3.up, m_rotationRoot.position);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float entry))
            {
                Vector3 dragCurrentPosition = ray.GetPoint(entry);

                // Convert positions to local space relative to the table center
                Vector3 startLocal = m_dragStartPosition - m_rotationRoot.position;
                Vector3 currentLocal = dragCurrentPosition - m_rotationRoot.position;

                // Normalize vectors to avoid scale issues
                startLocal.y = 0; // Ensure we only rotate around the Y-axis
                currentLocal.y = 0;

                // Calculate the signed angle change
                float angle = Vector3.SignedAngle(startLocal, currentLocal, Vector3.up);

                // Apply the rotation
                m_targetYRotation += angle;

                // Clamp the rotation within 0-360
                m_targetYRotation = Mathf.Repeat(m_targetYRotation, 360f);

                // Update the start position to avoid compounding errors
                m_dragStartPosition = dragCurrentPosition;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            m_isDragging = false;
            m_startedOnUI = false;
            Debug.Log($"Drag Stop.");
        }
    }
}