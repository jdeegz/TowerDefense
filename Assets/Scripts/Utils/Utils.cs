﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Coffee.UIEffects;
using JetBrains.Annotations;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public static class Util
{
    public static Vector2Int[] m_directions = new Vector2Int[]
    {
        new Vector2Int(-1, -1), // Top-left
        new Vector2Int(0, -1), // Top
        new Vector2Int(1, -1), // Top-right
        new Vector2Int(-1, 0), // Left
        new Vector2Int(1, 0), // Right
        new Vector2Int(-1, 1), // Bottom-left
        new Vector2Int(0, 1), // Bottom
        new Vector2Int(1, 1) // Bottom-right
    };

    public static Vector2Int[] m_cardinalDirections = new Vector2Int[]
    {
        new Vector2Int(0, -1), // Top
        new Vector2Int(-1, 0), // Left
        new Vector2Int(1, 0), // Right
        new Vector2Int(0, 1), // Bottom
    };

    public static List<Vector2Int> GetBoxAround3x3Grid(Vector2Int center)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        // Loop through the 5x5 grid centered around the target
        for (int x = -2; x <= 2; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                // Get the current grid cell
                Vector2Int currentCell = new Vector2Int(center.x + x, center.y + z);

                // Skip the inner 3x3 grid (which would be from -1 to 1 on both axes)
                if (Mathf.Abs(x) <= 1 && Mathf.Abs(z) <= 1)
                    continue;

                // Add outer cells to the list
                result.Add(currentCell);
            }
        }

        return result;
    }

    public static IEnumerator RebuildCoroutine(RectTransform rectTransform)
    {
        yield return null; // Wait one frame
        
        var contentSizeFitter = rectTransform.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = false;
            yield return null; // Wait another frame
            contentSizeFitter.enabled = true;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    public static bool IsAdjacentDiagonal(Vector2Int cellA, Vector2Int cellB)
    {
        //Debug.Log($"IsAdjacentDiagonal checking cells: {cellA} x {cellB}.");
        int dx = Mathf.Abs(cellA.x - cellB.x);
        int dy = Mathf.Abs(cellA.y - cellB.y);

        return Mathf.Max(dx, dy) == 1;
    }
    
    public static bool IsAdjacentDiagonalOrLess(Vector2Int cellA, Vector2Int cellB)
    {
        //Debug.Log($"IsAdjacentDiagonalOrLess checking cells: {cellA} x {cellB}.");
        int dx = Mathf.Abs(cellA.x - cellB.x);
        int dy = Mathf.Abs(cellA.y - cellB.y);

        return Mathf.Max(dx, dy) <= 1;
    }

    public static List<Cell> GetEmptyCellsAtDistance(Vector2Int center, int distance)
    {
        List<Cell> emptyCells = null;

        for (int x = -distance; x <= distance; x++)
        {
            for (int z = -distance; z <= distance; z++)
            {
                Vector2Int currentPos = new Vector2Int(center.x + x, center.y + z);

                if (!IsWithinBounds(currentPos.x, currentPos.y)) continue;

                if (Mathf.Abs(x) < distance && Mathf.Abs(z) < distance) continue;

                Cell cell = GetCellFromPos(currentPos);
                if (!cell.m_isOccupied)
                {
                    if (emptyCells == null) emptyCells = ListPool<Cell>.Get();
                    emptyCells.Add(cell);
                }
            }
        }

        return emptyCells; // Don't release it here! The caller must handle it.
    }

    private static int[] m_dX = { 0, 1, 0, -1 };
    private static int[] m_dY = { 0, 1, -1, 0 };

    public static Vector2Int FindNearestValidCellPos(Vector3 worldPos)
    {
        Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
        gridPos.x = Mathf.Clamp(gridPos.x, 0, GridManager.Instance.m_gridWidth - 1);
        gridPos.y = Mathf.Clamp(gridPos.y, 0, GridManager.Instance.m_gridHeight - 1);

        Cell cell = GetCellFromPos(gridPos);
        if (cell != null) return cell.m_cellPos;

        int step = 1;
        int longestAxis = Mathf.Max(GridManager.Instance.m_gridHeight, GridManager.Instance.m_gridWidth);
        while (step <= longestAxis)
        {
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < step; ++j)
                {
                    gridPos.x += m_dX[i];
                    gridPos.y += m_dY[i];

                    gridPos.x = Mathf.Clamp(gridPos.x, 0, GridManager.Instance.m_gridWidth - 1);
                    gridPos.y = Mathf.Clamp(gridPos.y, 0, GridManager.Instance.m_gridHeight - 1);

                    cell = GetCellFromPos(gridPos);
                    if (cell != null) return cell.m_cellPos;
                }
            }

            ++step;
        }

        return gridPos;
    }

    public static Vector2Int FindNearestValidCellPos(Vector3 worldPos, Vector2Int objectSize)
    {
        Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));

        //Expected outcomes: 1x1 = 0,0 | 2x2 = 1,1 | 3x3 = 1,1
        Vector2Int buildingPadding = new Vector2Int((int)Math.Floor(objectSize.x / 2.0), (int)Math.Floor(objectSize.y / 2.0));

        //Debug.Log($"Find Nearest Valid Cell Pos: {worldPos}, Building Size: {objectSize}, Building Padding: {buildingPadding}");

        gridPos.x = Mathf.Clamp(gridPos.x, 0 + buildingPadding.x, GridManager.Instance.m_gridWidth - buildingPadding.x);
        gridPos.y = Mathf.Clamp(gridPos.y, 0 + buildingPadding.y, GridManager.Instance.m_gridHeight - buildingPadding.y);

        Cell cell = GetCellFromPos(gridPos);
        if (cell != null) return cell.m_cellPos;

        int step = 1;
        int longestAxis = Mathf.Max(GridManager.Instance.m_gridHeight, GridManager.Instance.m_gridWidth);
        while (step <= longestAxis)
        {
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < step; ++j)
                {
                    gridPos.x += m_dX[i];
                    gridPos.y += m_dY[i];

                    gridPos.x = Mathf.Clamp(gridPos.x, 0, GridManager.Instance.m_gridWidth - 1);
                    gridPos.y = Mathf.Clamp(gridPos.y, 0, GridManager.Instance.m_gridHeight - 1);

                    cell = GetCellFromPos(gridPos);
                    if (cell != null) return cell.m_cellPos;
                }
            }

            ++step;
        }

        return gridPos;
    }

    public static Vector3 GetRandomPosition(Vector3 objPosition, Vector3 offset)
    {
        //Position
        objPosition += Vector3.Lerp(-offset, offset, UnityEngine.Random.value);
        return objPosition;
    }

    public static Quaternion GetRandomRotation(Quaternion objRotation, Vector3 offset)
    {
        //Rotation
        objRotation *= Quaternion.Euler(Vector3.Lerp(-offset, offset, UnityEngine.Random.value));
        return objRotation;
    }

    public static Vector3 GetRandomScale(Vector3 objScale, Vector3 offset)
    {
        //Scale
        objScale += Vector3.Lerp(-offset, offset, UnityEngine.Random.value);
        return objScale;
    }

    public static Vector3 RoundVectorToInt(Vector3 vector)
    {
        return new Vector3(Mathf.FloorToInt(vector.x + 0.5f), Mathf.FloorToInt(vector.y), Mathf.FloorToInt(vector.z + 0.5f));
    }


    public static List<Cell> GetCellsInRadius(int centerX, int centerY, int radius)
    {
        List<Cell> cellsWithinRadius = new List<Cell>();

        int x = centerX;
        int y = centerY;
        int dx = 0;
        int dy = -1;

        for (int i = 0; i < (2 * radius + 1) * (2 * radius + 1); i++)
        {
            if (Math.Abs(x - centerX) <= radius && Math.Abs(y - centerY) <= radius)
            {
                // Convert the 2D coordinates back to a 1D index
                int index = y * GridManager.Instance.m_gridWidth + x;

                // Check if the current cell is within the specified radius
                cellsWithinRadius.Add(GridManager.Instance.m_gridCells[index]);
            }

            if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
            {
                // Change direction in the spiral
                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                {
                    int temp = dx;
                    dx = -dy;
                    dy = temp;
                }
            }

            x += dx;
            y += dy;
        }

        return cellsWithinRadius;
    }

    public static List<Cell> GetCellsInRadius(Cell startCell, int maxDistance)
    {
        List<Cell> cellsWithinRadius = new List<Cell>();

        int gridWidth = GridManager.Instance.m_gridWidth;

        int startX = startCell.m_cellPos.x;
        int startY = startCell.m_cellPos.y;

        // List to keep track of visited nodes
        HashSet<Cell> visited = new HashSet<Cell>();

        Queue<(int, int, int)> queue = new Queue<(int, int, int)>();

        // Check if the starting point is within the grid and has the original value
        Debug.Log($"Starting Flood Fill");

        if (IsWithinBounds(startX, startY))
        {
            // Enqueue the starting point with distance 0
            queue.Enqueue((startX, startY, 0));

            while (queue.Count > 0)
            {
                var (x, y, distance) = queue.Dequeue();
                Cell curCell = GridManager.Instance.m_gridCells[x * gridWidth + y];


                // Check if the distance exceeds the maximum distance
                if (distance > maxDistance)
                {
                    Debug.Log($"Stopping Flood Fill, max distance of {distance} reached.");
                    break; // Stop filling if the distance exceeds the limit
                }

                // Check if the current point is within the grid and has the original value
                if (IsWithinBounds(x, y) && !visited.Contains(curCell))
                {
                    Debug.Log($"Added a cell to list.");
                    cellsWithinRadius.Add(curCell);

                    // Mark the current node as visited
                    if (!visited.Add(curCell))
                    {
                        continue;
                    }

                    // Enqueue neighboring points with increased distance
                    queue.Enqueue((x + 1, y, distance + 1));
                    queue.Enqueue((x - 1, y, distance + 1));
                    queue.Enqueue((x, y + 1, distance + 1));
                    queue.Enqueue((x, y - 1, distance + 1));
                }
            }

            Debug.Log($"Flood fill Queue complete.");
        }

        Debug.Log($"Returning flood fill list.");
        return cellsWithinRadius;
    }

    static bool IsWithinBounds(int x, int y)
    {
        bool valid = x >= 0 && x < GridManager.Instance.m_gridWidth && y >= 0 && y < GridManager.Instance.m_gridHeight;
        //Debug.Log($"Cell valid: {valid}");
        return valid;
    }


    public static bool CellIsBlocked(int start_x, int start_y, int end_x, int end_y)
    {
        /*var deltaCol = System.Math.Abs(end_x - start_x);
        //If start_x < end_x, stepCol is 1, else -1
        var stepCol = start_x < end_x ? 1 : -1;

        var deltaRow = System.Math.Abs(end_y - start_y);
        var stepRow = start_y < end_y ? 1 : -1;

        //Finds out which direction to go first (largest first)
        var error = (deltaCol > deltaRow ? deltaCol : -deltaRow) / 2;
        var wallFound = false;
        while (true)
        {
            // Ignore cells that are outside of the graph.

            if (start_x >= 0
                && start_x < gridManager.gridCells.GetLength(0)
                && start_y >= 0
                && start_y < gridManager.gridCells.GetLength(1)
            )


                if (start_y == end_y && start_x == end_x)
                {
                    break;
                }

            var errorTemp = error;
            if (errorTemp > -deltaCol)
            {
                error -= deltaRow;
                start_x += stepCol;
            }

            if (errorTemp < deltaRow)
            {
                error += deltaCol;
                start_y += stepRow;
            }

            {
                Vector2 pos = new Vector2(start_x, start_y);
                var gridCell = GetCellFromPos(pos);
                if (gridCell == null)
                {
                    return true;
                }

                if (wallFound)
                {
                    wallFound = false;
                    return true;
                }

                if (gridCell.GetComponent<Cell>().isBlocker)
                {
                    wallFound = true;
                }
            }
        }*/

        return false;
    }

    /*public static GameObject GetTargetForProjectile(int start_x, int start_y, int end_x, int end_y)
    {
        var deltaCol = System.Math.Abs(end_x - start_x);
        //If start_x < end_x, stepCol is 1, else -1
        var stepCol = start_x < end_x ? 1 : -1;

        var deltaRow = System.Math.Abs(end_y - start_y);
        var stepRow = start_y < end_y ? 1 : -1;

        //Finds out which direction to go first (largest first)
        var error = (deltaCol > deltaRow ? deltaCol : -deltaRow) / 2;


        while (true)
        {
            // Ignore cells that are outside of the graph.

            if (start_x >= 0
                && start_x < gridManager.gridCells.GetLength(0)
                && start_y >= 0
                && start_y < gridManager.gridCells.GetLength(1)
            )


                if (start_y == end_y && start_x == end_x)
                {
                    break;
                }

            var errorTemp = error;
            if (errorTemp > -deltaCol)
            {
                error -= deltaRow;
                start_x += stepCol;
            }

            if (errorTemp < deltaRow)
            {
                error += deltaCol;
                start_y += stepRow;
            }

            {
                Vector2 pos = new Vector2(start_x, start_y);
                var gridCell = GetCellFromPos(pos);

                if (gridCell.GetComponent<Cell>().isBlocker)
                {
                    return gridCell;
                }
            }
        }

        return GetCellFromPos(new Vector2(end_x, end_y));
    }*/

    /*public static Cell GetCellOfObj(GameObject obj)
    {
        int x = (int) obj.transform.position.x;
        int y = (int) obj.transform.position.y;

        if (x < 0 || x > gridManager.m_gridCells.GetLength(0) - 1)
        {
            return null;
        }

        if (y < 0 || y > gridManager.m_gridCells.GetLength(1) - 1)
        {
            return null;
        }

        Cell onCell = gridManager.m_gridCells[x, y];

        return onCell;
    }*/

    /*public static List<GameObject> GetClosestCells(GameObject obj, int cellsNeeded)
    {
        List<GameObject> cellList = new List<GameObject>(0);

        //Get cells of distance i from object.
        for (int i = 1; cellList.Count + 1 < cellsNeeded; i++)
        {
            foreach (GameObject cellObj in GetCellsInRange(obj, gridManager.gridCells, i))
            {
                Cell cell = cellObj.GetComponent<Cell>();

                //Do not add the cell if the cell is blocked.
                //if the cell has loot
                //if the cell has a character
                //if the cell is the same as the unit's cell
                //if the cell is already in the list
                if (!cell.isBlocker &&
                    //!cell.loot &&
                    !cell.m_residentTower &&
                    GetCellOfObj(obj) != cell &&
                    !cellList.Contains(cellObj))
                {
                    cellList.Add(cellObj);

                    if (cellList.Count == cellsNeeded)
                    {
                        return cellList;
                    }
                }
            }
        }

        return cellList;
    }*/

    /*public static List<GameObject> GetUnitsInRange(GameObject obj, int range)
    {
        List<GameObject> residentsInRange = new List<GameObject>();

        foreach (GameObject cell in GetCellsInRange(obj, gridManager.gridCells, range))
        {
            Cell cellConfig = cell.GetComponent<Cell>();


            if (cellConfig.m_residentTower != null && cellConfig.m_residentTower != obj)
            {
                residentsInRange.Add(cellConfig.m_residentTower);
            }
        }

        return residentsInRange;
    }

    public static List<GameObject> GetInteractablesInRange(GameObject obj, int range)
    {
        List<GameObject> interactablesInRange = new List<GameObject>();
        foreach (GameObject cell in GetCellsInRange(obj, gridManager.gridCells, range))
        {
            Cell cellConfig = cell.GetComponent<Cell>();

            if (cellConfig.interactable)
            {
                interactablesInRange.Add(cellConfig.interactable);
            }
        }

        return interactablesInRange;
    }*/

    public static int GetCellIndex(Vector3 pos)
    {
        Vector2Int vector2pos = GetVector2IntFrom3DPos(pos);
        return vector2pos.y * GridManager.Instance.m_gridWidth + vector2pos.x;
    }

    public static int GetCellIndex(Vector2Int pos)
    {
        return pos.y * GridManager.Instance.m_gridWidth + pos.x;
    }

    public static Cell GetCellFromPos(Vector2Int pos)
    {
        int x = pos.x;
        int z = pos.y;

        //Check we're within the grid width
        if (x < 0 || x >= GridManager.Instance.m_gridWidth)
        {
            //Debug.Log("X not within grid bounds.");
            return null;
        }

        //Check we're within the grid height
        if (z < 0 || z >= GridManager.Instance.m_gridHeight)
        {
            //Debug.Log("Z not within grid bounds.");
            return null;
        }

        int index = z * GridManager.Instance.m_gridWidth + x;
        //Debug.Log("Request Cell at: " + x + "," + z + " Index of: " + index);

        return GridManager.Instance.m_gridCells[index];
    }

    public static List<Cell> GetCellsFromPos(Vector2Int pos, int width, int height)
    {
        List<Cell> cellsFromPos = new List<Cell>();
        Vector2Int bottomLeftCellPos = pos;
        bottomLeftCellPos.x -= width / 2;
        bottomLeftCellPos.y -= height / 2;

        for (int x = 0; x < width; ++x)
        {
            for (int z = 0; z < height; ++z)
            {
                int xPos = bottomLeftCellPos.x + x;
                int zPos = bottomLeftCellPos.y + z;

                //Check we're within the grid width
                if (xPos < 0 || xPos >= GridManager.Instance.m_gridWidth)
                {
                    Debug.Log($"GetCellsFromPos: {new Vector2(xPos, zPos)} -- X not within grid bounds.");
                    return null;
                }

                //Check we're within the grid height
                if (zPos < 0 || zPos >= GridManager.Instance.m_gridHeight)
                {
                    Debug.Log($"GetCellsFromPos: {new Vector2(xPos, zPos)} -- Z not within grid bounds.");
                    return null;
                }

                Cell cell = GetCellFromPos(new Vector2Int(xPos, zPos));

                // Disabling out of bounds check 2/11/2025
                // Implementing precon changes that allow the obj to appear over water.
                /*if (cell.m_isOutOfBounds)
                {
                    Debug.Log($"Cell out of bounds.");
                    return null;
                }*/


                //Debug.Log($"GetCellsFromPos: Adding {cell.m_cellPos}.");
                cellsFromPos.Add(cell);
            }
        }

        return cellsFromPos;
    }

    public static List<Cell> GetCellsFromPos(Vector3 pos, int width, int height)
    {
        // Convert Object position to Vector2Int. What will this return?
        Vector2Int bottomLeftCellPos = GetCellFrom3DPos(pos).m_cellPos;
        bottomLeftCellPos.x -= width / 2;
        bottomLeftCellPos.y -= height / 2;

        List<Cell> cellsFromPos = new List<Cell>();
        for (int x = 0; x < width; ++x)
        {
            for (int z = 0; z < height; ++z)
            {
                int xPos = bottomLeftCellPos.x + x;
                int zPos = bottomLeftCellPos.y + z;

                //Check we're within the grid width
                if (xPos < 0 || xPos >= GridManager.Instance.m_gridWidth)
                {
                    //Debug.Log("X not within grid bounds.");
                    return null;
                }

                //Check we're within the grid height
                if (zPos < 0 || zPos >= GridManager.Instance.m_gridHeight)
                {
                    //Debug.Log("Z not within grid bounds.");
                    return null;
                }

                Cell cell = GetCellFromPos(new Vector2Int(xPos, zPos));

                if (cell.m_isOutOfBounds)
                {
                    return null;
                }

                cellsFromPos.Add(cell);
            }
        }

        return cellsFromPos;
    }

    public static Cell GetCellFrom3DPos(Vector3 pos)
    {
        //Debug.Log($"Getting cell from {pos}");
        int x = Mathf.FloorToInt(pos.x + 0.5f);
        int z = Mathf.FloorToInt(pos.z + 0.5f);

        //Check we're within the grid width
        if (x < 0 || x >= GridManager.Instance.m_gridWidth)
        {
            //Debug.Log("X not within grid bounds.");
            return null;
        }

        //Check we're within the grid height
        if (z < 0 || z >= GridManager.Instance.m_gridHeight)
        {
            //Debug.Log("Z not within grid bounds.");
            return null;
        }

        //Debug.Log($"Returning cell from {x},{z}");
        int index = z * GridManager.Instance.m_gridWidth + x;
        //Debug.Log("Request Cell at: " + x + "," + z + " Index of: " + index);

        return GridManager.Instance.m_gridCells[index];
    }

    public static (List<Cell>, List<Vector2Int>) GetNeighborHarvestPointCells(Vector2Int pos)
    {
        List<Vector2Int> neighborPos = new List<Vector2Int>();
        neighborPos.Add(new Vector2Int(pos.x, pos.y + 1)); //N
        neighborPos.Add(new Vector2Int(pos.x + 1, pos.y + 1)); //NE
        neighborPos.Add(new Vector2Int(pos.x + 1, pos.y)); //E
        neighborPos.Add(new Vector2Int(pos.x + 1, pos.y - 1)); //SE
        neighborPos.Add(new Vector2Int(pos.x, pos.y - 1)); //S
        neighborPos.Add(new Vector2Int(pos.x - 1, pos.y - 1)); //SW
        neighborPos.Add(new Vector2Int(pos.x - 1, pos.y)); //W
        neighborPos.Add(new Vector2Int(pos.x - 1, pos.y + 1)); //NW

        List<Cell> neighborCells = new List<Cell>();
        List<Vector2Int> harvestPos = new List<Vector2Int>();

        foreach (Vector2Int neighborCellPos in neighborPos)
        {
            if (IsWithinBounds(neighborCellPos.x, neighborCellPos.y))
            {
                Cell cell = GetCellFromPos(neighborCellPos);
                if (cell != null)
                {
                    neighborCells.Add(cell);
                    harvestPos.Add(neighborCellPos);
                }
            }
        }

        return (neighborCells, harvestPos);
    }

    public static string FormatAsPercentageString(float i)
    {
        float percentageValue = i * 100;
        string formattedPercentage = percentageValue.ToString("0") + "%";
        return formattedPercentage;
    }

    public static List<Cell> FindEdgeCells(List<Cell> islandCells)
    {
        HashSet<Cell> islandSet = new HashSet<Cell>(islandCells); // For quick lookup
        List<Cell> edgeCells = new List<Cell>();

        // Define 4-connectivity (use diagonals for 8-connectivity)
        Vector2Int[] directions =
        {
            new Vector2Int(-1, 0), // Left
            new Vector2Int(1, 0), // Right
            new Vector2Int(0, -1), // Down
            new Vector2Int(0, 1) // Up
        };

        // Loop through each cell in the island
        foreach (Cell cell in islandCells)
        {
            Vector2Int cellPos = cell.m_cellPos;
            foreach (Vector2Int direction in directions)
            {
                Cell neighborCell = GetCellFromPos(cellPos + direction);
                // If the neighbor is not part of the island, mark the current cell as an edge
                if (!islandSet.Contains(neighborCell))
                {
                    edgeCells.Add(cell);
                    break; // No need to check further directions for this cell
                }
            }
        }

        return edgeCells;
    }

    public static List<Cell> FindInteriorCells(List<Cell> islandCells)
    {
        HashSet<Cell> islandSet = new HashSet<Cell>(islandCells); // For quick lookup
        List<Cell> interiorCells = new List<Cell>();

        Vector2Int[] directions =
        {
            new Vector2Int(-1, 0), // Left
            new Vector2Int(1, 0), // Right
            new Vector2Int(0, -1), // Down
            new Vector2Int(0, 1) // Up
        };

        foreach (Cell cell in islandCells)
        {
            Vector2Int cellPos = cell.m_cellPos;
            bool isEdge = false;

            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighborPos = cellPos + direction;
                Cell neighborCell = GetCellFromPos(neighborPos);

                // If the neighbor is not part of the island, mark this cell as an edge
                if (!islandSet.Contains(neighborCell))
                {
                    isEdge = true;
                    break;
                }
            }

            if (!isEdge)
            {
                interiorCells.Add(cell);
            }
        }

        return interiorCells;
    }

    public static List<Cell> GetNeighborCells(Cell startCell, Cell[] gridCells)
    {
        // Get the neighboring cells (North, East, South, and West)
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Cell curCell = GetCellFromPos(new Vector2Int(startCell.m_cellPos.x, startCell.m_cellPos.y));

        // If we have additional neighbors, and we have not been set to Portal, we're in an exit. Add the neighbors and step through them.
        // The neighbor is then set as the Portal direction and should get it's 4 neighbors like normal.
        if (curCell.m_additionalNeighbors.Count > 0 && curCell.m_tempDirectionToNextCell != Cell.Direction.Portal)
        {
            foreach (Cell additionalNeighbor in curCell.m_additionalNeighbors)
            {
                neighbors.Add(additionalNeighbor.m_cellPos);
            }
        }
        else
        {
            if (curCell.m_canPathNorth) neighbors.Add(new Vector2Int(startCell.m_cellPos.x, startCell.m_cellPos.y + 1));
            if (curCell.m_canPathEast) neighbors.Add(new Vector2Int(startCell.m_cellPos.x + 1, startCell.m_cellPos.y));
            if (curCell.m_canPathSouth) neighbors.Add(new Vector2Int(startCell.m_cellPos.x, startCell.m_cellPos.y - 1));
            if (curCell.m_canPathWest) neighbors.Add(new Vector2Int(startCell.m_cellPos.x - 1, startCell.m_cellPos.y));
        }


        // Is this portal a cell?
        /*if (curCell.m_portalConnectionCell != null)
        {
            if (curCell.m_tempDirectionToNextCell != Cell.Direction.Portal) // We're at an exit.
            {
                // Only send the portal cell. as a neighbor. This assures no cells point into the exit cell.
                neighbors = new List<Vector2Int>();
                neighbors.Add(curCell.m_portalConnectionCell.m_cellPos);
                //Debug.Log($"Cell {curCell.m_cellPos} is a portal cell. Setting it's connection {curCell.m_portalConnectionCell} as the entrance.");
            }
        }*/


        List<Cell> neighborCells = new List<Cell>();

        for (int i = 0; i < neighbors.Count; i++)
        {
            if (IsWithinBounds(neighbors[i].x, neighbors[i].y))
            {
                neighborCells.Add(gridCells[GetCellIndex(neighbors[i])]);
            }
        }

        return neighborCells;
    }

    public static List<Vector2Int> GetAdjacentEmptyCellPos(Vector2Int center)
    {
        List<Vector2Int> adjacentCells = new List<Vector2Int>();

        foreach (Vector2Int direction in m_directions)
        {
            Vector2Int adjacentPosition = center + direction;
            Cell adjacentCell = GetCellFromPos(adjacentPosition);
            if (adjacentCell == null) continue;
            if (!adjacentCell.m_isOccupied)
            {
                adjacentCells.Add(adjacentCell.m_cellPos);
            }
        }

        return adjacentCells;
    }

    public static List<Cell> GetAdjacentEmptyCells(Vector2Int center)
    {
        List<Cell> adjacentCells = new List<Cell>();

        foreach (Vector2Int direction in m_directions)
        {
            Vector2Int adjacentPosition = center + direction;
            Cell adjacentCell = GetCellFromPos(adjacentPosition);
            if (adjacentCell == null) continue;
            if (!adjacentCell.m_isOccupied && adjacentCell.m_directionToNextCell != Cell.Direction.Portal)
            {
                adjacentCells.Add(adjacentCell);
            }
        }

        return adjacentCells;
    }

    public static List<Cell> GetPathableAdjacentEmptyCells(Cell startCell)
    {
        List<Cell> adjacentCells = new List<Cell>();

        Vector2Int startCellPos = startCell.m_cellPos;

        Cell northNeighborCell = GetCellFromPos(new Vector2Int(startCellPos.x, startCellPos.y + 1));
        if (startCell.m_canPathNorth &&
            northNeighborCell != null &&
            northNeighborCell.m_canPathSouth &&
            !northNeighborCell.m_isOccupied &&
            northNeighborCell.m_directionToNextCell != Cell.Direction.Portal)
        {
            //Debug.Log($"Get Neighbors of {startCell.m_cellPos} -- adding North cell: {northNeighborCell.m_cellPos}");
            adjacentCells.Add(northNeighborCell);
        }

        Cell southNeighborCell = GetCellFromPos(new Vector2Int(startCellPos.x, startCellPos.y - 1));
        if (startCell.m_canPathSouth &&
            southNeighborCell != null &&
            southNeighborCell.m_canPathNorth &&
            !southNeighborCell.m_isOccupied &&
            southNeighborCell.m_directionToNextCell != Cell.Direction.Portal)
        {
            //Debug.Log($"Get Neighbors of {startCell.m_cellPos} -- adding South cell: {southNeighborCell.m_cellPos}");
            adjacentCells.Add(southNeighborCell);
        }

        Cell eastNeighborCell = GetCellFromPos(new Vector2Int(startCellPos.x + 1, startCellPos.y));
        if (startCell.m_canPathEast &&
            eastNeighborCell != null &&
            eastNeighborCell.m_canPathWest &&
            !eastNeighborCell.m_isOccupied &&
            eastNeighborCell.m_directionToNextCell != Cell.Direction.Portal)
        {
            //Debug.Log($"Get Neighbors of {startCell.m_cellPos} -- adding East cell: {eastNeighborCell.m_cellPos}");
            adjacentCells.Add(eastNeighborCell);
        }

        Cell westNeighborCell = GetCellFromPos(new Vector2Int(startCellPos.x - 1, startCellPos.y));
        if (startCell.m_canPathWest &&
            westNeighborCell != null &&
            westNeighborCell.m_canPathEast &&
            !westNeighborCell.m_isOccupied &&
            westNeighborCell.m_directionToNextCell != Cell.Direction.Portal)
        {
            //Debug.Log($"Get Neighbors of {startCell.m_cellPos} -- adding West cell: {westNeighborCell.m_cellPos}");
            adjacentCells.Add(westNeighborCell);
        }

        return adjacentCells;
    }

    public static T GetRandomElement<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
            return default;

        return list[Random.Range(0, list.Count)];
    }

    public static List<Vector2Int> GetAdjacentCellPos(Vector2Int center)
    {
        List<Vector2Int> adjacentCells = new List<Vector2Int>();

        foreach (Vector2Int direction in m_directions)
        {
            Vector2Int adjacentPosition = center + direction;
            Cell adjacentCell = GetCellFromPos(adjacentPosition);

            if (adjacentCell == null) continue;

            adjacentCells.Add(adjacentCell.m_cellPos);
        }

        return adjacentCells;
    }

    public static Vector2Int GetVector2IntFrom3DPos(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x + 0.5f);
        int z = Mathf.FloorToInt(pos.z + 0.5f);
        return new Vector2Int(x, z);
    }

    /*public static List<GameObject> GetCellsInRange(GameObject obj, GameObject[,] grid, int range)
     {
         int row = (int) obj.transform.position.x;
         int col = (int) obj.transform.position.y;

         List<GameObject> neighbors = new List<GameObject>();
         for (int x = row - range; x <= row + range; x++)
         {
             if (x < 0 || x > grid.GetLength(0) - 1)
             {
                 continue;
             }

             for (int y = col - range; y <= col + range; y++)
             {
                 if (y < 0 || y > grid.GetLength(1) - 1)
                 {
                     continue;
                 }

                 //Manhattan distance to check range from obj
                 if (Mathf.Abs(row - x) + Mathf.Abs(col - y) <= range)
                 {
                     if (grid[x, y] != null)
                     {
                         neighbors.Add(grid[x, y]);
                     }
                 }
             }
         }

         return neighbors;
     }*/

    /*public static List<GameObject> GetCellsInView(GameObject obj, GameObject[,] grid, int range)
    {
        int row = (int) obj.transform.position.x;
        int col = (int) obj.transform.position.y;

        List<GameObject> neighbors = new List<GameObject>();
        for (int x = row - range; x <= row + range; x++)
        {
            if (x < 0 || x > grid.GetLength(0) - 1)
            {
                continue;
            }

            for (int y = col - range; y <= col + range; y++)
            {
                if (y < 0 || y > grid.GetLength(1) - 1)
                {
                    continue;
                }

                //Manhattan distance to check range from obj
                if (Mathf.Abs(row - x) + Mathf.Abs(col - y) <= range)
                {
                    if (grid[x, y] != null)
                    {
                        int start_x = (int) obj.transform.position.x;
                        int start_y = (int) obj.transform.position.y;

                        int end_x = (int) grid[x, y].transform.position.x;
                        int end_y = (int) grid[x, y].transform.position.y;

                        if (!CellIsBlocked(start_x, start_y, end_x, end_y))
                        {
                            neighbors.Add(grid[x, y]);
                        }
                    }
                }
            }
        }

        return neighbors;
    }*/

    /*public static List<GameObject> DetermineEnemies(List<GameObject> things, string myFaction)
    {
        List<GameObject> enemies = new List<GameObject>();

        for (int i = 0; i < things.Count; i++)
        {
            Attributes thingsAttributes = things[i].GetComponent<Attributes>();
            foreach (string factions in gameState.enemiesOf[myFaction])

                if (factions == thingsAttributes.faction.ToString())
                {
                    enemies.Add(things[i]);
                }
        }

        return enemies;
    }*/

    /*public static bool DetermineEnemy(GameObject thing, string myFaction)
    {
        if (thing != null)
        {
            Attributes thingAttributes = thing.GetComponent<Attributes>();
            foreach (string factions in gameState.enemiesOf[myFaction])

                if (factions == thingAttributes.faction.ToString())
                {
                    return true;
                }
        }

        return false;
    }*/

    /*public static List<GameObject> AcceptableTargets(string targets, string myFaction)
    {
        List<GameObject> acceptableTargets = new List<GameObject>();

        foreach (GameObject character in charactersState.visibleCharacters)
        {
            if (targets == "Friendly")
            {
                foreach (string faction in gameState.enemiesOf[myFaction])
                {
                    if (faction != character.GetComponent<Attributes>().faction.ToString())
                    {
                        acceptableTargets.Add(character);
                    }
                }
            }

            if (targets == "Enemies")
            {
                foreach (string faction in gameState.enemiesOf[myFaction])
                {
                    if (faction == character.GetComponent<Attributes>().faction.ToString())
                    {
                        acceptableTargets.Add(character);
                    }
                }
            }

            if (targets == "All")
            {
                acceptableTargets.Add(character);
            }
        }

        return acceptableTargets;
    }*/

    /*public static GameObject ClosestObj(List<GameObject> objList, GameObject gameObject)
    {
        List<float> distances = new List<float>();

        GameObject closestObj = null;

        foreach (GameObject obj in objList)
        {
            int distance = GetDistance(obj, gameObject);
            distances.Add(distance);

            if (distance <= distances.Min())
            {
                closestObj = obj;
            }
        }

        return closestObj;
    }*/

    /*public static GameObject FindNextCell(GameObject startCell, GameObject endCell)
    {
        //Getting a next best direction will require pathfinding (should I go up around the wall or down towards the door?)

        int startX = (int) startCell.transform.position.x;
        int startY = (int) startCell.transform.position.y;

        int endX = (int) endCell.transform.position.x;
        int endY = (int) endCell.transform.position.y;

        //Which axis do we want to move on?
        int distX = Mathf.Abs(startX - endX);
        int distY = Mathf.Abs(startY - endY);

        GameObject direction = null;

        if (distX >= distY)
        {
            //Move Horizontal
            //Which way, horizontally?
            if (startX >= endX)
            {
                //Move Left
                direction = gridManager.gridCells[startX - 1, startY];
            }
            else
            {
                //Move Right
                direction = gridManager.gridCells[startX + 1, startY];
            }
        }
        else
        {
            //Move Vertical
            if (startY >= endY)
            {
                //Move Down
                direction = gridManager.gridCells[startX, startY - 1];
            }
            else
            {
                //Move Up
                direction = gridManager.gridCells[startX, startY + 1];
            }
        }

        return direction;
    }*/

    /*public static int GetDistance(GameObject objA, GameObject objB)
    {
        int objAX = (int) objA.transform.position.x;
        int objAY = (int) objA.transform.position.y;

        int objBX = (int) objB.transform.position.x;
        int objBY = (int) objB.transform.position.y;

        int distance = Mathf.Abs(objAX - objBX) + Mathf.Abs(objAY - objBY);
        return distance;
    }*/
}