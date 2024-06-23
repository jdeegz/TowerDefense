using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXRandomizeUV : MonoBehaviour
{
    private Material m_material;
    private Vector2 m_offset;
    
    void Awake()
    {
        m_material = GetComponent<Renderer>().material;
        m_offset.x = Random.Range(0, 101) * .01f;
        m_offset.y = Random.Range(0, 101) * .01f;
        m_material.SetVector("_UVOffset", m_offset);
    }
}
