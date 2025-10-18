using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider slider;
    public Image fillImage;

    [Header("Colors")]
    public Color fullHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;

    private Canvas canvas;
    private Camera mainCamera;
    private Transform target;
    public Vector3 offset = new Vector3(0, 2.5f, 0);

    void Start()
    {
        mainCamera = Camera.main;
        canvas = GetComponent<Canvas>();

        if (canvas != null)
        {
            canvas.worldCamera = mainCamera;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void LateUpdate()
    {
        // Follow the target
        if (target != null)
        {
            transform.position = target.position + offset;

            // Make health bar face camera
            if (mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            }
        }
    }

    public void SetMaxHealth(int maxHealth)
    {
        if (slider != null)
        {
            slider.maxValue = maxHealth;
            slider.value = maxHealth;
        }
    }

    public void SetHealth(int health)
    {
        if (slider != null)
        {
            slider.value = health;

            // Update color based on health percentage
            float healthPercent = (float)health / slider.maxValue;
            UpdateHealthColor(healthPercent);
        }
    }

    void UpdateHealthColor(float healthPercent)
    {
        if (fillImage == null) return;

        if (healthPercent > 0.6f)
        {
            fillImage.color = fullHealthColor;
        }
        else if (healthPercent > 0.3f)
        {
            fillImage.color = midHealthColor;
        }
        else
        {
            fillImage.color = lowHealthColor;
        }
    }
}