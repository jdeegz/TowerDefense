using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreepManager : MonoBehaviour
{
    public static CreepManager Instance;
    public float creepSpreadDelay = 0.5f;

    private bool[,] m_creepGrid;
    private Queue<Vector2Int> spreadQueue = new Queue<Vector2Int>();

    private int m_gridWidth;
    private int m_gridHeight;
    
    private Texture2D m_creepTexture;
    public Material m_material;

    void Awake()
    {
        Instance = this;
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.BuildGrid)
        {
            m_gridWidth = GridManager.Instance.m_gridWidth;
            m_gridHeight = GridManager.Instance.m_gridHeight;
            m_creepGrid = new bool[m_gridWidth, m_gridHeight];
            m_creepTexture = new Texture2D(m_gridWidth, m_gridHeight, TextureFormat.R8, false);
            m_creepTexture.filterMode = FilterMode.Point; // Keep it crisp for grid-based look
            m_material.SetTexture("_CreepMap", m_creepTexture);
            m_material.SetFloat("_GridWidth", m_gridWidth); 
            m_material.SetFloat("_GridHeight", m_gridHeight); 
        }
    }

    public void AddCreepSource(Cell cell)
    {
        if (cell == null) return;

        int posX = cell.m_cellPos.x;
        int posY = cell.m_cellPos.y;
        if (!m_creepGrid[posX, posY])
        {
            m_creepGrid[posX, posY] = true;
            UpdateCreepTexture();
            //spreadQueue.Enqueue(position);
            //StartCoroutine(SpreadCreep());
        }
    }
    
    public void RemoveCreepSource(Vector2Int position)
    {
        if (!m_creepGrid[position.x, position.y])
        {
            m_creepGrid[position.x, position.y] = false;
            UpdateCreepTexture();
        }
    }

    private IEnumerator SpreadCreep()
    {
        while (spreadQueue.Count > 0)
        {
            Vector2Int current = spreadQueue.Dequeue();
            foreach (Vector2Int dir in new Vector2Int[] {
                         Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int neighbor = current + dir;
                if (IsValidCell(neighbor) && !m_creepGrid[neighbor.x, neighbor.y])
                {
                    m_creepGrid[neighbor.x, neighbor.y] = true;
                    spreadQueue.Enqueue(neighbor);
                    UpdateCreepTexture();
                    yield return new WaitForSeconds(creepSpreadDelay);
                }
            }
        }
    }
    
    private bool IsValidCell(Vector2Int pos)
    {
        Cell cell = Util.GetCellFromPos(pos);
        return cell != null && !cell.m_isOutOfBounds;
    }

    private void UpdateCreepTexture()
    {
        Color[] pixels = new Color[m_gridWidth * m_gridHeight];
        for (int x = 0; x < m_gridWidth; x++)
        {
            for (int y = 0; y < m_gridHeight; y++)
            {
                float value = m_creepGrid[x, y] ? 1f : 0f;
                pixels[y * m_gridWidth + x] = new Color(value, value, value, 1f);
            }
        }
        
        m_creepTexture.SetPixels(pixels);
        m_creepTexture.Apply();
    }
    
    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }
}

