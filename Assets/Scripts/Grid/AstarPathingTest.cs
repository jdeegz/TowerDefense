using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AstarPathingTest : MonoBehaviour
{
    public Transform m_goalPoint;
    public GameObject m_crosshair;

    private Vector2Int m_curCellPos;
    private Vector2Int m_goalPointPos;
    private Camera m_camera;
    private List<Vector2Int> m_pathCells;
    private List<Vector2Int> m_curPathCells;

    void Awake()
    {
        m_camera = Camera.main;
        m_goalPointPos = new Vector2Int(Mathf.FloorToInt(m_goalPoint.position.x + 0.5f), Mathf.FloorToInt(m_goalPoint.position.z + 0.5f));
        m_pathCells = new List<Vector2Int>();
        m_curPathCells = new List<Vector2Int>();
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector2Int rayPos = new Vector2Int(Mathf.FloorToInt(hit.point.x + 0.5f), Mathf.FloorToInt(hit.point.z + 0.5f));

            if (rayPos != m_curCellPos)
            {
                Util.GetCellFromPos(m_curCellPos).ResetState();
                
                m_curCellPos = rayPos;
                m_crosshair.transform.position = new Vector3(m_curCellPos.x, 0.05f, m_curCellPos.y);
                
                ResetCells();

                Cell curCell = Util.GetCellFromPos(m_curCellPos);
                
                if (curCell.m_isOccupied)
                {
                    SetCrosshairColor(Color.red);
                    return;
                }
                else
                {
                    SetCrosshairColor(Color.white);
                }
                
                curCell.UpdateCellState(Cell.CellState.Hovered);

                //Get neighbor cells.
                Vector2Int[] neighbors =
                {
                    new Vector2Int(m_curCellPos.x, m_curCellPos.y + 1), //North
                    new Vector2Int(m_curCellPos.x + 1, m_curCellPos.y), //East
                    new Vector2Int(m_curCellPos.x, m_curCellPos.y - 1), //South
                    new Vector2Int(m_curCellPos.x - 1, m_curCellPos.y) //West
                };

                for (int i = 0; i < neighbors.Length; ++i)
                {
                    Debug.Log("Pathing from:" + neighbors[i]);
                    Cell cell = Util.GetCellFromPos(neighbors[i]);
                    if (!cell.m_isOccupied)
                    {
                        List<Vector2Int> testPath = AStar.FindPath(neighbors[i], m_goalPointPos, Util.gridManager.gridCells);
                        if (testPath != null)
                        {
                            m_pathCells = new List<Vector2Int>(testPath);
                            ColorCells();
                        }
                        else
                        {
                            List<Vector2Int> islandCells = new List<Vector2Int>(AStar.FindIsland(neighbors[i]));
                            foreach (Vector2Int cellPos in islandCells)
                            {
                                Cell islandCell = Util.GetCellFromPos(cellPos);
                                islandCell.UpdateCellState(Cell.CellState.Island);
                                if (islandCell.m_actorCount > 0)
                                {
                                    SetCrosshairColor(Color.red);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void ResetCells()
    {
        foreach (Cell cell in Util.gridManager.gridCells)
        {
            if (cell.m_cellState == Cell.CellState.Path || cell.m_cellState == Cell.CellState.Island)
            {
                cell.ResetState();
            }
        }
    }

    void ColorCells()
    {
        for (var i = 0; i < m_pathCells.Count; i++)
        {
            var cellPos = m_pathCells[i];
            Cell cell = Util.GetCellFromPos(cellPos);
            cell.UpdateCellState(Cell.CellState.Path);
        }
    }

    void SetCrosshairColor(Color color)
    {
        m_crosshair.GetComponent<SpriteRenderer>().color = color;
    }
}