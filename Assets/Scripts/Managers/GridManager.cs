using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TechnoBabelGames;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public Material m_lineRendererMaterial;
    public Cell[] m_gridCells;
    public int m_gridWidth;
    public int m_gridHeight;

    public List<UnitPath> m_unitPaths;
    private List<Vector2Int> m_exits;
    private List<Vector2Int> m_spawners;
    private Vector2Int m_enemyGoalPos;
    private Vector2Int m_preconstructedTowerPos;
    
    public static event Action OnResourceNodeRemoved;


    void Awake()
    {
        Instance = this;
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
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
                int index = z * m_gridWidth + x;
                m_gridCells[index] = new Cell();
                //Debug.Log($"New Cell created at: {x},{z} with an index value of: {index}");
            }
        }

        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.PlaceObstacles);
    }

    public void ResourceNodeRemoved()
    {
        OnResourceNodeRemoved?.Invoke();
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
            unitPath.m_lineRenderer = null;
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
            GameObject lineObj = new GameObject("Line");
            lineObj.transform.SetParent(spawner.gameObject.transform);
            unitPath.m_lineRenderer = lineObj.AddComponent<TBLineRendererComponent>();
            unitPath.Setup();
            m_unitPaths.Add(unitPath);
        }

        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Setup);
    }
}

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
    public List<Vector2Int> m_lastGoodPath;
    public List<Vector2Int> m_exits;
    public List<Vector2Int> m_spawners;
    public TBLineRendererComponent m_lineRenderer;
    public bool m_hasPath;
    public bool m_isExit;
    public bool m_preconState;
    public bool m_pathDirty;
    

    public void Setup()
    {
        GameplayManager.OnPreconTowerMoved += PreconTowerMoved;
        GameplayManager.OnPreconstructedTowerClear += PreconstructedTowerClear;
        GameplayManager.OnTowerBuild += SetLastGoodPath;

        GridManager.OnResourceNodeRemoved += ResourceNodeRemoved;

        //Define desired properties of the line.
        if (m_lineRenderer != null)
        {
            m_lineRenderer.lineRendererProperties = new TBLineRenderer();
            //m_lineRenderer.lineRendererProperties.linePoints = m_path.Count;
            m_lineRenderer.lineRendererProperties.texture = GridManager.Instance.m_lineRendererMaterial;
            m_lineRenderer.lineRendererProperties.lineWidth = 0.1f;
            ColorUtility.TryParseHtmlString( "#73B549" , out Color lineColor );
            m_lineRenderer.lineRendererProperties.startColor = lineColor;
            m_lineRenderer.lineRendererProperties.endColor = lineColor;
            m_lineRenderer.lineRendererProperties.axis = TBLineRenderer.Axis.Y;

            //Assign the properties.
            m_lineRenderer.SetLineRendererProperties();
        }

        UpdateExitPath();
    }

    void OnDestroy()
    {
        GameplayManager.OnPreconTowerMoved -= PreconTowerMoved;
        GameplayManager.OnPreconstructedTowerClear -= PreconstructedTowerClear;
        GameplayManager.OnTowerBuild -= PreconstructedTowerClear;
        
        GridManager.OnResourceNodeRemoved -= ResourceNodeRemoved;
    }

    void Start()
    {
    }

    void ResourceNodeRemoved()
    {
        //If a resource node is removed, update the paths.
        UpdateExitPath();
        SetLastGoodPath();
    }
    
    void PreconstructedTowerClear()
    {
        if (m_preconState)
        {
            m_preconState = false;
            m_path = new List<Vector2Int>(m_lastGoodPath);
            
            if (m_lineRenderer != null)
            {
                m_lineRenderer.SetPoints(m_path);
            }
        }
    }

    void SetLastGoodPath()
    {
        m_lastGoodPath = new List<Vector2Int>(m_path);
    }

    void PreconTowerMoved(Vector2Int newPos)
    {
        //Everytime the precon tower moves, we need to check to see if our path needs to be recalculated.
        //A. Find a new path if we're in the current path.
        //B. Revert to last good path if we know we've changed the path during this precon phase, and we're not in the path.
        //C. Find a new path if we know we've changed the path and move the precon tower.
        
        //If we are entering precon, stash the path we had before we entered.
        if (!m_preconState)
        {
            m_preconState = true;
            SetLastGoodPath();
        }
        
        //Check to see if new position is in my list.
        m_preconstructedTowerPos = newPos;
        bool towerIsInPath = m_path.Contains(m_preconstructedTowerPos);
        
        if (towerIsInPath)
        {
            //Since we have found the cell in the list, we want to mark this unitPath as dirty, so we know
            m_pathDirty = true;
            UpdateExitPath();
        }
        
        //Reset back to previous path, since we're dirty we know the path has changed from when we started precon tower phase.
        //If the new position is within the last good path, we need to find a new path, else we can revert back to last good path.
        if (!towerIsInPath && m_pathDirty)
        {
            if (m_lastGoodPath.Contains(m_preconstructedTowerPos))
            {
                UpdateExitPath();
            }
            else
            {
                m_hasPath = true;
                m_path = new List<Vector2Int>(m_lastGoodPath);

                if (m_lineRenderer != null)
                {
                    m_lineRenderer.SetPoints(m_path);
                }

                m_pathDirty = false;
            }
        }
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

            //Debug.Log($"Checking path of {m_sourceObj.name}: {m_startPos} - {m_exits[i]}");
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
                List<Vector2Int> testPath = AStar.FindExitPath(m_startPos, m_spawners[i], m_preconstructedTowerPos, m_enemyGoalPos);
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
            if (m_lineRenderer != null)
            {
                m_lineRenderer.SetPoints(m_path);
            }
        }
        else
        {
            //We dont have a path.
            //m_path.Clear();
            Debug.Log("CANNOT BUILD.");
        }
    }
}