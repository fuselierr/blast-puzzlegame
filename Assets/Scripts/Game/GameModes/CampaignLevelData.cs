using UnityEngine;

public enum CampaignTutorialType
{
    None,
    BasicControls,
    BlastDirections,
    MultipleMines,
    Walls,
    StrongDebris,
    CrossMine,
    DiagonalMine,
    DoubleMine
}

// A single hand-authored campaign level. Create these via
[CreateAssetMenu(fileName = "CampaignLevel", menuName = "Minesweeper/Campaign Level")]
public class CampaignLevelData : ScriptableObject
{
    public string levelName;
    public int width = 8;
    public int height = 8;

    [TextArea(3, 8)]
    public string tutorialMessage;

    [Header("Interactive Tutorial")]
    public CampaignTutorialType tutorialType;

    [Header("Mine Limits")]
    [Min(0)] public int standardMines;
    [Min(0)] public int crossMines;
    [Min(0)] public int diagonalMines;
    [Min(0)] public int doubleMines;

    public string layout;

    public TileType GetTile(int x, int y)
    {
        return GetTileData(x, y).tileType;
    }

    public CustomLevelTileData GetTileData(int x, int row)
    {
        if (!TryToTileGrid(out CustomLevelTileData[,] grid, out _) ||
            x < 0 || x >= width || row < 0 || row >= height)
        {
            return new CustomLevelTileData(TileType.Tile);
        }

        return grid[x, row];
    }

    public bool TryToTileGrid(
        out CustomLevelTileData[,] grid,
        out string error)
    {
        CustomLevelData parser = new CustomLevelData
        {
            levelName = levelName,
            width = width,
            height = height,
            layout = NormalizeLayout(layout)
        };

        return parser.TryToTileGrid(out grid, out error);
    }

    public bool HasValidLayout(out string error)
    {
        return TryToTileGrid(out _, out error);
    }

    private static string NormalizeLayout(string value)
    {
        string normalized = (value ?? string.Empty).Replace("\r", string.Empty);
        return !normalized.Contains("\n") && normalized.Contains("\\n")
            ? normalized.Replace("\\n", "\n")
            : normalized;
    }
}