using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EditorManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject filePanel;

    [Header("Grid")]
    [SerializeField] private EditorGridSpawner gridSpawner;

    [Header("Verification")]
    [SerializeField] private GridSpawner gameplayGridSpawner;
    [SerializeField] private VerificationManager verificationManager;
    [Tooltip("Text used to show verification success/failure feedback.")]
    [SerializeField] private TMP_Text verifiedText;
    [Tooltip("How long the verification message stays visible before hiding.")]
    [SerializeField] private float messageDisplayDuration = 2f;

    [Header("Initial Selection")]
    [SerializeField] private TileType selectedTileType = TileType.Tile;
    [SerializeField] private MineType selectedMineType = MineType.Standard;
    [SerializeField] private TMP_InputField widthInput;
    [SerializeField] private TMP_InputField heightInput;

    public TileType SelectedTileType => selectedTileType;
    public MineType SelectedMineType => selectedMineType;

    public event Action<TileType, MineType> SelectionChanged;

    private CustomLevelData currentLevel;
    private Coroutine messageHideRoutine;

    public string CurrentLevelName =>
        currentLevel != null ? currentLevel.levelName : string.Empty;

    private void Awake()
    {
        // This VerificationManager lives in the editor scene and is only
        // started explicitly by VerifyCurrentLevel(). Disable it before any
        // Start methods run so it cannot consume the pending edit level before
        // EditorManager receives it. That pending object contains both the
        // chosen display name and the original filename used for overwriting.
        if (verificationManager != null)
        {
            verificationManager.enabled = false;
        }
    }

    private void Start()
    {
        if (filePanel != null)
        {
            filePanel.SetActive(false);
        }

        if (gridSpawner == null)
        {
            gridSpawner = FindObjectOfType<EditorGridSpawner>();
        }

        if (gridSpawner == null)
        {
            Debug.LogError("EditorManager has no EditorGridSpawner!", this);
            return;
        }

        gridSpawner.TileClicked += PaintTile;
        gridSpawner.TileDoubleClicked += DeleteTile;

        if (gameplayGridSpawner != null)
        {
            gameplayGridSpawner.gameObject.SetActive(false);
        }

        if (verificationManager != null)
        {
            verificationManager.OnVerificationComplete += HandleVerificationComplete;
        }

        if (widthInput != null)
        {
            widthInput.onEndEdit.AddListener(_ => ResizeGridFromInputs(true));
        }

        if (heightInput != null)
        {
            heightInput.onEndEdit.AddListener(_ => ResizeGridFromInputs(false));
        }

        CustomLevelData levelBeingEdited =
            CustomLevelStorage.TakePendingEdit();

        if (levelBeingEdited != null)
        {
            currentLevel = levelBeingEdited;
            ApplyLevelData(levelBeingEdited);

            Debug.Log(
                $"EditorManager received level " +
                $"'{currentLevel.levelName}'.",
                this);
        }
        else
        {
            Debug.LogWarning(
                "EditorManager did not receive pending level data. " +
                "A new unnamed level will be used.",
                this);
        }

        RefreshDimensionInputs();

        Debug.Log("EDITOR MANAGER CONNECTED TO GRID", this);
    }

    private void OnDestroy()
    {
        if (gridSpawner != null)
        {
            gridSpawner.TileClicked -= PaintTile;
            gridSpawner.TileDoubleClicked -= DeleteTile;
        }

        if (verificationManager != null)
        {
            verificationManager.OnVerificationComplete -= HandleVerificationComplete;
        }
    }

    public void SelectTileType(
        TileType tileType,
        MineType mineType = MineType.Standard)
    {
        selectedTileType = tileType;
        selectedMineType = mineType;

        Debug.Log(
            $"EDITOR MANAGER SELECTED: {selectedTileType}, {selectedMineType}",
            gameObject
        );

        SelectionChanged?.Invoke(selectedTileType, selectedMineType);
    }

    private void PaintTile(EditorTile tile)
    {
        if (tile == null)
        {
            return;
        }

        bool tileChanged =
            tile.Type != selectedTileType ||
            ((selectedTileType == TileType.Mine ||
              selectedTileType == TileType.Mirror) &&
             tile.MineType != selectedMineType);

        if (!tileChanged)
        {
            return;
        }

        Debug.Log(
            $"PAINTING {tile.GridPosition} WITH " +
            $"{selectedTileType}, {selectedMineType}",
            this
        );

        tile.SetTile(selectedTileType, selectedMineType);
        InvalidateVerification();
    }

    private void DeleteTile(EditorTile tile)
    {
        if (tile == null)
        {
            return;
        }

        if (tile.Type == TileType.Tile)
        {
            return;
        }

        Debug.Log(
            $"RESETTING {tile.GridPosition} TO A NORMAL TILE",
            this
        );

        tile.SetTile(TileType.Tile, MineType.Standard);
        InvalidateVerification();
    }

    public void ResizeGridFromInputs()
    {
        ResizeGridFromInputs(true);
    }

    private void ResizeGridFromInputs(bool preserveWidth)
    {
        if (gridSpawner == null)
        {
            return;
        }

        int newWidth = ParseIntField(widthInput, gridSpawner.Width);
        int newHeight = ParseIntField(heightInput, gridSpawner.Height);

        if ((long)newWidth * newHeight > EditorGridSpawner.MaxTileCount)
        {
            if (preserveWidth)
            {
                newHeight = Mathf.Max(
                    1,
                    EditorGridSpawner.MaxTileCount / newWidth);
            }
            else
            {
                newWidth = Mathf.Max(
                    1,
                    EditorGridSpawner.MaxTileCount / newHeight);
            }
        }

        if (newWidth != gridSpawner.Width || newHeight != gridSpawner.Height)
        {
            gridSpawner.ResizeGrid(newWidth, newHeight);
            InvalidateVerification();
        }

        // Reflect the valid non-zero, tile-limited dimensions in the UI.
        RefreshDimensionInputs();
    }

    private void InvalidateVerification()
    {
        if (currentLevel != null)
        {
            currentLevel.verified = false;
        }

        // Do not leave a previous "Level Verified!" result visible after the
        // board it referred to has changed.
        if (messageHideRoutine != null)
        {
            StopCoroutine(messageHideRoutine);
            messageHideRoutine = null;
        }

        if (verifiedText != null)
        {
            verifiedText.gameObject.SetActive(false);
        }
    }

    private int ParseIntField(TMP_InputField inputField, int fallback)
    {
        if (inputField == null)
        {
            return fallback;
        }

        if (int.TryParse(inputField.text, out int value))
        {
            // Zero and negative dimensions are invalid. Each individual
            // dimension can be as large as the total tile allowance; the
            // spawner then constrains the width x height product.
            return Mathf.Clamp(value, 1, EditorGridSpawner.MaxTileCount);
        }

        return fallback;
    }

    public void OpenFilePanel()
    {
        if (filePanel != null)
        {
            filePanel.SetActive(true);
        }
    }

    public void CloseFilePanel()
    {
        if (filePanel != null)
        {
            filePanel.SetActive(false);
        }
    }

    public void SaveLevel()
    {
        CustomLevelData level = BuildLevelData();
        CustomLevelStorage.Save(level);
    }

    public void SaveAndQuit()
    {
        CustomLevelData level = BuildLevelData();
        filePanel.SetActive(false);
        StartCoroutine(CapturePreviewAndSaveRoutine(level));
    }

    public void VerifyCurrentLevel()
    {
        if (verificationManager == null || gameplayGridSpawner == null || gridSpawner == null)
        {
            Debug.LogError("Cannot verify level because a required verification component is missing.", this);
            return;
        }

        CustomLevelData level = BuildLevelData();
        if (level == null)
        {
            Debug.LogError("Failed to build custom level data for verification.", this);
            return;
        }

        ResolveVerificationText();
        if (verifiedText != null)
        {
            verifiedText.gameObject.SetActive(false);
        }

        gridSpawner.SetTilesVisible(false);
        filePanel.SetActive(false);
        gameplayGridSpawner.gameObject.SetActive(true);
        verificationManager.enabled = true;
        verificationManager.StartVerification(level);
    }

    private void HandleVerificationComplete(bool debrisCleared)
    {
        // VerificationManager updates the same in-memory level instance, but
        // the main menu rebuilds its entries from the saved JSON files. Persist
        // the result now so the entry shows the latest verification state.
        if (currentLevel != null)
        {
            currentLevel.verified = debrisCleared;
            CustomLevelStorage.Save(currentLevel);
        }

        if (verificationManager != null)
        {
            verificationManager.enabled = false;
        }

        if (gameplayGridSpawner != null)
        {
            gameplayGridSpawner.gameObject.SetActive(false);
        }

        if (gridSpawner != null)
        {
            gridSpawner.SetTilesVisible(true);
        }

        ShowVerificationMessage(debrisCleared ? "Level Verified!" : "Verification Failed.", debrisCleared ? Color.green : Color.red);

        Debug.Log($"Custom level verification completed. Debris cleared: {debrisCleared}", this);
    }

    private void ResolveVerificationText()
    {
        if (verifiedText != null)
        {
            return;
        }

        verifiedText = GetComponentInChildren<TMP_Text>(true);
    }

    private void ShowVerificationMessage(string message, Color color)
    {
        ResolveVerificationText();

        if (verifiedText == null)
        {
            return;
        }

        if (messageHideRoutine != null)
        {
            StopCoroutine(messageHideRoutine);
        }

        verifiedText.text = message;
        verifiedText.color = color;
        verifiedText.gameObject.SetActive(true);
        messageHideRoutine = StartCoroutine(HideVerificationMessageAfterDelay());
    }

    private IEnumerator HideVerificationMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDisplayDuration);

        if (verifiedText != null)
        {
            verifiedText.gameObject.SetActive(false);
        }

        messageHideRoutine = null;
    }

    private IEnumerator CapturePreviewAndSaveRoutine(CustomLevelData level)
    {
        if (level == null)
        {
            LoadMainMenu();
            yield break;
        }

        Debug.Log($"CapturePreviewAndSaveRoutine: saving level name='{level.levelName}' file='{level.fileName}'", this);

        yield return new WaitForEndOfFrame();

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        if (screenshot != null)
        {
            Rect captureRect = GetGridCaptureRect();
            if (captureRect.width > 0f && captureRect.height > 0f)
            {
                Texture2D cropped = CropTexture(screenshot, captureRect);
                if (cropped != null)
                {
                    string previewFileName = SaveTextureToFile(cropped, level);
                    if (!string.IsNullOrWhiteSpace(previewFileName))
                    {
                        level.previewImage = previewFileName;
                    }

                    Destroy(cropped);
                }
            }

            Destroy(screenshot);
        }

        CustomLevelStorage.Save(level);
        LoadMainMenu();
    }

    private void LoadMainMenu()
    {
        MainMenu.PageToOpen = MainMenuPage.Community;

        if (BGPersist.Instance != null)
        {
            BGPersist.Instance.AnimateExitThenLoad("MainMenu");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private Rect GetGridCaptureRect()
    {
        if (gridSpawner == null)
        {
            return new Rect(0f, 0f, Screen.width, Screen.height);
        }

        RectTransform captureTarget = gridSpawner.GetPreviewCaptureTarget();
        if (captureTarget == null)
        {
            return new Rect(0f, 0f, Screen.width, Screen.height);
        }

        Vector3[] corners = new Vector3[4];
        captureTarget.GetWorldCorners(corners);

        float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        float maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private Texture2D CropTexture(Texture2D source, Rect rect)
    {
        if (source == null)
        {
            return null;
        }

        int x = Mathf.Clamp(Mathf.RoundToInt(rect.x), 0, source.width);
        int y = Mathf.Clamp(Mathf.RoundToInt(rect.y), 0, source.height);
        int width = Mathf.Clamp(Mathf.RoundToInt(rect.width), 1, source.width - x);
        int height = Mathf.Clamp(Mathf.RoundToInt(rect.height), 1, source.height - y);

        Texture2D cropped = new Texture2D(width, height, TextureFormat.RGBA32, false);
        cropped.ReadPixels(new Rect(x, y, width, height), 0, 0, false);
        cropped.Apply();
        return cropped;
    }

    private string SaveTextureToFile(Texture2D texture, CustomLevelData level)
    {
        if (texture == null || level == null)
        {
            return null;
        }

        string fileName = level.fileName;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            string safeName = string.IsNullOrWhiteSpace(level.levelName)
                ? "custom_level"
                : level.levelName.Replace(" ", "_");

            string uniqueId = Guid.NewGuid().ToString("N");
            fileName = safeName + "_" + uniqueId + ".json";
            level.fileName = fileName;
        }

        string previewFileName = Path.GetFileNameWithoutExtension(fileName) + "_preview.png";
        string path = Path.Combine(Application.persistentDataPath, previewFileName);

        byte[] bytes = texture.EncodeToPNG();
        if (bytes == null || bytes.Length == 0)
        {
            return null;
        }

        File.WriteAllBytes(path, bytes);
        return previewFileName;
    }

    public void LoadLevel(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return;
        }

        CustomLevelData level = CustomLevelStorage.Load(identifier);

        if (level == null)
        {
            return;
        }

        ApplyLevelData(level);
    }

    private CustomLevelData BuildLevelData()
    {
        CustomLevelData level = currentLevel ?? new CustomLevelData();

        // The name comes from CustomLevelStorage.BeginCreateNew() or from the
        // existing level being edited. There is no scene UI dependency here.
        if (string.IsNullOrWhiteSpace(level.levelName))
        {
            level.levelName = "Custom level";
        }
        level.width = gridSpawner.Width;
        level.height = gridSpawner.Height;

        StringBuilder layout = new StringBuilder();
        int teleporterIndex = 0;

        for (int y = 0; y < gridSpawner.Height; y++)
        {
            for (int x = 0; x < gridSpawner.Width; x++)
            {
                EditorTile tile = gridSpawner.GetEditorTile(x, y);

                if (x > 0)
                {
                    layout.Append(' ');
                }

                if (tile != null && tile.Type == TileType.Teleporter)
                {
                    // Teleporters are paired in row-major placement order:
                    // the first two are T1, the next two T2, and so on.
                    int pairNumber = teleporterIndex / 2 + 1;
                    layout.Append($"T{pairNumber}");
                    teleporterIndex++;
                }
                else
                {
                    layout.Append(tile != null ? tile.GetTileToken() : "_");
                }
            }

            if (y < gridSpawner.Height - 1)
            {
                layout.AppendLine();
            }
        }

        level.layout = layout.ToString();
        currentLevel = level;
        return level;
    }

    private void ApplyLevelData(CustomLevelData level)
    {
        if (level == null || gridSpawner == null)
        {
            return;
        }

        if (level.width > 0 && level.height > 0)
        {
            gridSpawner.ResizeGrid(level.width, level.height);
        }

        currentLevel = level;

        string[] rows = level.layout?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        int rowIndex = 0;

        for (int y = 0; y < gridSpawner.Height; y++)
        {
            if (rowIndex >= rows.Length)
            {
                break;
            }

            string[] tokens = rows[rowIndex].Split(' ');
            for (int x = 0; x < gridSpawner.Width; x++)
            {
                EditorTile tile = gridSpawner.GetEditorTile(x, y);
                if (tile == null)
                {
                    continue;
                }

                if (x < tokens.Length)
                {
                    ApplyTileToken(tile, tokens[x]);
                }
            }

            rowIndex++;
        }
    }

    private void RefreshDimensionInputs()
    {
        if (widthInput != null)
        {
            widthInput.text = gridSpawner.Width.ToString();
        }

        if (heightInput != null)
        {
            heightInput.text = gridSpawner.Height.ToString();
        }
    }

    private void ApplyTileToken(EditorTile tile, string token)
    {
        if (tile == null)
        {
            return;
        }

        switch ((token ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "_":
                tile.SetTile(TileType.None);
                break;

            case "-":
                tile.SetTile(TileType.Tile);
                break;

            case "W":
                tile.SetTile(TileType.Wall);
                break;

            case "S":
                tile.SetTile(TileType.Splitter);
                break;

            case "D":
                tile.SetTile(TileType.Debris);
                break;

            case "H":
                tile.SetTile(TileType.StrongDebris);
                break;

            case "NS":
                tile.SetTile(
                    TileType.Mine,
                    MineType.Standard);
                break;

            case "NC":
                tile.SetTile(
                    TileType.Mine,
                    MineType.Cross);
                break;

            case "NG":
                tile.SetTile(
                    TileType.Mine,
                    MineType.Diagonal);
                break;

            case "ND":
                tile.SetTile(
                    TileType.Mine,
                    MineType.Double);
                break;

            case "MH":
                tile.SetTile(
                    TileType.Mirror,
                    MineType.Standard);
                break;

            case "MV":
                tile.SetTile(
                    TileType.Mirror,
                    MineType.Cross);
                break;

            case "MR":
                tile.SetTile(
                    TileType.Mirror,
                    MineType.Diagonal);
                break;

            case "ML":
                tile.SetTile(
                    TileType.Mirror,
                    MineType.Double);
                break;

            default:
                if (token.StartsWith(
                        "T",
                        StringComparison.OrdinalIgnoreCase))
                {
                    tile.SetTile(TileType.Teleporter);
                }
                else
                {
                    tile.SetTile(TileType.None);
                }

                break;
        }
    }

}