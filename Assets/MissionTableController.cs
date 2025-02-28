using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class MissionTableController : MonoBehaviour
{
    public static MissionTableController Instance;
    [Header("Table Rotation")]
    [SerializeField] private Transform m_rotationRoot;
    [SerializeField] private float m_rotationSpeed = 10f; // Adjust sensitivity
    private float m_currentYRotation = 0f;

    void Awake()
    {
        Instance = this;
    }
    
    void Update()
    {
        HandleMouseScroll();

        HandleDrag();

        HandleMouseHover();

        RotateToTargetRotation();
    }

    private float m_targetYRotation = 0f;
    private float m_rotationVelocity = 0f;

    void RotateToTargetRotation()
    {
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

    public void SetTargetRotation(Transform targetObject)
    {
        // Get the direction from the root to the clicked object
        Vector3 direction = m_rotationRoot.position - targetObject.position;
    
        float targetYRotation = -Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        // Calculate the angle difference to ensure smooth rotation
        float angleDifference = Mathf.DeltaAngle(m_rotationRoot.eulerAngles.y, targetYRotation);

        // Apply the target rotation considering the current rotation of the root object
        m_targetYRotation = m_rotationRoot.eulerAngles.y + targetYRotation;

        //Debug.Log($"SetTargetRotation: Starting Rotation: {m_rotationRoot.eulerAngles.y}, Rotation to Obj: {targetYRotation}. Target Rotation: {m_targetYRotation}.");
    }

    void HandleMouseScroll()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scrollDelta) > 0.01f) // Prevent minor jittering
        {
            Debug.Log($"Scroll Delta {scrollDelta}.");
            m_targetYRotation += scrollDelta * m_rotationSpeed; // Update target rotation
            m_targetYRotation = Mathf.Repeat(m_targetYRotation, 360f);
            UIPopupManager.Instance.ClosePopup<UIMissionInfo>();
            Debug.Log($"Scrolled.");
        }
    }

    private bool m_startedOnUI;
    private Vector3 m_dragStartPosition;
    private Vector3 m_dragCurrentPosition;
    private bool m_isDragging;
    private float m_draggedDistance;

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
                m_draggedDistance = 0f;
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
                
                //Calculate drag distance
                m_draggedDistance += Vector3.Distance(m_dragStartPosition, dragCurrentPosition);

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
            
            //Determine if we've dragged enough to qualify for a drag or click.
            if (m_draggedDistance < 0.5f)
            {
                TryTriggerClick();
            }
            
            //Debug.Log($"Drag Stop. Dragged distance: {m_draggedDistance}");
        }
    }

    void TryTriggerClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if(interactable != null) interactable.OnClick();
        }
    }

    private Interactable m_previousInteractable;
    void HandleMouseHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // Build the ray.
        if (Physics.Raycast(ray, out RaycastHit hit)) // Shoot the ray and return the hit.
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();

            if (interactable != null) // We hit an interactable
            {
                if (m_previousInteractable != null && interactable != m_previousInteractable) // Its a new interactable
                {
                    m_previousInteractable.OnHoverExit();
                }

                interactable.OnHover();
                m_previousInteractable = interactable;    
            }
        }
        else
        {
            if (m_previousInteractable != null) // We did not hit an interactable, so exit then null.
            {
                m_previousInteractable.OnHoverExit();
                m_previousInteractable = null;
            }
        }
    }
}