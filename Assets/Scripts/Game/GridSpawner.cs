using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float debrisChance = 0.15f;

    [Header("Entrance Animation")]
    [SerializeField] private bool animateEntrance = true;
    [SerializeField, Min(0.1f)] private float entranceTotalDuration = 1f;
    [SerializeField, Min(0.05f)] private float tileDropDuration = 0.35f;
    [SerializeField, Min(0f)] private float spawnAboveScreen = 1f;

    private const float ResolutionVisualDelay = 1.4f;

    private GameObject[,] tiles;

    private static readonly Vector2Int[] solverDirections =
    {
        Vector2Int.up,
        new Vector2Int(1, 1),
        Vector2Int.right,
        new Vector2Int(1, -1),
        Vector2Int.down,
        new Vector2Int(-1, -1),
        Vector2Int.left,
        new Vector2Int(-1, 1)
    };

    private void Start()
    {
        // Only auto-run in isolation (e.g. testing this scene alone).
        // Normally GameManager -> IGameMode drives grid setup explicitly.
        if (tiles == null &&GameManager.Instance == null)
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
        int tileCount = width * height;
        float moveDuration = Mathf.Min(
            tileDropDuration,
            entranceTotalDuration);
        float delayRange = Mathf.Max(
            0f,
            entranceTotalDuration - moveDuration);
        float delayStep = tileCount > 1
            ? delayRange / (tileCount - 1)
            : 0f;
        float entranceStartY = GetEntranceStartY(spacing);

        // Build a pool with each column repeated once per row, then shuffle it.
        // Walking the shuffled pool and assigning each column its next row (in
        // order) keeps every column bottom-first while randomizing which
        // column drops next overall.
        int[] pool = new int[tileCount];
        int poolIndex = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                pool[poolIndex++] = x;
            }
        }
        for (int i = pool.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int[] nextRow = new int[width];
        int[,] animationOrder = new int[width, height];
        for (int i = 0; i < pool.Length; i++)
        {
            int x = pool[i];
            int y = nextRow[x];
            animationOrder[x, y] = i;
            nextRow[x]++;
        }

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

                if (animateEntrance)
                {
                    // Start with the visual above the screen. Each column
                    // always drops bottom row first, but the interleaving
                    // across columns is randomized.
                    tile.transform.position = new Vector3(
                        position.x,
                        entranceStartY,
                        position.z);

                    int animationIndex = animationOrder[x, y];

                    LeanTween.move(tile, position, moveDuration)
                        .setDelay(animationIndex * delayStep)
                        .setEase(LeanTweenType.easeOutQuint);
                }

                Tile tileScript = tile.GetComponent<Tile>();
                if (tileScript != null)
                {
                    tileScript.GridPosition = new Vector2Int(x, y);
                }
            }
        }
    }

    private float GetEntranceStartY(float spacing)
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            float distanceFromCamera =
                Mathf.Abs(transform.position.z - camera.transform.position.z);
            Vector3 screenTop = camera.ViewportToWorldPoint(
                new Vector3(0.5f, 1f, distanceFromCamera));

            return screenTop.y + spawnAboveScreen;
        }

        float boardTop = (height - 1) * spacing / 2f;
        return boardTop + spawnAboveScreen + spacing;
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
                    tile.SetTileType(type);
                }
            }
        }
    }

    public void LoadLevel(CustomLevelData data)
    {
        if (data == null)
        {
            Debug.LogWarning(
                "GridSpawner.LoadLevel called with null custom level data.");
            return;
        }

        if (!data.TryToTileGrid(
                out CustomLevelTileData[,] board,
                out string layoutError))
        {
            Debug.LogError(
                $"Custom level '{data.levelName}' has an invalid layout: " +
                layoutError);
            return;
        }

        Configure(data.width, data.height, 0f);
        GenerateGrid();

        Dictionary<string, List<Tile>> teleporters =
            new Dictionary<string, List<Tile>>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = GetTile(new Vector2Int(x, y));
                if (tile == null)
                {
                    continue;
                }

                // Layout strings are written top-to-bottom, while the world
                // grid's y coordinate increases bottom-to-top.
                CustomLevelTileData cell = board[x, height - 1 - y];

                if (cell.tileType == TileType.Mine)
                {
                    tile.SetTileType(cell.mineType);
                }
                else if (cell.tileType == TileType.Mirror)
                {
                    tile.SetTileType(TileType.Mirror, cell.mineType);
                }
                else if (cell.tileType == TileType.Debris)
                {
                    tile.PlaceDebris();
                }
                else
                {
                    tile.SetTileType(cell.tileType);
                }

                if (cell.tileType == TileType.Teleporter)
                {
                    if (!teleporters.TryGetValue(
                            cell.teleporterPairId,
                            out List<Tile> pair))
                    {
                        pair = new List<Tile>();
                        teleporters[cell.teleporterPairId] = pair;
                    }

                    pair.Add(tile);
                }
            }
        }

        foreach (var entry in teleporters)
        {
            Tile first = entry.Value[0];
            Tile second = entry.Value[1];
            first.SetTeleporterPair(entry.Key, second);
            second.SetTeleporterPair(entry.Key, first);
        }
    }

    private void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject tile = transform.GetChild(i).gameObject;
            LeanTween.cancel(tile);

            if (Application.isPlaying)
            {
                Destroy(tile);
            }
            else
            {
                DestroyImmediate(tile);
            }
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

    public void GenerateDebris(bool isDailyChallenge = false)
    {
        if (isDailyChallenge)
        {
            GenerateSeededSolvableDebris(Mathf.RoundToInt(width * height * debrisChance), mineBudget: 8);
        }
        else
        {
            GenerateSolvableDebris(Mathf.RoundToInt(width * height * debrisChance), mineBudget: 8);
        }
    }

    // Generates Classic Debris and only accepts the board after
    // the backtracking solver proves that the available mines can clear it.
    public bool GenerateSolvableDebris(int targetDebrisCount, int mineBudget)
    {
        if (tiles == null)
        {
            Debug.LogError("Generate must be run first");
            return false;
        }

        targetDebrisCount = Mathf.Clamp(targetDebrisCount, 1, width * height - mineBudget);

        const int maximumAttempts = 1000;

        for (int attempt = 0; attempt < maximumAttempts; attempt++)
        {
            ClearGeneratedTiles();

            List<Vector2Int> solutionMines = ChooseRandomMinePositions(mineBudget);

            if (!BuildDebrisForSolution(solutionMines, targetDebrisCount))
            {
                continue;
            }

            if (HasOneDetonationSolution(mineBudget))
            {
                return true;
            }
        }
        // No verified board is ever sent to the player
        ClearGeneratedTiles();
        return false;
    }

    private List<Vector2Int> ChooseRandomMinePositions(int mineBudget)
    {
        List<Vector2Int> posns = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                posns.Add(new Vector2Int(x, y));
            }
        }
        Shuffle(posns);
        return posns.GetRange(0, Mathf.Min(mineBudget, posns.Count));
    }

    private bool BuildDebrisForSolution(List<Vector2Int> solutionMines, int targetDebrisCount)
    {
        HashSet<Vector2Int> mineSet = new HashSet<Vector2Int>(solutionMines);
        List<Vector2Int> candidates = new List<Vector2Int>();

        foreach (Vector2Int mine in solutionMines)
        {
            foreach (Vector2Int direction in solverDirections)
            {
                Vector2Int current = mine + direction;
                while (IsInsideGrid(current))
                {
                    if (!mineSet.Contains(current) && !candidates.Contains(current))
                    {
                        candidates.Add(current);
                    }
                    current += direction;
                }
            }
        }

        Shuffle(candidates);

        foreach (Vector2Int candidate in candidates)
        {
            if (DebrisRemaining() >= targetDebrisCount)
            {
                break;
            }

            Tile candidateTile = GetTile(candidate);
            candidateTile.PlaceDebris();

            if (!PlacementClearsAllDebris(solutionMines))
            {
                candidateTile.SetTileType(TileType.Tile);
            }
        }
        return DebrisRemaining() == targetDebrisCount;
    }

    private bool PlacementClearsAllDebris(List<Vector2Int> minePositions)
    {
        HashSet<Vector2Int> cleared = new HashSet<Vector2Int>();

        foreach (Vector2Int mine in minePositions)
        {
            cleared.UnionWith(GetVisibleDebrisFrom(mine));
        }

        HashSet<Vector2Int> allDebris = GetAllDebrisPositions();
        return cleared.SetEquals(allDebris);
    }

    public bool HasOneDetonationSolution(int mineBudget)
    {
        HashSet<Vector2Int> remainingDebris =
            GetAllDebrisPositions();

        if (remainingDebris.Count == 0)
        {
            return false;
        }

        List<HashSet<Vector2Int>> placementOptions =
            BuildMinePlacementOptions();

        return SolveWithBacktracking(
            remainingDebris,
            placementOptions,
            mineBudget
        );
    }

    private List<HashSet<Vector2Int>>
        BuildMinePlacementOptions()
    {
        List<HashSet<Vector2Int>> options =
            new List<HashSet<Vector2Int>>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (GetTile(position).Type == TileType.Debris)
                {
                    continue;
                }

                HashSet<Vector2Int> debrisHit =
                    GetVisibleDebrisFrom(position);

                if (debrisHit.Count > 0)
                {
                    options.Add(debrisHit);
                }
            }
        }

        return options;
    }

    private bool SolveWithBacktracking(
        HashSet<Vector2Int> remainingDebris,
        List<HashSet<Vector2Int>> placementOptions,
        int minesRemaining
    )
    {
        if (remainingDebris.Count == 0)
        {
            return true;
        }

        if (minesRemaining <= 0)
        {
            return false;
        }

        Vector2Int hardestDebris = default;
        int fewestOptions = int.MaxValue;

        // Pick the debris tile that has the fewest possible
        // mine placements capable of clearing it.
        foreach (Vector2Int debris in remainingDebris)
        {
            int optionCount = 0;

            foreach (HashSet<Vector2Int> option in placementOptions)
            {
                if (option.Contains(debris))
                {
                    optionCount++;
                }
            }

            if (optionCount < fewestOptions)
            {
                fewestOptions = optionCount;
                hardestDebris = debris;
            }
        }

        if (fewestOptions == 0)
        {
            return false;
        }

        foreach (HashSet<Vector2Int> option in placementOptions)
        {
            if (!option.Contains(hardestDebris))
            {
                continue;
            }

            HashSet<Vector2Int> nextRemaining =
                new HashSet<Vector2Int>(remainingDebris);

            nextRemaining.ExceptWith(option);

            if (SolveWithBacktracking(
                nextRemaining,
                placementOptions,
                minesRemaining - 1))
            {
                return true;
            }
        }

        return false;
    }

    private HashSet<Vector2Int> GetVisibleDebrisFrom(
        Vector2Int minePosition
    )
    {
        HashSet<Vector2Int> visibleDebris =
            new HashSet<Vector2Int>();

        foreach (Vector2Int direction in solverDirections)
        {
            Vector2Int current = minePosition + direction;

            while (IsInsideGrid(current))
            {
                if (GetTile(current).Type == TileType.Debris)
                {
                    visibleDebris.Add(current);
                    break;
                }

                current += direction;
            }
        }

        return visibleDebris;
    }

    private HashSet<Vector2Int> GetAllDebrisPositions()
    {
        HashSet<Vector2Int> debris =
            new HashSet<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position = new Vector2Int(x, y);

                if (GetTile(position).Type == TileType.Debris)
                {
                    debris.Add(position);
                }
            }
        }

        return debris;
    }

    private void ClearGeneratedTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = GetTile(new Vector2Int(x, y));

                if (tile != null)
                {
                    tile.SetTileType(TileType.Tile);
                }
            }
        }
    }

    private Tile GetTile(Vector2Int position)
    {
        GameObject tileObject =
            tiles[position.x, position.y];

        return tileObject != null
            ? tileObject.GetComponent<Tile>()
            : null;
    }

    private bool IsInsideGrid(Vector2Int position)
    {
        return position.x >= 0 &&
               position.x < width &&
               position.y >= 0 &&
               position.y < height;
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex =
                UnityEngine.Random.Range(0, i + 1);

            T temporary = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temporary;
        }
    }


    // True while spawned projectiles are still traveling/resolving hits.
    // Modes (and GameManager) should avoid triggering another detonation
    // while this is true.
    public bool IsDetonating { get; private set; }
    public int LastDetonationDebrisBroken { get; private set; }

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

        int debrisBeforeDetonation = DebrisRemaining();
        LastDetonationDebrisBroken = 0;
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
                    SpawnProjectiles(tileObject.transform.position, tile.mineType);
                    tile.SetTileType(TileType.Tile);
                }
            }
        }

        if (!anyMines)
        {
            LastDetonationDebrisBroken = Mathf.Max(
                0,
                debrisBeforeDetonation - DebrisRemaining());
            onResolved?.Invoke(AllDebrisCleared);
            return;
        }

        IsDetonating = true;
        StartCoroutine(WaitForProjectilesToResolve(
            debrisBeforeDetonation,
            onResolved));
    }

    private IEnumerator WaitForProjectilesToResolve(
        int debrisBeforeDetonation,
        Action<bool> onResolved)
    {
        // Wait a frame so this detonation's projectiles are guaranteed to
        // have been created before we start polling for them.
        yield return null;

        while (FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length > 0)
        {
            yield return null;
        }

        // Projectile trails are separate particle objects that fade after the
        // projectile itself is destroyed. Keep the result panel out of the
        // way until that last visual feedback has had time to finish.
        if (ResolutionVisualDelay > 0f)
        {
            yield return new WaitForSeconds(ResolutionVisualDelay);
        }

        LastDetonationDebrisBroken = Mathf.Max(
            0,
            debrisBeforeDetonation - DebrisRemaining());
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
                if (tile != null &&
                    (tile.Type == TileType.Debris ||
                     tile.Type == TileType.StrongDebris))
                {
                    count++;
                }
            }
        }
        return count;
    }

    public bool AllDebrisCleared => DebrisRemaining() == 0;

    public void SetTutorialHighlight(Vector2Int position, bool highlighted)
    {
        if (tiles == null || !IsInsideGrid(position))
        {
            return;
        }

        Tile tile = GetTile(position);
        tile?.SetTutorialHighlight(highlighted);
    }

    private void SpawnProjectiles(Vector3 position, MineType mineType = MineType.Standard)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("No projectile prefab assigned to GridSpawner.");
            return;
        }
        Vector2[] directions;
        switch (mineType)
        {
            case MineType.Cross:
                directions = new Vector2[]
                {
                    new Vector2(0, 1),
                    new Vector2(1, 0),
                    new Vector2(0, -1),
                    new Vector2(-1, 0),
                };
                break;
            case MineType.Diagonal:
                directions = new Vector2[]
                {
                    new Vector2(1, 1).normalized,
                    new Vector2(1, -1).normalized,
                    new Vector2(-1, -1).normalized,
                    new Vector2(-1, 1).normalized
                };
                break;
            case MineType.Double:
                directions = new Vector2[]
                {
                    new Vector2(0, 1),
                    new Vector2(1, 1).normalized,
                    new Vector2(1, 0),
                    new Vector2(1, -1).normalized,
                    new Vector2(0, -1),
                    new Vector2(-1, -1).normalized,
                    new Vector2(-1, 0),
                    new Vector2(-1, 1).normalized,
                    new Vector2(0, 1),
                    new Vector2(1, 1).normalized,
                    new Vector2(1, 0),
                    new Vector2(1, -1).normalized,
                    new Vector2(0, -1),
                    new Vector2(-1, -1).normalized,
                    new Vector2(-1, 0),
                    new Vector2(-1, 1).normalized
                };
                break;
            case MineType.Standard:
            default:
                directions = new Vector2[]
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
                break;
        }

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
    private void GenerateSeededSolvableDebris(int targetDebrisCount, int mineBudget)
    {
        UnityEngine.Random.State previousState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(GetTodaySeed());
        
        bool generated = false;
        int temp = targetDebrisCount;
        while (!generated && temp > 0)
        {
            generated = GenerateSolvableDebris(temp, mineBudget);
            if (!generated)
            {
                temp--;
            }
        }

        if (!generated)
        {
            Debug.LogWarning("Failed to generate a solvable debris layout for today.");
        }

        UnityEngine.Random.state = previousState;

    }

    private static int GetTodaySeed()
    {
        DateTime today = DateTime.UtcNow.Date;
        return today.Year * 10000 + today.Month * 100 + today.Day;
    }
}