using UnityEngine;

public class TestMovement : MonoBehaviour
{
    public float m_baselookSpeed = 1;
    public float m_baseMoveSpeed = 1;

    public float m_lastSpeedModifierFaster = 1;
    public float m_lastSpeedModifierSlower = 1;
    
    
    // Update is called once per frame
    void Update()
    {
        //ROTATION
        float cumulativeLookSpeed = m_baselookSpeed * Time.deltaTime;
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.right);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, cumulativeLookSpeed);
        /*Quaternion targetRotation = Quaternion.LookRotation(Vector3.right);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, cumulativeLookSpeed);*/
        
        //MOVE
        //Move forward.
        //float speed = m_baseMoveSpeed/* * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower*/;
        float cumulativeMoveSpeed = m_baseMoveSpeed * Time.deltaTime;
        transform.position += (transform.forward * cumulativeMoveSpeed);
    }
}
