using TMPro;
using UnityEngine;
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

    public void Setup(CustomLevelData level)
    {
        if (level == null)
        {
            return;
        }

        levelNameText.text = string.IsNullOrWhiteSpace(level.levelName)
            ? "Untitled Level"
            : level.levelName;

        sizeText.text = $"Dimensions: {level.width} x {level.height}";
        likesText.text = $"{level.likes}";
        downloadsText.text = $"{level.downloads}";
        verifiedText.text = level.verified ? "Verified" : "Unverified";
        verifiedText.color = level.verified ? Color.green : Color.red;

        if (previewImage != null)
        {
            previewImage.sprite = defaultPreviewSprite;

            // If your custom level stores a preview sprite path or name,
            // load and assign it here when appropriate.
            // Example: previewImage.sprite = LoadPreviewSprite(level.previewImage) ?? defaultPreviewSprite;
        }
    }
}
