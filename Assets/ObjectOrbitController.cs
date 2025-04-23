using UnityEngine;

public class ObjectOrbitController : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Transform centerTransform;
    public Transform targetTransform;

    public bool clockwise = true;
    public float moveSpeed = 2f;
    public float turnSpeed = 90f;
    public float stopDistance = 0.2f;

    private float desiredRadius;
    private float initialRadius;
    private float initialHeight;
    private float desiredHeight;
    private float arcLength;
    private float arcTravelled = 0f;

    private bool hasArrived;

    private Vector3 prevPos;
    private Vector3 startDir;
    private Vector3 targetDir;
    private float angleBetween;
    private float remainingAngleBetween;
    private float progress;

    void Start()
    {
        Vector3 toStart = transform.position - centerTransform.position;
        Vector3 flatToStart = new Vector3(toStart.x, 0, toStart.z).normalized;

        Vector3 tangentDir = clockwise
            ? Vector3.Cross(Vector3.up, flatToStart)
            : Vector3.Cross(flatToStart, Vector3.up);
        transform.forward = tangentDir;

        Vector3 toTarget = targetTransform.position - centerTransform.position;
        Vector3 flatToTarget = new Vector3(toTarget.x, 0, toTarget.z);

        initialRadius = flatToStart.magnitude;
        desiredRadius = flatToTarget.magnitude;

        initialHeight = transform.position.y;
        desiredHeight = targetTransform.position.y;
        
        startDir = new Vector3(transform.position.x - centerTransform.position.x, 0,transform.position.z - centerTransform.position.z);
        targetDir = new Vector3(targetTransform.position.x - centerTransform.position.x, 0, targetTransform.position.z - centerTransform.position.z);

        angleBetween = Vector3.SignedAngle(startDir, targetDir, Vector3.up);
        angleBetween = (angleBetween + 360f) % 360f;
    }

    void Update()
    {
        if (hasArrived)
            return;

        Vector3 currentDir = new Vector3(transform.position.x - centerTransform.position.x, 0,transform.position.z - centerTransform.position.z);
        remainingAngleBetween = Vector3.SignedAngle(startDir, currentDir, Vector3.up);
        remainingAngleBetween = (remainingAngleBetween + 360f) % 360f;  // Keep it between 0 and 360

// Ensure the angle is always in the correct direction (respecting orbit direction)
        if (clockwise && remainingAngleBetween < 0)
        {
            remainingAngleBetween += 360f;  // Make sure the value stays positive for clockwise movement
        }
        else if (!clockwise && remainingAngleBetween > 0)
        {
            remainingAngleBetween -= 360f;  // Adjust for counter-clockwise movement
        }

// Calculate the progress
        float absRemainingAngle = Mathf.Abs(remainingAngleBetween);
        progress = Mathf.Clamp01(absRemainingAngle / angleBetween);
        
        prevPos = transform.position;

        // 2. Radius & Y interpolation
        float currentRadius = Mathf.Lerp(initialRadius, desiredRadius, progress);
        float currentY = Mathf.Lerp(initialHeight, desiredHeight, progress);

        // 3. Tangent direction
        Vector3 toCenter = transform.position - centerTransform.position;
        Vector3 flatToCenter = new Vector3(toCenter.x, 0, toCenter.z).normalized;
        Vector3 tangentDir = clockwise
            ? Vector3.Cross(Vector3.up, flatToCenter).normalized
            : Vector3.Cross(flatToCenter, Vector3.up).normalized;

        // 4. Homing influence
        Vector3 targetLookDir = (targetTransform.position - transform.position).normalized;
        float targetInfluence = Mathf.InverseLerp(0.75f, 1f, progress);
        Vector3 blendedDir = Vector3.Slerp(tangentDir, targetLookDir, targetInfluence).normalized;
        Quaternion targetRot = Quaternion.LookRotation(blendedDir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);

        // 5. Move forward and apply height
        Vector3 move = transform.forward * moveSpeed * Time.deltaTime;
        Vector3 nextPos = transform.position + move;
        nextPos.y = currentY;
        transform.position = nextPos;

        // 6. Stop when close
        if (Vector3.Distance(transform.position, targetTransform.position) < stopDistance)
        {
            hasArrived = true;
            moveSpeed = 0;
            turnSpeed = 0;
        }
    }
}
