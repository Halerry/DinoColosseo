using UnityEngine;
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

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Wait a frame for all units to initialize
        Invoke("Initialize", 0.1f);
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
        Debug.Log("=== PLAYER TURN ===");

        foreach (var unit in playerUnits)
        {
            if (unit != null)
                unit.ResetTurn();
        }
    }

    public void EndPlayerTurn()
    {
        currentState = GameState.EnemyTurn;
        Debug.Log("=== ENEMY TURN ===");

        // Deselect any selected unit
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
        // Simple AI: Attack nearest player unit
        foreach (var enemy in enemyUnits)
        {
            if (enemy == null || enemy.hasActedThisTurn) continue;

            DinoUnit nearestTarget = FindNearestTarget(enemy, playerUnits);
            if (nearestTarget != null)
            {
                int dist = GetDistance(enemy.currentTile, nearestTarget.currentTile);
                if (dist <= enemy.attackRange)
                {
                    enemy.Attack(nearestTarget);
                }
            }
        }

        Invoke("StartPlayerTurn", 1f);
    }

    public void OnTileClicked(Tile tile)
    {
        if (selectedUnit == null)
        {
            // Clicking empty tile does nothing when no unit selected
            return;
        }
        else
        {
            // Try to move or attack
            if (tile.occupyingUnit == null)
            {
                // Move
                int dist = GetDistance(selectedUnit.currentTile, tile);
                if (dist <= selectedUnit.moveRange && !selectedUnit.hasActedThisTurn)
                {
                    selectedUnit.MoveTo(tile);
                    DeselectUnit();
                }
            }
            else if (tile.occupyingUnit.team != selectedUnit.team)
            {
                // Attack
                int dist = GetDistance(selectedUnit.currentTile, tile);
                if (dist <= selectedUnit.attackRange && !selectedUnit.hasActedThisTurn)
                {
                    selectedUnit.Attack(tile.occupyingUnit);
                    DeselectUnit();
                }
            }
            else
            {
                // Clicked another friendly unit
                SelectUnit(tile.occupyingUnit);
            }
        }
    }

    public void SelectUnit(DinoUnit unit)
    {
        // Deselect previous unit
        if (selectedUnit != null)
            selectedUnit.ResetColor();

        // Select new unit (can be null to deselect)
        selectedUnit = unit;

        if (selectedUnit != null)
        {
            Debug.Log($"Selected: {unit.dinoName}");
            selectedUnit.Highlight(new Color(1f, 1f, 0.5f, 1f)); // Yellow highlight
        }
    }

    void DeselectUnit()
    {
        if (selectedUnit != null)
            selectedUnit.ResetColor();
        selectedUnit = null;
    }

    public void TryAttackUnit(DinoUnit target)
    {
        if (selectedUnit == null || target == null) return;
        if (selectedUnit.team == target.team) return;

        int dist = GetDistance(selectedUnit.currentTile, target.currentTile);
        if (dist <= selectedUnit.attackRange && !selectedUnit.hasActedThisTurn)
        {
            selectedUnit.Attack(target);
            DeselectUnit();
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
                EndPlayerTurn();
            }

            // Deselect with Escape key
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                DeselectUnit();
            }
        }
    }
}