using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MyLevelEntryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text levelNameText;
    [SerializeField] private TMP_Text sizeText;
    [SerializeField] private TMP_Text likesText;
    [SerializeField] private TMP_Text downloadsText;
    [SerializeField] private TMP_Text verifiedText;
    [SerializeField] private Image previewImage;
    [SerializeField] private Sprite defaultPreviewSprite;
    [SerializeField] private Button playButton;
    [SerializeField] private Button kebabButton;
    [SerializeField] private GridLayoutGroup tilesUsedGrid;
    [SerializeField] private Vector2 tileIconSize = new Vector2(24f, 24f);

    [Header("Tile Sprites")]
    [Tooltip("Assign the same Globals asset used by the game. This avoids Unity finding an unrelated loaded Globals asset.")]
    [SerializeField] private Globals globals;

    private CustomLevelData level;
    private KebabMenuController kebabMenuController;

    public CustomLevelData Level => level;

    public void Setup(
        CustomLevelData levelData,
        KebabMenuController menuController)
    {
        if (levelData == null)
        {
            return;
        }

        level = levelData;
        kebabMenuController = menuController;

        Debug.Log($"MyLevelEntryUI.Setup: levelFile={level.fileName} levelName={level.levelName} kebabMenuController={(kebabMenuController!=null)}", this);

        levelNameText.text =
            string.IsNullOrWhiteSpace(level.levelName)
                ? "Untitled Level"
                : level.levelName;

        sizeText.text =
            $"Dimensions: {level.width} x {level.height}";

        likesText.text = level.likes.ToString();
        downloadsText.text = level.downloads.ToString();

        verifiedText.text =
            level.verified ? "Verified" : "Unverified";

        verifiedText.color =
            level.verified ? Color.green : Color.red;

        if (previewImage != null)
        {
            previewImage.sprite = LoadPreviewSprite(levelData);
        }

        PopulateTileUsagePreview(levelData);

        kebabButton.onClick.RemoveListener(OpenKebabMenu);
        kebabButton.onClick.AddListener(OpenKebabMenu);
        playButton.onClick.RemoveListener(PlayLevel);
        playButton.onClick.AddListener(PlayLevel);
    }

    private void PopulateTileUsagePreview(CustomLevelData levelData)
    {
        if (levelData == null)
        {
            return;
        }

        GridLayoutGroup targetGrid = tilesUsedGrid;
        if (targetGrid == null)
        {
            Debug.LogWarning("MyLevelEntryUI has no tilesUsedGrid assigned.", this);
            return;
        }

        for (int i = targetGrid.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = targetGrid.transform.GetChild(i);
            if (child != null && child.name.StartsWith("UsedTileIcon_"))
            {
                Destroy(child.gameObject);
            }
        }

        if (string.IsNullOrWhiteSpace(levelData.layout))
        {
            return;
        }

        if (!levelData.TryToTileGrid(out CustomLevelTileData[,] grid, out string error))
        {
            Debug.LogWarning($"MyLevelEntryUI could not parse tile preview for {levelData.fileName}: {error}", this);
            return;
        }

        HashSet<string> usedTileKeys = new HashSet<string>();

        for (int y = 0; y < grid.GetLength(1); y++)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                CustomLevelTileData tile = grid[x, y];
                if (tile == null)
                {
                    continue;
                }

                if (tile.tileType == TileType.Tile ||
                    tile.tileType == TileType.None)
                {
                    continue;
                }

                string key = tile.tileType == TileType.Mine
                    ? $"{tile.tileType}:{tile.mineType}"
                    : tile.tileType.ToString();

                usedTileKeys.Add(key);
            }
        }

        foreach (string key in usedTileKeys)
        {
            string[] parts = key.Split(':');
            TileType tileType = parts.Length > 0 ? (TileType)System.Enum.Parse(typeof(TileType), parts[0]) : TileType.None;
            MineType mineType = parts.Length > 1 ? (MineType)System.Enum.Parse(typeof(MineType), parts[1]) : MineType.Standard;

            GameObject iconObject = new GameObject($"UsedTileIcon_{key}", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(targetGrid.transform, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.sizeDelta = tileIconSize;

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            iconImage.type = Image.Type.Simple;
            iconImage.sprite = GetSpriteForTileType(tileType, mineType);
            iconImage.color = iconImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);

            LayoutElement layoutElement = iconObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = tileIconSize.x;
            layoutElement.preferredHeight = tileIconSize.y;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(targetGrid.GetComponent<RectTransform>());
    }

    private Sprite GetSpriteForTileType(TileType tileType, MineType mineType = MineType.Standard)
    {
        if (globals != null)
        {
            Sprite sprite = globals.GetSpriteForTileType(tileType, mineType);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return CreateFallbackSprite(tileType, mineType);
    }

    private Sprite CreateFallbackSprite(TileType tileType, MineType mineType)
    {
        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color background = tileType == TileType.Mine
            ? new Color(0.82f, 0.22f, 0.22f, 1f)
            : tileType == TileType.Wall
                ? new Color(0.35f, 0.35f, 0.35f, 1f)
                : tileType == TileType.Mirror
                    ? new Color(0.2f, 0.4f, 0.9f, 1f)
                    : tileType == TileType.Teleporter
                        ? new Color(0.25f, 0.75f, 0.8f, 1f)
                        : tileType == TileType.Debris || tileType == TileType.StrongDebris
                            ? new Color(0.55f, 0.35f, 0.15f, 1f)
                            : tileType == TileType.Splitter
                                ? new Color(0.75f, 0.35f, 0.95f, 1f)
                                : new Color(0.2f, 0.2f, 0.2f, 1f);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                bool isBorder = x < 2 || x > texture.width - 3 || y < 2 || y > texture.height - 3;
                texture.SetPixel(x, y, isBorder ? Color.white : background);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private Sprite LoadPreviewSprite(CustomLevelData levelData)
    {
        if (levelData == null || string.IsNullOrWhiteSpace(levelData.previewImage))
        {
            return defaultPreviewSprite;
        }

        string previewPath = Path.Combine(Application.persistentDataPath, levelData.previewImage);
        if (!File.Exists(previewPath))
        {
            return defaultPreviewSprite;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(previewPath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                return defaultPreviewSprite;
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        catch
        {
            return defaultPreviewSprite;
        }
    }

    private void OpenKebabMenu()
    {
        if (kebabMenuController == null)
        {
            Debug.LogError(
                "MyLevelEntryUI has no KebabMenuController.",
                this
            );
            return;
        }

        Debug.Log($"MyLevelEntryUI.OpenKebabMenu called for {level.fileName}", this);
        kebabMenuController.Open(
            kebabButton.GetComponent<RectTransform>(),
            this
        );
    }

    private void PlayLevel()
    {
        if (level == null)
        {
            Debug.LogError("No level data was assigned to this entry.", this);
            return;
        }

        GameManager.SelectedMode = GameMode.Freeplay;
        GameManager.SelectedCustomLevel = level;

        if (BGPersist.Instance != null)
        {
            BGPersist.Instance.AnimateExitThenLoad("Game");
        }
        else
        {
            SceneManager.LoadScene("Game");
        }
    }

    public void EditLevel()
    {
        if (level == null)
        {
            Debug.LogError("No level data was assigned to this entry.", this);
            return;
        }

        CustomLevelStorage.BeginEdit(level);

        if (BGPersist.Instance != null)
        {
            BGPersist.Instance.AnimateExitThenLoad("LevelEditor");
        }
        else
        {
            SceneManager.LoadScene("LevelEditor");
        }
    }

    public void RenameLevel()
    {
        Debug.Log($"Rename level: {level.levelName}");

        // Open your rename dialog here.
    }

    public void DuplicateLevel()
    {
        if (level == null)
        {
            return;
        }

        CustomLevelData duplicate = CustomLevelStorage.Duplicate(level);
        if (duplicate == null)
        {
            Debug.LogError("Failed to duplicate the level.", this);
            return;
        }

        Debug.Log($"Duplicated '{level.levelName}' as '{duplicate.levelName}'.", this);
        RefreshLevelList();
    }

    public void UploadLevel()
    {
        Debug.Log($"Upload level: {level.levelName}");

        // Upload it to your backend here.
    }

    public void DeleteLevel()
    {
        if (level == null)
        {
            return;
        }

        if (!CustomLevelStorage.Delete(level))
        {
            Debug.LogError($"Failed to delete '{level.levelName}'.", this);
            return;
        }

        RefreshLevelList();
    }

    private void RefreshLevelList()
    {
        MyLevelsController controller = FindObjectOfType<MyLevelsController>();
        if (controller != null)
        {
            controller.PopulateLevels();
        }
    }

    private void OnDestroy()
    {
        if (kebabButton != null)
        {
            kebabButton.onClick.RemoveListener(OpenKebabMenu);
        }
    }
}