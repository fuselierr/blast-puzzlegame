using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LeaderboardController : MonoBehaviour
{
    [SerializeField]
    private Transform contentParent;
    [SerializeField] private ProfileAvatarCatalog avatarCatalog;

    private List<LeaderboardEntryUI> staticRows =
        new List<LeaderboardEntryUI>();

    private string currentPlayerName;

    private void Awake()
    {
        FindStaticRows();
    }

    public async void LoadDailyLeaderboard()
    {
        if (staticRows.Count == 0)
        {
            FindStaticRows();
        }

        foreach (LeaderboardEntryUI row in staticRows)
        {
            row.gameObject.SetActive(false);
        }

        currentPlayerName =
            await SupabaseGameService.GetCurrentUsername();

        List<LeaderboardEntryData> entries =
            await SupabaseGameService.GetDailyLeaderboard();

        Populate(entries);
    }

    public void Populate(
        List<LeaderboardEntryData> entries
    )
    {
        int visibleRows =
            Mathf.Min(entries.Count, staticRows.Count);

        for (int index = 0;
             index < staticRows.Count;
             index++)
        {
            bool hasPlayer = index < visibleRows;

            staticRows[index].gameObject.SetActive(
                hasPlayer
            );

            if (!hasPlayer)
            {
                continue;
            }

            LeaderboardEntryData entry =
                entries[index];

            bool isCurrentPlayer =
                !string.IsNullOrEmpty(currentPlayerName) &&
                entry.playerName == currentPlayerName;

            staticRows[index].Setup(
                entry,
                isCurrentPlayer,
                avatarCatalog
            );
        }
    }

    private void FindStaticRows()
    {
        staticRows.Clear();

        if (contentParent == null)
        {
            Debug.LogError(
                "Leaderboard Content Parent is not assigned."
            );

            return;
        }

        staticRows = contentParent
            .GetComponentsInChildren<LeaderboardEntryUI>(
                true
            )
            .OrderBy(
                row => row.transform.GetSiblingIndex()
            )
            .ToList();
    }
}