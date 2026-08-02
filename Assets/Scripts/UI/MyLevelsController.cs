using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyLevelsController : MonoBehaviour
{
    [Header("Scroll View")]
    [SerializeField] private Transform contentParent;      // ScrollView/Viewport/Content

    [Header("Prefabs")]
    [SerializeField] private GameObject levelEntryPrefab;

    [Header("Data")]
    [SerializeField] private List<CustomLevelData> customLevels = new List<CustomLevelData>();

    [Header("Kebab Menu")]
    [SerializeField] private KebabMenuController kebabMenuController;
    [Header("Popups")]
    [SerializeField] private LevelNamePopupController namePopup;

    private void Start()
    {
        PopulateLevels();
    }

    // Called from a UI Create button. Shows the shared name popup and
    // creates a new custom level with the entered name, then opens the
    // LevelEditor scene for editing.
    public void OpenCreatePopup()
    {
        if (namePopup == null)
        {
            Debug.LogError("MyLevelsController has no LevelNamePopupController assigned.", this);
            return;
        }

        namePopup.Show("New Level", newName =>
        {
            if (string.IsNullOrWhiteSpace(newName)) return;

            CustomLevelData level = CustomLevelStorage.BeginCreateNew(newName);

            if (BGPersist.Instance != null)
            {
                BGPersist.Instance.AnimateExitThenLoad("LevelEditor");
            }
            else
            {
                SceneManager.LoadScene("LevelEditor");
            }
        });
    }

    public void PopulateLevels()
    {
        if (contentParent == null)
        {
            return;
        }

        ClearEntries();
        customLevels.Clear();

        string[] files = Directory.GetFiles(Application.persistentDataPath, "*.json");

        foreach (string filePath in files)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                CustomLevelData level = JsonUtility.FromJson<CustomLevelData>(json);

                if (level == null || string.IsNullOrWhiteSpace(level.levelName))
                {
                    continue;
                }

                // Older level files may not contain their own filename.
                // Keep it so editing saves back to this exact JSON file.
                level.fileName = Path.GetFileName(filePath);

                customLevels.Add(level);
                CreateLevelEntry(level);
            }
            catch
            {
                // Ignore files that are not custom level data.
            }
        }
    }

    public void PopulateLevelsFromData(IEnumerable<CustomLevelData> levels)
    {
        if (contentParent == null)
        {
            return;
        }

        ClearEntries();
        customLevels.Clear();

        if (levels == null)
        {
            return;
        }

        foreach (CustomLevelData level in levels)
        {
            if (level == null)
            {
                continue;
            }

            customLevels.Add(level);
            CreateLevelEntry(level);
        }
    }

    private void CreateLevelEntry(CustomLevelData level)
    {
        if (levelEntryPrefab != null)
        {
            GameObject entryObject = Instantiate(levelEntryPrefab, contentParent, false);
            entryObject.name = $"LevelEntry_{level.levelName}";

            MyLevelEntryUI entryUI = entryObject.GetComponent<MyLevelEntryUI>();
            if (entryUI != null)
            {
                entryUI.Setup(level, kebabMenuController);
                return;
            }

            TMP_Text fallbackText = entryObject.GetComponentInChildren<TMP_Text>();
            if (fallbackText != null)
            {
                fallbackText.text = BuildLevelText(level);
                return;
            }

            Debug.LogWarning($"Level entry prefab '{levelEntryPrefab.name}' does not contain MyLevelEntryUI or TMP_Text.", this);
            return;
        }

        GameObject fallbackObject = new GameObject($"LevelEntry_{level.levelName}");
        fallbackObject.transform.SetParent(contentParent, false);

        RectTransform rectTransform = fallbackObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0f, 40f);

        TextMeshProUGUI text = fallbackObject.AddComponent<TextMeshProUGUI>();
        text.text = BuildLevelText(level);
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Left;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private string BuildLevelText(CustomLevelData level)
    {
        return string.IsNullOrWhiteSpace(level.levelName)
            ? "Untitled Level"
            : $"{level.levelName}  |  {level.width}x{level.height}  |  Likes: {level.likes}  |  Downloads: {level.downloads}";
    }

    private void ClearEntries()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }
}