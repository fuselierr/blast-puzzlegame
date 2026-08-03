using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommunityLevelManager : MonoBehaviour
{
    [Header("Level List")]
    [SerializeField] private Transform content;
    [SerializeField] private CommunityLevelEntryUI communityLevelEntryPrefab;

    [Header("Controls")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private bool sortByMostLiked = true;

    [Header("List States")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject emptyPanel;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TMP_Text errorText;

    private readonly List<CommunityLevelEntryUI> spawnedEntries =
        new List<CommunityLevelEntryUI>();

    private readonly HashSet<string> likedLevelIds =
        new HashSet<string>();

    private List<CustomLevelData> currentLevels =
        new List<CustomLevelData>();

    private bool isRefreshing;

    public event Action<CustomLevelData, bool> LikeChanged;

    public IReadOnlyList<CustomLevelData> CurrentLevels => currentLevels;

    private void Awake()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(Refresh);
            refreshButton.onClick.AddListener(Refresh);
        }

        SetState(false, false, false);
    }

    private void OnEnable()
    {
        Refresh();
    }

    public async void Refresh()
    {
        if (isRefreshing)
        {
            return;
        }

        isRefreshing = true;
        ShowLoading();

        try
        {
            // OnEnable can run before another object's Awake. Yield once so
            // SupabaseManager has a chance to establish its singleton.
            await System.Threading.Tasks.Task.Yield();

            if (SupabaseManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "SupabaseManager was not found in the scene.");
            }

            List<CustomLevelDataRow> rows =
                await SupabaseManager.Instance.GetCommunityLevels();

            if (this == null)
            {
                return;
            }

            List<CustomLevelData> levels = new List<CustomLevelData>();
            foreach (CustomLevelDataRow row in rows)
            {
                if (row != null)
                {
                    levels.Add(row.ToCustomLevelData());
                }
            }

            PopulateLevels(levels);
        }
        catch (Exception exception)
        {
            if (this != null)
            {
                Debug.LogError(
                    "Failed to refresh community levels: " + exception,
                    this);

                ShowError("Could not load community levels. Please try again.");
            }
        }
        finally
        {
            if (this != null)
            {
                isRefreshing = false;
            }
        }
    }

    public void PopulateLevels(IEnumerable<CustomLevelData> levels)
    {
        ClearEntries();
        currentLevels.Clear();

        if (levels != null)
        {
            HashSet<string> addedIds = new HashSet<string>();

            foreach (CustomLevelData level in levels)
            {
                if (level == null)
                {
                    continue;
                }

                string levelId = GetLevelId(level);
                if (addedIds.Add(levelId))
                {
                    currentLevels.Add(level);
                }
            }
        }

        if (sortByMostLiked)
        {
            currentLevels.Sort((left, right) =>
            {
                int likeComparison = right.likes.CompareTo(left.likes);
                if (likeComparison != 0)
                {
                    return likeComparison;
                }

                return string.Compare(
                    left.levelName,
                    right.levelName,
                    StringComparison.OrdinalIgnoreCase);
            });
        }

        if (content == null || communityLevelEntryPrefab == null)
        {
            ShowError("Assign Content and Community Level Entry Prefab in the inspector.");
            return;
        }

        foreach (CustomLevelData level in currentLevels)
        {
            CommunityLevelEntryUI entry = Instantiate(
                communityLevelEntryPrefab,
                content);

            bool isLiked = likedLevelIds.Contains(GetLevelId(level));
            entry.Setup(level, isLiked, HandleLikeChanged);
            spawnedEntries.Add(entry);
        }

        SetState(false, currentLevels.Count == 0, false);

        Canvas.ForceUpdateCanvases();
        RectTransform contentRect = content as RectTransform;
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }

    public void SetLikedLevelIds(IEnumerable<string> levelIds)
    {
        likedLevelIds.Clear();

        if (levelIds == null)
        {
            return;
        }

        foreach (string levelId in levelIds)
        {
            if (!string.IsNullOrWhiteSpace(levelId))
            {
                likedLevelIds.Add(levelId);
            }
        }
    }

    public void SetLevelLiked(CustomLevelData level, bool liked)
    {
        if (level == null)
        {
            return;
        }

        string levelId = GetLevelId(level);
        if (liked)
        {
            likedLevelIds.Add(levelId);
        }
        else
        {
            likedLevelIds.Remove(levelId);
        }
    }

    public bool IsLevelLiked(CustomLevelData level)
    {
        return level != null && likedLevelIds.Contains(GetLevelId(level));
    }

    public void ShowLoading()
    {
        SetState(true, false, false);

        if (refreshButton != null)
        {
            refreshButton.interactable = false;
        }
    }

    public void ShowError(string message)
    {
        SetState(false, false, true);

        if (errorText != null)
        {
            errorText.text = string.IsNullOrWhiteSpace(message)
                ? "Could not load community levels."
                : message;
        }

        if (refreshButton != null)
        {
            refreshButton.interactable = true;
        }
    }

    private void HandleLikeChanged(CustomLevelData level, bool liked)
    {
        SetLevelLiked(level, liked);
        LikeChanged?.Invoke(level, liked);
    }

    private void ClearEntries()
    {
        foreach (CommunityLevelEntryUI entry in spawnedEntries)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }
        }

        spawnedEntries.Clear();

        if (content == null)
        {
            return;
        }

        // Also remove entries that may have been created before this manager loaded.
        CommunityLevelEntryUI[] existingEntries =
            content.GetComponentsInChildren<CommunityLevelEntryUI>(true);

        foreach (CommunityLevelEntryUI entry in existingEntries)
        {
            if (entry != null && !spawnedEntries.Contains(entry))
            {
                entry.gameObject.SetActive(false);
                Destroy(entry.gameObject);
            }
        }
    }

    private string GetLevelId(CustomLevelData level)
    {
        if (!string.IsNullOrWhiteSpace(level.fileName))
        {
            return level.fileName;
        }

        return $"{level.author}|{level.levelName}|{level.width}x{level.height}";
    }

    private void SetState(bool loading, bool empty, bool error)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(loading);
        }

        if (emptyPanel != null)
        {
            emptyPanel.SetActive(empty);
        }

        if (errorPanel != null)
        {
            errorPanel.SetActive(error);
        }

        if (refreshButton != null && !loading)
        {
            refreshButton.interactable = true;
        }
    }

    private void OnDestroy()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(Refresh);
        }
    }
}