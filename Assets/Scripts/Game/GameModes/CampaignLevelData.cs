using UnityEngine;

// A single hand-authored campaign level. Create these via
[CreateAssetMenu(fileName = "CampaignLevel", menuName = "Minesweeper/Campaign Level")]
public class CampaignLevelData : ScriptableObject
{
    public string levelName;
    public int width = 8;
    public int height = 8;

    public string layout;

    public TileType GetTile(int x, int y)
    {
        string[] rows = GetRows();
        if (x < 0 || y < 0 || y >= rows.Length || x >= rows[y].Length)
        {
            return TileType.Tile;
        }

        char symbol = rows[y][x];
        return symbol == 'x' || symbol == 'X' || symbol == '#'
            ? TileType.Debris
            : TileType.Tile;
    }

    public bool HasValidLayout(out string error)
    {
        string[] rows = GetRows();
        if (rows.Length != height)
        {
            error = $"expected {height} rows but found {rows.Length}";
            return false;
        }

        for (int y = 0; y < rows.Length; y++)
        {
            if (rows[y].Length != width)
            {
                error = $"row {y + 1} should contain {width} characters but contains {rows[y].Length}";
                return false;
            }

            for (int x = 0; x < rows[y].Length; x++)
            {
                char symbol = rows[y][x];
                if (symbol != '-' && symbol != '.' && symbol != 'x' && symbol != 'X' && symbol != '#')
                {
                    error = $"unsupported character '{symbol}' at row {y + 1}, column {x + 1}";
                    return false;
                }
            }
        }

        error = string.Empty;
        return true;
    }

    private string[] GetRows()
    {
        if (string.IsNullOrWhiteSpace(layout))
        {
            return new string[0];
        }

        string normalizedLayout = layout.Replace("\r", string.Empty);

        if (!normalizedLayout.Contains("\n") && normalizedLayout.Contains("\\n"))
        {
            normalizedLayout = normalizedLayout.Replace("\\n", "\n");
        }

        return normalizedLayout.Trim().Split('\n');
    }
}
