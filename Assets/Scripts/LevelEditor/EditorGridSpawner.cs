using System;
using UnityEngine;
using UnityEngine.UI;

public class EditorGridSpawner : MonoBehaviour
{
    public const int MaxTileCount = 2000;

    [Header("Grid")]
    [SerializeField, Range(1, MaxTileCount)] private int width = 10;
    [SerializeField, Range(1, MaxTileCount)] private int height = 14;

    [Header("Shared Assets")]
    [SerializeField] private Globals globals;

    [Header("Scroll View")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private EditorTile editorTilePrefab;

    private EditorTile[,] tiles;
    private bool tilesVisible = true;

    public int Width => width;
    public int Height => height;
    public bool TilesVisible => tilesVisible;
    public event Action<EditorTile> TileClicked;
    public event Action<EditorTile> TileDoubleClicked;

    private void Start()
    {
        // EditorManager may already have generated and loaded this grid in its
        // Start method. Do not generate it again and overwrite that data.
        if (tiles == null)
        {
            GenerateGrid();
        }
    }

    public void GenerateGrid()
    {
        if (!ValidateReferences())
        {
            return;
        }

        ClampDimensionsToTileLimit(ref width, ref height);

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = width;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        EditorTile[,] previousTiles = tiles;
        tiles = new EditorTile[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                EditorTile existingTile = null;
                if (previousTiles != null && x < previousTiles.GetLength(0) && y < previousTiles.GetLength(1))
                {
                    existingTile = previousTiles[x, y];
                }

                if (existingTile != null)
                {
                    existingTile.transform.SetParent(content, false);
                    RectTransform existingRect = existingTile.GetComponent<RectTransform>();
                    existingRect.localScale = Vector3.one;
                    existingRect.localRotation = Quaternion.identity;
                    existingTile.name = $"EditorTile_{x}_{y}";
                    existingTile.Initialize(
                        globals,
                        new Vector2Int(x, y),
                        existingTile.Type,
                        existingTile.MineType);
                    existingTile.gameObject.SetActive(tilesVisible);
                    tiles[x, y] = existingTile;
                    continue;
                }

                EditorTile tile = Instantiate(editorTilePrefab, content, false);
                RectTransform newTileRect = tile.GetComponent<RectTransform>();
                newTileRect.localScale = Vector3.one;
                newTileRect.localRotation = Quaternion.identity;
                tile.Initialize(globals, new Vector2Int(x, y));
                tile.Clicked += HandleTileClicked;
                tile.DoubleClicked += HandleTileDoubleClicked;
                tile.gameObject.SetActive(tilesVisible);
                tiles[x, y] = tile;
            }
        }

        if (previousTiles != null)
        {
            for (int y = 0; y < previousTiles.GetLength(1); y++)
            {
                for (int x = 0; x < previousTiles.GetLength(0); x++)
                {
                    if (x >= width || y >= height)
                    {
                        EditorTile oldTile = previousTiles[x, y];
                        if (oldTile != null)
                        {
                            oldTile.Clicked -= HandleTileClicked;
                            oldTile.DoubleClicked -= HandleTileDoubleClicked;
                            Destroy(oldTile.gameObject);
                        }
                    }
                }
            }
        }
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                EditorTile tile = tiles[x, y];
                if (tile != null)
                {
                    tile.transform.SetSiblingIndex(y * width + x);
                }
            }
        }
        ResizeAndCenterContent();
    }

    private void HandleTileClicked(EditorTile tile)
    {
        Debug.Log($"GRID SPAWNER FORWARDING: {tile.GridPosition}", this);
        TileClicked?.Invoke(tile);
    }

    private void HandleTileDoubleClicked(EditorTile tile)
    {
        Debug.Log(
            $"GRID SPAWNER FORWARDING DOUBLE-CLICK: " +
            $"{tile.GridPosition}",
            this);

        TileDoubleClicked?.Invoke(tile);
    }

    public void ResizeGrid(int newWidth, int newHeight)
    {
        ClampDimensionsToTileLimit(ref newWidth, ref newHeight);
        width = newWidth;
        height = newHeight;
        GenerateGrid();
    }

    private static void ClampDimensionsToTileLimit(
        ref int requestedWidth,
        ref int requestedHeight)
    {
        // Neither dimension may be zero. A single dimension may use the full
        // tile allowance, while the other is reduced as needed so the grid
        // never contains more than MaxTileCount tiles.
        requestedWidth = Mathf.Clamp(requestedWidth, 1, MaxTileCount);

        int maximumHeight = Mathf.Max(1, MaxTileCount / requestedWidth);
        requestedHeight = Mathf.Clamp(requestedHeight, 1, maximumHeight);
    }

    public RectTransform GetPreviewCaptureTarget()
    {
        return viewport != null ? viewport : content;
    }

    public TileType GetTileType(int x, int y)
    {
        return IsInsideGrid(x, y) ? tiles[x, y].Type : TileType.None;
    }

    public MineType GetMineType(int x, int y)
    {
        return IsInsideGrid(x, y)
            ? tiles[x, y].MineType
            : MineType.Standard;
    }

    public EditorTile GetEditorTile(int x, int y)
    {
        return IsInsideGrid(x, y) ? tiles[x, y] : null;
    }

    private void ResizeAndCenterContent()
    {
        Canvas.ForceUpdateCanvases();

        float gridWidth =
            gridLayout.padding.left +
            gridLayout.padding.right +
            width * gridLayout.cellSize.x +
            Mathf.Max(0, width - 1) * gridLayout.spacing.x;

        float gridHeight =
            gridLayout.padding.top +
            gridLayout.padding.bottom +
            height * gridLayout.cellSize.y +
            Mathf.Max(0, height - 1) * gridLayout.spacing.y;

        float contentWidth = Mathf.Max(viewport.rect.width, gridWidth);
        float contentHeight = Mathf.Max(viewport.rect.height, gridHeight);

        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            contentWidth);
        content.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            contentHeight);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private bool IsInsideGrid(int x, int y)
    {
        return tiles != null &&
               x >= 0 && x < width &&
               y >= 0 && y < height;
    }

    public void SetTilesVisible(bool visible)
    {
        tilesVisible = visible;
        if (tiles == null)
        {
            return;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                EditorTile tile = tiles[x, y];
                if (tile != null)
                {
                    tile.gameObject.SetActive(visible);
                }
            }
        }
    }

    private void ClearGrid()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }

    private bool ValidateReferences()
    {
        if (globals == null ||
            viewport == null ||
            content == null ||
            gridLayout == null ||
            editorTilePrefab == null)
        {
            Debug.LogError(
                "EditorGridSpawner is missing one or more Inspector references.",
                this);
            return false;
        }

        return true;
    }
}