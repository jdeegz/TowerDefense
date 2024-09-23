using System;
using System.Buffers;
using System.Collections.Generic;
using TechnoBabelGames;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Analytics;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public Material m_lineRendererMaterial;
    public Cell[] m_gridCells;
    public int m_gridWidth;
    public int m_gridHeight;
    public GameObject m_gridVisualizerObj;
    public GameObject m_gridCellObj;
    public GameObject[] m_gridcellObjects;

    [FormerlySerializedAs("m_groundLayer")]
    public LayerMask m_waterLayer;

    [Header("Tile Map Data")]
    [SerializeField] private Tilemap m_tileMap;
    [SerializeField] private RuleTile m_ruleTile;

    public List<UnitPath> m_unitPaths;
    private List<Vector2Int> m_exits;
    private List<Vector2Int> m_spawners;
    private Vector2Int m_enemyGoalPos;
    private Vector2Int m_preconstructedTowerPos;
    private int m_previousPreconIndex;
    private bool m_previousPreconOccupancy;
    public bool m_spawnPointsAccessible;

    public static event Action OnResourceNodeRemoved;


    void Awake()
    {
        Instance = this;
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        GameplayManager.OnPreconTowerMoved += PreconTowerMoved;
        GameplayManager.OnTowerBuild += TowerBuilt;
        GameplayManager.OnPreconstructedTowerClear += PreconstructedTowerClear;
        GameplayManager.OnTowerSell += RefreshGrid;
        m_gridVisualizerObj.SetActive(false);
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
        GameplayManager.OnPreconTowerMoved -= PreconTowerMoved;
        GameplayManager.OnTowerBuild -= TowerBuilt;
        GameplayManager.OnPreconstructedTowerClear -= PreconstructedTowerClear;
        GameplayManager.OnTowerSell -= RefreshGrid;
    }

    void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        switch (newState)
        {
            case GameplayManager.GameplayState.BuildGrid:
                BuildGrid();
                break;
            case GameplayManager.GameplayState.PlaceObstacles:
                break;
            case GameplayManager.GameplayState.FloodFillGrid:
                FloodFillGrid(m_gridCells, null);
                break;
            case GameplayManager.GameplayState.CreatePaths:
                BuildPathList();
                break;
            case GameplayManager.GameplayState.Setup:
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
                break;
            case GameplayManager.GameplayState.BossWave:
                break;
            case GameplayManager.GameplayState.Combat:
                break;
            case GameplayManager.GameplayState.Build:
                break;
            case GameplayManager.GameplayState.Paused:
                break;
            case GameplayManager.GameplayState.Victory:
                break;
            case GameplayManager.GameplayState.Defeat:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }

    public void FloodFillGrid(Cell[] gridCells, Action callback)
    {
        int goalCellIndex = Util.GetCellIndex(GameplayManager.Instance.m_enemyGoal.position);
        Cell goalCell = gridCells[goalCellIndex];
        Dictionary<Cell, Cell> nextTileToGoal = new Dictionary<Cell, Cell>();
        Queue<Cell> frontier = new Queue<Cell>();
        List<Cell> visited = new List<Cell>();

        //How many spawners are in the game? What are their spawnPoint cells?
        List<Cell> spawnPointCells = new List<Cell>();
        foreach (UnitSpawner spawner in GameplayManager.Instance.m_unitSpawners)
        {
            spawnPointCells.Add(Util.GetCellFrom3DPos(spawner.m_spawnPoint.position));
        }

        int spawnPointCellsFound = 0;

        frontier.Enqueue(goalCell);

        while (frontier.Count > 0)
        {
            Cell curCell = frontier.Dequeue();

            Cell[] neighborCells = Util.GetNeighborCells(curCell, gridCells);
            for (var i = 0; i < neighborCells.Length; i++)
            {
                //Break out if the array entry for this neighbor is null
                if (neighborCells[i] == null) continue;

                var neighbor = neighborCells[i];
                if (visited.Contains(neighbor) == false && frontier.Contains(neighbor) == false)
                {
                    if (!neighbor.m_isOccupied && !neighbor.m_isTempOccupied)
                    {
                        //Get the direction of the neighbor to the curCell.
                        neighbor.UpdateOccupancyDisplay(false);
                        neighbor.UpdateTempDirection(i);
                        frontier.Enqueue(neighbor);
                        nextTileToGoal[neighbor] = curCell;

                        //Is this cell a spawnPoint? If so, holler.
                        foreach (Cell spawnPointCell in spawnPointCells)
                        {
                            if (neighbor == spawnPointCell)
                            {
                                ++spawnPointCellsFound;
                            }
                        }
                    }
                }
            }

            visited.Add(curCell);
        }

        //Debug.Log($"{spawnPointCellsFound} of {spawnPointCells.Count} spawners found during FloodFill.");

        //If we've found all the spawn points, this is good, we can update each path.
        if (m_spawnPointsAccessible = spawnPointCellsFound == spawnPointCells.Count)
        {
            //If we're setting up the game, continue doing so.
            if (GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.FloodFillGrid)
            {
                SetCellDirections();
                //Debug.Log("Grid has been Flood Filled.");
                GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.CreatePaths);
            }

            //Debug.Log($"GridManager: Drawing new Unit Paths");
            foreach (UnitPath unitPath in m_unitPaths)
            {
                //Break this into its own function outside of flood fill.
                unitPath.DrawExitPathLine();
            }
        }
        else
        {
            //Debug.Log($"We did not find all exit paths.");
        }

        if (callback != null)
        {
            callback.Invoke();
        }
    }

    private void SetCellDirections()
    {
        //Lock in the direction of all the cells.
        foreach (Cell cell in m_gridCells)
        {
            cell.SetDirection();
        }
    }

    void BuildGrid()
    {
        m_gridCells = new Cell[m_gridWidth * m_gridHeight];
        m_gridcellObjects = new GameObject[m_gridWidth * m_gridHeight];

        for (int x = 0; x < m_gridWidth; ++x)
        {
            for (int z = 0; z < m_gridHeight; ++z)
            {
                int index = z * m_gridWidth + x;
                m_gridcellObjects[index] = Instantiate(m_gridCellObj, new Vector3(x, 0.1f, z), Quaternion.Euler(90, 0, 0), m_gridVisualizerObj.transform);
                m_gridcellObjects[index].name = $"GridCellObject ({x},{z})";
                Cell cell = new Cell();
                cell.m_cellPos = new Vector2Int(x, z);
                cell.m_cellIndex = index;
                m_gridCells[index] = cell;
                cell.m_gridCellObj = m_gridcellObjects[index];

                //If we're a cell on the perimeter, mark it as occupied, else we hit test it.
                if (x == 0 || x == m_gridWidth - 1 || z == 0 || z == m_gridHeight - 1)
                {
                    cell.UpdateOccupancyDisplay(true);
                }
                else
                {
                    HitTestCellForGround(m_gridcellObjects[index].transform.position, cell);
                }
            }
        }

        //Debug.Log("Grid Built.");
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.PlaceObstacles);

        //Debug.Log("Obstacles Placed.");
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.FloodFillGrid);
    }

    //Get the index of the precon Tower cell position. We will flip this cell to Occupied in our temp grid.
    //This function is invoked by the event 'PreconTowerMoved' in the GameplayManager.
    void PreconTowerMoved(Vector2Int preconTowerPos)
    {
        int preconTowerCellIndex = preconTowerPos.y * m_gridWidth + preconTowerPos.x;
        //Debug.Log($"Precon Index: {preconTowerCellIndex}");

        //Debug.Log($"GridManager: Updating Grid Cell {preconTowerCellIndex} temp occupancy to TRUE.");
        m_gridCells[preconTowerCellIndex].UpdateTempOccupancyDisplay(true);

        //Set the values back to their original state if this is not our first iteration.
        if (m_previousPreconIndex > 0 && m_previousPreconIndex != preconTowerCellIndex)
        {
            //Debug.Log($"GridManager: Updating Grid Cell {m_previousPreconIndex} temp occupancy to FALSE.");
            m_gridCells[m_previousPreconIndex].UpdateTempOccupancyDisplay(false);
        }

        m_previousPreconIndex = preconTowerCellIndex;
        Debug.Log($"Precon Tower Moved, new index: {m_previousPreconIndex}");

        //Debug.Log($"GridManager: Precon Tower Moved, now flood filling.");
        FloodFillGrid(m_gridCells, null);
    }

    void TowerBuilt()
    {
        SetCellDirections();

        //RevertPreconTempChanges();
    }

    void PreconstructedTowerClear()
    {
        RevertPreconTempChanges();

        FloodFillGrid(m_gridCells, null);
    }

    void RevertPreconTempChanges()
    {
        //Set the values back to their original state if this is not our first iteration.
        if (m_previousPreconIndex > 0)
        {
            Debug.Log($"Reverting precon temp changes. Current index: {m_previousPreconIndex} new index: -1.");
            m_gridCells[m_previousPreconIndex].UpdateTempOccupancyDisplay(false);
            m_previousPreconIndex = -1;
        }
    }

    public void ToggleTileMap(Vector3Int pos, bool isOccupied)
    {
        TileBase tile = isOccupied ? m_ruleTile : null;
        m_tileMap.SetTile(pos, tile);
    }
    
    public void oldRefreshGrid()
    {
        FloodFillGrid(m_gridCells, SetCellDirections);
    }

    public void RefreshGrid()
    {
        //A node depleted!
        //If we're in precon, we want to unset precon mode's temporary assignments of occupancy and direction.
        //Then flood fill to update temp directions. Then SET those directions.
        //Then return to precon testing.
        //Debug.Log($"GridManager: Node Depletion Detected.");
        if (GameplayManager.Instance.m_interactionState == GameplayManager.InteractionState.PreconstructionTower)
        {
            //Debug.Log($"GridManager: Reverting Precon Temp Changes.");
            Debug.Log($"Attempting to refresh grid, m_previousPreconIndex of: {m_previousPreconIndex}");
            int preconIndex = m_previousPreconIndex;
            RevertPreconTempChanges();

            //Debug.Log($"GridManager: Flood Filling and Setting directions.");
            FloodFillGrid(m_gridCells, SetCellDirections);

            //I think I could also cheat this by setting GameplayManagers m_preconstructedTowerPos to Vector2Int.zero, which will get flagged as the 'new pos' invoking its action.
            //Debug.Log($"GridManager: Returning to Precon Tower temp changes.");
            Debug.Log($"Moving precon tower back to cell index: {preconIndex}");
            PreconTowerMoved(m_gridCells[preconIndex].m_cellPos);
        }
        else
        {
            FloodFillGrid(m_gridCells, SetCellDirections);
        }
    }


    private void HitTestCellForGround(Vector3 pos, Cell cell)
    {
        //Shoot a ray from the cell. Adding a yBuffer to each position above, so we want to shoot the rays down.
        Ray ray = new Ray(pos, Vector3.down);

        //Shoot the ray and collect all the hits.
        RaycastHit[] hits = Physics.RaycastAll(ray, 0.22f);

        //If we recieved no hits from our cast, exit.
        if (hits.Length == 0) return;

        //If we received hits, check to see if water was the last one hit.
        if (IsLayerInMask(hits[hits.Length - 1].transform.gameObject.layer, m_waterLayer))
        {
            cell.UpdateOccupancyDisplay(true);
        }
    }

    bool IsLayerInMask(int layer, LayerMask layerMask)
    {
        // Use the LayerMask method to check if the layer is in the layer mask
        return layerMask == (layerMask | (1 << layer));
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
            //Debug.Log($"Added Unit Path for {obj.name}");
        }

        //Create Spawners UnitPaths
        foreach (UnitSpawner spawner in GameplayManager.Instance.m_unitSpawners)
        {
            Vector2Int start = Util.GetVector2IntFrom3DPos(spawner.m_spawnPoint.position);
            UnitPath unitPath = new UnitPath();
            unitPath.m_unitSpawner = spawner;
            unitPath.m_startPos = start;
            unitPath.m_isExit = false;
            unitPath.m_exits = m_exits;
            unitPath.m_spawners = m_spawners;
            unitPath.m_enemyGoalPos = m_enemyGoalPos;
            GameObject lineObj = new GameObject("Line");
            lineObj.transform.SetParent(spawner.gameObject.transform);
            //Debug.Log($"Line Renderer Made.");
            unitPath.m_lineRenderer = lineObj.AddComponent<TBLineRendererComponent>();
            unitPath.Setup();
            m_unitPaths.Add(unitPath);
            //Debug.Log($"Added Unit Path for {spawner.gameObject.name}");
        }

        //Debug.Log("Path List Built.");
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Setup);
    }
}

[System.Serializable]
public class Cell
{
    //Cell Data
    public bool m_isGoal;
    public bool m_isOccupied;
    public bool m_isTempOccupied;
    public bool m_isBuildRestricted;
    public int m_actorCount;
    public List<string> m_actorsList;
    public int m_cellIndex;
    public Vector2Int m_cellPos;
    public bool m_canPathNorth = true;
    public bool m_canPathEast = true;
    public bool m_canPathSouth = true;
    public bool m_canPathWest = true;

    //Visualizer
    public GameObject m_gridCellObj;
    public Vector3 m_directionToNextCell;
    public Vector3 m_tempDirectionToNextCell;
    private Material m_gridCellObjMaterial;
    private Color m_pathableColor = Color.cyan;
    private Color m_occupiedColor = Color.yellow;
    private Color m_tempOccupiedColor = Color.magenta;
    private Color m_goalColor = Color.green;


    public void UpdateActorCount(int i, string name)
    {
        if (m_actorsList == null)
        {
            m_actorsList = new List<string>();
        }

        m_actorCount += i;

        if (i > 0)
        {
            m_actorsList.Add(name);
        }
        else
        {
            m_actorsList.Remove(name);
        }
    }
    
    public void UpdateBuildRestrictedValue(bool value)
    {
        m_isBuildRestricted = value;
    }

    public void UpdateOccupancyDisplay(bool b)
    {
        m_isOccupied = b;
        if (m_isOccupied)
        {
            UpdateGridCellColor(m_occupiedColor);
        }
        else
        {
            UpdateGridCellColor(m_pathableColor);
        }
    }

    public void UpdateTempOccupancyDisplay(bool b)
    {
        m_isTempOccupied = b;
        if (m_isTempOccupied)
        {
            UpdateGridCellColor(m_tempOccupiedColor);
        }
        else
        {
            //To easily get the last known color.
            UpdateOccupancyDisplay(m_isOccupied);
        }
    }

    public void UpdateGoalDisplay(bool b)
    {
        m_isGoal = b;
        if (m_isGoal) UpdateGridCellColor(m_goalColor);
    }

    private void UpdateGridCellColor(Color color)
    {
        if (!m_gridCellObjMaterial)
        {
            m_gridCellObjMaterial = m_gridCellObj.GetComponent<Renderer>().material;
        }

        m_gridCellObjMaterial.color = color;
    }

    public void UpdateTempDirection(int i)
    {
        Vector3 direction = new Vector3();

        switch (i)
        {
            case 0:
                //Tile is north, face south.
                m_gridCellObj.transform.rotation = Quaternion.Euler(90, 0, 180);
                //Run south
                direction = new Vector3(0, 0, -1);
                break;
            case 1:
                //Tile is east, face west.
                m_gridCellObj.transform.rotation = Quaternion.Euler(90, 0, 90);
                //Run west
                direction = new Vector3(-1, 0, 0);
                break;
            case 2:
                //Tile is south, face north.
                m_gridCellObj.transform.rotation = Quaternion.Euler(90, 0, 0);
                //Run north
                direction = new Vector3(0, 0, 1);
                break;
            case 3:
                //Tile is west, face east.
                m_gridCellObj.transform.rotation = Quaternion.Euler(90, 0, 270);
                //Run east
                direction = new Vector3(1, 0, 0);
                break;
            default:
                break;
        }

        m_tempDirectionToNextCell = direction;
    }

    public void SetDirection()
    {
        m_directionToNextCell = m_tempDirectionToNextCell;
    }
}

[System.Serializable]
public class UnitPath
{
    public UnitSpawner m_unitSpawner;
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

    private Color m_lineRendererColorOn;
    private Color m_lineRendererColorOff;


    public void Setup()
    {
        m_path = AStar.GetExitPath(m_startPos, m_enemyGoalPos);

        //Define desired properties of the line.
        if (m_lineRenderer != null)
        {
            m_unitSpawner.OnActiveWaveSet += UnitSpawnActiveWaveSet;
            m_lineRenderer.lineRendererProperties = new TBLineRenderer();
            //m_lineRenderer.lineRendererProperties.linePoints = m_path.Count;
            Material instancedMaterial = new Material(GridManager.Instance.m_lineRendererMaterial);
            m_lineRenderer.lineRendererProperties.texture = instancedMaterial;
            m_lineRenderer.lineRendererProperties.lineWidth = 0.1f;
            ColorUtility.TryParseHtmlString("#b5770b", out Color colorOn);
            m_lineRendererColorOn = colorOn;
            ColorUtility.TryParseHtmlString("#4674A3", out Color colorOff);
            m_lineRendererColorOff = colorOff;
            m_lineRenderer.lineRendererProperties.startColor = m_lineRendererColorOff;
            m_lineRenderer.lineRendererProperties.endColor = m_lineRendererColorOff;
            m_lineRenderer.lineRendererProperties.axis = TBLineRenderer.Axis.Y;

            //Assign the properties.
            m_lineRenderer.SetLineRendererProperties();

            //UpdateExitPath();
            if (m_path == null)
            {
                //Debug.Log("Unit Path could not path to exit.");
                return;
            }

            m_lineRenderer.SetPoints(m_path);
        }
    }

    private void UnitSpawnActiveWaveSet(List<Creep> creepList)
    {
        if (creepList.Count > 0)
        {
            //Debug.Log($"Setting Line Renderer to {m_lineRendererColorOn}");
            m_lineRenderer.lineRendererProperties.startColor = m_lineRendererColorOn;
            m_lineRenderer.lineRendererProperties.endColor = m_lineRendererColorOn;
            m_lineRenderer.SetLineRendererColors();
        }
        else
        {
            //Debug.Log($"Setting Line Renderer to {m_lineRendererColorOff}");
            m_lineRenderer.lineRendererProperties.startColor = m_lineRendererColorOff;
            m_lineRenderer.lineRendererProperties.endColor = m_lineRendererColorOff;
            m_lineRenderer.SetLineRendererColors();
        }
    }

    void PreconstructedTowerClear()
    {
        //Clear the precon state for this Unit Path then set the path back to our saved LastGoodPath.
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

                /*if (m_lineRenderer != null)
                {
                    m_lineRenderer.SetPoints(m_path);
                }*/

                m_pathDirty = false;
            }
        }
    }

    void UpdateExitPath()
    {
        int shortestPathCount = Int32.MaxValue;
        m_hasPath = false;

        //Path from the UnitPath Start position (can be spawner OR exit, to each exit)
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

        if (!m_hasPath)
        {
            //Debug.Log("CANNOT BUILD.");
        }
    }

    public void DrawExitPathLine()
    {
        if (m_lineRenderer == null) return;
        List<Vector2Int> path = AStar.GetExitPath(m_startPos, m_enemyGoalPos);
        
        float length = 0;
        for (int i = 1; i < path.Count; i++)
        {
            length += Vector2Int.Distance(path[i - 1], path[i]);
        }

        length *= 2;
        m_lineRenderer.lineRendererProperties.texture.SetFloat("_Tiling", length);
        
        m_path = path;
        m_lineRenderer.SetPoints(path);
    }
}