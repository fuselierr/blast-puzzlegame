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

        kebabButton.onClick.RemoveListener(OpenKebabMenu);
        kebabButton.onClick.AddListener(OpenKebabMenu);
        playButton.onClick.RemoveListener(PlayLevel);
        playButton.onClick.AddListener(PlayLevel);
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
        SceneManager.LoadScene("Game");
    }

    public void EditLevel()
    {
        if (level == null)
        {
            Debug.LogError("No level data was assigned to this entry.", this);
            return;
        }

        CustomLevelStorage.BeginEdit(level);
        SceneManager.LoadScene("LevelEditor");
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