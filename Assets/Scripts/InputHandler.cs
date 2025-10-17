using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public Camera mainCamera;
    public bool showDebugRay = true;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("InputHandler: No camera found!");
        }
        else
        {
            Debug.Log($"InputHandler: Using camera at position {mainCamera.transform.position}");
        }
    }

    void Update()
    {
        // Try to detect mouse clicks using multiple methods
        bool clicked = false;

        // Method 1: Try new Input System
        try
        {
            if (UnityEngine.InputSystem.Mouse.current != null &&
                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                clicked = true;
                Debug.Log("Click detected via NEW Input System");
            }
        }
        catch
        {
            // New Input System not available, try old input
            if (Input.GetMouseButtonDown(0))
            {
                clicked = true;
                Debug.Log("Click detected via OLD Input System");
            }
        }

        if (clicked)
        {
            HandleClick();
        }
    }

    void HandleClick()
    {
        if (mainCamera == null)
        {
            Debug.LogError("No camera assigned!");
            return;
        }

        Vector3 mousePos;

        // Get mouse position
        try
        {
            mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        }
        catch
        {
            mousePos = Input.mousePosition;
        }

        Debug.Log($"=== CLICK DEBUG ===");
        Debug.Log($"Mouse screen position: {mousePos}");
        Debug.Log($"Camera position: {mainCamera.transform.position}");
        Debug.Log($"Camera rotation: {mainCamera.transform.rotation.eulerAngles}");

        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        Debug.Log($"Ray origin: {ray.origin}");
        Debug.Log($"Ray direction: {ray.direction}");

        // Draw debug ray in Scene view
        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);
        }

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            Debug.Log($"✓ HIT SOMETHING!");
            Debug.Log($"Hit object: {hit.collider.gameObject.name}");
            Debug.Log($"Hit position: {hit.point}");
            Debug.Log($"Hit distance: {hit.distance}");

            // Visual feedback - draw a sphere at hit point
            Debug.DrawLine(ray.origin, hit.point, Color.green, 2f);

            // Check if we clicked a dino
            DinoUnit dino = hit.collider.GetComponent<DinoUnit>();
            if (dino != null)
            {
                Debug.Log($">>> Found DinoUnit: {dino.dinoName} (Team: {dino.team})");
                dino.OnClick();
                return;
            }

            // Check if we clicked a tile
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null)
            {
                Debug.Log($">>> Found Tile at ({tile.x}, {tile.z})");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnTileClicked(tile);
                }
                else
                {
                    Debug.LogError("GameManager.Instance is null!");
                }
                return;
            }

            Debug.LogWarning($"Clicked object '{hit.collider.gameObject.name}' has no DinoUnit or Tile component");
        }
        else
        {
            Debug.LogWarning("Raycast hit NOTHING! Check camera position and rotation.");
        }

        Debug.Log($"=== END CLICK DEBUG ===");
    }
}