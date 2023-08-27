using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TechnoBabelGames;
using UnityEngine;
using UnityEngine.Serialization;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public Cell[] m_gridCells;
    public int m_gridWidth = 10;
    public int m_gridHeight = 10;

    public List<UnitPath> m_unitPaths;
    public List<GameObject> m_lineRenderers;
    private List<Vector2Int> m_exits;
    private List<Vector2Int> m_spawners;
    private Vector2Int m_enemyGoalPos;
    private Vector2Int m_preconstructedTowerPos;


    void Awake()
    {
        Instance = this;
        BuildGrid();
    }

    void Start()
    {
        //BuildPathList();
    }

    void BuildGrid()
    {
        m_gridCells = new Cell[m_gridWidth * m_gridHeight];


        for (int x = 0; x < m_gridWidth; ++x)
        {
            for (int z = 0; z < m_gridHeight; ++z)
            {
                int index = x * m_gridWidth + z;
                m_gridCells[index] = new Cell();
                //Debug.Log("New Cell created at: " + index);
            }
        }
    }

    public void BuildPathList()
    {
        /*m_lineRenderers = new List<GameObject>();
        m_shortestPathCells = new List<Vector2Int>();
        m_shortestPathCells.AddRange(GetExitPathCells());
        m_shortestPathCells.AddRange(GetSpawnerPaths());*/

        GameObject goalObj = GameplayManager.Instance.m_castleController.gameObject;
        m_enemyGoalPos = Util.GetVector2IntFrom3DPos(GameplayManager.Instance.m_enemyGoal.position);
        m_spawners = new List<Vector2Int>();

        //Build Exits to check.
        m_exits = new List<Vector2Int>();
        m_exits.Add(new Vector2Int(m_enemyGoalPos.x, m_enemyGoalPos.y + 1));
        m_exits.Add(new Vector2Int(m_enemyGoalPos.x + 1, m_enemyGoalPos.y));
        m_exits.Add(new Vector2Int(m_enemyGoalPos.x, m_enemyGoalPos.y - 1));
        m_exits.Add(new Vector2Int(m_enemyGoalPos.x - 1, m_enemyGoalPos.y));

        foreach (Vector2Int pos in m_exits)
        {
            UnitPath unitPath = new UnitPath();
            unitPath.m_sourceObj = goalObj;
            unitPath.m_startPos = pos;
            unitPath.m_isExit = true;
            m_unitPaths.Add(unitPath);
        }

        //Build Spawners to check.
        foreach (UnitSpawner spawner in GameplayManager.Instance.m_unitSpawners)
        {
            Vector2Int start = Util.GetVector2IntFrom3DPos(spawner.m_spawnPoint.position);
            UnitPath unitPath = new UnitPath();
            unitPath.m_sourceObj = spawner.gameObject;
            unitPath.m_startPos = start;
            unitPath.m_isExit = false;
            m_spawners.Add(start);
            m_unitPaths.Add(unitPath);
        }
        
        //Check Exits paths.
        UpdatePaths();
    }

    public void UpdatePaths()
    {
        //Delete exisiting paths.
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        //Reset the list.
        m_lineRenderers.Clear();
        Debug.Log("Paths cleared.");
        
        foreach (UnitPath unitPath in m_unitPaths)
        {
            GetExitPath(unitPath);
        }
    }

    void GetExitPath(UnitPath unitPath)
    {
        int shortestPathCount = Int32.MaxValue;
        unitPath.m_hasPath = false;
        
        //Path from the exit to each exit.
        for (int i = 0; i < m_exits.Count; ++i)
        {
            if (unitPath.m_startPos == m_exits[i])
            {
                continue;
            }
            Debug.Log($"Checking path {unitPath.m_startPos} - {m_exits[i]}");
            List<Vector2Int> testPath = AStar.FindExitPath(unitPath.m_startPos, m_exits[i], m_preconstructedTowerPos, m_enemyGoalPos);

            if (testPath != null)
            {
                unitPath.m_hasPath = true;

                //Compare to current shortest path found.
                if (testPath.Count <= shortestPathCount)
                {
                    shortestPathCount = testPath.Count;
                    unitPath.m_path = testPath;
                    unitPath.m_endPos = m_exits[i];
                }
            }
        }

        //We did not find any path-able exits. Now try spawners.
        if (!unitPath.m_hasPath && unitPath.m_isExit)
        {
            for (int i = 0; i < m_spawners.Count; ++i)
            {
                List<Vector2Int> testPath = AStar.FindExitPath(unitPath.m_startPos, m_spawners[i], m_preconstructedTowerPos, m_enemyGoalPos);
                if (testPath != null)
                {
                    unitPath.m_hasPath = true;
                
                    //Compare to current shortest path found.
                    if (testPath.Count <= shortestPathCount)
                    {
                        shortestPathCount = testPath.Count;
                        unitPath.m_path = testPath;
                        unitPath.m_endPos = m_exits[i];
                    }
                }
            }
        }

        if (unitPath.m_hasPath)
        {
            DrawPathLineRenderer(unitPath.m_path);
        }
        else
        {
            Debug.Log("CANNOT BUILD.");
        }
    }
    

    List<Vector2Int> GetExitPathCells()
    {
        //Move most of these out of the function as the Exits and Goals never change. Precon tower cell does though.
        List<Vector2Int> exitPathCells = new List<Vector2Int>();
        Vector2Int precon = GameplayManager.Instance.m_preconstructedTowerPos;
        Vector2Int goal = Util.GetVector2IntFrom3DPos(GameplayManager.Instance.m_enemyGoal.position);

        //Exits to check.
        List<Vector2Int> exits = new List<Vector2Int>();
        exits.Add(new Vector2Int(goal.x, goal.y + 1));
        exits.Add(new Vector2Int(goal.x + 1, goal.y));
        exits.Add(new Vector2Int(goal.x, goal.y - 1));
        exits.Add(new Vector2Int(goal.x - 1, goal.y));

        List<Vector2Int> starts = new List<Vector2Int>(exits);

        for (int i = 0; i < starts.Count; ++i)
        {
            for (int x = 0; x < exits.Count; ++x)
            {
                if (starts[i] == exits[x])
                {
                    continue;
                }

                List<Vector2Int> testPath = AStar.FindExitPath(starts[i], exits[x], precon, goal);
                if (testPath != null)
                {
                    //Add the cells to the list.
                    exitPathCells.AddRange(testPath);

                    //Remove the exit from the starts, because we dont need to check it (Eliminate duplicate checks)
                    starts.Remove(exits[x]);

                    DrawPathLineRenderer(testPath);
                }
                else
                {
                    return null;
                }
            }
        }

        return exitPathCells;
    }

    List<Vector2Int> GetSpawnerPaths()
    {
        //Move most of these out of the function as the Exits and Goals never change. Precon tower cell does though.
        List<Vector2Int> exitPathCells = new List<Vector2Int>();
        Vector2Int precon = GameplayManager.Instance.m_preconstructedTowerPos;
        Vector2Int goal = Util.GetVector2IntFrom3DPos(GameplayManager.Instance.m_enemyGoal.position);

        //Exits to check.
        List<Vector2Int> exits = new List<Vector2Int>();
        exits.Add(new Vector2Int(goal.x, goal.y + 1));
        exits.Add(new Vector2Int(goal.x + 1, goal.y));
        exits.Add(new Vector2Int(goal.x, goal.y - 1));
        exits.Add(new Vector2Int(goal.x - 1, goal.y));

        //Spawners to start from.
        foreach (UnitSpawner spawner in GameplayManager.Instance.m_unitSpawners)
        {
            Vector2Int start = Util.GetVector2IntFrom3DPos(spawner.m_spawnPoint.position);

            List<Vector2Int> shortestPathFromSpawner = new List<Vector2Int>();
            int shortestPathCount = Int32.MaxValue;

            //Path form the spawner to each exit.
            for (int i = 0; i < exits.Count; ++i)
            {
                List<Vector2Int> testPath = AStar.FindExitPath(start, exits[i], precon, goal);
                if (testPath != null)
                {
                    //Compare to current shortest path found.
                    if (testPath.Count <= shortestPathCount)
                    {
                        shortestPathCount = testPath.Count;
                        shortestPathFromSpawner = testPath;
                    }
                }
            }

            if (shortestPathFromSpawner.Count > 0)
            {
                //Add the cells to the list.
                exitPathCells.AddRange(shortestPathFromSpawner);

                DrawPathLineRenderer(shortestPathFromSpawner);
            }
            else
            {
                return null;
            }
        }

        return exitPathCells;
    }

    void DrawPathLineRenderer(List<Vector2Int> points)
    {
        GameObject m_lineObj = new GameObject("Line");
        m_lineObj.transform.SetParent(gameObject.transform);
        m_lineRenderers.Add(m_lineObj);

        TBLineRendererComponent m_lineRenderer = m_lineObj.AddComponent<TBLineRendererComponent>();

        //Define desired properties of the line.
        m_lineRenderer.lineRendererProperties = new TBLineRenderer();
        m_lineRenderer.lineRendererProperties.linePoints = points.Count;
        m_lineRenderer.lineRendererProperties.lineWidth = 0.5f;
        m_lineRenderer.lineRendererProperties.startColor = Color.red;
        m_lineRenderer.lineRendererProperties.endColor = Color.yellow;
        m_lineRenderer.lineRendererProperties.axis = TBLineRenderer.Axis.Y;

        //Assign the properties.
        m_lineRenderer.SetLineRendererProperties();

        //Create the points.
        for (int i = 0; i < points.Count; ++i)
        {
            GameObject point = new GameObject("Point: " + i);
            point.transform.SetParent(m_lineObj.transform);
            point.transform.position = new Vector3(points[i].x, 0.2f, points[i].y);
        }

        //Assign the child objects to the line renderer as points.
        m_lineRenderer.SetPoints();
    }
}

[System.Serializable]
public class Cell
{
    public bool m_isGoal;
    public bool m_isOccupied;
    public int m_actorCount;

    public void UpdateActorCount(int i)
    {
        m_actorCount += i;
    }

    public void UpdateOccupancy(bool b)
    {
        m_isOccupied = b;
    }

    public void UpdateGoal(bool b)
    {
        m_isGoal = b;
    }
}

[System.Serializable]
public class UnitPath
{
    public GameObject m_sourceObj;
    public Vector2Int m_startPos;
    public Vector2Int m_endPos;
    public List<Vector2Int> m_path;
    public bool m_hasPath;
    public bool m_isExit;
}