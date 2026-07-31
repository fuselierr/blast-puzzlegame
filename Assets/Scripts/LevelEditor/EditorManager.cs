using System;
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
        SaveLevel();

        MainMenu.PageToOpen = MainMenuPage.Community;
        SceneManager.LoadScene("MainMenu");
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
        level.levelName =
            levelNameInput != null ? levelNameInput.text : "Custom level";
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
            "D" => TileType.Debris,
            "H" => TileType.StrongDebris,
            "MH" or "MV" or "MR" or "ML" => TileType.Mirror,
            "NS" or "NC" or "NG" or "ND" => TileType.Mine,
            _ => TileType.None
        };
    }

}