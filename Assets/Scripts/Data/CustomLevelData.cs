using System.Collections.Generic;

[System.Serializable]
public class CustomLevelTileData
{
    public TileType tileType;
    public MineType mineType;
    public string teleporterPairId;

    public CustomLevelTileData(
        TileType tileType,
        MineType mineType = MineType.Standard,
        string teleporterPairId = null)
    {
        this.tileType = tileType;
        this.mineType = mineType;
        this.teleporterPairId = teleporterPairId;
    }
}

[System.Serializable]
public class CustomLevelData
{
    public string levelName;
    public string fileName;
    public int width;
    public int height;
    public string layout;
    public string author;
    public bool verified;
    public int likes;
    public int downloads;
    public string previewImage;
    public string createdAt;

    public CustomLevelData CreatePlayableCopy(out Dictionary<MineType, int> mineCounts)
    {
        mineCounts = new Dictionary<MineType, int>();

        if (string.IsNullOrWhiteSpace(layout))
        {
            return new CustomLevelData
            {
                levelName = levelName,
                fileName = fileName,
                width = width,
                height = height,
                layout = string.Empty,
                author = author,
                verified = verified,
                likes = likes,
                downloads = downloads,
                previewImage = previewImage,
                createdAt = createdAt
            };
        }

        string[] rows = (layout ?? string.Empty).Split(
            new[] { '\n', '\r' },
            System.StringSplitOptions.RemoveEmptyEntries);

        List<string> strippedRows = new List<string>(rows.Length);

        foreach (string row in rows)
        {
            string[] tokens = row.Split(
                new[] { ' ', '\t' },
                System.StringSplitOptions.RemoveEmptyEntries);

            List<string> cleanedTokens = new List<string>(tokens.Length);
            foreach (string token in tokens)
            {
                string trimmedToken = token.Trim();
                if (TryParseMineToken(trimmedToken, out MineType mineType))
                {
                    mineCounts[mineType] = mineCounts.TryGetValue(mineType, out int count)
                        ? count + 1
                        : 1;
                    cleanedTokens.Add("-");
                }
                else
                {
                    cleanedTokens.Add(trimmedToken);
                }
            }

            strippedRows.Add(string.Join(" ", cleanedTokens));
        }

        return new CustomLevelData
        {
            levelName = levelName,
            fileName = fileName,
            width = width,
            height = height,
            layout = string.Join("\n", strippedRows),
            author = author,
            verified = verified,
            likes = likes,
            downloads = downloads,
            previewImage = previewImage,
            createdAt = createdAt
        };
    }

    private static bool TryParseMineToken(string token, out MineType mineType)
    {
        switch ((token ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "NS":
                mineType = MineType.Standard;
                return true;
            case "NC":
                mineType = MineType.Cross;
                return true;
            case "NG":
                mineType = MineType.Diagonal;
                return true;
            case "ND":
                mineType = MineType.Double;
                return true;
            default:
                mineType = MineType.Standard;
                return false;
        }
    }

    // Returns [x, row], where row 0 is the top line of the layout string.
    public CustomLevelTileData[,] ToTileGrid()
    {
        if (!TryToTileGrid(
                out CustomLevelTileData[,] grid,
                out string error))
        {
            throw new System.FormatException(error);
        }

        return grid;
    }

    public bool TryToTileGrid(
        out CustomLevelTileData[,] grid,
        out string error)
    {
        grid = null;
        error = null;

        if (width <= 0 || height <= 0)
        {
            error = "Width and height must both be greater than zero.";
            return false;
        }

        string[] rows = (layout ?? string.Empty).Split(
            new[] { '\n', '\r' },
            System.StringSplitOptions.RemoveEmptyEntries);

        if (rows.Length != height)
        {
            error =
                $"Expected {height} layout rows but found {rows.Length}.";
            return false;
        }

        CustomLevelTileData[,] parsed =
            new CustomLevelTileData[width, height];

        System.Collections.Generic.Dictionary<string, int>
            teleporterCounts =
                new System.Collections.Generic.Dictionary<string, int>();

        for (int row = 0; row < height; row++)
        {
            string[] tokens = rows[row].Split(
                new[] { ' ', '\t' },
                System.StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length != width)
            {
                error =
                    $"Row {row + 1} expected {width} tokens but found " +
                    $"{tokens.Length}.";
                return false;
            }

            for (int x = 0; x < width; x++)
            {
                string token = tokens[x].Trim().ToUpperInvariant();
                if (!TryParseToken(token, out CustomLevelTileData tile))
                {
                    error =
                        $"Unknown token '{tokens[x]}' at row {row + 1}, " +
                        $"column {x + 1}.";
                    return false;
                }

                parsed[x, row] = tile;

                if (tile.tileType == TileType.Teleporter)
                {
                    teleporterCounts.TryGetValue(
                        tile.teleporterPairId,
                        out int count);
                    teleporterCounts[tile.teleporterPairId] = count + 1;
                }
            }
        }

        foreach (var pair in teleporterCounts)
        {
            if (pair.Value != 2)
            {
                error =
                    $"Teleporter '{pair.Key}' must appear exactly twice, " +
                    $"but appears {pair.Value} time(s).";
                return false;
            }
        }

        grid = parsed;
        return true;
    }

    private static bool TryParseToken(
        string token,
        out CustomLevelTileData tile)
    {
        tile = null;

        switch (token)
        {
            case "_": tile = new CustomLevelTileData(TileType.None); break;
            case "-": tile = new CustomLevelTileData(TileType.Tile); break;
            case "W": tile = new CustomLevelTileData(TileType.Wall); break;
            case "D": tile = new CustomLevelTileData(TileType.Debris); break;
            case "H": tile = new CustomLevelTileData(TileType.StrongDebris); break;
            case "S": tile = new CustomLevelTileData(TileType.Splitter); break;
            case "MH": tile = new CustomLevelTileData(TileType.Mirror, MineType.Standard); break;
            case "MV": tile = new CustomLevelTileData(TileType.Mirror, MineType.Cross); break;
            case "MR": tile = new CustomLevelTileData(TileType.Mirror, MineType.Diagonal); break;
            case "ML": tile = new CustomLevelTileData(TileType.Mirror, MineType.Double); break;
            case "NS": tile = new CustomLevelTileData(TileType.Mine, MineType.Standard); break;
            case "NC": tile = new CustomLevelTileData(TileType.Mine, MineType.Cross); break;
            case "NG": tile = new CustomLevelTileData(TileType.Mine, MineType.Diagonal); break;
            case "ND": tile = new CustomLevelTileData(TileType.Mine, MineType.Double); break;
            default:
                if (token.Length > 1 && token[0] == 'T')
                {
                    tile = new CustomLevelTileData(
                        TileType.Teleporter,
                        MineType.Standard,
                        token);
                    break;
                }

                return false;
        }

        return true;
    }
}