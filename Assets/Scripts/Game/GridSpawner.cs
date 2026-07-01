using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float debrisChance = 0.15f;

    private GameObject[,] tiles;

    private void Start()
    {
        GenerateGrid();
        GenerateDebris();
    }

    private void GenerateGrid()
    {
        ClearGrid();
        tiles = new GameObject[width, height];

        float spacing = tileSize;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 origin = new Vector3(
                    -(width - 1) * spacing / 2f,
                    -(height - 1) * spacing / 2f,
                    0);

                Vector3 position = origin + new Vector3(
                    x * spacing,
                    y * spacing,
                    0);

                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                ApplyTileScale(tile);
                tiles[x, y] = tile;

                Tile tileScript = tile.GetComponent<Tile>();
                if (tileScript != null)
                {
                    tileScript.GridPosition = new Vector2Int(x, y);
                }
            }
        }
    }

    private void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    private void ApplyTileScale(GameObject tile)
    {
        if (tile == null)
        {
            return;
        }

        SpriteRenderer renderer = tile.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null && renderer.sprite != null)
        {
            Vector2 spriteSize = renderer.sprite.bounds.size;
            float currentSize = Mathf.Max(spriteSize.x, spriteSize.y);
            if (currentSize > 0f)
            {
                tile.transform.localScale = Vector3.one * (tileSize / currentSize);
                return;
            }
        }

        tile.transform.localScale = Vector3.one * tileSize;
    }

    private void GenerateDebris()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (Random.value < debrisChance)
                {
                    GameObject tileObject = tiles[x, y];
                    Tile tile = tileObject != null ? tileObject.GetComponent<Tile>() : null;
                    if (tile != null)
                    {
                        tile.PlaceDebris();
                    }
                }
            }
        }
    }

    public void DetonateMines()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject tileObject = tiles[x, y];
                Tile tile = tileObject != null ? tileObject.GetComponent<Tile>() : null;
                if (tile != null && tile.Type == TileType.Mine)
                {
                    SpawnProjectiles(tileObject.transform.position);
                    tile.SetTileType(TileType.Tile);
                }
            }
        }
    }

    public int DebrisRemaining()
    {
        int count = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject tileObject = tiles[x, y];
                Tile tile = tileObject != null ? tileObject.GetComponent<Tile>() : null;
                if (tile != null && tile.Type == TileType.Debris)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private void SpawnProjectiles(Vector3 position)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("No projectile prefab assigned to GridSpawner.");
            return;
        }

        Vector2[] directions = new Vector2[]
        {
            new Vector2(0, 1),   // Up
            new Vector2(1, 1).normalized, // Up-Right
            new Vector2(1, 0),   // Right
            new Vector2(1, -1).normalized, // Down-Right
            new Vector2(0, -1),  // Down
            new Vector2(-1, -1).normalized, // Down-Left
            new Vector2(-1, 0),  // Left
            new Vector2(-1, 1).normalized  // Up-Left
        };

        foreach (var dir in directions)
        {
            GameObject proj = Instantiate(projectilePrefab, position, Quaternion.identity);
            Projectile p = proj.GetComponent<Projectile>();
            if (p == null)
            {
                p = proj.AddComponent<Projectile>();
            }

            p.Init(dir);
        }
    }
}