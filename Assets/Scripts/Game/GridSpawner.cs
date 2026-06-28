using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
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
}