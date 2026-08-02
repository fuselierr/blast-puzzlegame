using UnityEngine;

public class ClassicLevelManager : MonoBehaviour
{
    public ClassicLevelData currentLevel;

    [Header("Prefabs")]
    public GameObject normalTilePrefab;
    public GameObject stonePrefab;

    [Header("Grid")]
    public Transform gridParent;
    public float tileSpacing = 0.36f;

    [Header("Projectile Blast")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 4f;
    public float projectileScale = 2f;
    public float projectileSpawnOffset = 0.2f;

    [Header("Mine Inventory")]
    public MineInventory mineInventory;

    private readonly Vector2[] blastDirections =
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right,
        new Vector2(1, 1).normalized,
        new Vector2(1, -1).normalized,
        new Vector2(-1, 1).normalized,
        new Vector2(-1, -1).normalized
    };

    private void Start()
    {
        if (mineInventory != null && currentLevel != null)
        {
            mineInventory.SetMineCount(MineType.Standard, currentLevel.minesAvailable);
        }

        LoadLevel();
    }

    private void LoadLevel()
    {
        if (currentLevel == null)
        {
            Debug.LogError("ClassicLevelManager: No current level assigned.");
            return;
        }

        if (normalTilePrefab == null)
        {
            Debug.LogError("ClassicLevelManager: No normal tile prefab assigned.");
            return;
        }

        if (stonePrefab == null)
        {
            Debug.LogError("ClassicLevelManager: No stone prefab assigned.");
            return;
        }

        if (gridParent == null)
        {
            Debug.LogError("ClassicLevelManager: No grid parent assigned.");
            return;
        }

        string[] rows = currentLevel.layout.Split('\n');

        float startX = -((currentLevel.width - 1) * tileSpacing) / 2f;
        float startY = ((currentLevel.height - 1) * tileSpacing) / 2f;

        for (int y = 0; y < currentLevel.height; y++)
        {
            string row = rows[y].Trim();

            for (int x = 0; x < currentLevel.width; x++)
            {
                char tileCharacter = row[x];

                Vector3 localPosition = new Vector3(
                    startX + x * tileSpacing,
                    startY - y * tileSpacing,
                    0f
                );

                GameObject tile = Instantiate(normalTilePrefab, gridParent, false);
                tile.transform.localPosition = localPosition;

                Tile tileScript = tile.GetComponent<Tile>();

                if (tileScript != null)
                {
                    tileScript.GridPosition = new Vector2Int(x, y);
                }

                if (tileCharacter == 'S')
                {
                    SpawnStone(localPosition, x, y);
                }
            }
        }
    }

    private void SpawnStone(Vector3 localPosition, int x, int y)
    {
        Debug.Log("Spawning stone at x: " + x + ", y: " + y);

        GameObject stone = Instantiate(stonePrefab, gridParent, false);
        stone.name = "Classic Stone";

        stone.transform.localPosition = new Vector3(
            localPosition.x,
            localPosition.y,
            -1f
        );

        SpriteRenderer[] renderers = stone.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            renderer.enabled = true;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 100;
        }

        ClassicStone classicStone = stone.GetComponent<ClassicStone>();

        if (classicStone == null)
        {
            stone.AddComponent<ClassicStone>();
        }

        Collider2D stoneCollider = stone.GetComponent<Collider2D>();

        if (stoneCollider == null)
        {
            CircleCollider2D circleCollider = stone.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
            circleCollider.radius = 0.12f;
        }
        else
        {
            stoneCollider.enabled = true;
            stoneCollider.isTrigger = true;
        }
    }

    public void DetonateMines()
    {
        if (gridParent == null)
        {
            Debug.LogError("ClassicLevelManager: No grid parent assigned.");
            return;
        }

        Tile[] tiles = gridParent.GetComponentsInChildren<Tile>();

        foreach (Tile tile in tiles)
        {
            if (tile.Type == TileType.Mine)
            {
                Vector3 minePosition = tile.transform.position;

                tile.PlaceDebris();

                SpawnBlastProjectiles(minePosition);
            }
        }

        Debug.Log("Classic mines detonated.");
    }

    private void SpawnBlastProjectiles(Vector3 minePosition)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("ClassicLevelManager: No projectile prefab assigned.");
            return;
        }

        foreach (Vector2 direction in blastDirections)
        {
            Vector3 spawnPosition = minePosition + (Vector3)(direction * projectileSpawnOffset);

            GameObject projectileObject = Instantiate(
                projectilePrefab,
                spawnPosition,
                Quaternion.identity
            );

            Projectile projectile = projectileObject.GetComponent<Projectile>();

            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<Projectile>();
            }

            projectile.direction = direction;
            projectile.speed = projectileSpeed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            projectileObject.transform.rotation = Quaternion.Euler(0, 0, angle);
            projectileObject.transform.localScale = Vector3.one * projectileScale;
        }
    }
}

