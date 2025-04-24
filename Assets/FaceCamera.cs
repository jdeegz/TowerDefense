using DG.Tweening;
using UnityEngine;

public class FaceCamera : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        Vector3 toCamera = (Camera.main.transform.position - transform.position).normalized;

        // Y axis points to the camera
        Vector3 yAxis = toCamera;

        // Use the table's right (or a world right) as a stable reference for the X axis
       // Vector3 referenceRight = transform.parent.right; // or Vector3.right if unparented
        Vector3 referenceRight = Vector3.right; // or Vector3.right if unparented
        Vector3 xAxis = Vector3.Cross(referenceRight, yAxis).normalized;

        // Recalculate forward (Z) from new basis
        Vector3 zAxis = Vector3.Cross(yAxis, xAxis).normalized;

        // Build rotation matrix from basis vectors
        transform.rotation = Quaternion.LookRotation(zAxis, yAxis);
    }
}
