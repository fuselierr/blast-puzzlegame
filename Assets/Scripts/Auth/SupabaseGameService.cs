using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class SupabaseGameService
{
    private const int ClassicLevelCount = 10;
    private const int LeaderboardLimit = 100;
    private static DateTime? dailyChallengeStartUtc;
    private static string dailyChallengeStartDate;

    public static async Task<HashSet<int>> GetCompletedClassicLevels()
    {
        HashSet<int> completedLevels = new HashSet<int>();

        try
        {
            string userId = await GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return completedLevels;
            }

            var response = await SupabaseManager.Instance.SupabaseClient
                .From<ClassicLevelProgressRow>()
                .Where(row => row.UserId == userId)
                .Get();

            foreach (ClassicLevelProgressRow row in response.Models)
            {
                completedLevels.Add(row.LevelNumber);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not load Classic progress: " +
                exception.Message
            );
        }

        return completedLevels;
    }

    public static async Task MarkClassicLevelCompleted(
        int levelNumber
    )
    {
        if (levelNumber < 1 || levelNumber > ClassicLevelCount)
        {
            Debug.LogWarning(
                "Invalid Classic level number: " +
                levelNumber
            );

            return;
        }

        try
        {
            string userId = await GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var existing = await SupabaseManager.Instance
                .SupabaseClient
                .From<ClassicLevelProgressRow>()
                .Where(row =>
                    row.UserId == userId &&
                    row.LevelNumber == levelNumber)
                .Get();

            if (existing.Models.Count > 0)
            {
                return;
            }

            ClassicLevelProgressRow progress =
                new ClassicLevelProgressRow
                {
                    UserId = userId,
                    LevelNumber = levelNumber,
                    CompletedAt = DateTime.UtcNow
                };

            await SupabaseManager.Instance.SupabaseClient
                .From<ClassicLevelProgressRow>()
                .Insert(progress);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not save Classic progress: " +
                exception.Message
            );
        }
    }

    public static async Task<bool> TryStartDailyChallenge()
    {
        try
        {
            string userId = await GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning(
                    "Cannot start Daily Challenge because no Supabase user is signed in."
                );

                return false;
            }

            string today = GetUtcDateKey();

            var existing = await SupabaseManager.Instance
                .SupabaseClient
                .From<DailyChallengeScoreRow>()
                .Where(row => row.UserId == userId)
                .Where(row => row.ChallengeDate == today)
                .Get();

            if (existing.Models.Any(IsCompletedDailyScore))
            {
                return false;
            }

            dailyChallengeStartDate = today;
            dailyChallengeStartUtc = DateTime.UtcNow;

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not start Daily Challenge: " +
                exception.Message
            );

            return false;
        }
    }

    public static async Task<bool>
        HasAttemptedDailyChallengeToday()
    {
        try
        {
            string userId = await GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            string today = GetUtcDateKey();

            var response = await SupabaseManager.Instance
                .SupabaseClient
                .From<DailyChallengeScoreRow>()
                .Where(row => row.UserId == userId)
                .Where(row => row.ChallengeDate == today)
                .Get();

            return response.Models.Any(IsCompletedDailyScore);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not check Daily Challenge: " +
                exception.Message
            );

            return false;
        }
    }

    public static async Task<int?> GetTodayDailyScore()
    {
        try
        {
            string userId = await GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            string today = GetUtcDateKey();

            var response = await SupabaseManager.Instance
                .SupabaseClient
                .From<DailyChallengeScoreRow>()
                .Where(row => row.UserId == userId)
                .Where(row => row.ChallengeDate == today)
                .Get();

            if (response.Models.Count == 0)
            {
                return null;
            }

            DailyChallengeScoreRow attempt =
                response.Models.FirstOrDefault(IsCompletedDailyScore);

            if (attempt == null)
            {
                return null;
            }

            return ToSafeInt(attempt.Score.Value);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not load Daily score: " +
                exception.Message
            );

            return null;
        }
    }

    public static async Task<bool> SubmitDailyChallengeScore(
      int score
  )
    {
        score = Math.Max(0, score);

        try
        {
            string userId = await GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            string today = GetUtcDateKey();

            var response = await SupabaseManager.Instance
                .SupabaseClient
                .From<DailyChallengeScoreRow>()
                .Where(row => row.UserId == userId)
                .Where(row => row.ChallengeDate == today)
                .Get();

            DailyChallengeScoreRow attempt =
                response.Models.FirstOrDefault(IsCompletedDailyScore);

            if (attempt != null)
            {
                return true;
            }

            bool insertNewRow =
                response.Models.Count == 0;

            DateTime completedAt = DateTime.UtcNow;

            attempt = insertNewRow
                ? new DailyChallengeScoreRow
                {
                    UserId = userId,
                    ChallengeDate = today
                }
                : response.Models[0];

            if (attempt.AttemptedAt == default ||
                attempt.AttemptedAt >= completedAt)
            {
                attempt.AttemptedAt =
                    GetDailyChallengeStartTime(
                        today,
                        completedAt
                    );
            }

            attempt.Score = score;
            attempt.CompletedAt = completedAt;

            if (insertNewRow)
            {
                await SupabaseManager.Instance
                    .SupabaseClient
                    .From<DailyChallengeScoreRow>()
                    .Insert(attempt);
            }
            else
            {
                await SupabaseManager.Instance
                    .SupabaseClient
                    .From<DailyChallengeScoreRow>()
                    .Update(attempt);
            }

            Debug.Log(
                $"Daily Challenge score saved: {score}"
            );

            return true;
        }
        catch (Exception exception)
        {
            if (IsDuplicateRowError(exception))
            {
                Debug.LogError(
                    "Daily Challenge score was blocked by a duplicate " +
                    "database constraint. The daily_challenge_scores " +
                    "unique constraint should include both user_id and " +
                    "challenge_date."
                );
            }

            Debug.LogError(
                "Could not save Daily score: " +
                exception.Message
            );

            return false;
        }
    }
    public static async Task<List<LeaderboardEntryData>>
        GetDailyLeaderboard()
    {
        List<LeaderboardEntryData> entries =
            new List<LeaderboardEntryData>();

        try
        {
            if (!await EnsureReady())
            {
                return entries;
            }

            string today = GetUtcDateKey();

            var scores = await SupabaseManager.Instance
                .SupabaseClient
                .From<DailyChallengeScoreRow>()
                .Where(row => row.ChallengeDate == today)
                .Get();

            var profiles = await SupabaseManager.Instance
                .SupabaseClient
                .From<Profile>()
                .Get();

            Dictionary<string, string> usernames =
                profiles.Models.ToDictionary(
                    profile => profile.Id,
                    profile => profile.Username
                );

            List<DailyChallengeScoreRow> sorted =
                scores.Models
                    .Where(IsCompletedDailyScore)
                    .OrderByDescending(
                        row => row.Score.Value
                    )
                    .Take(LeaderboardLimit)
                    .ToList();

            for (int index = 0; index < sorted.Count; index++)
            {
                DailyChallengeScoreRow row = sorted[index];

                entries.Add(new LeaderboardEntryData
                {
                    rank = index + 1,
                    playerName = GetUsername(
                        usernames,
                        row.UserId
                    ),
                    score = ToSafeInt(row.Score.Value)
                });
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not load Daily leaderboard: " +
                exception.Message
            );
        }

        return entries;
    }

    public static async Task<string> GetCurrentUsername()
    {
        try
        {
            string userId = await GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return string.Empty;
            }

            var response = await SupabaseManager.Instance
                .SupabaseClient
                .From<Profile>()
                .Where(profile => profile.Id == userId)
                .Get();

            return response.Models.Count > 0
                ? response.Models[0].Username
                : string.Empty;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not load username: " +
                exception.Message
            );

            return string.Empty;
        }
    }

    private static async Task<string> GetCurrentUserId()
    {
        if (!await EnsureReady())
        {
            return null;
        }

        // CurrentUser is updated when accounts change. Depending on the
        // session persistence implementation, CurrentSession can briefly
        // still reference the previous account during a switch.
        return SupabaseManager.Instance
            .SupabaseClient
            .Auth
            .CurrentUser?
            .Id;
    }

    private static async Task<bool> EnsureReady()
    {
        if (SupabaseManager.Instance == null)
        {
            Debug.LogError(
                "SupabaseManager was not found."
            );

            return false;
        }

        await SupabaseManager.Instance.EnsureInitialized();

        return SupabaseManager.Instance.SupabaseClient != null;
    }

    private static string GetUtcDateKey()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd");
    }

    private static DateTime GetDailyChallengeStartTime(
        string today,
        DateTime completedAt
    )
    {
        if (dailyChallengeStartUtc.HasValue &&
            dailyChallengeStartDate == today &&
            dailyChallengeStartUtc.Value < completedAt)
        {
            return dailyChallengeStartUtc.Value;
        }

        return completedAt.AddSeconds(-1);
    }

    private static bool IsCompletedDailyScore(
        DailyChallengeScoreRow row
    )
    {
        if (row == null ||
            !row.Score.HasValue ||
            !row.CompletedAt.HasValue)
        {
            return false;
        }

        if (row.AttemptedAt == default)
        {
            return true;
        }

        return row.CompletedAt.Value > row.AttemptedAt;
    }

    private static bool IsDuplicateRowError(
        Exception exception
    )
    {
        return MessageContains(exception, "23505") ||
            MessageContains(exception, "duplicate key");
    }

    private static bool MessageContains(
        Exception exception,
        string value
    )
    {
        return exception != null &&
            exception.Message != null &&
            exception.Message.IndexOf(
                value,
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
    }

    private static string GetUsername(
        Dictionary<string, string> usernames,
        string userId
    )
    {
        return usernames.TryGetValue(
            userId,
            out string username
        )
            ? username
            : "Unknown";
    }

    private static int ToSafeInt(long value)
    {
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (value < int.MinValue)
        {
            return int.MinValue;
        }

        return (int)value;
    }
}
