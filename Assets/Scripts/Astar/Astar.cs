using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Diagnostics;
using Vector2Int = UnityEngine.Vector2Int;

public class AStar
{
    public static (List<Vector2Int>, Vector3 endPosition) FindShortestPath(Vector2Int start, List<Vector2Int> endPositions)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        List<Vector2Int> shortestPath = null;
        Vector3 endPosition = new Vector3();

        foreach (Vector2Int end in endPositions)
        {
            path = FindPath(start, end);
            if (path == null)
            {
                continue;
            }
            
            //Debug.Log($"Path to {end} is {path.Count} long.");
            
            if (shortestPath == null)
            {
                shortestPath = path;
                endPosition = new Vector3(end.x, 0, end.y);
            }
            else if (path.Count < shortestPath.Count)
            {
                shortestPath = path;
                endPosition = new Vector3(end.x, 0, end.y);
            }
        }

        return (shortestPath, endPosition);
    }


    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        //if (start == end || Util.GetCellFromPos(start).m_isOccupied || Util.GetCellFromPos(end).m_isOccupied) return null;

        if (start == end)
        {
            Debug.Log($"End is same as start, returning end as path.");
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
        Debug.Log("No Path Found.");
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

    public static List<Vector2Int> FindIsland(Vector2Int startCell, Vector2Int preconTowerCell)
    {
        List<Vector2Int> islandCells = new List<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        PerformDFS(startCell, islandCells, visited, preconTowerCell);
        return islandCells;
    }

    private static void PerformDFS(Vector2Int currentCell, List<Vector2Int> islandCells, HashSet<Vector2Int> visited, Vector2Int preconTowerCell)
    {
        // Check if the current cell is valid and not already visited
        Cell curCell = Util.GetCellFromPos(new Vector2Int(currentCell.x, currentCell.y));

        if (currentCell.x >= 0 && currentCell.x < GridManager.Instance.m_gridWidth &&
            currentCell.y >= 0 && currentCell.y < GridManager.Instance.m_gridHeight &&
            !visited.Contains(currentCell) &&
            !curCell.m_isOccupied &&
            currentCell != preconTowerCell)
        {
            // Mark the current cell as visited
            visited.Add(currentCell);

            // Add the current cell to the list of island cells
            islandCells.Add(currentCell);

            // Recursively explore the neighbors of the current cell
            if (curCell.m_canPathNorth) PerformDFS(new Vector2Int(currentCell.x, currentCell.y + 1), islandCells, visited, preconTowerCell); // Up neighbor
            if (curCell.m_canPathEast) PerformDFS(new Vector2Int(currentCell.x + 1, currentCell.y), islandCells, visited, preconTowerCell); // Right neighbor
            if (curCell.m_canPathSouth) PerformDFS(new Vector2Int(currentCell.x, currentCell.y - 1), islandCells, visited, preconTowerCell); // Down neighbor
            if (curCell.m_canPathWest) PerformDFS(new Vector2Int(currentCell.x - 1, currentCell.y), islandCells, visited, preconTowerCell); // Left neighbor
        }
    }

    public static List<Vector2Int> FindExitPath(Vector2Int start, Vector2Int end, Vector2Int precon, Vector2Int exit)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        if (start == end || Util.GetCellFromPos(start).m_isOccupied || Util.GetCellFromPos(end).m_isOccupied)
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
        Debug.Log($"No Path Found. {start} - {end}");
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
            if (curCell.m_isOccupied && curCell.m_isTempOccupied)
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