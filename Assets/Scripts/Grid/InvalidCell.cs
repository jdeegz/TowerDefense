using UnityEngine;

public class InvalidCell : MonoBehaviour
{
    
    [SerializeField] private Renderer m_renderer;
    [SerializeField] private SelectionColors m_selectionColors;

    private Material m_material;
    private bool m_cellIsBuildable;
    private Vector3 m_curCellPos;
    private float m_stoppingDistance = 0.05f;

    public Vector3 CurrentCellPosition
    {
        get { return m_curCellPos; }
        set
        {
            m_curCellPos = value;
        }
    }
    
    public bool CellIsBuildable
    {
        get { return m_cellIsBuildable; }
        set
        {
            if (value != m_cellIsBuildable)
            {
                m_cellIsBuildable = value;
                if(m_material == null) m_material = m_renderer.material;
                m_material.color = m_cellIsBuildable ? m_selectionColors.m_outlineBaseColor : m_selectionColors.m_outlineRestrictedColor;
            }
        }
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, m_curCellPos) < m_stoppingDistance) return;
        
        transform.position = Vector3.Lerp(transform.position, m_curCellPos, 20f * Time.unscaledDeltaTime);
    }
}
