using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum Team { Player, Enemy }

public class DinoUnit : MonoBehaviour
{
    [Header("Unit Info")]
    public string dinoName = "T-Rex";
    public Team team;

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int attackPower = 20;
    public int defense = 5;
    public int moveRange = 3;
    public int attackRange = 2;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("State")]
    public bool hasMoved = false;
    public bool hasAttacked = false;
    public bool isDefending = false;
    public Tile currentTile;
    public bool isMoving = false;

    [Header("Visual Feedback")]
    private Renderer rend;
    private Color originalColor;
    private bool isInitialized = false;

    [Header("UI")]
    public HealthBar healthBar;

    // Check if unit has finished all actions (only move + attack matter)
    public bool HasFinishedTurn => hasMoved && hasAttacked;
    public bool CanMove => !hasMoved && !isMoving;
    public bool CanAttack => !hasAttacked;

    void Start()
    {
        currentHealth = maxHealth;

        // Get renderer for highlight effects
        rend = GetComponent<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;

        // Make sure we have a collider
        if (GetComponent<Collider>() == null)
        {
            Debug.LogError($"{dinoName} is missing a Collider! Add a Capsule Collider.");
        }

        // Wait for grid to be created before snapping to tile
        Invoke("SnapToTile", 0.2f);

        // Create health bar
        Invoke("CreateHealthBar", 0.3f);
    }

    void CreateHealthBar()
    {
        // Find or create health bar
        GameObject healthBarObj = transform.Find("HealthBar")?.gameObject;

        if (healthBarObj == null && HealthBarManager.Instance != null)
        {
            // Health bar will be created by HealthBarManager
            HealthBarManager.Instance.CreateHealthBar(this);
        }
        else if (healthBarObj != null)
        {
            healthBar = healthBarObj.GetComponent<HealthBar>();
        }

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
    }

    void SnapToTile()
    {
        if (isInitialized) return;

        // Try to snap to nearest tile
        Tile nearestTile = FindNearestTile();
        if (nearestTile != null)
        {
            // Set position WITHOUT marking as acted
            if (currentTile != null)
                currentTile.occupyingUnit = null;

            currentTile = nearestTile;
            nearestTile.occupyingUnit = this;

            float tileSize = GridManager.Instance != null ? GridManager.Instance.tileSize : 1f;
            transform.position = new Vector3(nearestTile.x * tileSize, 0.5f, nearestTile.z * tileSize);

            isInitialized = true;
            Debug.Log($"{dinoName} initialized at tile ({currentTile.x}, {currentTile.z})");
        }
        else
        {
            // Grid not ready yet, try again
            Invoke("SnapToTile", 0.1f);
        }
    }

    // Called by InputHandler when this dino is clicked
    public void OnClick()
    {
        if (isMoving) return; // Can't click while moving

        Debug.Log($"Clicked on {dinoName}! (Moved: {hasMoved}, Attacked: {hasAttacked})");

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null!");
            return;
        }

        if (GameManager.Instance.currentState != GameState.PlayerTurn)
        {
            Debug.Log("Not player turn!");
            return;
        }

        if (team == Team.Player && !HasFinishedTurn)
        {
            Debug.Log($"Selecting {dinoName}");
            GameManager.Instance.SelectUnit(this);
        }
        else if (team == Team.Player && HasFinishedTurn)
        {
            Debug.Log($"{dinoName} has finished all actions this turn!");
        }
        else if (team == Team.Enemy)
        {
            Debug.Log("Trying to attack enemy...");
            GameManager.Instance.TryAttackUnit(this);
        }
    }

    public void Highlight(Color color)
    {
        if (rend != null)
            rend.material.color = color;
    }

    public void ResetColor()
    {
        if (rend != null)
            rend.material.color = originalColor;
    }

    Tile FindNearestTile()
    {
        // Check if grid exists
        if (GridManager.Instance == null) return null;
        if (GridManager.Instance.GetTile(0, 0) == null) return null; // Grid not ready

        Vector3 pos = transform.position;
        int x = Mathf.RoundToInt(pos.x / GridManager.Instance.tileSize);
        int z = Mathf.RoundToInt(pos.z / GridManager.Instance.tileSize);

        return GridManager.Instance.GetTile(x, z);
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        Debug.Log($"{dinoName} healed {amount} HP! HP: {currentHealth}/{maxHealth}");

        // Update health bar
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        // Check if defending
        if (isDefending)
        {
            Debug.Log($"{dinoName} blocked the attack with Defend!");
            isDefending = false;
            return;
        }

        int actualDamage = Mathf.Max(1, damage - defense);
        currentHealth -= actualDamage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"{dinoName} took {actualDamage} damage! HP: {currentHealth}/{maxHealth}");

        // Update health bar
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Attack(DinoUnit target)
    {
        Debug.Log($"{dinoName} attacks {target.dinoName}!");
        target.TakeDamage(attackPower);
        hasAttacked = true; // Only one attack per turn

        // Update highlight to show unit is done
        if (HasFinishedTurn && GameManager.Instance != null && GameManager.Instance.selectedUnit == this)
        {
            Highlight(new Color(0.5f, 0.5f, 0.5f, 1f)); // Gray out when done
        }
    }

    // Charge attack - deals 10 damage, doesn't count as attack
    public void ChargeAttack(DinoUnit target)
    {
        Debug.Log($"{dinoName} charges into {target.dinoName} for 10 damage!");
        target.TakeDamage(10);
        // DON'T set hasAttacked = true, because Charge doesn't count as an attack
    }

    // Move along a path tile by tile
    public void MoveAlongPath(List<Tile> path)
    {
        if (path == null || path.Count == 0) return;

        StartCoroutine(MoveCoroutine(path));
    }

    IEnumerator MoveCoroutine(List<Tile> path)
    {
        isMoving = true;

        // Skip first tile (it's the current position)
        for (int i = 1; i < path.Count; i++)
        {
            Tile targetTile = path[i];

            // Update tile occupancy
            if (currentTile != null)
                currentTile.occupyingUnit = null;

            currentTile = targetTile;
            targetTile.occupyingUnit = this;

            // Animate movement
            float tileSize = GridManager.Instance != null ? GridManager.Instance.tileSize : 1f;
            Vector3 targetPos = new Vector3(targetTile.x * tileSize, 0.5f, targetTile.z * tileSize);

            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPos;
        }

        hasMoved = true;
        isMoving = false;

        Debug.Log($"{dinoName} finished moving to ({currentTile.x}, {currentTile.z})");
    }

    // Instant move (for compatibility)
    public void MoveTo(Tile tile)
    {
        Debug.Log($"{dinoName} moving to tile ({tile.x}, {tile.z})");

        if (currentTile != null)
            currentTile.occupyingUnit = null;

        currentTile = tile;
        tile.occupyingUnit = this;

        float tileSize = GridManager.Instance != null ? GridManager.Instance.tileSize : 1f;
        transform.position = new Vector3(tile.x * tileSize, 0.5f, tile.z * tileSize);

        hasMoved = true;
    }

    void Die()
    {
        Debug.Log($"{dinoName} has been defeated!");

        // Destroy health bar first
        if (healthBar != null && healthBar.gameObject != null)
        {
            Destroy(healthBar.gameObject);
        }

        if (currentTile != null)
            currentTile.occupyingUnit = null;

        // If this was the selected unit, deselect it
        if (GameManager.Instance != null && GameManager.Instance.selectedUnit == this)
        {
            GameManager.Instance.SelectUnit(null);
        }

        Destroy(gameObject);
    }

    public void ResetTurn()
    {
        hasMoved = false;
        hasAttacked = false;
        isDefending = false;
        Debug.Log($"{dinoName} turn reset - can move and attack again");
    }
}