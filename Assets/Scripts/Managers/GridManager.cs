using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TechnoBabelGames;
using UnityEngine;
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
    public LayerMask m_groundLayer;

    private List<UnitPath> m_unitPaths;
    private List<Vector2Int> m_exits;
    private List<Vector2Int> m_spawners;
    private Vector2Int m_enemyGoalPos;
    private Vector2Int m_preconstructedTowerPos;
    private int m_previousPreconIndex;
    private List<Cell> m_previousPreconCells;
    private bool m_previousPreconOccupancy;
    public bool m_spawnPointsAccessible;
    private List<Cell> m_outOfBoundsSpawnCells;



    void Awake()
    {
        Instance = this;
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        GameplayManager.OnPreconBuildingMoved += PreconBuildingMoved;
        GameplayManager.OnPreconBuildingClear += PreconBuildingClear;
        m_gridVisualizerObj.SetActive(false);
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
        GameplayManager.OnPreconBuildingMoved -= PreconBuildingMoved;
        GameplayManager.OnPreconBuildingClear -= PreconBuildingClear;
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
                FloodFillGrid(m_gridCells, () => GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.CreatePaths));
                break;
            case GameplayManager.GameplayState.CreatePaths:
                BuildPathList();
                SetCellDirections();
                GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Setup);
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
            case GameplayManager.GameplayState.CutScene:
                break;
            case GameplayManager.GameplayState.Victory:
                break;
            case GameplayManager.GameplayState.Defeat:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }

    public List<Cell> GetOutOfBoundsSpawnCells()
    {
        return m_outOfBoundsSpawnCells;
    }

    public void FloodFillGrid(Cell[] gridCells, Action callback)
    {
        //Debug.Log($"Begin Flood Fill.");
        int goalCellIndex = Util.GetCellIndex(GameplayManager.Instance.m_enemyGoal.position);
        Cell goalCell = gridCells[goalCellIndex];
        Dictionary<Cell, Cell> nextTileToGoal = new Dictionary<Cell, Cell>();
        Queue<Cell> frontier = new Queue<Cell>();
        List<Cell> visited = new List<Cell>();

        //How many spawners are in the game? What are their spawnPoint cells?
        List<Cell> spawnPointCells = new List<Cell>();
        foreach (EnemySpawner spawner in GameplayManager.Instance.m_enemySpawners)
        {
            spawnPointCells.Add(Util.GetCellFrom3DPos(spawner.GetSpawnPointTransform().position));
        }

        int spawnPointCellsFound = 0;

        frontier.Enqueue(goalCell);

        while (frontier.Count > 0)
        {
            Cell curCell = frontier.Dequeue();

            List<Cell> neighborCells = Util.GetNeighborCells(curCell, gridCells);
            for (var i = 0; i < neighborCells.Count; i++)
            {
                //Break out if the array entry for this neighbor is null
                if (neighborCells[i] == null) continue;

                var neighbor = neighborCells[i];
                if (visited.Contains(neighbor) == false && frontier.Contains(neighbor) == false)
                {
                    if (!neighbor.m_isOccupied && !neighbor.m_isTempOccupied || neighbor.m_isUpForSale)
                    {
                        //Get the direction of the neighbor to the curCell.
                        
                        neighbor.UpdateTempDirection(curCell.m_cellPos);
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

        //If we've found all the spawn points, this is good, we can update each path.
        m_spawnPointsAccessible = spawnPointCellsFound == spawnPointCells.Count;
        
        if (!m_spawnPointsAccessible)
        {
            //Debug.Log($"We did not find all spawn points when flood filling from the exit.");
            return;
        }

        UpdateUnitPaths();

        if (callback != null)
        {
            callback.Invoke();
        }
    }

    private void UpdateUnitPaths()
    {
        if (m_unitPaths == null || m_unitPaths.Count == 0) return;

        //Debug.Log($"Updating Unit Paths");
        
        foreach (UnitPath unitPath in m_unitPaths)
        {
            //Break this into its own function outside of flood fill.
            unitPath.DrawExitPathLine();
        }
    }

    private void SetCellDirections()
    {
        //Lock in the direction of all the cells.
        foreach (Cell cell in m_gridCells)
        {
            cell.SetDirection();
        }

        //Debug.Log($"Cell Directions Set.");
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
                cell.UpdateOccupancy(false);

                HitTestCellForGround(m_gridcellObjects[index].transform.position, cell);
            }
        }

        m_gridCellObj.SetActive(false);

        //Debug.Log("Grid Built.");
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.PlaceObstacles);

        //Debug.Log("Obstacles Placed.");
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.FloodFillGrid);
    }

    void PreconBuildingMoved(List<Cell> cells)
    {
        if (m_previousPreconCells == null) m_previousPreconCells = new List<Cell>();

        foreach (Cell cell in m_previousPreconCells)
        {
            cell.UpdateTempOccupancyDisplay(false);
        }

        foreach (Cell cell in cells)
        {
            // There could be cells that flip off then back on. Is that an optimization necessary?
            cell.UpdateTempOccupancyDisplay(true);
        }

        m_previousPreconCells = cells;

        FloodFillGrid(m_gridCells, null);
    }

    void PreconBuildingClear()
    {
        RevertBuildingCellTempChanges();

        FloodFillGrid(m_gridCells, null);
    }

    void RevertBuildingCellTempChanges()
    {
        if (m_previousPreconCells == null) return;

        foreach (Cell cell in m_previousPreconCells)
        {
            cell.UpdateTempOccupancyDisplay(false);
        }
    }

    public void PreviewSellBuildingTempChanges(List<Cell> cells)
    {
        if (m_previousPreconCells == null) m_previousPreconCells = new List<Cell>();

        foreach (Cell cell in cells)
        {
            cell.m_isUpForSale = true;
        }

        m_previousPreconCells = cells;

        FloodFillGrid(m_gridCells, null);
    }

    void RevertPreviewSellBuildingCellTempChanges()
    {
        if (m_previousPreconCells == null) return;

        foreach (Cell cell in m_previousPreconCells)
        {
            cell.m_isUpForSale = false;
        }
    }

    public void SellBuildingClear()
    {
        RevertPreviewSellBuildingCellTempChanges();

        FloodFillGrid(m_gridCells, null);
    }

    public void RefreshGrid()
    {
        if (GameplayManager.Instance.m_interactionState == GameplayManager.InteractionState.PreconstructionTower)
        {
            List<Cell> previousPreconCells = new List<Cell>(m_previousPreconCells);
            RevertBuildingCellTempChanges();

            //Debug.Log($"GridManager: Flood Filling and Setting directions.");
            FloodFillGrid(m_gridCells, SetCellDirections);

            //I think I could also cheat this by setting GameplayManagers m_preconstructedTowerPos to Vector2Int.zero, which will get flagged as the 'new pos' invoking its action.
            //Debug.Log($"GridManager: Returning to Precon Tower temp changes.");
            //Debug.Log($"Moving precon tower back to cell index: {preconIndex}");
            PreconBuildingMoved(previousPreconCells);
            //PreconTowerMoved(m_gridCells[preconIndex].m_cellPos);
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

        /*//Shoot the ray and collect all the hits.
        RaycastHit[] hits = Physics.RaycastAll(ray, 0.22f);

        //If we recieved no hits from our cast, exit.
        if (hits.Length == 0) return;

        //If we received hits, check to see if water was the last one hit.
        if (IsLayerInMask(hits[0].transform.gameObject.layer, m_waterLayer))
        {
            cell.UpdateOccupancy(true);
        }*/

        RaycastHit hit;

        // Perform the raycast using the layer mask
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, m_groundLayer))
        {
            // Ray hit something on the specified layer
            //Debug.Log("Hit object: " + hit.collider.gameObject.name);

            if (hit.collider.gameObject.CompareTag("Enemy"))
            {
                if (m_outOfBoundsSpawnCells == null) m_outOfBoundsSpawnCells = new List<Cell>();
                cell.UpdateBuildRestrictedValue(true);
                m_outOfBoundsSpawnCells.Add(cell);
                //Debug.Log($"and it's an enemy object.");
            }
        }
        else
        {
            cell.SetIsOutOfBounds(true);
        }
    }

    bool IsLayerInMask(int layer, LayerMask layerMask)
    {
        // Use the LayerMask method to check if the layer is in the layer mask
        return layerMask == (layerMask | (1 << layer));
    }

    public void BuildPathList()
    {
        m_unitPaths = new List<UnitPath>();
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
        foreach (EnemySpawner spawner in GameplayManager.Instance.m_enemySpawners)
        {
            m_spawners.Add(Util.GetVector2IntFrom3DPos(spawner.GetSpawnPointTransform().position));
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
            //Debug.Log($"Added Unit Path for {obj.name}");
        }

        // Dont make paths for survival mode.
        if (GameplayManager.Instance.m_gameplayData.m_gameMode == MissionGameplayData.GameMode.Standard)
        {
            //Create Spawners UnitPaths
            foreach (StandardSpawner spawner in GameplayManager.Instance.m_enemySpawners)
            {
                Vector2Int start = Util.GetVector2IntFrom3DPos(spawner.GetSpawnPointTransform().position);
                UnitPath unitPath = new UnitPath();
                unitPath.m_standardSpawner = spawner;
                unitPath.m_startPos = start;
                unitPath.m_isExit = false;
                unitPath.m_exits = m_exits;
                unitPath.m_spawners = m_spawners;
                unitPath.m_enemyGoalPos = m_enemyGoalPos;
                unitPath.m_displayThisPath = true;
                unitPath.Setup();
                m_unitPaths.Add(unitPath);
                //Debug.Log($"Added Unit Path for {spawner.gameObject.name}");
            }
        }
    }
}

[System.Serializable]
public class Cell
{
    //Cell Data
    public bool m_isGoal;
    public bool m_isOccupied;
    public bool m_isOutOfBounds;
    public bool m_isTempOccupied;
    public bool m_isUpForSale;
    public bool m_isBuildRestricted;

    public GameObject m_occupant;
    public ResourceNode m_cellResourceNode;
    public List<string> m_actorsList;

    public int m_actorCount;
    public int m_cellIndex;
    public Vector2Int m_cellPos;
    public Cell m_portalConnectionCell;

    public bool m_canPathNorth = true;
    public bool m_canPathEast = true;
    public bool m_canPathSouth = true;
    public bool m_canPathWest = true;

    //Visualizer
    public GameObject m_gridCellObj;
    private Material m_gridCellObjMaterial;
    private Color m_pathableColor = Color.cyan;
    private Color m_occupiedColor = Color.yellow;
    private Color m_tempOccupiedColor = Color.magenta;
    private Color m_goalColor = Color.green;
    
    public Direction m_directionToNextCell = Direction.Unset;
    public Direction m_tempDirectionToNextCell = Direction.Unset;

    public enum Direction
    {
        Unset,
        North,
        East,
        South,
        West,
        Portal
    }
    
    public Vector2Int GetDirectionVector(Direction direction)
    {
        switch (direction)
        {
            case Direction.Unset:
                return new Vector2Int(0,0);
            
            case Direction.North:
                return new Vector2Int(0,1);
            
            case Direction.East:
                return new Vector2Int(1,0);
            
            case Direction.South:
                return new Vector2Int(0,-1);
            
            case Direction.West:
                return new Vector2Int(-1,0);
            
            case Direction.Portal:
                return new Vector2Int(0,0);
            
            default:
                return new Vector2Int(99,99);
        }
    }

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

    public void UpdateOccupancy(bool b)
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

    public void SetPortalConnectionCell(Cell cell)
    {
        m_portalConnectionCell = cell;
    }

    public void SetIsOutOfBounds(bool b)
    {
        m_isOutOfBounds = b;
        
        UpdateOccupancy(b);
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
            UpdateOccupancy(m_isOccupied);
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

    public void UpdateTempDirection(Vector2Int destinationCellPos)
    {
        // Subtract our position from the cell we wish to direct towards.
        Vector2Int directionPos = destinationCellPos - m_cellPos;

        Direction direction = Direction.Unset;
        
        if (directionPos == new Vector2Int(0, 1))
        {
            //Tile is north, face south.
            m_gridCellObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            direction = Direction.North;
        }
        else if (directionPos == new Vector2Int(1, 0))
        {
            //Tile is east, face west.
            m_gridCellObj.transform.rotation = Quaternion.Euler(90, 0, 270);
            direction = Direction.East;
        }
        else if (directionPos == new Vector2Int(0, -1))
        {
            //Tile is south, face north.
            m_gridCellObj.transform.rotation = Quaternion.Euler(90, 0, 180);
            direction = Direction.South;
        }
        else if (directionPos == new Vector2Int(-1, 0))
        {
            //Tile is west, face east.
            m_gridCellObj.transform.rotation = Quaternion.Euler(90, 0, 90);
            direction = Direction.West;
        }
        else if (Math.Abs(directionPos.x) + Math.Abs(directionPos.y) > 1)
        {
            direction = Direction.Portal;
        }

        m_tempDirectionToNextCell = direction;
    }


    public void SetDirection()
    {
        m_directionToNextCell = m_tempDirectionToNextCell;
        m_tempDirectionToNextCell = Direction.Unset;
    }
}

[System.Serializable]
public class UnitPath
{
    public StandardSpawner m_standardSpawner;
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
    public bool m_displayThisPath;

    private Color m_lineRendererColorOn;
    private Color m_lineRendererColorOff;
    private List<TBLineRendererComponent> m_lineRenderers;

    public void Setup()
    {
        m_path = AStar.GetExitPath(m_startPos, m_enemyGoalPos);
        //UpdateExitPath();
        if (m_path == null)
        {
            //Debug.Log("Unit Path could not path to exit.");
            return;
        }

        if (m_displayThisPath)
        {
            // We want to display this path.
            m_standardSpawner.OnActiveWaveSet += StandardSpawnActiveWaveSet;
            CreateLineRenderer();
            DrawExitPathLine();
            DisplayAsSpawnerActive(false);
        }
    }

    private void CreateLineRenderer()
    {
        if (m_lineRenderers == null)
        {
            m_lineRenderers = new List<TBLineRendererComponent>();
        }

        TBLineRendererComponent newLine = new GameObject("Line Segment").AddComponent<TBLineRendererComponent>();
        newLine.transform.parent = m_standardSpawner.transform;
        m_lineRenderers.Add(newLine);

        

        newLine.lineRendererProperties = new TBLineRenderer();
        Material instancedMaterial = new Material(GridManager.Instance.m_lineRendererMaterial);
        newLine.lineRendererProperties.texture = instancedMaterial;
        newLine.lineRendererProperties.lineWidth = 0.1f;
        ColorUtility.TryParseHtmlString("#eca816", out Color colorOn);
        ColorUtility.TryParseHtmlString("#9fa7af", out Color colorOff);
        newLine.lineRendererProperties.roundedCorners = true;
        newLine.lineRendererProperties.startColor = colorOn;
        newLine.lineRendererProperties.endColor = colorOff;
        newLine.lineRendererProperties.axis = TBLineRenderer.Axis.Y;

        //Assign the properties.
        newLine.SetLineRendererProperties();
    }

    private void DisplayAsSpawnerActive(bool value)
    {
        foreach (TBLineRendererComponent lineRenderer in m_lineRenderers)
        {
            lineRenderer.SetSpawnerActive(value);
        }
    }

    private void StandardSpawnActiveWaveSet(CreepWave activeWave)
    {
        DisplayAsSpawnerActive(activeWave.m_creeps != null && activeWave.m_creeps.Count > 0);
    }

    void PreconstructedTowerClear()
    {
        //Clear the precon state for this Unit Path then set the path back to our saved LastGoodPath.
        if (m_preconState)
        {
            m_preconState = false;
            m_path = new List<Vector2Int>(m_lastGoodPath);

            if (m_displayThisPath)
            {
                // We want to display this path.
                DrawExitPathLine();
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
        if (!m_displayThisPath) return;

        //Debug.Log($"Drawing Exit Path Line from {m_startPos} - {m_enemyGoalPos}.");

        List<Vector2Int> path = AStar.GetExitPath(m_startPos, m_enemyGoalPos);

        /*Debug.Log($"Got an exit path of count: {path.Count}");
        Debug.Log($"Drawing Path from {path[0]} to {path.Last()}");*/

        int pathCount = 0;
        List<Vector2Int> curPath = new List<Vector2Int>();
        for (int i = 1; i < path.Count; i++)
        {
            curPath.Add(path[i]);
            Cell curCell = Util.GetCellFromPos(path[i]);
            m_lineRenderers[pathCount].gameObject.SetActive(true);
            if (curCell.m_portalConnectionCell != null && curCell.m_tempDirectionToNextCell == Cell.Direction.Portal) 
            {
                m_lineRenderers[pathCount].SetPoints(curPath);
                ++pathCount;

                //Create a new list to start adding positions to. If there are no line renderers to use, add one to the game ojbect, and then add it to the pool.
                if (pathCount == m_lineRenderers.Count)
                {
                    //Debug.Log($"Preparing a new Line Renderer.");
                    CreateLineRenderer();
                }

                curPath = new List<Vector2Int>();
            }
        }


        m_lineRenderers[pathCount].SetPoints(curPath);
        ++pathCount;

        //If we're done, and there are active linerenderers, disable them.
        //Debug.Log($"Line Renderers list is null: {m_lineRenderers == null}");

        for (int i = pathCount; i < m_lineRenderers.Count; ++i)
        {
            m_lineRenderers[i].gameObject.SetActive(false);
            //Debug.Log($"Disabling the Line Renderer Component {pathCount} of {m_lineRenderers.Count}");
        }


        m_path = path;
    }
}