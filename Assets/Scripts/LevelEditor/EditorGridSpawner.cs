using System;
using UnityEngine;
using UnityEngine.UI;

public class EditorGridSpawner : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField, Min(1)] private int width = 10;
    [SerializeField, Min(1)] private int height = 14;

    [Header("Shared Assets")]
    [SerializeField] private Globals globals;

    [Header("Scroll View")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private EditorTile editorTilePrefab;

    private EditorTile[,] tiles;

    public int Width => width;
    public int Height => height;
    public event Action<EditorTile> TileClicked;

    private void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        if (!ValidateReferences())
        {
            return;
        }

        ClearGrid();

        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = width;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        tiles = new EditorTile[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                EditorTile tile = Instantiate(editorTilePrefab, content, false);
                RectTransform tileRect = tile.GetComponent<RectTransform>();
                tileRect.localScale = Vector3.one;
                tileRect.localRotation = Quaternion.identity;
                tile.Initialize(globals, new Vector2Int(x, y));
                tile.Clicked += HandleTileClicked;
                tiles[x, y] = tile;
            }
        }

        ResizeAndCenterContent();
    }

    private void HandleTileClicked(EditorTile tile)
    {
        Debug.Log($"GRID SPAWNER FORWARDING: {tile.GridPosition}", this);
        TileClicked?.Invoke(tile);
    }

    public void ResizeGrid(int newWidth, int newHeight)
    {
        width = Mathf.Max(1, newWidth);
        height = Mathf.Max(1, newHeight);
        GenerateGrid();
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