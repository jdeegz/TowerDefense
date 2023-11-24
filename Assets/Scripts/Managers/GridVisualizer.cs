using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    private GameObject m_gridCellObj;
    private Cell m_curCell;
    private GridManager m_gridManager;
    public Color m_openCellColor;
    public Color m_unpathableCellColor;
    
    
    void Start()
    {
        m_gridManager = GridManager.Instance;
        
        
    }
    // Start is called before the first frame update
    void MakeMeshGrid()
    {
        Mesh mesh = new Mesh();

        int m_gridWidth = 2;
        int m_gridHeight = 2;
        float m_tileSize = 1;

        Vector3[] m_vertices = new Vector3[4 * m_gridWidth * m_gridHeight];
        Vector2[] m_uv = new Vector2[4 * m_gridWidth * m_gridHeight];
        int[] m_triangles = new int[6 * m_gridWidth * m_gridHeight];
        
        for (int i= 0; i < m_gridWidth; ++i)
        {
            for (int j = 0; j < m_gridHeight; ++j)
            {
                int index = i * m_gridHeight + j;

                m_vertices[index * 4 + 0] = new Vector3(m_tileSize * i, m_tileSize * j);
                m_vertices[index * 4 + 1] = new Vector3(m_tileSize * i, m_tileSize * (j + 1));
                m_vertices[index * 4 + 2] = new Vector3(m_tileSize * (i + 1), m_tileSize * (j + 1));
                m_vertices[index * 4 + 3] = new Vector3(m_tileSize * (i + 1), m_tileSize * j);

                m_uv[index * 4 + 0] = new Vector2(0, 0);
                m_uv[index * 4 + 1] = new Vector2(0, 1);
                m_uv[index * 4 + 2] = new Vector2(1, 1);
                m_uv[index * 4 + 3] = new Vector2(1, 0);

                m_triangles[index * 6 + 0] = index * 4 + 0;
                m_triangles[index * 6 + 1] = index * 4 + 1;
                m_triangles[index * 6 + 2] = index * 4 + 2;
                
                m_triangles[index * 6 + 3] = index * 4 + 0;
                m_triangles[index * 6 + 4] = index * 4 + 2;
                m_triangles[index * 6 + 5] = index * 4 + 3;
            }
        }

        mesh.vertices = m_vertices;
        mesh.uv = m_uv;
        mesh.triangles = m_triangles;

        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
