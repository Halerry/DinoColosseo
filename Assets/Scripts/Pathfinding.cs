using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Pathfinding : MonoBehaviour
{
    public static Pathfinding Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    // Find all tiles within movement range using flood fill
    public List<Tile> GetTilesInRange(Tile startTile, int range)
    {
        List<Tile> tilesInRange = new List<Tile>();
        Queue<Tile> queue = new Queue<Tile>();
        Dictionary<Tile, int> distances = new Dictionary<Tile, int>();

        queue.Enqueue(startTile);
        distances[startTile] = 0;

        while (queue.Count > 0)
        {
            Tile current = queue.Dequeue();
            int currentDist = distances[current];

            if (currentDist < range)
            {
                // Check all 4 neighbors (up, down, left, right)
                List<Tile> neighbors = GetNeighbors(current);

                foreach (Tile neighbor in neighbors)
                {
                    // Only consider walkable tiles (empty or with units we can attack)
                    if (!distances.ContainsKey(neighbor))
                    {
                        distances[neighbor] = currentDist + 1;

                        // Only add empty tiles to movement range
                        if (neighbor.occupyingUnit == null)
                        {
                            queue.Enqueue(neighbor);
                            tilesInRange.Add(neighbor);
                        }
                    }
                }
            }
        }

        return tilesInRange;
    }

    // A* pathfinding algorithm
    public List<Tile> FindPath(Tile start, Tile goal)
    {
        if (start == null || goal == null) return null;

        // If goal is occupied, can't move there
        if (goal.occupyingUnit != null) return null;

        List<Tile> openSet = new List<Tile>();
        HashSet<Tile> closedSet = new HashSet<Tile>();
        Dictionary<Tile, Tile> cameFrom = new Dictionary<Tile, Tile>();
        Dictionary<Tile, int> gScore = new Dictionary<Tile, int>();
        Dictionary<Tile, int> fScore = new Dictionary<Tile, int>();

        openSet.Add(start);
        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);

        while (openSet.Count > 0)
        {
            // Get tile with lowest fScore
            Tile current = openSet.OrderBy(t => fScore.ContainsKey(t) ? fScore[t] : int.MaxValue).First();

            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Tile neighbor in GetNeighbors(current))
            {
                // Skip occupied tiles (except the goal)
                if (neighbor.occupyingUnit != null && neighbor != goal)
                    continue;

                if (closedSet.Contains(neighbor))
                    continue;

                int tentativeGScore = gScore[current] + 1;

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeGScore >= (gScore.ContainsKey(neighbor) ? gScore[neighbor] : int.MaxValue))
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);
            }
        }

        return null; // No path found
    }

    List<Tile> ReconstructPath(Dictionary<Tile, Tile> cameFrom, Tile current)
    {
        List<Tile> path = new List<Tile>();
        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    int Heuristic(Tile a, Tile b)
    {
        // Manhattan distance
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }

    List<Tile> GetNeighbors(Tile tile)
    {
        List<Tile> neighbors = new List<Tile>();

        // Up, Down, Left, Right
        Tile up = GridManager.Instance.GetTile(tile.x, tile.z + 1);
        Tile down = GridManager.Instance.GetTile(tile.x, tile.z - 1);
        Tile left = GridManager.Instance.GetTile(tile.x - 1, tile.z);
        Tile right = GridManager.Instance.GetTile(tile.x + 1, tile.z);

        if (up != null) neighbors.Add(up);
        if (down != null) neighbors.Add(down);
        if (left != null) neighbors.Add(left);
        if (right != null) neighbors.Add(right);

        return neighbors;
    }
}