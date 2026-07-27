using System;
using System.Collections;
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
        // Only auto-run in isolation (e.g. testing this scene alone).
        // Normally GameManager -> IGameMode drives grid setup explicitly.
        if (GameManager.Instance == null)
        {
            GenerateGrid();
            GenerateDebris();
        }
    }

    // Lets a mode set difficulty/size before generating (Freeplay/Classic).
    public void Configure(int newWidth, int newHeight, float newDebrisChance)
    {
        width = newWidth;
        height = newHeight;
        debrisChance = newDebrisChance;
    }

    public void GenerateGrid()
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

    // Campaign: build the grid from a hand-authored layout instead of random debris.
    public void LoadLevel(CampaignLevelData data)
    {
        if (data == null)
        {
            Debug.LogWarning("GridSpawner.LoadLevel called with null data.");
            return;
        }

        if (!data.HasValidLayout(out string layoutError))
        {
            Debug.LogError($"Campaign level '{data.levelName}' has an invalid layout: {layoutError}.");
            return;
        }

        Configure(data.width, data.height, 0f);
        GenerateGrid();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject tileObject = tiles[x, y];
                Tile tile = tileObject != null ? tileObject.GetComponent<Tile>() : null;
                if (tile == null)
                {
                    continue;
                }

                TileType type = data.GetTile(x, height - 1 - y);
                if (type == TileType.Debris)
                {
                    tile.PlaceDebris();
                }
                else
                {
                    tile.SetTileType(TileType.Tile);
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

    public void GenerateDebris()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (UnityEngine.Random.value < debrisChance)
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

    // True while spawned projectiles are still traveling/resolving hits.
    // Modes (and GameManager) should avoid triggering another detonation
    // while this is true.
    public bool IsDetonating { get; private set; }

    // Detonation is NOT instantaneous: projectiles take time to travel,
    // hit debris, and run their own explode coroutine. Checking
    // AllDebrisCleared right after calling this would read stale state.
    // Pass a callback and it'll fire once every spawned projectile has
    // actually finished (hit something or flown off-grid).
    public void DetonateMinesAndResolve(Action<bool> onResolved)
    {
        if (IsDetonating)
        {
            return; // ignore re-entrant detonate calls mid-resolution
        }

        bool anyMines = false;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject tileObject = tiles[x, y];
                Tile tile = tileObject != null ? tileObject.GetComponent<Tile>() : null;
                if (tile != null && tile.Type == TileType.Mine)
                {
                    anyMines = true;
                    SpawnProjectiles(tileObject.transform.position);
                    tile.SetTileType(TileType.Tile);
                }
            }
        }

        if (!anyMines)
        {
            onResolved?.Invoke(AllDebrisCleared);
            return;
        }

        IsDetonating = true;
        StartCoroutine(WaitForProjectilesToResolve(onResolved));
    }

    private IEnumerator WaitForProjectilesToResolve(Action<bool> onResolved)
    {
        // Wait a frame so this detonation's projectiles are guaranteed to
        // have been created before we start polling for them.
        yield return null;

        while (FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length > 0)
        {
            yield return null;
        }

        IsDetonating = false;
        onResolved?.Invoke(AllDebrisCleared);
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

    public bool AllDebrisCleared => DebrisRemaining() == 0;

    private void SpawnProjectiles(Vector3 position)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("No projectile prefab assigned to GridSpawner.");
            return;
        }

        Vector2[] directions = new Vector2[]
        {
            new Vector2(0, 1),
            new Vector2(1, 1).normalized,
            new Vector2(1, 0),
            new Vector2(1, -1).normalized,
            new Vector2(0, -1),
            new Vector2(-1, -1).normalized,
            new Vector2(-1, 0),
            new Vector2(-1, 1).normalized
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
