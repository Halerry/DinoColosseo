using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int width = 12;
    public int height = 12;
    public float tileSize = 1f;
    public GameObject tilePrefab;

    private Tile[,] grid;
    private bool isGridGenerated = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Tile Prefab is not assigned in GridManager!");
            return;
        }

        grid = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0, z * tileSize);
                GameObject tileObj = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                tileObj.name = $"Tile_{x}_{z}";

                Tile tile = tileObj.GetComponent<Tile>();
                if (tile != null)
                {
                    tile.x = x;
                    tile.z = z;
                    grid[x, z] = tile;
                }
                else
                {
                    Debug.LogError("Tile prefab is missing Tile script!");
                }
            }
        }

        isGridGenerated = true;
        Debug.Log($"Grid generated: {width}x{height}");
    }

    public Tile GetTile(int x, int z)
    {
        if (!isGridGenerated || grid == null)
            return null;

        if (x >= 0 && x < width && z >= 0 && z < height)
            return grid[x, z];
        return null;
    }

    public bool IsGridReady()
    {
        return isGridGenerated && grid != null;
    }
}