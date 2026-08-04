using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CommunityLevelEntryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text levelNameText;
    [SerializeField] private TMP_Text sizeText;
    [SerializeField] private TMP_Text likesText;
    [SerializeField] private TMP_Text downloadsText;
    [SerializeField] private TMP_Text creatorText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button likeButton;
    [SerializeField] private Image likeButtonImage;
    [SerializeField] private GridLayoutGroup tilesUsedGrid;
    [SerializeField] private Vector2 tileIconSize = new Vector2(24f, 24f);

    [Header("Like Button")]
    [SerializeField] private Sprite likedSprite;
    [SerializeField] private Sprite unlikedSprite;
    [SerializeField] private Color likedColor = new Color(1f, 0.3f, 0.4f, 1f);
    [SerializeField] private Color unlikedColor = Color.white;

    [Header("Tile Sprites")]
    [SerializeField] private Globals globals;

    private CustomLevelData level;
    private bool isLiked;
    private bool isLikeRequestPending;
    private bool isLaunchingLevel;
    private int displayedLikes;
    private Action<CustomLevelData, bool> onLikeChanged;

    public CustomLevelData Level => level;
    public bool IsLiked => isLiked;

    public void Setup(
        CustomLevelData levelData,
        bool likedByCurrentUser = false,
        Action<CustomLevelData, bool> likeChangedCallback = null)
    {
        if (levelData == null)
        {
            Debug.LogError("CommunityLevelEntryUI received null level data.", this);
            return;
        }

        level = levelData;
        isLiked = likedByCurrentUser;
        displayedLikes = Mathf.Max(0, level.likes);
        onLikeChanged = likeChangedCallback;

        if (levelNameText != null)
        {
            levelNameText.text = string.IsNullOrWhiteSpace(level.levelName)
                ? "Untitled Level"
                : level.levelName;
        }

        if (sizeText != null)
        {
            sizeText.text = $"Dimensions: {level.width} x {level.height}";
        }

        if (downloadsText != null)
        {
            downloadsText.text = level.downloads.ToString();
        }

        ResolveCreatorText();
        if (creatorText != null)
        {
            creatorText.text = string.IsNullOrWhiteSpace(level.author)
                ? "Unknown Creator"
                : level.author;
        }

        RefreshLikeUI();
        PopulateTileUsagePreview(level);

        if (playButton != null)
        {
            playButton.onClick.RemoveListener(PlayLevel);
            playButton.onClick.AddListener(PlayLevel);
        }

        if (likeButton != null)
        {
            likeButton.onClick.RemoveListener(ToggleLike);
            likeButton.onClick.AddListener(ToggleLike);
        }
    }

    private void ResolveCreatorText()
    {
        if (creatorText != null)
        {
            return;
        }

        TMP_Text[] textComponents = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text textComponent in textComponents)
        {
            if (textComponent != null &&
                (textComponent.name.Contains("Creator", StringComparison.OrdinalIgnoreCase) ||
                 textComponent.name.Contains("Author", StringComparison.OrdinalIgnoreCase)))
            {
                creatorText = textComponent;
                return;
            }
        }
    }

    private async void ToggleLike()
    {
        if (level == null || isLikeRequestPending)
        {
            return;
        }

        if (SupabaseManager.Instance == null ||
            string.IsNullOrWhiteSpace(level.fileName))
        {
            Debug.LogError(
                "Cannot update this like because Supabase or the level ID is missing.",
                this);
            return;
        }

        isLikeRequestPending = true;
        if (likeButton != null)
        {
            likeButton.interactable = false;
        }

        bool newLikedState = !isLiked;

        try
        {
            bool databaseChanged;
            if (newLikedState)
            {
                databaseChanged =
                    await SupabaseManager.Instance.LikeCustomLevel(
                    level.fileName);
            }
            else
            {
                databaseChanged =
                    await SupabaseManager.Instance.UnlikeCustomLevel(
                    level.fileName);
            }

            if (this == null)
            {
                return;
            }

            isLiked = newLikedState;
            if (databaseChanged)
            {
                displayedLikes = Mathf.Max(
                    0,
                    displayedLikes + (isLiked ? 1 : -1));
            }
            level.likes = displayedLikes;
            RefreshLikeUI();

            onLikeChanged?.Invoke(level, isLiked);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Could not update like for level '{level.levelName}': " +
                exception,
                this);
        }
        finally
        {
            if (this != null)
            {
                isLikeRequestPending = false;
                if (likeButton != null)
                {
                    likeButton.interactable = true;
                }
            }
        }
    }

    private void RefreshLikeUI()
    {
        if (likesText != null)
        {
            likesText.text = displayedLikes.ToString();
        }

        Image targetImage = likeButtonImage;
        if (targetImage == null && likeButton != null)
        {
            targetImage = likeButton.targetGraphic as Image;
        }

        if (targetImage == null)
        {
            return;
        }

        Sprite stateSprite = isLiked ? likedSprite : unlikedSprite;
        if (stateSprite != null)
        {
            targetImage.sprite = stateSprite;
        }

        targetImage.color = isLiked ? likedColor : unlikedColor;
    }

    private async void PlayLevel()
    {
        if (level == null || isLaunchingLevel)
        {
            if (level == null)
            {
                Debug.LogError(
                    "No community level was assigned to this entry.",
                    this);
            }
            return;
        }

        isLaunchingLevel = true;
        if (playButton != null)
        {
            playButton.interactable = false;
        }

        if (SupabaseManager.Instance != null &&
            !string.IsNullOrWhiteSpace(level.fileName))
        {
            try
            {
                await SupabaseManager.Instance.RegisterCustomLevelDownload(
                    level.fileName);

                level.downloads++;
                if (downloadsText != null)
                {
                    downloadsText.text = level.downloads.ToString();
                }
            }
            catch (Exception exception)
            {
                // A temporary network/counting failure should not prevent play.
                Debug.LogWarning(
                    $"Could not register play for '{level.levelName}': " +
                    exception.Message,
                    this);
            }
        }

        if (this == null)
        {
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

    private void PopulateTileUsagePreview(CustomLevelData levelData)
    {
        if (tilesUsedGrid == null)
        {
            Debug.LogWarning("CommunityLevelEntryUI has no tilesUsedGrid assigned.", this);
            return;
        }

        for (int i = tilesUsedGrid.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = tilesUsedGrid.transform.GetChild(i);
            if (child != null && child.name.StartsWith("UsedTileIcon_"))
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        if (string.IsNullOrWhiteSpace(levelData.layout))
        {
            return;
        }

        if (!levelData.TryToTileGrid(out CustomLevelTileData[,] grid, out string error))
        {
            Debug.LogWarning($"Could not parse tile preview for {levelData.fileName}: {error}", this);
            return;
        }

        HashSet<string> usedTileKeys = new HashSet<string>();

        for (int y = 0; y < grid.GetLength(1); y++)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                CustomLevelTileData tile = grid[x, y];
                if (tile == null || tile.tileType == TileType.Tile || tile.tileType == TileType.None)
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
            TileType tileType = (TileType)Enum.Parse(typeof(TileType), parts[0]);
            MineType mineType = parts.Length > 1
                ? (MineType)Enum.Parse(typeof(MineType), parts[1])
                : MineType.Standard;

            GameObject iconObject = new GameObject(
                $"UsedTileIcon_{key}",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));

            iconObject.transform.SetParent(tilesUsedGrid.transform, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.sizeDelta = tileIconSize;

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            iconImage.sprite = GetSpriteForTileType(tileType, mineType);

            LayoutElement layoutElement = iconObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = tileIconSize.x;
            layoutElement.preferredHeight = tileIconSize.y;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            tilesUsedGrid.GetComponent<RectTransform>());
    }

    private Sprite GetSpriteForTileType(
        TileType tileType,
        MineType mineType = MineType.Standard)
    {
        if (globals != null)
        {
            Sprite sprite = globals.GetSpriteForTileType(tileType, mineType);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return CreateFallbackSprite(tileType);
    }

    private Sprite CreateFallbackSprite(TileType tileType)
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
                bool isBorder = x < 2 || x > texture.width - 3 ||
                                y < 2 || y > texture.height - 3;
                texture.SetPixel(x, y, isBorder ? Color.white : background);
            }
        }

        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));
    }

    private void OnDestroy()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(PlayLevel);
        }

        if (likeButton != null)
        {
            likeButton.onClick.RemoveListener(ToggleLike);
        }
    }
}
