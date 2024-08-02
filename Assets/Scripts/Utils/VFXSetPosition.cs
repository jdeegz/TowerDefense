using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXSetPosition : MonoBehaviour
{
    public bool m_setOnGround;
    
    void OnEnable()
    {
        if(m_setOnGround) SetOnGround();
    }

    void SetOnGround()
    {
        transform.localPosition = new Vector3(transform.localPosition.x, -transform.parent.position.y+0.1f, transform.localPosition.z);
    }
}
