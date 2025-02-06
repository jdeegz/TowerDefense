using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Diagnostics;
using Vector2Int = UnityEngine.Vector2Int;

public class AStar
{
    public static List<Vector2Int> FindPathToGoal(Cell goalCell, Cell currentCell)
    {
        List<Vector2Int> path = null;
        List<Cell> emptyCells = null; // Pooled list
        List<Vector2Int> emptyCellPositions = null;

        int currentDistance = Math.Max(Math.Abs(currentCell.m_cellPos.x - goalCell.m_cellPos.x), Math.Abs(currentCell.m_cellPos.y - goalCell.m_cellPos.y));
        int searchDistance = goalCell.m_isOccupied ? 1 : 0;
        int maxSearchDistance = Math.Min(currentDistance, 9);

        while (searchDistance <= maxSearchDistance && path == null)
        {
            // Before assigning a new list, release the previous one if it exists
            if (emptyCells != null)
            {
                ListPool<Cell>.Release(emptyCells);
            }

            if (emptyCellPositions != null)
            {
                ListPool<Vector2Int>.Release(emptyCellPositions);
            }

            emptyCells = Util.GetEmptyCellsAtDistance(goalCell.m_cellPos, searchDistance);

            if (emptyCells == null) // No empty cells found, expand search range.
            {
                ++searchDistance;
                continue;
            }

            emptyCellPositions = ListPool<Vector2Int>.Get();

            foreach (Cell cell in emptyCells)
            {
                emptyCellPositions.Add(cell.m_cellPos);
            }

            path = FindShortestPath(currentCell.m_cellPos, emptyCellPositions);

            if (path == null)
            {
                ++searchDistance; // No pathable empty cells, expand range.
            }
        }

        // Ensure we release the last emptyCells before returning
        if (emptyCells != null)
        {
            ListPool<Cell>.Release(emptyCells);
        }

        if (emptyCellPositions != null)
        {
            ListPool<Vector2Int>.Release(emptyCellPositions);
        }

        return path;
    }


    public static List<Vector2Int> FindShortestPath(Vector2Int start, List<Vector2Int> endPositions)
    {
        List<Vector2Int> shortestPath = null;
        List<Vector2Int> path = null;

        foreach (Vector2Int end in endPositions)
        {
            path = FindPath(start, end);
            if (path == null)
            {
                continue;
            }

            if (shortestPath == null)
            {
                shortestPath = new List<Vector2Int>(path);;
            }
            else if (path.Count < shortestPath.Count)
            {
                shortestPath = new List<Vector2Int>(path);;
            }

            ListPool<Vector2Int>.Release(path);
        }

        return shortestPath;
    }

    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = ListPool<Vector2Int>.Get();
        List<Vector2Int> neighbors = ListPool<Vector2Int>.Get(); // Use a pooled list for neighbors

        if (start == end)
        {
            path.Add(end);
            ListPool<Vector2Int>.Release(neighbors); // Release neighbors before returning
            return path;
        }

        PriorityQueue<Node> openNodes = new PriorityQueue<Node>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        openNodes.Enqueue(new Node(start, null, 0, Heuristic(start, end)));

        while (openNodes.Count > 0)
        {
            Node current = openNodes.Dequeue();

            if (current.position == end)
            {
                Node node = current;
                while (node != null)
                {
                    path.Add(node.position);
                    node = node.parent;
                }

                path.Reverse();

                ListPool<Vector2Int>.Release(neighbors); // Release neighbors
                return path;
            }

            if (!visited.Add(current.position))
            {
                continue;
            }

            // Clear and reuse the pooled neighbors list
            neighbors.Clear();

            Cell curCell = Util.GetCellFromPos(current.position);
            if (curCell.m_canPathNorth) neighbors.Add(new Vector2Int(current.position.x, current.position.y + 1));
            if (curCell.m_canPathEast) neighbors.Add(new Vector2Int(current.position.x + 1, current.position.y));
            if (curCell.m_canPathSouth) neighbors.Add(new Vector2Int(current.position.x, current.position.y - 1));
            if (curCell.m_canPathWest) neighbors.Add(new Vector2Int(current.position.x - 1, current.position.y));

            foreach (Vector2Int neighbor in neighbors)
            {
                if (!visited.Contains(neighbor) && neighbor.x >= 0 && neighbor.x < GridManager.Instance.m_gridWidth &&
                    neighbor.y >= 0 && neighbor.y < GridManager.Instance.m_gridHeight &&
                    !Util.GetCellFromPos(neighbor).m_isOccupied)
                {
                    float g = current.g + 1;
                    float h = Heuristic(neighbor, end);

                    openNodes.Enqueue(new Node(neighbor, current, g, h));
                }
            }
        }

        // If no path is found, release both lists before returning null
        ListPool<Vector2Int>.Release(path);
        ListPool<Vector2Int>.Release(neighbors);

        return null;
    }


    private static float Heuristic(Vector2Int from, Vector2Int to)
    {
        // Manhattan distance heuristic
        return Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
    }

    private class Node : IComparable<Node>
    {
        public Vector2Int position;
        public Node parent;
        public float g; // Cost from start to this node
        public float h; // Heuristic value to goal

        public float f => g + h; // Total cost (f = g + h)

        public Node(Vector2Int position, Node parent, float g, float h)
        {
            this.position = position;
            this.parent = parent;
            this.g = g;
            this.h = h;
        }

        public int CompareTo(Node other)
        {
            // Compare nodes based on their f value for priority queue
            return f.CompareTo(other.f);
        }
    }

    public static List<Cell> FindIsland(Cell startCell)
    {
        List<Cell> islandCells = new List<Cell>();
        HashSet<Cell> visited = new HashSet<Cell>();
        PerformDFS(startCell, islandCells, visited);
        return islandCells;
    }

    private static void PerformDFS(Cell curCell, List<Cell> islandCells, HashSet<Cell> visited)
    {
        Vector2Int curCellPos = curCell.m_cellPos;
        if (curCellPos.x >= 0 && curCellPos.x < GridManager.Instance.m_gridWidth &&
            curCellPos.y >= 0 && curCellPos.y < GridManager.Instance.m_gridHeight &&
            !visited.Contains(curCell) &&
            !curCell.m_isOccupied &&
            !curCell.m_isTempOccupied)
        {
            // Mark the current cell as visited
            visited.Add(curCell);

            // Add the current cell to the list of island cells
            islandCells.Add(curCell);

            if (curCell.m_portalConnectionCell != null && curCell.m_tempDirectionToNextCell != Cell.Direction.Portal)
            {
                // We've crawled to this tile, we want to include it in the island, but not add any of its neighbors.
                return;
            }

            // Recursively explore the neighbors of the current cell
            // Up neighbor
            Cell northNeighborCell = Util.GetCellFromPos(new Vector2Int(curCellPos.x, curCellPos.y + 1));
            if (curCell.m_canPathNorth && northNeighborCell != null) PerformDFS(northNeighborCell, islandCells, visited);

            // Right neighbor
            Cell eastNeighborCell = Util.GetCellFromPos(new Vector2Int(curCellPos.x + 1, curCellPos.y));
            if (curCell.m_canPathEast && eastNeighborCell != null) PerformDFS(eastNeighborCell, islandCells, visited);

            // Down neighbor
            Cell southNeighborCell = Util.GetCellFromPos(new Vector2Int(curCellPos.x, curCellPos.y - 1));
            if (curCell.m_canPathSouth && southNeighborCell != null) PerformDFS(southNeighborCell, islandCells, visited);

            // Left neighbor
            Cell westNeighborCell = Util.GetCellFromPos(new Vector2Int(curCellPos.x - 1, curCellPos.y));
            if (curCell.m_canPathWest && westNeighborCell != null) PerformDFS(westNeighborCell, islandCells, visited);
        }
    }

    public static int CalculateGridDistance(Vector2Int unitCellPosition, Vector2Int goalCellPos)
    {
        int cellCount = 0;
        Vector2Int currentPosition = unitCellPosition;

        // Continue moving along each cell's direction until we reach the goal
        while (currentPosition != goalCellPos)
        {
            Cell curCell = Util.GetCellFromPos(currentPosition);
            Vector2Int direction = curCell.GetDirectionVector(curCell.m_directionToNextCell);

            // Move to the next cell position
            currentPosition += direction;
            cellCount++;

            // Safety check to avoid infinite loops (optional, adjust limit as needed)
            if (cellCount > 1000)
            {
                Debug.LogWarning("Exceeded cell iteration limit.");
                break;
            }
        }

        return cellCount;
    }

    public static List<Vector2Int> FindExitPath(Vector2Int start, Vector2Int end, Vector2Int precon, Vector2Int exit)
    {
        List<Vector2Int> path = ListPool<Vector2Int>.Get();
        List<Vector2Int> neighbors = ListPool<Vector2Int>.Get();

        if (start == end
            || Util.GetCellFromPos(start).m_isOccupied
            || Util.GetCellFromPos(end).m_isOccupied
            || Util.GetCellFromPos(start).m_isTempOccupied
            || Util.GetCellFromPos(end).m_isTempOccupied)
        {
            ListPool<Vector2Int>.Release(neighbors);
            ListPool<Vector2Int>.Release(path);
            return null;
        }

        PriorityQueue<Node> openNodes = new PriorityQueue<Node>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        openNodes.Enqueue(new Node(start, null, 0, Heuristic(start, end)));

        while (openNodes.Count > 0)
        {
            Node current = openNodes.Dequeue();

            if (current.position == end)
            {
                Node node = current;
                while (node != null)
                {
                    path.Add(node.position);
                    node = node.parent;
                }

                path.Reverse();
                ListPool<Vector2Int>.Release(neighbors);
                return path; // Caller must release this
            }

            if (!visited.Add(current.position))
            {
                continue;
            }

            neighbors.Clear();
            Cell curCell = Util.GetCellFromPos(current.position);
            if (curCell.m_canPathNorth) neighbors.Add(new Vector2Int(current.position.x, current.position.y + 1));
            if (curCell.m_canPathEast) neighbors.Add(new Vector2Int(current.position.x + 1, current.position.y));
            if (curCell.m_canPathSouth) neighbors.Add(new Vector2Int(current.position.x, current.position.y - 1));
            if (curCell.m_canPathWest) neighbors.Add(new Vector2Int(current.position.x - 1, current.position.y));

            foreach (Vector2Int neighbor in neighbors)
            {
                if (!visited.Contains(neighbor) &&
                    neighbor.x >= 0 &&
                    neighbor.x < GridManager.Instance.m_gridWidth &&
                    neighbor.y >= 0 &&
                    neighbor.y < GridManager.Instance.m_gridHeight &&
                    !Util.GetCellFromPos(neighbor).m_isOccupied &&
                    !Util.GetCellFromPos(neighbor).m_isTempOccupied &&
                    neighbor != precon &&
                    neighbor != exit)
                {
                    float g = current.g + 1;
                    float h = Heuristic(neighbor, end);
                    openNodes.Enqueue(new Node(neighbor, current, g, h));
                }
            }
        }

        // No path found
        ListPool<Vector2Int>.Release(path);
        ListPool<Vector2Int>.Release(neighbors);
        return null;
    }


    public static List<Vector2Int> GetExitPath(Vector2Int startPos, Vector2Int endPos)
    {
        //Debug.Log($"Getting Exit Path from {startPos} - {endPos}");
        List<Vector2Int> path = ListPool<Vector2Int>.Get();
        Vector2Int current = startPos;

        while (current != endPos)
        {
            Cell curCell = Util.GetCellFromPos(current);

            //If the current cell is occupied, we cannot find the exit, this is not a valid path.
            if ((curCell.m_isOccupied && !curCell.m_isUpForSale) || curCell.m_isTempOccupied)
            {
                ListPool<Vector2Int>.Release(path);
                return null;
            }

            path.Add(current);

            if (curCell.m_portalConnectionCell != null)
            {
                //Debug.Log($"Found a portal connection.");

                if (curCell.m_tempDirectionToNextCell == Cell.Direction.Portal && curCell.m_directionToNextCell != Cell.Direction.Portal && curCell.m_actorCount > 0)
                {
                    ListPool<Vector2Int>.Release(path);
                    return null;
                }

                if (curCell.m_tempDirectionToNextCell == Cell.Direction.Portal)
                {
                    //Debug.Log($"Portal exit has direction, stepping into it.");
                    curCell = curCell.m_portalConnectionCell;
                    current = curCell.m_cellPos;
                    //Add the portal exit cell
                    path.Add(current);
                }
            }

            Cell.Direction direction = curCell.m_tempDirectionToNextCell;

            if (direction == Cell.Direction.Unset)
            {
                ListPool<Vector2Int>.Release(path);
                return null;
            }

            current += curCell.GetDirectionVector(curCell.m_tempDirectionToNextCell);

            // Path goes out of bounds, return null
            if (current.x < 0 || current.x == GridManager.Instance.m_gridWidth || current.y < 0 || current.y == GridManager.Instance.m_gridHeight)
            {
                ListPool<Vector2Int>.Release(path);
                return null;
            }
        }

        path.Add(endPos);

        return path;
    }
}