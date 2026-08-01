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

    [Header("Initial Selection")]
    [SerializeField] private TileType selectedTileType = TileType.Tile;
    [SerializeField] private MineType selectedMineType = MineType.Standard;
    [SerializeField] private TMP_InputField levelNameInput;
    [SerializeField] private TMP_InputField widthInput;
    [SerializeField] private TMP_InputField heightInput;

    public TileType SelectedTileType => selectedTileType;
    public MineType SelectedMineType => selectedMineType;

    public event Action<TileType, MineType> SelectionChanged;

    private CustomLevelData currentLevel;

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

        if (gameplayGridSpawner != null)
        {
            gameplayGridSpawner.gameObject.SetActive(false);
        }

        if (verificationManager != null)
        {
            verificationManager.enabled = false;
            verificationManager.OnVerificationComplete += HandleVerificationComplete;
        }

        if (widthInput != null)
        {
            widthInput.onEndEdit.AddListener(_ => ResizeGridFromInputs());
        }

        if (heightInput != null)
        {
            heightInput.onEndEdit.AddListener(_ => ResizeGridFromInputs());
        }

        CustomLevelData levelBeingEdited =
            CustomLevelStorage.TakePendingEdit();
        if (levelBeingEdited != null)
        {
            Debug.Log($"EditorManager.Start: found pending edit level name='{levelBeingEdited.levelName}' file='{levelBeingEdited.fileName}'", this);
        }

        if (levelBeingEdited != null)
        {
            ApplyLevelData(levelBeingEdited);
        }

        RefreshInputFields(levelBeingEdited);

        Debug.Log("EDITOR MANAGER CONNECTED TO GRID", this);
    }

    private void OnDestroy()
    {
        if (gridSpawner != null)
        {
            gridSpawner.TileClicked -= PaintTile;
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
        Debug.Log(
            $"PAINTING {tile.GridPosition} WITH " +
            $"{selectedTileType}, {selectedMineType}",
            this
        );

        tile.SetTile(selectedTileType, selectedMineType);
    }

    public void ResizeGridFromInputs()
    {
        if (gridSpawner == null)
        {
            return;
        }

        int newWidth = ParseIntField(widthInput, gridSpawner.Width);
        int newHeight = ParseIntField(heightInput, gridSpawner.Height);

        if (newWidth <= 0 || newHeight <= 0)
        {
            return;
        }

        if (newWidth != gridSpawner.Width || newHeight != gridSpawner.Height)
        {
            gridSpawner.ResizeGrid(newWidth, newHeight);
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
            return Mathf.Max(1, value);
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

        gridSpawner.SetTilesVisible(false);
        filePanel.SetActive(false);
        gameplayGridSpawner.gameObject.SetActive(true);
        verificationManager.enabled = true;
        verificationManager.StartVerification(level);
    }

    private void HandleVerificationComplete(bool debrisCleared)
    {
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

        Debug.Log($"Custom level verification completed. Debris cleared: {debrisCleared}", this);
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
        SceneManager.LoadScene("MainMenu");
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

    public void LoadLevel()
    {
        if (levelNameInput == null)
        {
            return;
        }

        string fileName = levelNameInput.text;
        CustomLevelData level = CustomLevelStorage.Load(fileName);

        if (level == null)
        {
            return;
        }

        ApplyLevelData(level);
    }

    private CustomLevelData BuildLevelData()
    {
        CustomLevelData level = currentLevel ?? new CustomLevelData();
        // Preserve the existing level name if the UI input field is
        // not assigned (to avoid overwriting a freshly-created name).
        if (levelNameInput != null)
        {
            level.levelName = levelNameInput.text;
        }
        else if (!string.IsNullOrWhiteSpace(level.levelName))
        {
            // keep currentLevel.levelName as-is
        }
        else
        {
            level.levelName = "Custom level";
        }
        level.width = gridSpawner.Width;
        level.height = gridSpawner.Height;

        StringBuilder layout = new StringBuilder();

        for (int y = 0; y < gridSpawner.Height; y++)
        {
            for (int x = 0; x < gridSpawner.Width; x++)
            {
                EditorTile tile = gridSpawner.GetEditorTile(x, y);

                if (x > 0)
                {
                    layout.Append(' ');
                }

                layout.Append(tile != null ? tile.GetTileToken() : "_");
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
                    tile.SetTile(ParseTileToken(tokens[x]));
                }
            }

            rowIndex++;
        }
    }

    private void RefreshInputFields(CustomLevelData loadedLevel)
    {
        if (levelNameInput != null && loadedLevel != null)
        {
            levelNameInput.text = loadedLevel.levelName;
        }

        if (widthInput != null)
        {
            widthInput.text = gridSpawner.Width.ToString();
        }

        if (heightInput != null)
        {
            heightInput.text = gridSpawner.Height.ToString();
        }
    }

    private TileType ParseTileToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return TileType.None;
        }

        return token switch
        {
            "_" => TileType.None,
            "-" => TileType.Tile,
            "W" => TileType.Wall,
            "S" => TileType.Splitter,
            "D" => TileType.Debris,
            "H" => TileType.StrongDebris,
            "MH" or "MV" or "MR" or "ML" => TileType.Mirror,
            "NS" or "NC" or "NG" or "ND" => TileType.Mine,
            _ => TileType.None
        };
    }

}