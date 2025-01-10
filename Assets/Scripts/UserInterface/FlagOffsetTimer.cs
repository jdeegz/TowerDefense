using UnityEngine;

public class FlagOffsetTimer : MonoBehaviour
{
    [SerializeField] private Renderer m_flagRenderer;
    void OnEnable()
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetFloat("_TimeOffset", Random.Range(0f, 10f));
        m_flagRenderer.SetPropertyBlock(block);
    }
}
