using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Diagnostics;
using Vector2Int = UnityEngine.Vector2Int;

public class AStar
{
    public static List<Vector2Int> FindPathToGoal(Cell goalCell, Cell currentCell)
    {
        List<Vector2Int> path = null;
        List<Cell> emptyCells = null;
        int currentDistance = Math.Max(Math.Abs(currentCell.m_cellPos.x - goalCell.m_cellPos.x), Math.Abs(currentCell.m_cellPos.y - goalCell.m_cellPos.y));
        int searchDistance = goalCell.m_isOccupied ? 1 : 0;
        int maxSearchDistance = Math.Min(currentDistance, 9);
        while (searchDistance <= maxSearchDistance && path == null)
        {
            emptyCells = Util.GetEmptyCellsAtDistance(goalCell.m_cellPos, searchDistance);

            if (emptyCells == null) // We found no empty cells, expand range.
            {
                ++searchDistance;
                continue;
            }
            
            List<Vector2Int> emptyCellPositions = new List<Vector2Int>();
            
            foreach (Cell cell in emptyCells)
            {
                emptyCellPositions.Add(cell.m_cellPos);
            }

            path = FindShortestPath(currentCell.m_cellPos, emptyCellPositions);
            
            if (path == null)
            {
                ++searchDistance; // We found no pathable empty cells, expand range.
            }
        }

        return path;
    }
    
    
    public static List<Vector2Int> FindShortestPath(Vector2Int start, List<Vector2Int> endPositions)
    {
        List<Vector2Int> shortestPath = null;

        foreach (Vector2Int end in endPositions)
        {
            List<Vector2Int> path = FindPath(start, end);
            if (path == null)
            {
                continue;
            }
            
            //Debug.Log($"Path to {end} is {path.Count} long.");
            
            if (shortestPath == null)
            {
                shortestPath = path;
            }
            else if (path.Count < shortestPath.Count)
            {
                shortestPath = path;
            }
        }

        return shortestPath;
    }
    
    public static Cell PickClosestPathableCell(Vector2Int start, List<Vector2Int> endPositions)
    {
        List<Vector2Int> shortestPath = null;
        Cell closestPathableCell = null;

        foreach (Vector2Int end in endPositions)
        {
            List<Vector2Int> path = FindPath(start, end);
            if (path == null)
            {
                continue;
            }
            
            //Debug.Log($"Path to {end} is {path.Count} long.");
            
            if (shortestPath == null)
            {
                shortestPath = path;
                closestPathableCell = Util.GetCellFromPos(end);
            }
            else if (path.Count < shortestPath.Count)
            {
                shortestPath = path;
                closestPathableCell = Util.GetCellFromPos(end);
            }
        }

        return closestPathableCell;
    }


    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        //if (start == end || Util.GetCellFromPos(start).m_isOccupied || Util.GetCellFromPos(end).m_isOccupied) return null;

        if (start == end)
        {
            //Debug.Log($"End is same as start, returning end as path.");
            path.Add(end);
            return path;
        }

        // Priority queue for open nodes
        PriorityQueue<Node> openNodes = new PriorityQueue<Node>();

        // List to keep track of visited nodes
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // Add the start node to openNodes
        openNodes.Enqueue(new Node(start, null, 0, Heuristic(start, end)));

        while (openNodes.Count > 0)
        {
            // Get the node with the lowest f value from openNodes
            Node current = openNodes.Dequeue();

            // Check if we reached the goal
            if (current.position == end)
            {
                // Reconstruct the path from the goal node to the start node
                Node node = current;
                while (node != null)
                {
                    path.Add(node.position);
                    node = node.parent;
                }

                path.Reverse(); // Reverse the path to get the correct order
                //Debug.Log("Path Found.");
                return path;
            }

            // Mark the current node as visited
            if (!visited.Add(current.position))
            {
                continue;
            }

            // Get the neighboring cells (North, East, South, and West)
            List<Vector2Int> neighbors = new List<Vector2Int>();

            Cell curCell = Util.GetCellFromPos(new Vector2Int(current.position.x, current.position.y));
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
                    // Calculate the g value (cost from start to neighbor)
                    float g = current.g + 1;

                    // Calculate the h value (heuristic value from neighbor to goal)
                    float h = Heuristic(neighbor, end);

                    // Add the neighbor node to openNodes
                    openNodes.Enqueue(new Node(neighbor, current, g, h));
                }
            }
        }

        // No path found
        //Debug.Log($"No Path found between {start} and {end}");
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

    public static List<Cell> FindIsland(Cell startCell, List<Cell> preconTowerCells)
    {
        List<Cell> islandCells = new List<Cell>();
        HashSet<Cell> visited = new HashSet<Cell>();
        PerformDFS(startCell, islandCells, visited, preconTowerCells);
        return islandCells;
    }

    private static void PerformDFS(Cell curCell, List<Cell> islandCells, HashSet<Cell> visited, List<Cell> preconTowerCellsPos)
    {
        Vector2Int curCellPos = curCell.m_cellPos;
        if (curCellPos.x >= 0 && curCellPos.x < GridManager.Instance.m_gridWidth &&
            curCellPos.y >= 0 && curCellPos.y < GridManager.Instance.m_gridHeight &&
            !visited.Contains(curCell) &&
            !curCell.m_isOccupied &&
            !curCell.m_isTempOccupied &&
            !preconTowerCellsPos.Contains(curCell))
        {
            // Mark the current cell as visited
            visited.Add(curCell);

            // Add the current cell to the list of island cells
            islandCells.Add(curCell);

            // Recursively explore the neighbors of the current cell
            if (curCell.m_canPathNorth) PerformDFS(Util.GetCellFromPos(new Vector2Int(curCellPos.x, curCellPos.y + 1)), islandCells, visited, preconTowerCellsPos); // Up neighbor
            if (curCell.m_canPathEast) PerformDFS(Util.GetCellFromPos(new Vector2Int(curCellPos.x + 1, curCellPos.y)), islandCells, visited, preconTowerCellsPos); // Right neighbor
            if (curCell.m_canPathSouth) PerformDFS(Util.GetCellFromPos(new Vector2Int(curCellPos.x, curCellPos.y - 1)), islandCells, visited, preconTowerCellsPos); // Down neighbor
            if (curCell.m_canPathWest) PerformDFS(Util.GetCellFromPos(new Vector2Int(curCellPos.x - 1, curCellPos.y)), islandCells, visited, preconTowerCellsPos); // Left neighbor
        }
    }
    
    public static int CalculateGridDistance(Vector2Int unitCellPosition, Vector2Int goalCellPos)
    {
        int cellCount = 0;
        Vector2Int currentPosition = unitCellPosition;

        // Continue moving along each cell's direction until we reach the goal
        while (currentPosition != goalCellPos)
        {
            Vector3 directionToNextCell = Util.GetCellFromPos(currentPosition).m_directionToNextCell;
            Vector2Int direction = new Vector2Int((int)directionToNextCell.x, (int)directionToNextCell.z);
            
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
        List<Vector2Int> path = new List<Vector2Int>();

        if (start == end 
            || Util.GetCellFromPos(start).m_isOccupied 
            || Util.GetCellFromPos(end).m_isOccupied
            || Util.GetCellFromPos(start).m_isTempOccupied 
            || Util.GetCellFromPos(end).m_isTempOccupied)
        {
            //Debug.Log("Start is End: " + (start == end));
            //Debug.Log("Start is occupied: " + Util.GetCellFromPos(start).m_isOccupied);
            //Debug.Log("End is occupied: " + Util.GetCellFromPos(end).m_isOccupied);
            return null;
        }


        // Priority queue for open nodes
        PriorityQueue<Node> openNodes = new PriorityQueue<Node>();

        // List to keep track of visited nodes
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // Add the start node to openNodes
        openNodes.Enqueue(new Node(start, null, 0, Heuristic(start, end)));
        while (openNodes.Count > 0)
        {
            // Get the node with the lowest f value from openNodes
            Node current = openNodes.Dequeue();

            // Check if we reached the goal
            if (current.position == end)
            {
                // Reconstruct the path from the goal node to the start node
                Node node = current;
                while (node != null)
                {
                    path.Add(node.position);
                    node = node.parent;
                }

                path.Reverse(); // Reverse the path to get the correct order
                //Debug.Log($"Path Found. {start} - {end}");
                return path;
            }

            // Mark the current node as visited
            if (!visited.Add(current.position))
            {
                continue;
            }

            // Get the neighboring cells (North, East, South, and West)
            List<Vector2Int> neighbors = new List<Vector2Int>();

            Cell curCell = Util.GetCellFromPos(new Vector2Int(current.position.x, current.position.y));
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
                    // Calculate the g value (cost from start to neighbor)
                    float g = current.g + 1;

                    // Calculate the h value (heuristic value from neighbor to goal)
                    float h = Heuristic(neighbor, end);

                    // Add the neighbor node to openNodes
                    openNodes.Enqueue(new Node(neighbor, current, g, h));
                }
            }
        }

        // No path found
        // Debug.Log($"No Path Found. {start} - {end}");
        return null;
    }

    public static List<Vector2Int> GetExitPath(Vector2Int startPos, Vector2Int endPos)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = startPos;

        while (current != endPos)
        {
            Cell curCell = Util.GetCellFromPos(current);

            //If the current cell is occupied, we cannot find the exit, this is not a valid path.
            if (curCell.m_isOccupied || curCell.m_isTempOccupied)
            {
                Debug.Log("GetExitPath did not find a path.");
                return null;
            }

            path.Add(current);
            Vector2Int direction = Util.GetVector2IntFrom3DPos(curCell.m_tempDirectionToNextCell);

            if (direction == Vector2Int.zero)
            {
                Debug.Log("Cell has no direction value.");
                return null;
            }

            current += direction;

            // Path goes out of bounds, return null
            if (current.x < 0 || current.x == GridManager.Instance.m_gridWidth - 1 || current.y < 0 || current.y == GridManager.Instance.m_gridHeight - 1)
            {
                Debug.LogError("Path goes out of bounds.");
                return null;
            }
        }

        path.Add(endPos);

        return path;
    }
}