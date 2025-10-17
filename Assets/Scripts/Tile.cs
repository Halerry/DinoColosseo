using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x, z;
    public DinoUnit occupyingUnit;

    private Renderer rend;
    private Color originalColor;

    [Header("Highlight Colors")]
    public Color moveableColor = new Color(0.5f, 0.5f, 1f, 0.5f);
    public Color attackableColor = new Color(1f, 0.3f, 0.3f, 0.5f);

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;
    }

    public void HighlightTile(Color color)
    {
        if (rend != null)
            rend.material.color = color;
    }

    public void ResetTile()
    {
        if (rend != null)
            rend.material.color = originalColor;
    }
}