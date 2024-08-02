using UnityEngine;

public class FaceNorth : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.LookRotation(Vector3.forward);
    }
}
