using UnityEngine;

public class HealthBarManager : MonoBehaviour
{
    public static HealthBarManager Instance { get; private set; }

    [Header("Prefab")]
    public GameObject healthBarPrefab;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2f, 0);

    void Awake()
    {
        Instance = this;
    }

    public HealthBar CreateHealthBar(DinoUnit unit)
    {
        if (healthBarPrefab == null)
        {
            Debug.LogError("Health Bar Prefab is not assigned!");
            return null;
        }

        // Instantiate health bar
        GameObject healthBarObj = Instantiate(healthBarPrefab);
        healthBarObj.name = $"HealthBar_{unit.dinoName}";

        HealthBar healthBar = healthBarObj.GetComponent<HealthBar>();
        if (healthBar != null)
        {
            healthBar.SetTarget(unit.transform);
            healthBar.offset = offset;
            healthBar.SetMaxHealth(unit.maxHealth);
            healthBar.SetHealth(unit.currentHealth);

            unit.healthBar = healthBar;
        }

        return healthBar;
    }
}