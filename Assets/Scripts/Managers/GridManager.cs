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
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.BuildGrid)
        {
            BuildGrid();
        }

        if (newState == GameplayManager.GameplayState.CreatePaths)
        {
            BuildPathList();
        }
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

        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.PlaceObstacles);
    }

    public void BuildPathList()
    {
        CastleController castleController = GameplayManager.Instance.m_castleController;
        m_enemyGoalPos = Util.GetVector2IntFrom3DPos(GameplayManager.Instance.m_enemyGoal.position);

        //Build Exits Position List
        m_exits = new List<Vector2Int>();
        m_exits.Add(new Vector2Int(m_enemyGoalPos.x, m_enemyGoalPos.y + 1));
        m_exits.Add(new Vector2Int(m_enemyGoalPos.x + 1, m_enemyGoalPos.y));
        m_exits.Add(new Vector2Int(m_enemyGoalPos.x, m_enemyGoalPos.y - 1));
        m_exits.Add(new Vector2Int(m_enemyGoalPos.x - 1, m_enemyGoalPos.y));

        //Build Spawners Position List
        m_spawners = new List<Vector2Int>();
        foreach (UnitSpawner spawner in GameplayManager.Instance.m_unitSpawners)
        {
            m_spawners.Add(Util.GetVector2IntFrom3DPos(spawner.m_spawnPoint.position));
        }

        //Create Exit UnitPaths
        foreach (GameObject obj in castleController.m_castleEntrancePoints)
        {
            UnitPath unitPath = new UnitPath();
            Vector2Int pos = Util.GetVector2IntFrom3DPos(obj.transform.position);
            unitPath.m_sourceObj = obj;
            unitPath.m_startPos = pos;
            unitPath.m_isExit = true;
            unitPath.m_exits = m_exits;
            unitPath.m_spawners = m_spawners;
            unitPath.m_enemyGoalPos = m_enemyGoalPos;
            unitPath.Setup();
            m_unitPaths.Add(unitPath);
        }

        //Create Spawners UnitPaths
        foreach (UnitSpawner spawner in GameplayManager.Instance.m_unitSpawners)
        {
            Vector2Int start = Util.GetVector2IntFrom3DPos(spawner.m_spawnPoint.position);
            UnitPath unitPath = new UnitPath();
            unitPath.m_sourceObj = spawner.gameObject;
            unitPath.m_startPos = start;
            unitPath.m_isExit = false;
            unitPath.m_exits = m_exits;
            unitPath.m_spawners = m_spawners;
            unitPath.m_enemyGoalPos = m_enemyGoalPos;
            unitPath.Setup();
            m_unitPaths.Add(unitPath);
        }
        
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Setup);
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
    public Vector2Int m_preconstructedTowerPos;
    public Vector2Int m_enemyGoalPos;
    public List<Vector2Int> m_path;
    public List<Vector2Int> m_exits;
    public List<Vector2Int> m_spawners;
    public bool m_hasPath;
    public bool m_pathChecked;
    public bool m_isExit;

    public void Setup()
    {
        GameplayManager.OnPreconTowerMoved += PreconTowerMoved;
        UpdateExitPath();
    }

    void OnDestroy()
    {
        GameplayManager.OnPreconTowerMoved -= PreconTowerMoved;
    }

    void Start()
    {
    }

    void PreconTowerMoved(Vector2Int newPos)
    {
        //Check to see if new position is in my list.
        m_preconstructedTowerPos = newPos;

        if (m_path.Contains(m_preconstructedTowerPos) || !m_hasPath)
        {
            UpdateExitPath();
        }

        //If true, try to update the path.
    }

    void UpdateExitPath()
    {
        int shortestPathCount = Int32.MaxValue;
        m_hasPath = false;

        //Path from the exit to each exit.
        for (int i = 0; i < m_exits.Count; ++i)
        {
            if (m_startPos == m_exits[i])
            {
                continue;
            }

            Debug.Log($"Checking path of {m_sourceObj.name}: {m_startPos} - {m_exits[i]}");
            List<Vector2Int> testPath =
                AStar.FindExitPath(m_startPos, m_exits[i], m_preconstructedTowerPos, m_enemyGoalPos);

            if (testPath != null)
            {
                m_hasPath = true;

                //Compare to current shortest path found.
                if (testPath.Count <= shortestPathCount)
                {
                    shortestPathCount = testPath.Count;
                    m_path = testPath;
                    m_endPos = m_exits[i];
                }
            }
        }

        //We did not find any path-able exits. Now try spawners.
        if (!m_hasPath && m_isExit)
        {
            for (int i = 0; i < m_spawners.Count; ++i)
            {
                List<Vector2Int> testPath =
                    AStar.FindExitPath(m_startPos, m_spawners[i], m_preconstructedTowerPos, m_enemyGoalPos);
                if (testPath != null)
                {
                    m_hasPath = true;

                    //Compare to current shortest path found.
                    if (testPath.Count <= shortestPathCount)
                    {
                        shortestPathCount = testPath.Count;
                        m_path = testPath;
                        m_endPos = m_spawners[i];
                    }
                }
            }
        }

        if (m_hasPath)
        {
            //DrawPathLineRenderer(unitPath.m_path);
        }
        else
        {
            //We dont have a path.
            m_path.Clear();
            Debug.Log("CANNOT BUILD.");
        }

        /*void DrawPathLineRenderer(List<Vector2Int> points)
        {
            GameObject m_lineObj = new GameObject("Line");
            m_lineObj.transform.SetParent(gameObject.transform);
            m_lineRenderers.Add(m_lineObj);

            TBLineRendererComponent m_lineRenderer = m_lineObj.AddComponent<TBLineRendererComponent>();

            //Define desired properties of the line.
            m_lineRenderer.lineRendererProperties = new TBLineRenderer();
            m_lineRenderer.lineRendererProperties.linePoints = points.Count;
            m_lineRenderer.lineRendererProperties.lineWidth = 0.1f;
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
        }*/
    }

}