using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    private CardType currentCardMode = CardType.Attack;
    private bool isPlayingCard = false;
    private bool waitingForChargeTarget = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
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

        if (HandManager.Instance != null)
        {
            HandManager.Instance.DrawCards(2, false);
        }
    }

    public void EndPlayerTurn()
    {
        Debug.Log("=== PLAYER TURN END ===");
        currentState = GameState.EnemyTurn;

        ClearHighlights();
        if (selectedUnit != null)
        {
            selectedUnit.ResetColor();
            selectedUnit = null;
        }

        isPlayingCard = false;
        waitingForChargeTarget = false;

        foreach (var unit in enemyUnits)
        {
            if (unit != null)
                unit.ResetTurn();
        }

        if (HandManager.Instance != null)
        {
            HandManager.Instance.DrawCards(2, true);
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

        List<Card> aiHand = HandManager.Instance != null ? HandManager.Instance.GetAIHand() : new List<Card>();

        foreach (var enemy in enemyUnits)
        {
            if (enemy == null) continue;

            DinoUnit nearestTarget = FindNearestTarget(enemy, playerUnits);
            if (nearestTarget == null) continue;

            int dist = GetDistance(enemy.currentTile, nearestTarget.currentTile);
            Debug.Log($"{enemy.dinoName} is {dist} tiles away from {nearestTarget.dinoName}");

            // AI uses cards
            if (aiHand.Count > 0)
            {
                yield return StartCoroutine(AIUseCards(enemy, nearestTarget, aiHand));
            }

            dist = GetDistance(enemy.currentTile, nearestTarget.currentTile);

            // Move
            if (enemy.CanMove && dist > enemy.attackRange)
            {
                List<Tile> tilesInRange = Pathfinding.Instance.GetTilesInRange(enemy.currentTile, enemy.moveRange);
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
                    List<Tile> path = Pathfinding.Instance.FindPath(enemy.currentTile, bestTile);
                    if (path != null && path.Count > 0)
                    {
                        enemy.MoveAlongPath(path);
                        while (enemy.isMoving)
                        {
                            yield return null;
                        }
                    }
                }

                dist = GetDistance(enemy.currentTile, nearestTarget.currentTile);
            }

            yield return new WaitForSeconds(0.3f);

            // Attack
            if (enemy.CanAttack && dist <= enemy.attackRange)
            {
                Card attackCard = aiHand.FirstOrDefault(c => c.cardType == CardType.Attack);
                if (attackCard != null)
                {
                    Debug.Log($"{enemy.dinoName} using Attack card!");
                    HandManager.Instance.AIPlayCard(attackCard, enemy, nearestTarget, null);
                    aiHand.Remove(attackCard);
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        Debug.Log("=== ENEMY TURN END ===");
        yield return new WaitForSeconds(1f);
        StartPlayerTurn();
    }

    IEnumerator AIUseCards(DinoUnit enemy, DinoUnit target, List<Card> aiHand)
    {
        // 1. Defend if low HP
        if (enemy.currentHealth < enemy.maxHealth / 3)
        {
            Card defendCard = aiHand.FirstOrDefault(c => c.cardType == CardType.Defend);
            if (defendCard != null)
            {
                Debug.Log($"AI {enemy.dinoName} uses Defend");
                HandManager.Instance.AIPlayCard(defendCard, enemy, null, null);
                aiHand.Remove(defendCard);
                yield return new WaitForSeconds(0.5f);
            }
        }

        // 2. Medicine if damaged
        if (enemy.currentHealth < enemy.maxHealth)
        {
            Card medicineCard = aiHand.FirstOrDefault(c => c.cardType == CardType.Medicine);
            if (medicineCard != null)
            {
                Debug.Log($"AI {enemy.dinoName} uses Medicine");
                HandManager.Instance.AIPlayCard(medicineCard, enemy, null, null);
                aiHand.Remove(medicineCard);
                yield return new WaitForSeconds(0.5f);
            }
        }

        // 3. Charge if beneficial
        int dist = GetDistance(enemy.currentTile, target.currentTile);
        if (dist > enemy.attackRange && dist <= 6)
        {
            Card chargeCard = aiHand.FirstOrDefault(c => c.cardType == CardType.Charge);
            if (chargeCard != null)
            {
                Tile chargeTile = FindBestChargeTarget(enemy, target);
                if (chargeTile != null)
                {
                    Debug.Log($"AI {enemy.dinoName} uses Charge!");
                    List<Tile> path = Pathfinding.Instance.FindPath(enemy.currentTile, chargeTile);
                    if (path != null && path.Count > 0)
                    {
                        enemy.MoveAlongPath(path);
                        while (enemy.isMoving)
                        {
                            yield return null;
                        }

                        enemy.ChargeAttack(target);
                        HandManager.Instance.AIPlayCard(chargeCard, enemy, target, chargeTile);
                        aiHand.Remove(chargeCard);
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }
        }
    }

    Tile FindBestChargeTarget(DinoUnit charger, DinoUnit target)
    {
        List<Tile> tilesInRange = Pathfinding.Instance.GetTilesInRange(charger.currentTile, 5);
        Tile[] adjacentToTarget = new Tile[]
        {
            GridManager.Instance.GetTile(target.currentTile.x + 1, target.currentTile.z),
            GridManager.Instance.GetTile(target.currentTile.x - 1, target.currentTile.z),
            GridManager.Instance.GetTile(target.currentTile.x, target.currentTile.z + 1),
            GridManager.Instance.GetTile(target.currentTile.x, target.currentTile.z - 1)
        };

        foreach (Tile adjTile in adjacentToTarget)
        {
            if (adjTile != null && adjTile.occupyingUnit == null && tilesInRange.Contains(adjTile))
            {
                return adjTile;
            }
        }
        return null;
    }

    public void SetCardMode(CardType cardType)
    {
        currentCardMode = cardType;
        isPlayingCard = (cardType == CardType.Attack || cardType == CardType.Charge);

        if (cardType == CardType.Attack && selectedUnit != null)
        {
            ShowAttackRange();
        }
    }

    public void ClearCardMode()
    {
        isPlayingCard = false;
        waitingForChargeTarget = false;
        currentCardMode = CardType.Attack;
    }

    public void ShowChargeRange(DinoUnit unit)
    {
        ClearHighlights();
        List<Tile> tilesInRange = Pathfinding.Instance.GetTilesInRange(unit.currentTile, 5);

        foreach (Tile tile in tilesInRange)
        {
            if (tile.occupyingUnit != null) continue;
            if (IsAdjacentToEnemy(tile, unit.team))
            {
                tile.HighlightTile(new Color(1f, 0.8f, 0.2f, 0.6f));
                highlightedTiles.Add(tile);
            }
        }
    }

    bool IsAdjacentToEnemy(Tile tile, Team playerTeam)
    {
        Tile[] adjacentTiles = new Tile[]
        {
            GridManager.Instance.GetTile(tile.x + 1, tile.z),
            GridManager.Instance.GetTile(tile.x - 1, tile.z),
            GridManager.Instance.GetTile(tile.x, tile.z + 1),
            GridManager.Instance.GetTile(tile.x, tile.z - 1)
        };

        foreach (Tile adjTile in adjacentTiles)
        {
            if (adjTile != null && adjTile.occupyingUnit != null && adjTile.occupyingUnit.team != playerTeam)
            {
                return true;
            }
        }
        return false;
    }

    public void OnTileClicked(Tile tile)
    {
        if (selectedUnit == null) return;

        if (isPlayingCard && currentCardMode == CardType.Charge && !waitingForChargeTarget)
        {
            if (highlightedTiles.Contains(tile) && tile.occupyingUnit == null)
            {
                List<Tile> path = Pathfinding.Instance.FindPath(selectedUnit.currentTile, tile);
                if (path != null && path.Count > 0)
                {
                    ClearHighlights();
                    selectedUnit.MoveAlongPath(path);
                    StartCoroutine(ShowChargeTargetsAfterMove(selectedUnit, tile));
                }
            }
            return;
        }

        if (tile.occupyingUnit != null && tile.occupyingUnit.team != selectedUnit.team)
        {
            if (waitingForChargeTarget)
            {
                int dist = GetDistance(selectedUnit.currentTile, tile);
                if (dist == 1)
                {
                    selectedUnit.ChargeAttack(tile.occupyingUnit);
                    HandManager.Instance.PlayCard(selectedUnit, tile.occupyingUnit, null);
                    ClearHighlights();
                    isPlayingCard = false;
                    waitingForChargeTarget = false;
                }
                return;
            }

            if (isPlayingCard && currentCardMode == CardType.Attack)
            {
                if (!selectedUnit.hasAttacked)
                {
                    int dist = GetDistance(selectedUnit.currentTile, tile);
                    if (dist <= selectedUnit.attackRange)
                    {
                        HandManager.Instance.PlayCard(selectedUnit, tile.occupyingUnit, null);
                        ClearHighlights();
                        isPlayingCard = false;
                    }
                }
            }
            return;
        }

        if (tile.occupyingUnit != null && tile.occupyingUnit.team == selectedUnit.team)
        {
            SelectUnit(tile.occupyingUnit);
            return;
        }

        if (tile.occupyingUnit == null && !isPlayingCard)
        {
            if (!selectedUnit.CanMove) return;

            if (highlightedTiles.Contains(tile))
            {
                List<Tile> path = Pathfinding.Instance.FindPath(selectedUnit.currentTile, tile);
                if (path != null && path.Count > 0)
                {
                    ClearHighlights();
                    selectedUnit.MoveAlongPath(path);
                }
            }
        }
    }

    IEnumerator ShowChargeTargetsAfterMove(DinoUnit charger, Tile targetTile)
    {
        while (charger.isMoving)
        {
            yield return null;
        }

        Tile[] adjacentTiles = new Tile[]
        {
            GridManager.Instance.GetTile(targetTile.x + 1, targetTile.z),
            GridManager.Instance.GetTile(targetTile.x - 1, targetTile.z),
            GridManager.Instance.GetTile(targetTile.x, targetTile.z + 1),
            GridManager.Instance.GetTile(targetTile.x, targetTile.z - 1)
        };

        List<DinoUnit> adjacentEnemies = new List<DinoUnit>();
        foreach (Tile adjTile in adjacentTiles)
        {
            if (adjTile != null && adjTile.occupyingUnit != null && adjTile.occupyingUnit.team != charger.team)
            {
                adjacentEnemies.Add(adjTile.occupyingUnit);
                adjTile.HighlightTile(new Color(1f, 0.3f, 0.3f, 0.8f));
                highlightedTiles.Add(adjTile);
            }
        }

        if (adjacentEnemies.Count > 0)
        {
            waitingForChargeTarget = true;
        }
        else
        {
            HandManager.Instance.PlayCard(charger, null, targetTile);
            isPlayingCard = false;
        }
    }

    public void SelectUnit(DinoUnit unit)
    {
        ClearHighlights();
        isPlayingCard = false;
        waitingForChargeTarget = false;

        if (selectedUnit != null)
            selectedUnit.ResetColor();

        selectedUnit = unit;

        if (selectedUnit != null && !selectedUnit.HasFinishedTurn)
        {
            selectedUnit.Highlight(new Color(1f, 1f, 0.5f, 1f));
            if (selectedUnit.CanMove)
            {
                HighlightMovementRange();
            }
        }
    }

    void HighlightMovementRange()
    {
        if (selectedUnit == null || Pathfinding.Instance == null) return;
        highlightedTiles = Pathfinding.Instance.GetTilesInRange(selectedUnit.currentTile, selectedUnit.moveRange);
        foreach (Tile tile in highlightedTiles)
        {
            tile.HighlightTile(new Color(0.5f, 0.8f, 1f, 0.6f));
        }
    }

    public void ShowAttackRange()
    {
        if (selectedUnit == null) return;
        ClearHighlights();

        for (int x = -selectedUnit.attackRange; x <= selectedUnit.attackRange; x++)
        {
            for (int z = -selectedUnit.attackRange; z <= selectedUnit.attackRange; z++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(z) <= selectedUnit.attackRange)
                {
                    Tile tile = GridManager.Instance.GetTile(selectedUnit.currentTile.x + x, selectedUnit.currentTile.z + z);
                    if (tile != null && tile != selectedUnit.currentTile)
                    {
                        if (tile.occupyingUnit != null && tile.occupyingUnit.team != selectedUnit.team)
                        {
                            tile.HighlightTile(new Color(1f, 0.3f, 0.3f, 0.7f));
                        }
                        else if (tile.occupyingUnit == null)
                        {
                            tile.HighlightTile(new Color(1f, 0.6f, 0.2f, 0.5f));
                        }
                        highlightedTiles.Add(tile);
                    }
                }
            }
        }
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
        isPlayingCard = false;
        waitingForChargeTarget = false;
    }

    public void TryAttackUnit(DinoUnit target)
    {
        if (selectedUnit == null || target == null) return;
        if (selectedUnit.team == target.team) return;

        if (waitingForChargeTarget)
        {
            int dist = GetDistance(selectedUnit.currentTile, target.currentTile);
            if (dist == 1)
            {
                selectedUnit.ChargeAttack(target);
                HandManager.Instance.PlayCard(selectedUnit, target, null);
                ClearHighlights();
                isPlayingCard = false;
                waitingForChargeTarget = false;
            }
            return;
        }

        if (!isPlayingCard || currentCardMode != CardType.Attack) return;
        if (!selectedUnit.CanAttack) return;

        int distance = GetDistance(selectedUnit.currentTile, target.currentTile);
        if (distance <= selectedUnit.attackRange)
        {
            HandManager.Instance.PlayCard(selectedUnit, target, null);
            ClearHighlights();
            isPlayingCard = false;
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
        if (currentState == GameState.PlayerTurn)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                EndPlayerTurn();
            }

            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ClearHighlights();
                DeselectUnit();
            }
        }
    }
}