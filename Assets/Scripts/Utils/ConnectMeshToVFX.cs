using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class ConnectMeshToVFX : MonoBehaviour
{
    private VisualEffect m_visualEffectObj;
    [SerializeField] private  MeshFilter m_meshFilter;

    void Start()
    {
        m_visualEffectObj = GetComponent<VisualEffect>();
        if (m_visualEffectObj && m_meshFilter)
        {
            m_visualEffectObj.SetMesh("MeshFilter", m_meshFilter.sharedMesh);
            m_visualEffectObj.SetMatrix4x4("MeshTransform", m_meshFilter.transform.localToWorldMatrix);
        }
    }
}
