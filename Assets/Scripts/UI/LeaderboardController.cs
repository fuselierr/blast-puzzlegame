using System.Collections.Generic;
using UnityEngine;

public class LeaderboardController : MonoBehaviour
{
    [Header("Scroll View")]
    [SerializeField] private Transform contentParent;      // ScrollView/Viewport/Content
    [SerializeField] private LeaderboardEntryUI rowPrefab;  // the row prefab

    [Header("Optional")]
    [SerializeField] private string currentPlayerName;      // to highlight the local player's row

    private readonly List<LeaderboardEntryUI> spawnedRows = new List<LeaderboardEntryUI>();

    /// Call this whenever you have fresh leaderboard data to display.
    public void Populate(List<LeaderboardEntryData> entries)
    {
        Clear();

        foreach (var entry in entries)
        {
            LeaderboardEntryUI row = Instantiate(rowPrefab, contentParent);
            bool isCurrentPlayer = !string.IsNullOrEmpty(currentPlayerName)
                                    && entry.playerName == currentPlayerName;

            row.Setup(entry, isCurrentPlayer);
            spawnedRows.Add(row);
        }
    }

    private void Clear()
    {
        foreach (var row in spawnedRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        spawnedRows.Clear();
    }

    // --- Example usage / test data ---
    [ContextMenu("Populate With Test Data")]
    private void PopulateTestData()
    {
        var testData = new List<LeaderboardEntryData>
        {
            new LeaderboardEntryData { rank = 1, playerName = "Ava",   score = 15420 },
            new LeaderboardEntryData { rank = 2, playerName = "Kenji", score = 14980 },
            new LeaderboardEntryData { rank = 3, playerName = "Mo",    score = 13750 },
            new LeaderboardEntryData { rank = 4, playerName = "You",   score = 9200 },
            new LeaderboardEntryData { rank = 5, playerName = "Liam",  score = 8700 },
            new LeaderboardEntryData { rank = 6, playerName = "Zara",  score = 8100 },
            new LeaderboardEntryData { rank = 7, playerName = "Ravi",  score = 7800 },
            new LeaderboardEntryData { rank = 8, playerName = "Nia",   score = 7200 },
            new LeaderboardEntryData { rank = 9, playerName = "Eli",   score = 6900 }
        };
        Populate(testData);
    }

}