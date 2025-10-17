using UnityEngine;

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
    public int attackRange = 1;

    [Header("State")]
    public bool hasActedThisTurn = false;
    public Tile currentTile;

    [Header("Visual Feedback")]
    private Renderer rend;
    private Color originalColor;
    private bool isInitialized = false;

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
    }

    void SnapToTile()
    {
        if (isInitialized) return;

        // Try to snap to nearest tile
        Tile nearestTile = FindNearestTile();
        if (nearestTile != null)
        {
            MoveTo(nearestTile);
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
        Debug.Log($"Clicked on {dinoName}!");

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null!");
            return;
        }

        Debug.Log($"Current game state: {GameManager.Instance.currentState}");

        if (GameManager.Instance.currentState != GameState.PlayerTurn)
        {
            Debug.Log("Not player turn!");
            return;
        }

        if (team == Team.Player && !hasActedThisTurn)
        {
            Debug.Log($"Selecting {dinoName}");
            GameManager.Instance.SelectUnit(this);
        }
        else if (team == Team.Player && hasActedThisTurn)
        {
            Debug.Log($"{dinoName} has already acted this turn!");
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

    public void TakeDamage(int damage)
    {
        int actualDamage = Mathf.Max(1, damage - defense);
        currentHealth -= actualDamage;

        Debug.Log($"{dinoName} took {actualDamage} damage! HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Attack(DinoUnit target)
    {
        Debug.Log($"{dinoName} attacks {target.dinoName}!");
        target.TakeDamage(attackPower);
        hasActedThisTurn = true;
    }

    public void MoveTo(Tile tile)
    {
        Debug.Log($"{dinoName} moving to tile ({tile.x}, {tile.z})");

        if (currentTile != null)
            currentTile.occupyingUnit = null;

        currentTile = tile;
        tile.occupyingUnit = this;

        float tileSize = GridManager.Instance != null ? GridManager.Instance.tileSize : 1f;
        transform.position = new Vector3(tile.x * tileSize, 0.5f, tile.z * tileSize);

        hasActedThisTurn = true;
    }

    void Die()
    {
        Debug.Log($"{dinoName} has been defeated!");
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
        hasActedThisTurn = false;
    }
}