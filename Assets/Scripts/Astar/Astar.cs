using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Diagnostics;

public class AStar
{
    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        if (start == goal || Util.GetCellFromPos(start).m_isOccupied || Util.GetCellFromPos(goal).m_isOccupied)
            return null;

        // Priority queue for open nodes
        PriorityQueue<Node> openNodes = new PriorityQueue<Node>();

        // List to keep track of visited nodes
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // Add the start node to openNodes
        openNodes.Enqueue(new Node(start, null, 0, Heuristic(start, goal)));

        while (openNodes.Count > 0)
        {
            // Get the node with the lowest f value from openNodes
            Node current = openNodes.Dequeue();

            // Check if we reached the goal
            if (current.position == goal)
            {
                // Reconstruct the path from the goal node to the start node
                Node node = current;
                while (node != null)
                {
                    path.Add(node.position);
                    node = node.parent;
                }
                path.Reverse(); // Reverse the path to get the correct order
                Debug.Log("Path Found.");
                return path;
            }

            // Mark the current node as visited
            visited.Add(current.position);

            // Get the neighboring cells (North, East, South, and West)
            Vector2Int[] neighbors = {
                new Vector2Int(current.position.x, current.position.y + 1),
                new Vector2Int(current.position.x + 1, current.position.y),
                new Vector2Int(current.position.x, current.position.y - 1),
                new Vector2Int(current.position.x - 1, current.position.y)
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                if (!visited.Contains(neighbor) && neighbor.x >= 0 && neighbor.x < GridManager.Instance.m_gridWidth &&
                    neighbor.y >= 0 && neighbor.y < GridManager.Instance.m_gridHeight && !Util.GetCellFromPos(neighbor).m_isOccupied)
                {
                    // Calculate the g value (cost from start to neighbor)
                    float g = current.g + 1;

                    // Calculate the h value (heuristic value from neighbor to goal)
                    float h = Heuristic(neighbor, goal);

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
    
    public static List<Vector2Int> FindIsland(Vector2Int startCell)
    {
        List<Vector2Int> islandCells = new List<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        PerformDFS(startCell, islandCells, visited);
        return islandCells;
    }
    
    private static void PerformDFS(Vector2Int currentCell, List<Vector2Int> islandCells, HashSet<Vector2Int> visited)
    {
        // Check if the current cell is valid and not already visited
        if (currentCell.x >= 0 && currentCell.x < GridManager.Instance.m_gridWidth &&
            currentCell.y >= 0 && currentCell.y <  GridManager.Instance.m_gridHeight &&
            !visited.Contains(currentCell) &&
            !Util.GetCellFromPos(currentCell).m_isOccupied)
        {
            // Mark the current cell as visited
            visited.Add(currentCell);

            // Add the current cell to the list of island cells
            islandCells.Add(currentCell);
            
            // Recursively explore the neighbors of the current cell
            PerformDFS(new Vector2Int(currentCell.x + 1, currentCell.y), islandCells, visited); // Right neighbor
            PerformDFS(new Vector2Int(currentCell.x - 1, currentCell.y), islandCells, visited); // Left neighbor
            PerformDFS(new Vector2Int(currentCell.x, currentCell.y + 1), islandCells, visited); // Up neighbor
            PerformDFS(new Vector2Int(currentCell.x, currentCell.y - 1), islandCells, visited); // Down neighbor
        }
    }
}
