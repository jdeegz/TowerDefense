using UnityEngine;

public class UnscaledTimeMaterialController : MonoBehaviour
{
    [SerializeField] private Renderer m_renderer;
    private Material m_targetMaterial;
    private int m_unscaledTimePropertyID;

    void Start()
    {
        m_targetMaterial = m_renderer.material;
        
        m_unscaledTimePropertyID = Shader.PropertyToID("_UnscaledTime");
    }

    void Update()
    {
        if (m_targetMaterial != null)
        {
            m_targetMaterial.SetFloat(m_unscaledTimePropertyID, Time.unscaledTime);
        }
    }
}