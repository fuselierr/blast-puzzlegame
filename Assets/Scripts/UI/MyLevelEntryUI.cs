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
            previewImage.sprite = defaultPreviewSprite;
        }

        kebabButton.onClick.RemoveListener(OpenKebabMenu);
        kebabButton.onClick.AddListener(OpenKebabMenu);
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

        kebabMenuController.Open(
            kebabButton.GetComponent<RectTransform>(),
            this
        );
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
        Debug.Log($"Duplicate level: {level.levelName}");

        // Create and save a copy here.
    }

    public void UploadLevel()
    {
        Debug.Log($"Upload level: {level.levelName}");

        // Upload it to your backend here.
    }

    public void DeleteLevel()
    {
        Debug.Log($"Delete level: {level.levelName}");

        // Delete the JSON file and destroy or refresh this entry.
    }

    private void OnDestroy()
    {
        if (kebabButton != null)
        {
            kebabButton.onClick.RemoveListener(OpenKebabMenu);
        }
    }
}