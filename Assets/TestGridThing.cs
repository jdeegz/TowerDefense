using System.Collections.Generic;
using UnityEngine;

public class TestGridThing : MonoBehaviour
{
    public GameObject m_preconstructedTowerObj;
    private List<Cell> m_preconstructedTowerCells;
    private List<Cell> m_preconNeighborCells;
    public int m_width;
    public int m_height;
    public LayerMask m_buildSurface;

    private Camera m_mainCamera;
    private Vector2Int m_preconstructedTowerPos;
    private Material m_material;
    private List<GameObject> m_cellVisualsObjs;
    private List<GameObject> m_neighborVisualsObjs;

    private float m_preconxOffset;
    private float m_preconzOffset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        VariableLink();
        m_mainCamera = Camera.main;
        Vector3 scale = new Vector3(m_width, .2f, m_height);
        m_preconstructedTowerObj.transform.localScale = scale;
        m_material = m_preconstructedTowerObj.GetComponent<Renderer>().material;
        
        m_preconxOffset = m_width % 2 == 0 ? 0.5f : 0;
        m_preconzOffset = m_height % 2 == 0 ? 0.5f : 0;
    }

    // Update is called once per frame
    void Update()
    {
        PreconPeriodicTimer();

        PositionPrecon();

        HandlePreconMousePosition();
    }

    private void HandlePreconMousePosition()
    {
        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 100f, m_buildSurface))
        {
            //Raycast has hit the ground, round that point to the nearest int.
            Vector3 gridPos = raycastHit.point;

            //Convert hit point to 2d grid position
            gridPos.x += m_preconxOffset;
            gridPos.z += m_preconzOffset;

            Vector2Int newPos = Util.GetVector2IntFrom3DPos(gridPos);

            if (newPos == m_preconstructedTowerPos) return;

            m_preconstructedTowerPos = newPos;

            List<Cell> newCells = Util.GetCellsFromPos(m_preconstructedTowerPos, m_width, m_height);
            if (newCells != null)
            {
                m_preconstructedTowerCells = newCells;
                m_preconNeighborCells = GetNeighborCells(m_preconstructedTowerPos, m_width, m_height);
                GameplayManager.Instance.TriggerPreconBuildingMoved(newCells);
                CheckPreconRestrictions();
            }
        }
    }

    private void PositionPrecon()
    {
        //Define the new destination of the Precon Tower Obj. Offset the tower on Y.
        Vector3 moveToPosition = new Vector3(m_preconstructedTowerPos.x - m_preconxOffset, 0, m_preconstructedTowerPos.y - m_preconzOffset);

        //Position the precon Tower at the cursor position.
        m_preconstructedTowerObj.transform.position = Vector3.Lerp(m_preconstructedTowerObj.transform.position,
            moveToPosition, 20f * Time.unscaledDeltaTime);
    }

    private float m_preconPeriodTimeElapsed;
    private float m_preconPeriod = 0.1f;

    void PreconPeriodicTimer()
    {
        m_preconPeriodTimeElapsed += Time.deltaTime;
        
        if (m_preconPeriodTimeElapsed > m_preconPeriod)
        {
            CheckPreconRestrictions();
            m_preconPeriodTimeElapsed = 0;
        }
    }

    void CheckPreconRestrictions()
    {
        if (m_preconstructedTowerCells == null || m_preconNeighborCells == null) return;
        
        bool canPlace = false;
        bool canPath = false;

        canPlace = IsPlacementRestricted(m_preconstructedTowerCells);
        canPath = IsPathingRestricted(m_preconNeighborCells);
        PositionCellVisuals(m_preconstructedTowerPos, m_width, m_height);

        m_material.color = canPlace && canPath ? Color.blue : Color.red;
    }

    private bool IsPlacementRestricted(List<Cell> cells)
    {
        if (cells == null || cells.Count != m_width * m_height)
        {
            Debug.Log($"Invalid Position.");
            return false;
        }

        // Check the individual cells
        for (int i = 0; i < cells.Count; ++i)
        {
            Cell curCell = cells[i];

            //If we have actors on the cell.
            if (curCell.m_actorCount > 0)
            {
                Debug.Log($"Cannot Place: There are actors on this cell.");
                //m_pathRestrictedReason = m_UIStringData.m_buildRestrictedActorsOnCell;
                return false;
            }

            //If we're hovering on the exit cell
            if (m_preconstructedTowerPos == Util.GetVector2IntFrom3DPos(GameplayManager.Instance.m_enemyGoal.position))
            {
                Debug.Log($"Cannot Place: This is the exit cell.");
                return false;
            }

            if (curCell.m_isOccupied)
            {
                if (curCell.m_occupant != null && curCell.m_occupant.GetComponent<TowerBlueprint>() == null) // If we DO have a tower blueprint, we're ok placing here.
                {
                    Debug.Log($"Cannot Place: This cell is already occupied.");
                    //m_pathRestrictedReason = m_UIStringData.m_buildRestrictedOccupied;
                    return false;
                }
            }

            //If the currenct cell is build restricted (bridges, obelisk ground, pathways), not a valid spot.
            if (curCell.m_isBuildRestricted)
            {
                Debug.Log($"Cannot Place: This cell is build restricted.");
                //m_pathRestrictedReason = m_UIStringData.m_buildRestrictedRestricted;
                return false;
                
            }
        }

        return true;
    }

    public void PositionCellVisuals(Vector2Int pos, int width, int height)
    {
        Vector2Int bottomLeftCellPos = pos;
        bottomLeftCellPos.x -= width / 2;
        bottomLeftCellPos.y -= height / 2;

        if (m_cellVisualsObjs == null)
        {
            m_cellVisualsObjs = new List<GameObject>();
            for (int i = 0; i < width * height; ++i)
            {
                GameObject obj = Instantiate(m_preconstructedTowerObj, transform);
                obj.transform.localScale = new Vector3(.5f, .5f, .5f);
                m_cellVisualsObjs.Add(obj);
            }
        }

        int index = 0;
        for (int x = 0; x < width; ++x)
        {
            for (int z = 0; z < height; ++z)
            {
                int xPos = bottomLeftCellPos.x + x;
                int zPos = bottomLeftCellPos.y + z;
                Vector3 objPos = new Vector3(xPos, 0, zPos);
                m_cellVisualsObjs[index].transform.position = objPos;
                ++index;
            }
        }
    }

    public List<Cell> GetNeighborCells(Vector2Int pos, int width, int height)
    {
        List<Cell> neighborCells = new List<Cell>();
        Vector2Int bottomLeftCellPos = pos;
        bottomLeftCellPos.x -= (width + 2) / 2;
        bottomLeftCellPos.y -= (height + 2) / 2;

        int numberOfNeighbors = ((width + 2) * (height + 2)) - (width * height) - 4; // Minus 4 corners
        if (m_neighborVisualsObjs == null || m_neighborVisualsObjs.Count < numberOfNeighbors)
        {
            Debug.Log($"Creating {numberOfNeighbors} neighbor objects.");
            if (m_neighborVisualsObjs == null)
                m_neighborVisualsObjs = new List<GameObject>();

            while (m_neighborVisualsObjs.Count < numberOfNeighbors)
            {
                GameObject obj = Instantiate(m_preconstructedTowerObj, transform);
                obj.transform.localScale = new Vector3(.25f, .25f, .25f);
                m_neighborVisualsObjs.Add(obj);
            }
        }

        int index = 0;
        for (int x = 0; x < width + 2; ++x)
        {
            for (int z = 0; z < height + 2; ++z)
            {
                // Skip the middle area and corners
                if ((x > 0 && x < width + 1) && (z > 0 && z < height + 1)) continue; // Inside box
                if (x == 0 && z == 0) continue; // SW corner
                if (x == 0 && z == height + 1) continue; // NW corner
                if (x == width + 1 && z == 0) continue; // SE corner
                if (x == width + 1 && z == height + 1) continue; // NE corner

                // Calculate position for this neighbor
                Vector3 objPos = new Vector3(bottomLeftCellPos.x + x, 0, bottomLeftCellPos.y + z);

                if (index < m_neighborVisualsObjs.Count)
                {
                    m_neighborVisualsObjs[index].transform.position = objPos;

                    Cell neighborCell = Util.GetCellFrom3DPos(objPos);
                    if (neighborCell != null) neighborCells.Add(neighborCell);
                    index++;
                }
            }
        }

        return neighborCells;
    }

    public Vector2Int m_goalPointPos;
    public CastleController m_castleController;
    public List<UnitSpawner> m_unitSpawners;

    void VariableLink()
    {
        GameplayManager gameplayManager = GameplayManager.Instance;
        m_goalPointPos = gameplayManager.m_goalPointPos;
        m_castleController = gameplayManager.m_castleController;
        m_unitSpawners = gameplayManager.m_unitSpawners;
    }

    bool IsPathingRestricted(List<Cell> cells)
    {
        if (cells == null) return false;

        for (int i = 0; i < cells.Count; ++i)
        {
            Cell cell = cells[i];
            if (cell.m_isOccupied) continue;

            Debug.Log($"Neighbor cells {cell.m_cellPos} is unoccupied. Checking for path.");
            List<Vector2Int> testPath = AStar.GetExitPath(cell.m_cellPos, m_goalPointPos);

            if (testPath != null) continue;

            Debug.Log($"No path found from Neighbor: {cell.m_cellPos} to exit, checking for inhabited islands.");
            List<Cell> islandCells = new List<Cell>(AStar.FindIsland(cell, cells));

            Debug.Log($"Returning FALSE because an actor was found on a single-cell island.");
            if (islandCells.Count == 0 && cell.m_actorCount > 0) return false;

            foreach (Cell islandCell in islandCells)
            {
                Debug.Log($"Island Cell found: {islandCell.m_cellPos} and has actors: {islandCell.m_actorCount}.");
                if (islandCell.m_actorCount > 0)
                {
                    Debug.Log($"Cannot Place: {islandCells.Count} Island created, and Cell: {islandCell.m_cellPos} contains actors");
                    //m_pathRestrictedReason = m_UIStringData.m_buildRestrictedActorsInIsland;
                    return false;
                }
            }
        }

        // EXITS AND SPAWNERS
        // Check that the Grid to make sure exits can path to one another.
        int exitsPathable = 0;
        for (int x = 0; x < m_castleController.m_castleEntrancePoints.Count; ++x)
        {
            bool startObjPathable = false;
            GameObject startObj = m_castleController.m_castleEntrancePoints[x];
            Vector2Int startPos = Util.GetVector2IntFrom3DPos(startObj.transform.position);

            //Define the end
            for (int z = 0; z < m_castleController.m_castleEntrancePoints.Count; ++z)
            {
                GameObject endObj = m_castleController.m_castleEntrancePoints[z];
                Vector2Int endPos = Util.GetVector2IntFrom3DPos(endObj.transform.position);

                //Go next if they're the same.
                if (startObj == endObj)
                {
                    continue;
                }

                //Path from Start to End and exclude the Game's Goal cell.
                List<Vector2Int> testPath = AStar.FindExitPath(startPos, endPos, m_preconstructedTowerPos, m_goalPointPos);
                if (testPath != null)
                {
                    //If we do get a path, this exit is good. Increment and break out of this for loop to check other exits.
                    ++exitsPathable;
                    startObjPathable = true;
                    break;
                }
            }

            // We could not path to any other exit. So check to see if we can path to atleast one spawner.
            if (!startObjPathable)
            {
                for (int y = 0; y < m_unitSpawners.Count; ++y)
                {
                    Vector2Int spawnerPos = Util.GetVector2IntFrom3DPos(m_unitSpawners[y].GetSpawnPointTransform().position);
                    List<Vector2Int> testPath = AStar.FindExitPath(spawnerPos, startPos, m_preconstructedTowerPos, m_goalPointPos);
                    if (testPath != null)
                    {
                        // If we do get a path, this exit is good. Increment and break out of this for loop to check other exits.
                        Debug.Log($"Castle Exit: {startPos} was able to path to Spawner: {spawnerPos}.");
                        ++exitsPathable;
                        break;
                    }
                    else
                    {
                        Debug.Log($"Castle Exit: {startPos} was NOT able to path to Spawner: {spawnerPos}.");
                    }
                }
            }
        }

        //If the number of pathable exits is less than the number of exits, return false. One cannot path to another exit.
        if (exitsPathable < m_castleController.m_castleEntrancePoints.Count)
        {
            //Debug.Log($"An exit cannot path to another exit or spawner.");
            //m_pathRestrictedReason = m_UIStringData.m_buildRestrictedBlocksPath;
            return false;
        }


        return true;
    }
}