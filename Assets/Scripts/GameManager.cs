using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum GameState
{
    PlayerTurn,
    EnemyTurn,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState currentState;
    public DinoUnit selectedUnit;

    public List<DinoUnit> playerUnits = new List<DinoUnit>();
    public List<DinoUnit> enemyUnits = new List<DinoUnit>();

    private List<Tile> highlightedTiles = new List<Tile>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Wait a frame for all units to initialize
        Invoke("Initialize", 0.3f);
    }

    void Initialize()
    {
        FindAllUnits();
        StartPlayerTurn();
    }

    void FindAllUnits()
    {
        playerUnits.Clear();
        enemyUnits.Clear();

        DinoUnit[] allUnits = FindObjectsOfType<DinoUnit>();
        foreach (var unit in allUnits)
        {
            if (unit.team == Team.Player)
                playerUnits.Add(unit);
            else
                enemyUnits.Add(unit);
        }

        Debug.Log($"Found {playerUnits.Count} player units and {enemyUnits.Count} enemy units");
    }

    public void StartPlayerTurn()
    {
        currentState = GameState.PlayerTurn;
        Debug.Log("=== PLAYER TURN START ===");

        foreach (var unit in playerUnits)
        {
            if (unit != null)
                unit.ResetTurn();
        }
    }

    public void EndPlayerTurn()
    {
        Debug.Log("=== PLAYER TURN END ===");
        currentState = GameState.EnemyTurn;

        // Clear highlights and deselect
        ClearHighlights();
        if (selectedUnit != null)
        {
            selectedUnit.ResetColor();
            selectedUnit = null;
        }

        foreach (var unit in enemyUnits)
        {
            if (unit != null)
                unit.ResetTurn();
        }

        Invoke("ExecuteEnemyTurn", 1f);
    }

    void ExecuteEnemyTurn()
    {
        StartCoroutine(EnemyTurnCoroutine());
    }

    IEnumerator EnemyTurnCoroutine()
    {
        Debug.Log("=== ENEMY TURN START ===");

        bool anyEnemyActed = false;

        // Each enemy: move then attack if possible
        foreach (var enemy in enemyUnits)
        {
            if (enemy == null) continue;

            DinoUnit nearestTarget = FindNearestTarget(enemy, playerUnits);
            if (nearestTarget == null) continue;

            int dist = GetDistance(enemy.currentTile, nearestTarget.currentTile);
            Debug.Log($"{enemy.dinoName} is {dist} tiles away from {nearestTarget.dinoName}");

            // Try to move if haven't moved yet and not in attack range
            if (enemy.CanMove && dist > enemy.attackRange)
            {
                List<Tile> tilesInRange = Pathfinding.Instance.GetTilesInRange(enemy.currentTile, enemy.moveRange);

                // Find the tile in range that's closest to target
                Tile bestTile = null;
                int bestDist = int.MaxValue;

                foreach (Tile tile in tilesInRange)
                {
                    int distToTarget = GetDistance(tile, nearestTarget.currentTile);
                    if (distToTarget < bestDist)
                    {
                        bestDist = distToTarget;
                        bestTile = tile;
                    }
                }

                if (bestTile != null)
                {
                    Debug.Log($"{enemy.dinoName} moving closer to {nearestTarget.dinoName}");
                    List<Tile> path = Pathfinding.Instance.FindPath(enemy.currentTile, bestTile);
                    if (path != null && path.Count > 0)
                    {
                        enemy.MoveAlongPath(path);
                        anyEnemyActed = true;

                        // Wait for movement to complete
                        while (enemy.isMoving)
                        {
                            yield return null;
                        }
                    }
                }

                // Recalculate distance after moving
                dist = GetDistance(enemy.currentTile, nearestTarget.currentTile);
            }

            // Small delay between actions
            yield return new WaitForSeconds(0.3f);

            // Try to attack if in range and haven't attacked
            if (enemy.CanAttack && dist <= enemy.attackRange)
            {
                Debug.Log($"{enemy.dinoName} attacking {nearestTarget.dinoName}!");
                enemy.Attack(nearestTarget);
                anyEnemyActed = true;

                yield return new WaitForSeconds(0.5f);
            }
        }

        if (!anyEnemyActed)
        {
            Debug.Log("No enemies could act this turn");
        }

        Debug.Log("=== ENEMY TURN END ===");
        yield return new WaitForSeconds(1f);
        StartPlayerTurn();
    }

    public void OnTileClicked(Tile tile)
    {
        Debug.Log($"Tile clicked: ({tile.x}, {tile.z})");

        if (selectedUnit == null)
        {
            Debug.Log("No unit selected");
            return;
        }

        Debug.Log($"Selected unit: {selectedUnit.dinoName} (Moved: {selectedUnit.hasMoved}, Attacked: {selectedUnit.hasAttacked})");

        // Check if clicking on an enemy to attack
        if (tile.occupyingUnit != null && tile.occupyingUnit.team != selectedUnit.team)
        {
            if (!selectedUnit.CanAttack)
            {
                Debug.Log("Already attacked this turn!");
                return;
            }

            int dist = GetDistance(selectedUnit.currentTile, tile);
            Debug.Log($"Distance to enemy: {dist}, Attack range: {selectedUnit.attackRange}");

            if (dist <= selectedUnit.attackRange)
            {
                Debug.Log("Attacking enemy!");
                selectedUnit.Attack(tile.occupyingUnit);

                // If unit is done, deselect, otherwise show movement range
                if (selectedUnit.HasFinishedTurn)
                {
                    ClearHighlights();
                    DeselectUnit();
                }
                else if (!selectedUnit.hasMoved)
                {
                    ClearHighlights();
                    HighlightMovementRange();
                }
            }
            else
            {
                Debug.Log("Too far to attack!");
            }
            return;
        }

        // Check if clicking on a friendly unit
        if (tile.occupyingUnit != null && tile.occupyingUnit.team == selectedUnit.team)
        {
            Debug.Log("Selecting another friendly unit");
            SelectUnit(tile.occupyingUnit);
            return;
        }

        // Try to move to empty tile
        if (tile.occupyingUnit == null)
        {
            if (!selectedUnit.CanMove)
            {
                Debug.Log("Already moved this turn!");
                return;
            }

            // Check if tile is in highlighted range
            if (highlightedTiles.Contains(tile))
            {
                Debug.Log("Moving unit along path!");
                List<Tile> path = Pathfinding.Instance.FindPath(selectedUnit.currentTile, tile);

                if (path != null && path.Count > 0)
                {
                    ClearHighlights();
                    selectedUnit.MoveAlongPath(path);
                    // Don't deselect - unit can still attack after moving
                }
                else
                {
                    Debug.Log("No path found!");
                }
            }
            else
            {
                Debug.Log("Tile not in movement range!");
            }
        }
    }

    public void SelectUnit(DinoUnit unit)
    {
        // Clear previous highlights
        ClearHighlights();

        // Deselect previous unit
        if (selectedUnit != null)
            selectedUnit.ResetColor();

        // Select new unit
        selectedUnit = unit;

        if (selectedUnit != null && !selectedUnit.HasFinishedTurn)
        {
            Debug.Log($">>> Selected: {unit.dinoName} <<<");
            selectedUnit.Highlight(new Color(1f, 1f, 0.5f, 1f)); // Yellow highlight

            // Show appropriate range based on state
            if (selectedUnit.CanMove)
            {
                HighlightMovementRange();
            }
            else if (selectedUnit.CanAttack)
            {
                ShowAttackRange();
            }
        }
    }

    void HighlightMovementRange()
    {
        if (selectedUnit == null || Pathfinding.Instance == null) return;

        highlightedTiles = Pathfinding.Instance.GetTilesInRange(selectedUnit.currentTile, selectedUnit.moveRange);

        foreach (Tile tile in highlightedTiles)
        {
            tile.HighlightTile(new Color(0.5f, 0.8f, 1f, 0.6f)); // Light blue
        }

        Debug.Log($"Highlighted {highlightedTiles.Count} movement tiles");
    }

    public void ShowAttackRange()
    {
        if (selectedUnit == null) return;

        ClearHighlights();

        // Highlight tiles in attack range
        for (int x = -selectedUnit.attackRange; x <= selectedUnit.attackRange; x++)
        {
            for (int z = -selectedUnit.attackRange; z <= selectedUnit.attackRange; z++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(z) <= selectedUnit.attackRange)
                {
                    Tile tile = GridManager.Instance.GetTile(selectedUnit.currentTile.x + x, selectedUnit.currentTile.z + z);
                    if (tile != null && tile != selectedUnit.currentTile)
                    {
                        // Red highlight for enemy tiles, orange for empty
                        if (tile.occupyingUnit != null && tile.occupyingUnit.team != selectedUnit.team)
                        {
                            tile.HighlightTile(new Color(1f, 0.3f, 0.3f, 0.7f)); // Red
                        }
                        else if (tile.occupyingUnit == null)
                        {
                            tile.HighlightTile(new Color(1f, 0.6f, 0.2f, 0.5f)); // Orange
                        }
                        highlightedTiles.Add(tile);
                    }
                }
            }
        }

        Debug.Log($"Highlighted {highlightedTiles.Count} attack tiles");
    }

    void ClearHighlights()
    {
        foreach (Tile tile in highlightedTiles)
        {
            if (tile != null)
                tile.ResetTile();
        }
        highlightedTiles.Clear();
    }

    void DeselectUnit()
    {
        if (selectedUnit != null)
            selectedUnit.ResetColor();
        selectedUnit = null;
        Debug.Log("Unit deselected");
    }

    public void TryAttackUnit(DinoUnit target)
    {
        if (selectedUnit == null || target == null) return;
        if (selectedUnit.team == target.team) return;
        if (!selectedUnit.CanAttack) return;

        int dist = GetDistance(selectedUnit.currentTile, target.currentTile);
        if (dist <= selectedUnit.attackRange)
        {
            selectedUnit.Attack(target);

            if (selectedUnit.HasFinishedTurn)
            {
                ClearHighlights();
                DeselectUnit();
            }
        }
        else
        {
            Debug.Log("Target out of range!");
        }
    }

    DinoUnit FindNearestTarget(DinoUnit unit, List<DinoUnit> targets)
    {
        DinoUnit nearest = null;
        int minDist = int.MaxValue;

        foreach (var target in targets)
        {
            if (target == null || target.currentTile == null) continue;

            int dist = GetDistance(unit.currentTile, target.currentTile);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = target;
            }
        }

        return nearest;
    }

    int GetDistance(Tile a, Tile b)
    {
        if (a == null || b == null) return int.MaxValue;
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }

    void Update()
    {
        // Check for keyboard input using new Input System
        if (currentState == GameState.PlayerTurn)
        {
            // End turn with E key
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                Debug.Log("E pressed - ending turn");
                EndPlayerTurn();
            }

            // Deselect with Escape key
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ClearHighlights();
                DeselectUnit();
            }
        }
    }
}